using UnityEngine;
using Verse;

namespace RocketsRanks
{
    public class Dialog_WriteCeremonyCitation : Window
    {
        private const int MAX_CITATION_LENGTH = 300;

        private readonly PromotionCeremonyState state;
        private string draft;

        public Dialog_WriteCeremonyCitation(PromotionCeremonyState state)
        {
            this.state = state;
            draft = state?.citation ?? "";
            forcePause = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new(520f, 280f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            var headerRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            var awardeeName = state?.awardee?.LabelShortCap ?? "Awardee";
            Widgets.Label(headerRect, "ROCKET_CitationDialogHeader".Translate(awardeeName));
            Text.Font = GameFont.Small;

            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            var hintRect = new Rect(inRect.x, headerRect.yMax + 6f, inRect.width, 18f);
            Widgets.Label(hintRect, "ROCKET_PromotionCitation".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            var textRect = new Rect(inRect.x, hintRect.yMax + 4f,
                inRect.width, inRect.height - hintRect.yMax - 4f - 50f);
            draft = Widgets.TextArea(textRect, draft);
            if (draft.Length > MAX_CITATION_LENGTH)
                draft = draft.Substring(0, MAX_CITATION_LENGTH);

            var countColor = draft.Length > MAX_CITATION_LENGTH - 30 ? Color.yellow : Color.gray;
            GUI.color = countColor;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(inRect.x, textRect.yMax + 2f, inRect.width, 18f),
                $"{draft.Length} / {MAX_CITATION_LENGTH}");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            var btnY = inRect.yMax - 35f;
            var btnW = (inRect.width - 10f) / 2f;
            if (Widgets.ButtonText(new Rect(inRect.x, btnY, btnW, 30f), "ROCKET_CitationDialogSave".Translate()))
            {
                if (state != null)
                    state.citation = string.IsNullOrWhiteSpace(draft) ? null : draft.Trim();
                Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.x + btnW + 10f, btnY, btnW, 30f), "ROCKET_CitationDialogSkip".Translate()))
            {
                Close();
            }
        }
    }
}
