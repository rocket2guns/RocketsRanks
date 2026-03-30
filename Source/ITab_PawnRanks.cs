using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var humanDef = ThingDef.Named("Human");
            var corpseDef = ThingDef.Named("Corpse_Human");
            InjectIntoDef(humanDef, tabType, tabInstance);
            InjectIntoDef(corpseDef, tabType, tabInstance);
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
        private Vector2 _scrollPosition;
        private const float PADDING = 10f;
        private const float TAB_WIDTH = 400f;
        private const float TAB_HEIGHT = 480f;
        private const float RECORD_ROW_HEIGHT = 60f;

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
                var comp = RankGameComponent.Instance;
                return comp?.GetData(pawn) != null;
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
            if (pawn == null) return;

            var comp = RankGameComponent.Instance;
            var data = comp?.GetData(pawn);
            if (data == null) return;

            var rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(PADDING);
            var curY = rect.y;

            // Rank icon, centered
            if (data.currentRank?.Icon != null)
            {
                var iconSize = 48f;
                var iconX = rect.x + (rect.width - iconSize) / 2f;
                GUI.DrawTexture(new Rect(iconX, curY, iconSize, iconSize), data.currentRank.Icon);
                curY += iconSize + 6f;
            }

            // Header: Current rank
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            var rankLabel = data.currentRank != null
                ? data.currentRank.LabelCap.ToString()
                : "No Rank";
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
            if (data.currentRank?.description != null)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = MutedColor;
                var descHeight = Text.CalcHeight(data.currentRank.description, rect.width);
                Widgets.Label(new Rect(rect.x, curY, rect.width, descHeight), data.currentRank.description);
                GUI.color = Color.white;
                curY += descHeight + 4f;
            }

            curY += 8f;

            // Separator
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            Widgets.DrawLineHorizontal(rect.x, curY, rect.width);
            GUI.color = Color.white;
            curY += 8f;

            // History header
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, curY, rect.width, 24f), "ROCKET_PromotionHistory".Translate());
            curY += 28f;

            // History list
            if (data.history.Count == 0)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(rect.x, curY, rect.width, 24f), "No promotion records.");
                GUI.color = Color.white;
            }
            else
            {
                var listRect = new Rect(rect.x, curY, rect.width, rect.yMax - curY);
                DrawHistoryList(listRect, data.history, pawn);
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawHistoryList(Rect listRect, List<PromotionRecord> history, Pawn pawn)
        {
            var viewWidth = listRect.width - 16f;
            var totalHeight = 0f;
            foreach (var record in history.AsEnumerable().Reverse())
                totalHeight += GetRecordHeight(record, viewWidth) + 6f;

            var viewRect = new Rect(0f, 0f, viewWidth, totalHeight);
            Widgets.BeginScrollView(listRect, ref _scrollPosition, viewRect);

            var curY = 0f;
            // Show newest first
            foreach (var record in history.AsEnumerable().Reverse())
            {
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
            // Rank label line
            height += 22f;
            Text.Font = GameFont.Tiny;
            // Date line
            height += 18f;
            // Citation
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

            // Rank icon inline
            var iconSize = 20f;
            if (record.rank?.Icon != null)
            {
                var iconY = curY + (22f - iconSize) / 2f;
                GUI.DrawTexture(new Rect(rect.x + 6f, iconY, iconSize, iconSize), record.rank.Icon);
                textX = rect.x + 6f + iconSize + 4f;
                textWidth = rect.width - 12f - iconSize - 4f;
            }

            // Rank change line
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            string changeText;
            Color changeColor;
            if (record.rank == null)
            {
                changeText = "Stripped of rank";
                changeColor = MutedColor;
            }
            else if (record.previousRank == null)
            {
                changeText = $"Promoted to {record.rank.LabelCap}";
                changeColor = GreenColor;
            }
            else if (record.rank.rankLevel > record.previousRank.rankLevel)
            {
                changeText = $"Promoted to {record.rank.LabelCap}";
                changeColor = GreenColor;
            }
            else if (record.rank.rankLevel < record.previousRank.rankLevel)
            {
                changeText = $"Demoted to {record.rank.LabelCap}";
                changeColor = RedColor;
            }
            else
            {
                changeText = $"Transferred to {record.rank.LabelCap}";
                changeColor = MutedColor;
            }

            GUI.color = changeColor;
            Widgets.Label(new Rect(textX, curY, textWidth, 22f), changeText);
            GUI.color = Color.white;
            curY += 22f;

            // Date
            Text.Font = GameFont.Tiny;
            GUI.color = MutedColor;
            if (record.tick >= 0)
            {
                var tile = pawn.Map?.Tile ?? Find.CurrentMap?.Tile ?? 0;
                var dateStr = GenDate.DateFullStringAt(
                    GenDate.TickGameToAbs(record.tick),
                    Find.WorldGrid.LongLatOf(tile));
                Widgets.Label(new Rect(rect.x + 6f, curY, rect.width - 12f, 18f), dateStr);
            }
            GUI.color = Color.white;
            curY += 18f;

            // Citation
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