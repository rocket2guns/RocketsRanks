using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RocketsRanks
{
    [StaticConstructorOnStartup]
    public static class RankTextures
    {
        public static readonly Texture2D PromoteIcon =
            ContentFinder<Texture2D>.Get("UI/ButtonPromote", false) ?? BaseContent.BadTex;
    }

    public class RanksMod : Mod
    {
        public static RanksModSettings Settings;

        public RanksMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RanksModSettings>();
        }

        public override string SettingsCategory() => "Rocket's Ranks";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var viewHeight = 500f;
            if (Settings.ShowRankBadge) viewHeight += 150f;
            if (Settings.ShowRankOnMap) viewHeight += 130f;
            if (Settings.ShowBodyTypeDebug) viewHeight += RankRenderSettings.BodyTypeLabels.Length * 420f;

            var viewRect = new Rect(0f, 0f, inRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(inRect, ref Settings.settingsScroll, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            listing.CheckboxLabeled(
                "Show rank in pawn label",
                ref Settings.ShowRankInLabel,
                "If enabled, a pawn's rank will be shown as a prefix in their name label."
            );

            listing.CheckboxLabeled(
                "Show rank badge on portraits",
                ref Settings.ShowRankBadge,
                "If enabled, the pawn's rank icon will be displayed on their colonist bar portrait."
            );
            if (Settings.ShowRankBadge)
            {
                listing.Label($"Badge size: {Settings.BadgeSize:F0}px");
                Settings.BadgeSize = listing.Slider(Settings.BadgeSize, 12f, 128f);
                listing.Label($"Badge offset X: {Settings.BadgeOffsetX:F0}px");
                Settings.BadgeOffsetX = listing.Slider(Settings.BadgeOffsetX, -64f, 64f);
                listing.Label($"Badge offset Y: {Settings.BadgeOffsetY:F0}px");
                Settings.BadgeOffsetY = listing.Slider(Settings.BadgeOffsetY, -64f, 64f);
            }

            // --- Colonist bar ---
            listing.GapLine();
            listing.CheckboxLabeled(
                "Hide cryptosleep colonists from bar",
                ref Settings.HideCryptosleep,
                "If enabled, colonists inside cryptosleep caskets will not appear on the colonist bar."
            );
            listing.CheckboxLabeled(
                "Hide off-map colonists from bar",
                ref Settings.HideOffMap,
                "If enabled, colonists on a different map than the one you are viewing will not appear on the colonist bar."
            );
            listing.CheckboxLabeled(
                "Hide colonists when in map view",
                ref Settings.HideInMap,
                "If enabled, colonists in bar will be hidden while in map view."
            );
            listing.GapLine();

            listing.CheckboxLabeled(
                "Show rank icon on map labels",
                ref Settings.ShowRankOnMap,
                "If enabled, the pawn's rank icon will be displayed next to their name label on the map."
            );
            if (Settings.ShowRankOnMap)
            {
                listing.Label($"Map icon size: {Settings.MapIconSize:F0}px");
                Settings.MapIconSize = listing.Slider(Settings.MapIconSize, 8f, 32f);
                listing.Label($"Map icon offset X: {Settings.MapIconOffsetX:F0}px");
                Settings.MapIconOffsetX = listing.Slider(Settings.MapIconOffsetX, -16f, 16f);
                listing.Label($"Map icon offset Y: {Settings.MapIconOffsetY:F0}px");
                Settings.MapIconOffsetY = listing.Slider(Settings.MapIconOffsetY, -16f, 16f);
            }

            listing.GapLine();
            listing.CheckboxLabeled(
                "Show worn render debug settings",
                ref Settings.ShowBodyTypeDebug,
                "Expand per-body-type offset and scale controls for worn rank insignia."
            );

            if (Settings.ShowBodyTypeDebug)
            {
                RankRenderSettings.EnsureDefaults(Settings.BodySettings);

                for (var i = 0; i < (int)RankBodyType.Count; i++)
                {
                    var bs = Settings.BodySettings[i];
                    if (bs == null) continue;

                    listing.Gap(6f);
                    GUI.color = new Color(0.9f, 0.85f, 0.4f);
                    listing.Label($"— {RankRenderSettings.BodyTypeLabels[i]} —");
                    GUI.color = Color.white;

                    // North/South settings
                    listing.Label("  North / South:");
                    listing.Label($"    Offset X: {bs.north.offsetX:F3}");
                    bs.north.offsetX = listing.Slider(bs.north.offsetX, -0.3f, 0.3f);
                    listing.Label($"    Offset Z: {bs.north.offsetZ:F3}");
                    bs.north.offsetZ = listing.Slider(bs.north.offsetZ, -0.3f, 0.3f);
                    listing.Label($"    Scale: {bs.north.scale:F2}");
                    bs.north.scale = listing.Slider(bs.north.scale, 0.1f, 3f);

                    // East/West settings
                    listing.Label("  East / West:");
                    listing.Label($"    Offset X: {bs.east.offsetX:F3}");
                    bs.east.offsetX = listing.Slider(bs.east.offsetX, -0.3f, 0.3f);
                    listing.Label($"    Offset Z: {bs.east.offsetZ:F3}");
                    bs.east.offsetZ = listing.Slider(bs.east.offsetZ, -0.3f, 0.3f);
                    listing.Label($"    Scale: {bs.east.scale:F2}");
                    bs.east.scale = listing.Slider(bs.east.scale, 0.1f, 3f);
                }
            }

            listing.End();
            Widgets.EndScrollView();
        }
    }

    public class RanksModSettings : ModSettings
    {
        public bool ShowRankInLabel = true;
        public bool ShowRankBadge = true;
        public float BadgeSize = 46f;
        public float BadgeOffsetX = 4f;
        public float BadgeOffsetY = 4f;
        public bool ShowRankOnMap = true;
        public float MapIconSize = 16f;
        public float MapIconOffsetX = 0f;
        public float MapIconOffsetY = -3f;
        public bool HideCryptosleep = false;
        public bool HideOffMap;
        public bool HideInMap;
        public bool ShowBodyTypeDebug;
        public RankBodyTypeSettings[] BodySettings = new RankBodyTypeSettings[(int)RankBodyType.Count];

        // UI state (not saved)
        public Vector2 settingsScroll;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ShowRankInLabel, "ShowRankInLabel", true);
            Scribe_Values.Look(ref ShowRankBadge, "ShowRankBadge", true);
            Scribe_Values.Look(ref BadgeSize, "BadgeSize", 46f);
            Scribe_Values.Look(ref BadgeOffsetX, "BadgeOffsetX", 4f);
            Scribe_Values.Look(ref BadgeOffsetY, "BadgeOffsetY", 4f);
            Scribe_Values.Look(ref ShowRankOnMap, "ShowRankOnMap", true);
            Scribe_Values.Look(ref MapIconSize, "MapIconSize", 16f);
            Scribe_Values.Look(ref MapIconOffsetX, "MapIconOffsetX", 0f);
            Scribe_Values.Look(ref MapIconOffsetY, "MapIconOffsetY", -3f);
            Scribe_Values.Look(ref HideCryptosleep, "HideCryptosleep", false);
            Scribe_Values.Look(ref HideOffMap, "HideOffMap", false);
            Scribe_Values.Look(ref HideInMap, "HideInMap", false);
            Scribe_Values.Look(ref ShowBodyTypeDebug, "ShowBodyTypeDebug", false);

            if (BodySettings == null)
                BodySettings = new RankBodyTypeSettings[(int)RankBodyType.Count];

            Scribe_Deep.Look(ref BodySettings[(int)RankBodyType.Male], "bodyMale");
            Scribe_Deep.Look(ref BodySettings[(int)RankBodyType.Female], "bodyFemale");
            Scribe_Deep.Look(ref BodySettings[(int)RankBodyType.Thin], "bodyThin");
            Scribe_Deep.Look(ref BodySettings[(int)RankBodyType.Fat], "bodyFat");
            Scribe_Deep.Look(ref BodySettings[(int)RankBodyType.Hulk], "bodyHulk");

            RankRenderSettings.EnsureDefaults(BodySettings);
        }
    }
    
    /// <summary>
    /// Marker subclass of Apparel for rank insignia items.
    /// </summary>
    public class RocketRank : Apparel
    {

    }

    [StaticConstructorOnStartup]
    public static class RanksModInit
    {
        static RanksModInit()
        {
            new Harmony("com.rocket.ranks").PatchAll();
            Log.Message("[RocketsRanks] Harmony patches applied successfully.");
        }
    }
}