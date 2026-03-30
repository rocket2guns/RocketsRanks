using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RocketsRanks
{
    /// <summary>
    /// Adds "Promote" gizmo to all player colony pawns.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (var gizmo in __result)
                yield return gizmo;

            if (__instance.IsColonistPlayerControlled)
            {
                var comp = RankGameComponent.Instance;
                var currentRank = comp?.GetRank(__instance);
                var label = currentRank != null
                    ? currentRank.LabelCap.ToString()
                    : "ROCKET_Promote".Translate().ToString();
                var icon = currentRank?.Icon ?? RankTextures.PromoteIcon;

                yield return new Command_Action
                {
                    defaultLabel = label,
                    defaultDesc = "ROCKET_PromoteDesc".Translate(),
                    icon = icon,
                    action = () => Find.WindowStack.Add(new Dialog_Promote(__instance))
                };
            }
        }
    }

    /// <summary>
    /// Draws the pawn's rank icon in the top-left corner of their colonist bar portrait.
    /// </summary>
    [HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawColonist))]
    public static class Patch_DrawColonist_RankBadge
    {
        public static void Postfix(Rect rect, Pawn colonist)
        {
            if (!RanksMod.Settings.ShowRankBadge) return;

            var comp = RankGameComponent.Instance;
            var rank = comp?.GetRank(colonist);
            if (rank?.Icon == null) return;

            var size = RanksMod.Settings.BadgeSize;
            var halfSize = size / 2f;

            // Anchor point is portrait top-left (0,0) plus offsets;
            // texture is centered on that anchor.
            var centerX = rect.x + RanksMod.Settings.BadgeOffsetX;
            var centerY = rect.y + RanksMod.Settings.BadgeOffsetY;

            var badgeRect = new Rect(
                centerX - halfSize,
                centerY - halfSize,
                size,
                size);

            GUI.DrawTexture(badgeRect, rank.Icon);
        }
    }

    /// <summary>
    /// Draws the pawn's rank icon next to their floating name label on the map.
    /// </summary>
    [HarmonyPatch(typeof(PawnUIOverlay), nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
    public static class Patch_PawnUIOverlay_RankIcon
    {
        private static readonly AccessTools.FieldRef<PawnUIOverlay, Pawn> PawnRef =
            AccessTools.FieldRefAccess<PawnUIOverlay, Pawn>("pawn");

        public static void Postfix(PawnUIOverlay __instance)
        {
            if (!RanksMod.Settings.ShowRankOnMap) return;

            var pawn = PawnRef(__instance);
            if (pawn == null || !pawn.Spawned) return;

            if (Find.CameraDriver.CurrentZoom > CameraZoomRange.Middle) return;
            if (!pawn.IsColonistPlayerControlled) return;

            var comp = RankGameComponent.Instance;
            var rank = comp?.GetRank(pawn);
            if (rank?.Icon == null) return;

            var pos = GenMapUI.LabelDrawPosFor(pawn, -0.6f);

            Text.Font = GameFont.Tiny;
            var name = pawn.Name?.ToStringShort ?? pawn.LabelShort;
            var textSize = Text.CalcSize(name);

            var iconSize = RanksMod.Settings.MapIconSize;

            // Horizontal: right edge of icon meets left edge of text, plus user offset
            var iconX = pos.x - textSize.x / 2f - iconSize + RanksMod.Settings.MapIconOffsetX;

            // Vertical: find the text's vertical centre, then centre the icon on it
            var textCenterY = pos.y + textSize.y * 0.5f;
            var iconY = textCenterY - iconSize * 0.5f + RanksMod.Settings.MapIconOffsetY;

            GUI.DrawTexture(new Rect(iconX, iconY, iconSize, iconSize), rank.Icon);
        }
    }

    /// <summary>
    /// Hard block: prevent any code path from equipping rank-restricted apparel
    /// if the pawn doesn't hold the required rank.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
    public static class Patch_ApparelTracker_Wear
    {
        public static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            var ext = RankUtility.GetRankExtension(newApparel.def);
            if (ext?.requiredRank == null) return true;

            if (!RankUtility.PawnMeetsRankRequirement(__instance.pawn, newApparel.def))
            {
                Messages.Message(
                    "ROCKET_RankRequired".Translate(__instance.pawn.LabelShort, ext.requiredRank.LabelCap),
                    __instance.pawn,
                    MessageTypeDefOf.RejectInput,
                    false);
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Prevent the auto-outfit system from scoring rank-restricted apparel
    /// that the pawn can't wear.
    /// </summary>
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), nameof(JobGiver_OptimizeApparel.ApparelScoreRaw))]
    public static class Patch_ApparelScoreRaw
    {
        public static void Postfix(Pawn pawn, Apparel ap, ref float __result)
        {
            var ext = RankUtility.GetRankExtension(ap.def);
            if (ext?.requiredRank == null) return;

            if (!RankUtility.PawnMeetsRankRequirement(pawn, ap.def))
                __result = -10000f;
        }
    }

    /// <summary>
    /// Appends rank requirement info to the inspect string of rank-restricted apparel.
    /// Shows the required rank name and level when the item is selected.
    /// </summary>
    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.GetInspectString))]
    public static class Patch_RankApparel_InspectString
    {
        public static void Postfix(ThingWithComps __instance, ref string __result)
        {
            var ext = RankUtility.GetRankExtension(__instance.def);
            if (ext?.requiredRank == null) return;

            var rank = ext.requiredRank;
            var sb = new System.Text.StringBuilder(__result);
            if (sb.Length > 0) sb.AppendLine();
            sb.Append($"Rank required: {rank.LabelCap} (Lvl {rank.rankLevel})");

            if (!rank.description.NullOrEmpty())
            {
                sb.AppendLine();
                sb.Append(rank.description);
            }

            __result = sb.ToString();
        }
    }

    /// <summary>
    /// When a pawn's rank changes, explicitly remove any rank apparel they're
    /// wearing that no longer matches, then nudge the optimizer to pick up new gear.
    /// </summary>
    public static class RankApparelRefresh
    {
        public static void RefreshApparelFor(Pawn pawn)
        {
            if (pawn?.apparel == null) return;

            // Find worn rank apparel that no longer matches the pawn's rank
            var toRemove = new List<Apparel>();
            foreach (var worn in pawn.apparel.WornApparel)
            {
                var ext = RankUtility.GetRankExtension(worn.def);
                if (ext?.requiredRank == null) continue;
                if (!RankUtility.PawnMeetsRankRequirement(pawn, worn.def))
                    toRemove.Add(worn);
            }

            // Drop mismatched rank gear (unforbidden so it gets hauled to stockpile)
            foreach (var ap in toRemove)
            {
                if (pawn.apparel.IsLocked(ap)) continue;
                pawn.apparel.TryDrop(ap, out _, pawn.PositionHeld, false);
            }

            // Nudge optimizer to pick up the correct rank gear
            if (pawn.mindState != null)
                pawn.mindState.nextApparelOptimizeTick = 0;
        }
    }

    /// <summary>
    /// Bypass per-bodytype/per-facing texture lookup for rank apparel.
    /// Uses Graphic_Multi from the wornGraphicPath so each rank needs
    /// one set of directional textures with masks.
    /// </summary>
    [HarmonyPatch(typeof(ApparelGraphicRecordGetter), nameof(ApparelGraphicRecordGetter.TryGetGraphicApparel))]
    public static class Patch_RankGraphicRecord
    {
        public static bool Prefix(Apparel apparel, BodyTypeDef bodyType, ref ApparelGraphicRecord rec, ref bool __result)
        {
            if (RankUtility.GetRankExtension(apparel.def) == null) return true;

            var path = apparel.def.apparel.wornGraphicPath;
            if (path.NullOrEmpty()) return true;

            var graphic = GraphicDatabase.Get<Graphic_Multi>(
                path,
                ShaderDatabase.CutoutComplex,
                Vector2.one,
                apparel.DrawColor,
                apparel.DrawColorTwo);

            rec = new ApparelGraphicRecord(graphic, apparel);
            __result = true;
            return false;
        }
    }

    /// <summary>
    /// Scale rank apparel insignia.
    /// Uses XML def defaults, or mod settings when debug is enabled.
    /// </summary>
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.ScaleFor))]
    public static class Patch_RankApparel_Scale
    {
        private const float BaseScale = 0.35f;

        public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            if (node is not PawnRenderNode_Apparel apparelNode) return;
            if (RankUtility.GetRankExtension(apparelNode.apparel.def) == null) return;

            RankRenderSettings.GetOffsetAndScale(apparelNode.apparel.def,
                parms.pawn?.story?.bodyType, parms.facing,
                out _, out _, out var scale);
            __result *= BaseScale * scale;
        }
    }

    /// <summary>
    /// Position rank insignia per facing and body type.
    /// Uses XML def defaults, or mod settings when debug is enabled.
    /// </summary>
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.OffsetFor))]
    public static class Patch_RankApparel_Offset
    {
        private const float BaseZ = -0.1f;
        private const float SideShiftX = 0.02f;
        private const float NorthDepthPush = 0.005f;
        private const float SideDepthPush = 0.005f;

        public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            if (node is not PawnRenderNode_Apparel apparelNode) return;
            if (RankUtility.GetRankExtension(apparelNode.apparel.def) == null) return;

            var facing = parms.facing;
            RankRenderSettings.GetOffsetAndScale(apparelNode.apparel.def,
                parms.pawn?.story?.bodyType, facing,
                out var bodyOffsetX, out var bodyOffsetZ, out _);

            __result.z += BaseZ + bodyOffsetZ;

            switch (facing.AsInt)
            {
                case 0: // North
                    __result.x += bodyOffsetX;
                    __result.y += NorthDepthPush;
                    break;
                case 1: // East
                    __result.x += SideShiftX + bodyOffsetX;
                    __result.y += SideDepthPush;
                    break;
                case 2: // South
                    __result.x += bodyOffsetX;
                    break;
                case 3: // West
                    __result.x -= SideShiftX + bodyOffsetX;
                    __result.y += SideDepthPush;
                    break;
            }
        }
    }

}