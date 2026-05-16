using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RocketsRanks
{
    [StaticConstructorOnStartup]
    public static class InjectRankTab
    {
        static InjectRankTab()
        {
            var tabType = typeof(ITab_PawnRanks);
            var tabInstance = InspectTabManager.GetSharedInstance(tabType);

            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.race is not { Humanlike: true }) continue;
                InjectIntoDef(def, tabType, tabInstance);
                if (def.race.corpseDef != null)
                    InjectIntoDef(def.race.corpseDef, tabType, tabInstance);
                def.comps ??= new List<CompProperties>();
                if (!def.comps.Any(c => c.compClass == typeof(CompRank)))
                    def.comps.Add(new CompProperties { compClass = typeof(CompRank) });
            }
        }

        private static void InjectIntoDef(ThingDef def, Type tabType, InspectTabBase tabInstance)
        {
            if (def == null) return;
            def.inspectorTabs ??= new List<Type>();
            if (!def.inspectorTabs.Contains(tabType))
                def.inspectorTabs.Add(tabType);
            def.inspectorTabsResolved ??= new List<InspectTabBase>();
            if (!def.inspectorTabsResolved.Contains(tabInstance))
                def.inspectorTabsResolved.Add(tabInstance);
        }
    }

    public class ITab_PawnRanks : ITab
    {
        private enum SubTab { History, Settings }

        private Vector2 _scrollPosition;
        private static SubTab currentSubTab = SubTab.History;
        private static readonly List<TabRecord> tabBuf = new();

        private const float PADDING = 10f;
        private const float TAB_WIDTH = 400f;
        private const float TAB_HEIGHT = 480f;
        private const float RECORD_ROW_HEIGHT = 60f;
        private const float SUB_TAB_BAR_HEIGHT = 32f;

        private static readonly Color GoldColor = new(0.9f, 0.85f, 0.4f);
        private static readonly Color GreenColor = new(0.4f, 0.9f, 0.4f);
        private static readonly Color RedColor = new(0.9f, 0.4f, 0.4f);
        private static readonly Color MutedColor = new(0.7f, 0.7f, 0.7f);

        public ITab_PawnRanks()
        {
            labelKey = "ROCKET_RanksTab";
            size = new Vector2(TAB_WIDTH, TAB_HEIGHT);
        }

        public override bool IsVisible
        {
            get
            {
                var pawn = SelPawnForGear;
                if (pawn == null) return false;
                var comp = pawn.GetComp<CompRank>();
                return comp?.history.Count > 0 || comp?.currentRank != null;
            }
        }

        private Pawn SelPawnForGear => SelThing switch
        {
            Pawn p => p,
            Corpse corpse => corpse.InnerPawn,
            _ => null
        };

        protected override void FillTab()
        {
            var pawn = SelPawnForGear;
            var comp = pawn?.GetComp<CompRank>();
            if (comp == null) return;

            var rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(PADDING);
            var curY = DrawHeader(rect, rect.y, comp, pawn);

            var subTabContentRect = new Rect(rect.x, curY + SUB_TAB_BAR_HEIGHT, rect.width, rect.yMax - curY - SUB_TAB_BAR_HEIGHT);
            Widgets.DrawMenuSection(subTabContentRect);
            DrawSubTabBar(subTabContentRect);

            var innerContentRect = subTabContentRect.ContractedBy(8f);
            switch (currentSubTab)
            {
                case SubTab.History:
                    DrawHistorySubTab(innerContentRect, comp, pawn);
                    break;
                case SubTab.Settings:
                    DrawSettingsSubTab(innerContentRect, comp);
                    break;
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private float DrawHeader(Rect rect, float curY, CompRank comp, Pawn pawn)
        {
            // Rank icon, centered
            if (comp.currentRank?.Icon != null)
            {
                var iconSize = 48f;
                var iconX = rect.x + (rect.width - iconSize) / 2f;
                GUI.DrawTexture(new Rect(iconX, curY, iconSize, iconSize), comp.currentRank.Icon);
                curY += iconSize + 6f;
            }

            // Header: Current rank
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            var rankLabel = comp.currentRank != null
                ? comp.currentRank.RankLabel
                : "ROCKET_NoRank".Translate().ToString();
            var headerHeight = Text.CalcHeight(rankLabel, rect.width);
            Widgets.Label(new Rect(rect.x, curY, rect.width, headerHeight), rankLabel);
            curY += headerHeight + 4f;

            // Pawn name under rank
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            var nameHeight = Text.CalcHeight(pawn.NameFullColored, rect.width);
            Widgets.Label(new Rect(rect.x, curY, rect.width, nameHeight), pawn.NameFullColored);
            curY += nameHeight + 4f;

            // Rank description
            if (comp.currentRank?.description != null)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = MutedColor;
                var descHeight = Text.CalcHeight(comp.currentRank.description, rect.width);
                Widgets.Label(new Rect(rect.x, curY, rect.width, descHeight), comp.currentRank.description);
                GUI.color = Color.white;
                curY += descHeight + 4f;
            }

            curY += 8f;

            // Separator
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            Widgets.DrawLineHorizontal(rect.x, curY, rect.width);
            GUI.color = Color.white;
            curY += 8f;

            return curY;
        }

        private void DrawSubTabBar(Rect contentRect)
        {
            tabBuf.Clear();
            tabBuf.Add(new TabRecord(
                "ROCKET_PromotionHistory".Translate(),
                () => currentSubTab = SubTab.History,
                currentSubTab == SubTab.History));
            tabBuf.Add(new TabRecord(
                "ROCKET_RanksTab_SettingsSubTab".Translate(),
                () => currentSubTab = SubTab.Settings,
                currentSubTab == SubTab.Settings));
            TabDrawer.DrawTabs(contentRect, tabBuf);
        }

        private void DrawHistorySubTab(Rect rect, CompRank comp, Pawn pawn)
        {
            if (comp.history.Count == 0)
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(rect.x, rect.y + 4f, rect.width, 24f), "ROCKET_NoPromotionRecords".Translate());
                GUI.color = Color.white;
                return;
            }

            DrawHistoryList(rect, comp.history, pawn);
        }

        private void DrawSettingsSubTab(Rect rect, CompRank comp)
        {
            Widgets.BeginGroup(rect);
            var curY = 4f;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.ListSeparator(ref curY, rect.width, "ROCKET_Settings_MapDisplayHeader".Translate());
            curY += 4f;

            const float rowHeight = 24f;
            var checkboxRect = new Rect(0f, curY, rect.width, rowHeight);
            var showTitle = comp.showTitleOnMap;
            Widgets.CheckboxLabeled(checkboxRect, "ROCKET_Settings_ShowTitleOnMap".Translate(), ref showTitle);
            if (Mouse.IsOver(checkboxRect))
                TooltipHandler.TipRegion(checkboxRect, "ROCKET_Settings_ShowTitleOnMapTip".Translate());
            comp.showTitleOnMap = showTitle;
            curY += rowHeight + 4f;

            var enabled = comp.showTitleOnMap;
            var rowRect = new Rect(0f, curY, rect.width, rowHeight);
            GUI.color = enabled ? Color.white : MutedColor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rowRect, "ROCKET_Settings_TitleColor".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            const float swatchSize = 24f;
            var swatchRect = new Rect(rowRect.xMax - swatchSize, curY, swatchSize, swatchSize);
            var swatchColor = enabled
                ? comp.titleOnMapColor
                : new Color(comp.titleOnMapColor.r * 0.5f, comp.titleOnMapColor.g * 0.5f, comp.titleOnMapColor.b * 0.5f);
            Widgets.DrawBoxSolid(swatchRect, swatchColor);
            GUI.color = enabled ? Color.white : MutedColor;
            Widgets.DrawBox(swatchRect, 1);
            GUI.color = Color.white;

            if (enabled)
            {
                if (Mouse.IsOver(swatchRect))
                    Widgets.DrawHighlight(swatchRect);
                if (Widgets.ButtonInvisible(swatchRect))
                    OpenColorFloatMenu(comp);
            }

            Widgets.EndGroup();
        }

        private static void OpenColorFloatMenu(CompRank comp)
        {
            var options = new List<FloatMenuOption>(TitleColorPresets.All.Length);
            foreach (var preset in TitleColorPresets.All)
            {
                var capturedColor = preset.color;
                options.Add(new FloatMenuOption(
                    preset.LabelKey.Translate(),
                    () => comp.titleOnMapColor = capturedColor,
                    BaseContent.WhiteTex,
                    capturedColor));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void DrawHistoryList(Rect listRect, List<PromotionRecord> history, Pawn pawn)
        {
            var viewWidth = listRect.width - 16f;
            var totalHeight = 0f;
            for (var i = history.Count - 1; i >= 0; i--)
                totalHeight += GetRecordHeight(history[i], viewWidth) + 6f;

            var viewRect = new Rect(0f, 0f, viewWidth, totalHeight);
            Widgets.BeginScrollView(listRect, ref _scrollPosition, viewRect);

            var curY = 0f;
            for (var i = history.Count - 1; i >= 0; i--)
            {
                var record = history[i];
                var rowHeight = GetRecordHeight(record, viewWidth);
                var rowRect = new Rect(0f, curY, viewWidth, rowHeight);

                if (Mouse.IsOver(rowRect))
                    Widgets.DrawHighlight(rowRect);

                DrawRecord(rowRect, record, pawn);
                curY += rowHeight + 6f;
            }

            Widgets.EndScrollView();
        }

        private float GetRecordHeight(PromotionRecord record, float width)
        {
            var height = 4f;
            Text.Font = GameFont.Small;
            height += 22f;
            Text.Font = GameFont.Tiny;
            height += 18f;
            if (record.presentedBy != null || record.ceremonyQuality >= 0)
                height += 18f;
            if (!record.citation.NullOrEmpty())
            {
                var citHeight = Text.CalcHeight($"\"{record.citation}\"", width - 20f);
                height += citHeight + 2f;
            }
            height += 4f;
            return Mathf.Max(RECORD_ROW_HEIGHT, height);
        }

        private void DrawRecord(Rect rect, PromotionRecord record, Pawn pawn)
        {
            var curY = rect.y + 4f;
            var textX = rect.x + 6f;
            var textWidth = rect.width - 12f;

            var iconSize = 20f;
            if (record.rank?.Icon != null)
            {
                var iconY = curY + (22f - iconSize) / 2f;
                GUI.DrawTexture(new Rect(rect.x + 6f, iconY, iconSize, iconSize), record.rank.Icon);
                textX = rect.x + 6f + iconSize + 4f;
                textWidth = rect.width - 12f - iconSize - 4f;
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            string changeText;
            Color changeColor;
            if (record.rank == null)
            {
                changeText = "ROCKET_StrippedOfRank".Translate();
                changeColor = MutedColor;
            }
            else if (record.previousRank == null)
            {
                changeText = "ROCKET_PromotedTo".Translate(record.rank.RankLabel);
                changeColor = GreenColor;
            }
            else if (record.rank.rankLevel > record.previousRank.rankLevel)
            {
                changeText = "ROCKET_PromotedTo".Translate(record.rank.RankLabel);
                changeColor = GreenColor;
            }
            else if (record.rank.rankLevel < record.previousRank.rankLevel)
            {
                changeText = "ROCKET_DemotedTo".Translate(record.rank.RankLabel);
                changeColor = RedColor;
            }
            else
            {
                changeText = "ROCKET_RankChangedTo".Translate(record.rank.RankLabel);
                changeColor = MutedColor;
            }

            GUI.color = changeColor;
            Widgets.Label(new Rect(textX, curY, textWidth, 22f), changeText);
            GUI.color = Color.white;
            curY += 22f;

            Text.Font = GameFont.Tiny;
            GUI.color = MutedColor;
            if (record.tick >= 0)
            {
                var tile = pawn.Map?.Tile ?? Find.CurrentMap?.Tile ?? 0;
                var dateStr = GenDate.DateFullStringAt(
                    GenDate.TickGameToAbs(record.tick),
                    Find.WorldGrid.LongLatOf(tile));
                Widgets.Label(new Rect(textX, curY, textWidth, 18f), dateStr);
            }
            GUI.color = Color.white;
            curY += 18f;

            if (record.presentedBy != null || record.ceremonyQuality >= 0)
            {
                GUI.color = MutedColor;
                string ceremonyText;
                if (record.presentedBy != null && record.ceremonyQuality >= 0)
                    ceremonyText = $"Presented by {record.presentedBy.LabelShort} ({CeremonyQuality.GetQualityLabel(record.ceremonyQuality)} ceremony)";
                else if (record.presentedBy != null)
                    ceremonyText = $"Presented by {record.presentedBy.LabelShort}";
                else
                    ceremonyText = $"{CeremonyQuality.GetQualityLabel(record.ceremonyQuality).CapitalizeFirst()} ceremony";
                Widgets.Label(new Rect(textX, curY, textWidth, 18f), ceremonyText);
                GUI.color = Color.white;
                curY += 18f;
            }

            if (!record.citation.NullOrEmpty())
            {
                GUI.color = GoldColor;
                var citText = $"\"{record.citation}\"";
                var citHeight = Text.CalcHeight(citText, rect.width - 20f);
                Widgets.Label(new Rect(rect.x + 10f, curY, rect.width - 20f, citHeight), citText);
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
