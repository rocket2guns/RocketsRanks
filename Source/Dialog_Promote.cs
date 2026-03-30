using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RocketsRanks
{
    public class Dialog_Promote : Window
    {
        private readonly Pawn pawn;
        private readonly List<RankDef> allRanks;
        private RankDef selectedRank;
        private string draft = "";
        private Vector2 rankScroll;

        private const int MAX_CITATION_LENGTH = 300;
        private const float PAWN_COLUMN_WIDTH = 160f;
        private const float RANK_LIST_HEIGHT = 200f;
        private const float ROW_HEIGHT = 34f;
        private const float ICON_SIZE = 24f;

        public Dialog_Promote(Pawn pawn)
        {
            this.pawn = pawn;
            allRanks = DefDatabase<RankDef>.AllDefsListForReading
                .OrderBy(r => r.rankLevel)
                .ToList();

            var comp = RankGameComponent.Instance;
            selectedRank = comp?.GetRank(pawn);

            forcePause = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new(620f, 510f);

        public override void DoWindowContents(Rect inRect)
        {
            var pawnCol = new Rect(inRect.x, inRect.y, PAWN_COLUMN_WIDTH, inRect.height - 45f);
            DrawPawnColumn(pawnCol);

            var separatorX = pawnCol.xMax + 10f;
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            Widgets.DrawLineVertical(separatorX, inRect.y, inRect.height - 45f);
            GUI.color = Color.white;

            var rightCol = new Rect(separatorX + 10f, inRect.y,
                inRect.width - PAWN_COLUMN_WIDTH - 30f, inRect.height);
            DrawRightColumn(rightCol);
        }

        private void DrawPawnColumn(Rect rect)
        {
            // Pawn portrait
            var portraitSize = 128f;
            var portraitX = rect.x + (rect.width - portraitSize) / 2f;
            var portraitRect = new Rect(portraitX, rect.y + 10f, portraitSize, portraitSize);
            GUI.DrawTexture(portraitRect, PortraitsCache.Get(pawn, new Vector2(portraitSize, portraitSize),
                Rot4.South, default, 1f, true, true, false, false, null));

            // Pawn name
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            var nameHeight = Text.CalcHeight(pawn.LabelShort, rect.width);
            var nameRect = new Rect(rect.x, portraitRect.yMax + 10f, rect.width, nameHeight);
            Widgets.Label(nameRect, pawn.LabelShort);

            // Current rank with icon
            var comp = RankGameComponent.Instance;
            var currentRank = comp?.GetRank(pawn);
            var curY = nameRect.yMax + 6f;

            if (currentRank != null)
            {
                var rankIconSize = 32f;
                if (currentRank.Icon != null)
                {
                    var iconX = rect.x + (rect.width - rankIconSize) / 2f;
                    GUI.DrawTexture(new Rect(iconX, curY, rankIconSize, rankIconSize), currentRank.Icon);
                    curY += rankIconSize + 4f;
                }

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperCenter;
                GUI.color = new Color(0.9f, 0.85f, 0.4f);
                var rankText = currentRank.LabelCap.ToString();
                var rankHeight = Text.CalcHeight(rankText, rect.width);
                Widgets.Label(new Rect(rect.x, curY, rect.width, rankHeight), rankText);
                GUI.color = Color.white;
                curY += rankHeight + 2f;
            }
            else
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperCenter;
                GUI.color = Color.gray;
                var noRankText = "No rank assigned";
                var noRankHeight = Text.CalcHeight(noRankText, rect.width);
                Widgets.Label(new Rect(rect.x, curY, rect.width, noRankHeight), noRankText);
                GUI.color = Color.white;
                curY += noRankHeight + 2f;
            }

            // Promotion count
            var data = comp?.GetData(pawn);
            if (data is { history.Count: > 0 })
            {
                GUI.color = Color.gray;
                var histText = $"{data.history.Count} promotion(s)";
                var histHeight = Text.CalcHeight(histText, rect.width);
                Widgets.Label(new Rect(rect.x, curY, rect.width, histHeight), histText);
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawRightColumn(Rect rect)
        {
            // Header
            Text.Font = GameFont.Medium;
            var headerRect = new Rect(rect.x, rect.y + 10f, rect.width, 30f);
            Widgets.Label(headerRect, "ROCKET_PromoteHeader".Translate());
            Text.Font = GameFont.Small;

            // Rank list
            var listLabel = new Rect(rect.x, headerRect.yMax + 6f, rect.width, 20f);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(listLabel, "ROCKET_SelectRank".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            var listRect = new Rect(rect.x, listLabel.yMax + 4f, rect.width, RANK_LIST_HEIGHT);
            DrawRankList(listRect);

            // Citation
            Text.Font = GameFont.Tiny;
            var citLabel = new Rect(rect.x, listRect.yMax + 8f, rect.width, 18f);
            GUI.color = Color.gray;
            Widgets.Label(citLabel, "ROCKET_PromotionCitation".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            var textRect = new Rect(rect.x, citLabel.yMax + 4f, rect.width, 80f);
            draft = Widgets.TextArea(textRect, draft);
            if (draft.Length > MAX_CITATION_LENGTH)
                draft = draft.Substring(0, MAX_CITATION_LENGTH);

            // Char count
            var countColor = draft.Length > MAX_CITATION_LENGTH - 30 ? Color.yellow : Color.gray;
            GUI.color = countColor;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(rect.x, textRect.yMax + 2f, rect.width, 18f),
                $"{draft.Length} / {MAX_CITATION_LENGTH}");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // Buttons
            Text.Font = GameFont.Small;
            var btnY = textRect.yMax + 24f;
            var btnWidth = (rect.width - 10f) / 2f;

            var comp = RankGameComponent.Instance;
            var currentRank = comp?.GetRank(pawn);
            var canConfirm = selectedRank != currentRank;

            if (canConfirm)
            {
                if (Widgets.ButtonText(new Rect(rect.x, btnY, btnWidth, 35f), "ROCKET_ConfirmPromotion".Translate()))
                {
                    ApplyPromotion();
                    Close();
                }
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.ButtonText(new Rect(rect.x, btnY, btnWidth, 35f), "ROCKET_ConfirmPromotion".Translate());
                GUI.color = Color.white;
            }

            if (Widgets.ButtonText(new Rect(rect.x + btnWidth + 10f, btnY, btnWidth, 35f), "Cancel"))
                Close();
        }

        private void DrawRankList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            var innerRect = rect.ContractedBy(4f);
            var viewHeight = (allRanks.Count + 1) * ROW_HEIGHT; // +1 for "No rank"
            var viewRect = new Rect(0f, 0f, innerRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(innerRect, ref rankScroll, viewRect);

            var curY = 0f;

            // "No rank" option
            var noRankRect = new Rect(0f, curY, viewRect.width, ROW_HEIGHT);
            if (selectedRank == null)
                Widgets.DrawHighlight(noRankRect);
            if (Mouse.IsOver(noRankRect))
                Widgets.DrawHighlight(noRankRect);
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = selectedRank == null ? Color.white : Color.gray;
            Widgets.Label(new Rect(10f, curY, viewRect.width - 10f, ROW_HEIGHT), "No rank");
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(noRankRect))
                selectedRank = null;
            curY += ROW_HEIGHT;

            // Rank entries
            foreach (var rank in allRanks)
            {
                var rowRect = new Rect(0f, curY, viewRect.width, ROW_HEIGHT);
                var isSelected = selectedRank == rank;

                if (isSelected)
                    Widgets.DrawHighlight(rowRect);
                if (Mouse.IsOver(rowRect))
                    Widgets.DrawHighlight(rowRect);

                // Icon
                var textOffset = 10f;
                if (rank.Icon != null)
                {
                    var iconY = curY + (ROW_HEIGHT - ICON_SIZE) / 2f;
                    GUI.DrawTexture(new Rect(8f, iconY, ICON_SIZE, ICON_SIZE), rank.Icon);
                    textOffset = 8f + ICON_SIZE + 6f;
                }

                // Label
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(textOffset, curY, viewRect.width - textOffset - 60f, ROW_HEIGHT), rank.LabelCap);

                // Rank level on the right
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleRight;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(0f, curY, viewRect.width - 10f, ROW_HEIGHT), $"Lvl {rank.rankLevel}");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                if (Widgets.ButtonInvisible(rowRect))
                    selectedRank = rank;

                Text.Anchor = TextAnchor.UpperLeft;
                curY += ROW_HEIGHT;
            }

            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void ApplyPromotion()
        {
            var comp = RankGameComponent.Instance;
            if (comp == null) return;

            var previousRank = comp.GetRank(pawn);
            comp.SetRank(pawn, selectedRank, draft);

            string actionLabel;
            if (selectedRank == null)
                actionLabel = $"{pawn.NameShortColored} has been stripped of rank.";
            else if (previousRank == null)
                actionLabel = $"{pawn.NameShortColored} has been assigned the rank of {selectedRank.LabelCap}.";
            else if (selectedRank.rankLevel > previousRank.rankLevel)
                actionLabel = $"{pawn.NameShortColored} has been promoted to {selectedRank.LabelCap}.";
            else if (selectedRank.rankLevel < previousRank.rankLevel)
                actionLabel = $"{pawn.NameShortColored} has been demoted to {selectedRank.LabelCap}.";
            else
                actionLabel = $"{pawn.NameShortColored} has been transferred to {selectedRank.LabelCap}.";

            Messages.Message(actionLabel, pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}
