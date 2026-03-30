using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

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
                    ? currentRank.RankLabel
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
    /// Draws rank badge on colonist bar portraits.
    /// </summary>
    [HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawColonist))]
    public static class Patch_DrawColonist
    {
        public static void Postfix(Rect rect, Pawn colonist)
        {
            if (!RanksMod.Settings.ShowRankBadge) return;

            var comp = RankGameComponent.Instance;
            var rank = comp?.GetRank(colonist);
            if (rank?.Icon == null) return;

            var size = RanksMod.Settings.BadgeSize;
            var halfSize = size / 2f;
            var centerX = rect.x + RanksMod.Settings.BadgeOffsetX;
            var centerY = rect.y + RanksMod.Settings.BadgeOffsetY;

            GUI.DrawTexture(new Rect(
                centerX - halfSize,
                centerY - halfSize,
                size, size), rank.Icon);
        }
    }
    
    [HarmonyPatch(typeof(ColonistBar), "CheckRecacheEntries")]
    public static class Patch_HideColonistBarEntries
    {
        private static readonly AccessTools.FieldRef<ColonistBar, List<ColonistBar.Entry>> CachedEntriesRef =
            AccessTools.FieldRefAccess<ColonistBar, List<ColonistBar.Entry>>("cachedEntries");

        private static readonly AccessTools.FieldRef<ColonistBar, List<Vector2>> CachedDrawLocsRef =
            AccessTools.FieldRefAccess<ColonistBar, List<Vector2>>("cachedDrawLocs");

        private static WorldRenderMode _prevMode = WorldRenderMode.None;

        public static void Postfix(ColonistBar __instance)
        {
            var mode = WorldRendererUtility.CurrentWorldRenderMode;
            var justReturnedFromWorld = _prevMode == WorldRenderMode.Planet && mode != WorldRenderMode.Planet;
            _prevMode = mode;

            // Force a clean recache next frame so vanilla repopulates entries fully
            if (justReturnedFromWorld)
            {
                __instance.MarkColonistsDirty();
                return;
            }

            var hideCrypto = RanksMod.Settings.HideCryptosleep;
            var hideOffMap = RanksMod.Settings.HideOffMap;
            var hideInMap = RanksMod.Settings.HideInMap;
            if (!hideCrypto && !hideOffMap && !hideInMap) return;

            var entries = CachedEntriesRef(__instance);
            var drawLocs = CachedDrawLocsRef(__instance);
            if (entries == null || entries.Count == 0) return;

            for (var i = entries.Count - 1; i >= 0; i--)
            {
                if (!ShouldHide(entries[i].pawn)) continue;
                entries.RemoveAt(i);
                if (drawLocs != null && i < drawLocs.Count)
                    drawLocs.RemoveAt(i);
            }
        }

        private static bool ShouldHide(Pawn pawn)
        {
            if (pawn == null) return true;

            if (RanksMod.Settings.HideInMap && WorldRendererUtility.CurrentWorldRenderMode is WorldRenderMode.Planet)
                return true;

            if (RanksMod.Settings.HideCryptosleep && pawn.ParentHolder is Building_CryptosleepCasket)
                return true;

            if (RanksMod.Settings.HideOffMap && Find.CurrentMap != null && pawn.Map != null && pawn.Map != Find.CurrentMap)
                return true;

            return false;
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
    /// if the pawn doesn't hold the required rank. Only shows a message for
    /// manual (player-initiated) wear attempts.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
    public static class Patch_ApparelTracker_Wear
    {
        public static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            if (newApparel is not RocketRank) return true;

            var ext = RankUtility.GetRankExtension(newApparel.def);
            if (ext?.requiredRank == null) return true;

            if (!RankUtility.PawnMeetsRankRequirement(__instance.pawn, newApparel.def))
            {
                Log.Warning($"[RocketsRanks] Wear BLOCKED: {__instance.pawn.LabelShort} (rank={RankGameComponent.Instance?.GetRank(__instance.pawn)?.label ?? "none"}) tried to wear {newApparel.def.defName} (requires {ext.requiredRank.label}). This should have been caught by ScoreGain!");
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Safety net: if a Wear job for rank gear slips through,
    /// kill it before the pawn starts walking.
    /// </summary>
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), "TryGiveJob")]
    public static class Patch_OptimizeApparel_TryGiveJob
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result == null) return;
            if (__result.def != JobDefOf.Wear) return;
            if (__result.targetA.Thing is not RocketRank) return;

            if (!RankUtility.PawnMeetsRankRequirement(pawn, __result.targetA.Thing.def))
            {
                __result = null;
                pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + 12000;
            }
        }
    }

    /// <summary>
    /// Prevent auto-equip of rank gear the pawn can't wear.
    /// Mirrors the proven pattern from RocketMedals.
    /// </summary>
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), nameof(JobGiver_OptimizeApparel.ApparelScoreRaw))]
    public static class Patch_ApparelScoreRaw
    {
        public static void Postfix(Pawn pawn, Apparel ap, ref float __result)
        {
            if (ap is RocketRank && !RankUtility.PawnMeetsRankRequirement(pawn, ap.def))
                __result = -10000f;
        }
    }
    
    [HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.HasPartsToWear))]
    public static class Patch_ApparelUtility_HasPartsToWear
    {
        public static void Postfix(Pawn p, ThingDef apparel, ref bool __result)
        {
            // If the base game already decided they can't wear it, skip
            if (!__result) return;

            // Check if the item is a Rank
            if (typeof(RocketRank).IsAssignableFrom(apparel.thingClass))
            {
                // If they don't meet the requirement, they physically "cannot" wear it
                if (!RankUtility.PawnMeetsRankRequirement(p, apparel))
                {
                    __result = false;
                }
            }
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
            if (__instance is not RocketRank) return;

            var ext = RankUtility.GetRankExtension(__instance.def);
            if (ext?.requiredRank == null) return;

            var rank = ext.requiredRank;
            var sb = new System.Text.StringBuilder(__result);
            if (sb.Length > 0) sb.AppendLine();
            sb.Append($"Can only be worn by pawns with the rank of <color=#E6D966>{rank.RankLabel}</color>");
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

            var toRemove = new List<Apparel>(pawn.apparel.WornApparel.Count);
            foreach (var worn in pawn.apparel.WornApparel)
            {
                if (worn is not RocketRank) continue;
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
            pawn.mindState?.nextApparelOptimizeTick = 0;
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
            if (apparel is not RocketRank) return true;

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
            if (apparelNode.apparel is not RocketRank) return;

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
        private const float DepthAboveShell = 0.01f;
        private const float NorthExtraDepth = 0.02f;

        public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            if (node is not PawnRenderNode_Apparel apparelNode) return;
            if (apparelNode.apparel is not RocketRank) return;

            var facing = parms.facing;
            RankRenderSettings.GetOffsetAndScale(apparelNode.apparel.def,
                parms.pawn?.story?.bodyType, facing,
                out var bodyOffsetX, out var bodyOffsetZ, out _);

            __result.z += BaseZ + bodyOffsetZ;
            __result.y += DepthAboveShell;

            switch (facing.AsInt)
            {
                case 0: // North
                    __result.x += bodyOffsetX;
                    __result.y += NorthExtraDepth;
                    break;
                case 1: // East
                    __result.x += SideShiftX + bodyOffsetX;
                    break;
                case 2: // South
                    __result.x += bodyOffsetX;
                    break;
                case 3: // West
                    __result.x -= SideShiftX + bodyOffsetX;
                    break;
            }
        }
    }

}