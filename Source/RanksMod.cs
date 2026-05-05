using System.Collections.Generic;
using System.Linq;
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

        private enum SettingsTab { Packs, Labels, Bar, Debug }
        private static SettingsTab currentTab = SettingsTab.Packs;
        private static readonly List<TabRecord> tabBuf = new();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            const float tabBarHeight = 32f;
            var contentRect = new Rect(inRect.x, inRect.y + tabBarHeight, inRect.width, inRect.height - tabBarHeight);

            tabBuf.Clear();
            tabBuf.Add(new TabRecord("Rank Packs", () => currentTab = SettingsTab.Packs, currentTab == SettingsTab.Packs));
            tabBuf.Add(new TabRecord("Pawn Labels", () => currentTab = SettingsTab.Labels, currentTab == SettingsTab.Labels));
            tabBuf.Add(new TabRecord("Colonist Bar", () => currentTab = SettingsTab.Bar, currentTab == SettingsTab.Bar));
            tabBuf.Add(new TabRecord("Debug", () => currentTab = SettingsTab.Debug, currentTab == SettingsTab.Debug));

            Widgets.DrawMenuSection(contentRect);
            TabDrawer.DrawTabs(contentRect, tabBuf);

            var inner = contentRect.ContractedBy(12f);
            switch (currentTab)
            {
                case SettingsTab.Packs:  DrawPacksTab(inner); break;
                case SettingsTab.Labels: DrawLabelsTab(inner); break;
                case SettingsTab.Bar:    DrawBarTab(inner); break;
                case SettingsTab.Debug:  DrawDebugTab(inner); break;
            }
        }

        private static void DrawPacksTab(Rect rect)
        {
            var packs = DefDatabase<RankPackDef>.AllDefsListForReading;
            Settings.HiddenPacks ??= new HashSet<string>();

            var listing = new Listing_Standard();
            listing.Begin(rect);
            Text.Font = GameFont.Small;

            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            listing.Label("Tick a pack to enable it. Disabled packs have their ranks hidden from the promote menu and their apparel removed from crafting menus. Existing ranks and items in the world are not affected.");
            GUI.color = Color.white;
            listing.Gap(8f);

            foreach (var pack in packs.OrderBy(p => p.defName))
            {
                var enabled = !Settings.HiddenPacks.Contains(pack.defName);
                var wasEnabled = enabled;
                listing.CheckboxLabeled(pack.LabelCap, ref enabled, pack.description);
                if (enabled == wasEnabled) continue;
                if (enabled) Settings.HiddenPacks.Remove(pack.defName);
                else Settings.HiddenPacks.Add(pack.defName);
            }

            listing.End();
        }

        private static void DrawLabelsTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            Text.Font = GameFont.Small;

            listing.CheckboxLabeled(
                "Show rank in pawn label",
                ref Settings.ShowRankInLabel,
                "If enabled, a pawn's rank will be shown as a prefix in their name label."
            );
            listing.CheckboxLabeled(
                "Show rank icon on map labels",
                ref Settings.ShowRankOnMap,
                "If enabled, the pawn's rank icon will be displayed next to their name label on the map."
            );
            listing.CheckboxLabeled(
                "Hide map rank icon when not drafted",
                ref Settings.HideRankWhenUndrafted,
                "If enabled, the rank icon on map labels will only be shown when the pawn is drafted."
            );
            if (Settings.ShowRankOnMap)
            {
                listing.Gap(6f);
                listing.Label($"Map icon size: {Settings.MapIconSize:F0}px");
                Settings.MapIconSize = listing.Slider(Settings.MapIconSize, 8f, 32f);
                listing.Label($"Map icon offset X: {Settings.MapIconOffsetX:F0}px");
                Settings.MapIconOffsetX = listing.Slider(Settings.MapIconOffsetX, -16f, 16f);
                listing.Label($"Map icon offset Y: {Settings.MapIconOffsetY:F0}px");
                Settings.MapIconOffsetY = listing.Slider(Settings.MapIconOffsetY, -16f, 16f);
            }

            listing.End();
        }

        private static void DrawBarTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            Text.Font = GameFont.Small;

            // Visibility filters
            listing.CheckboxLabeled(
                "Hide cryptosleep colonists",
                ref Settings.HideCryptosleep,
                "Colonists inside cryptosleep caskets will not appear on the colonist bar."
            );
            listing.CheckboxLabeled(
                "Hide off-map colonists",
                ref Settings.HideOffMap,
                "Colonists on a different map than the one you are viewing will not appear on the colonist bar."
            );
            listing.CheckboxLabeled(
                "Hide colonists in map view",
                ref Settings.HideInMap,
                "Colonists in bar will be hidden while in map view."
            );

            listing.Gap(10f);
            listing.GapLine();

            // Rank badge
            SubHeader(listing, "Rank Badge");
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

            listing.Gap(10f);
            listing.GapLine();

            // Weapon icon
            SubHeader(listing, "Weapon Icon");
            listing.Label($"Offset X: {Settings.WeaponOffsetX:F0}px");
            Settings.WeaponOffsetX = listing.Slider(Settings.WeaponOffsetX, -64f, 64f);
            listing.Label($"Offset Y: {Settings.WeaponOffsetY:F0}px");
            Settings.WeaponOffsetY = listing.Slider(Settings.WeaponOffsetY, -64f, 64f);
            listing.Label($"Scale: {Settings.WeaponScale:F2}x");
            Settings.WeaponScale = listing.Slider(Settings.WeaponScale, 0.1f, 3.0f);

            listing.End();
        }

        private static void DrawDebugTab(Rect rect)
        {
            // Debug content can be tall when expanded — wrap in a scroll view.
            var viewHeight = 90f;
            if (Settings.ShowBodyTypeDebug)
                viewHeight += RankRenderSettings.BodyTypeLabels.Length * 420f;

            var viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);
            Widgets.BeginScrollView(rect, ref Settings.settingsScroll, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);
            Text.Font = GameFont.Small;

            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            listing.Label("Tools for content creators tuning rank insignia placement. Most players can leave this off.");
            GUI.color = Color.white;
            listing.Gap(6f);

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

                    listing.Label("  North / South:");
                    listing.Label($"    Offset X: {bs.north.offsetX:F3}");
                    bs.north.offsetX = listing.Slider(bs.north.offsetX, -0.3f, 0.3f);
                    listing.Label($"    Offset Z: {bs.north.offsetZ:F3}");
                    bs.north.offsetZ = listing.Slider(bs.north.offsetZ, -0.3f, 0.3f);
                    listing.Label($"    Scale: {bs.north.scale:F2}");
                    bs.north.scale = listing.Slider(bs.north.scale, 0.1f, 3f);

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

        private static void SubHeader(Listing_Standard listing, string text)
        {
            Text.Font = GameFont.Medium;
            listing.Label(text);
            Text.Font = GameFont.Small;
            listing.Gap(4f);
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
        public float WeaponOffsetX = 0f;
        public float WeaponOffsetY = 0f;
        public float WeaponScale = 1f;
        public bool HideCryptosleep = false;
        public bool HideOffMap;
        public bool HideInMap;
        public bool HideRankWhenUndrafted;
        public bool ShowBodyTypeDebug;
        public HashSet<string> HiddenPacks = new();
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
            Scribe_Values.Look(ref WeaponOffsetX, "WeaponOffsetX", 0f);
            Scribe_Values.Look(ref WeaponOffsetY, "WeaponOffsetY", 0f);
            Scribe_Values.Look(ref WeaponScale, "WeaponScale", 1f);
            Scribe_Values.Look(ref MapIconOffsetX, "MapIconOffsetX", 0f);
            Scribe_Values.Look(ref MapIconOffsetY, "MapIconOffsetY", -3f);
            Scribe_Values.Look(ref HideCryptosleep, "HideCryptosleep", false);
            Scribe_Values.Look(ref HideOffMap, "HideOffMap", false);
            Scribe_Values.Look(ref HideInMap, "HideInMap", false);
            Scribe_Values.Look(ref HideRankWhenUndrafted, "HideRankWhenUndrafted", false);
            Scribe_Values.Look(ref ShowBodyTypeDebug, "ShowBodyTypeDebug", false);
            Scribe_Collections.Look(ref HiddenPacks, "HiddenPacks", LookMode.Value);
            HiddenPacks ??= new HashSet<string>();

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