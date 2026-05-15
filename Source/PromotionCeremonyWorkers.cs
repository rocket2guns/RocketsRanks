using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;

namespace RocketsRanks
{
    public class RitualStageAction_StartPromotionBestowal : RitualStageAction
    {
        public override void Apply(LordJob_Ritual ritual)
        {
            var leader = ritual.PawnWithRole("leader");
            var awardee = ritual.PawnWithRole("awardee");
            if (leader == null || awardee == null) return;

            var state = PromotionCeremonyRegistry.Get(ritual.Ritual);
            if (state != null
                && string.IsNullOrEmpty(state.citation)
                && !state.prompted
                && RanksMod.Settings.PromptForCitationDuringCeremony)
            {
                state.prompted = true;
                Find.WindowStack.Add(new Dialog_WriteCeremonyCitation(state));
            }

            var newRankLabel = state?.toRank?.RankLabel ?? "ROCKET_BestowalNewRankFallback".Translate().ToString();
            Messages.Message(
                "ROCKET_BestowalBegunMsg".Translate(leader.NameShortColored, awardee.NameShortColored, newRankLabel),
                leader,
                MessageTypeDefOf.PositiveEvent);
        }

        public override void ExposeData() { }
    }

    public class RitualOutcomeEffectWorkerPromote : RitualOutcomeEffectWorker
    {
        public RitualOutcomeEffectWorkerPromote() { }
        public RitualOutcomeEffectWorkerPromote(RitualOutcomeEffectDef def) : base(def) { }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            var state = PromotionCeremonyRegistry.Get(jobRitual.Ritual);
            if (state == null)
            {
                Log.Warning("[RocketsRanks] Promotion ceremony finished without state; skipping apply.");
                return;
            }

            var awardee = jobRitual.assignments.FirstAssignedPawn("awardee") ?? state.awardee;
            var presenter = jobRitual.assignments.FirstAssignedPawn("leader");
            if (awardee == null || presenter == null)
            {
                Log.Warning("[RocketsRanks] Promotion ceremony missing awardee or presenter; skipping apply.");
                PromotionCeremonyRegistry.Clear(jobRitual.Ritual);
                return;
            }

            var attendees = totalPresence.Count;
            var totalColonists = jobRitual.Map.mapPawns.FreeColonistsSpawnedCount;
            var roomImpressiveness = CeremonyQuality.GetRoomImpressiveness(jobRitual.selectedTarget);
            var hasCitation = !string.IsNullOrEmpty(state.citation);

            var qualityScore = CeremonyQuality.GetQualityScore(attendees, totalColonists, roomImpressiveness, hasCitation);
            var stageIndex = CeremonyQuality.GetStageIndex(qualityScore);
            var qualityLabel = CeremonyQuality.GetQualityLabel(stageIndex);

            var comp = awardee.GetComp<CompRank>();
            comp?.SetRank(state.toRank, state.citation, presenter, stageIndex);

            var awardedThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("ROCKET_PromotedByCeremony_Thought");
            if (awardedThought != null && awardee.needs?.mood != null)
            {
                var memory = (Thought_Memory)ThoughtMaker.MakeThought(awardedThought, stageIndex);
                awardee.needs.mood.thoughts.memories.TryGainMemory(memory);
            }

            var spectatorThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("ROCKET_WitnessedPromotionCeremony_Thought");
            if (spectatorThought != null)
            {
                foreach (var p in totalPresence.Keys)
                {
                    if (p == awardee) continue;
                    p.needs?.mood?.thoughts.memories.TryGainMemory(spectatorThought);
                }
            }

            var rankLabel = state.toRank?.RankLabel ?? "civilian";
            var letterLabel = $"{awardee.LabelShortCap} promoted";
            var letterText = $"{awardee.NameFullColored} has been officially promoted to {rankLabel} " +
                             $"by {presenter.NameFullColored}.\n\n" +
                             $"The {qualityLabel} ceremony was attended by {attendees} colonists, " +
                             $"and the deeds of {awardee.NameShortColored} have been recognized by the colony.";
            if (hasCitation)
                letterText += $"\n\nCitation: \"{state.citation}\"";

            Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.PositiveEvent, lookTargets: awardee);

            var tale = DefDatabase<TaleDef>.GetNamedSilentFail("ROCKET_PromotedTale");
            if (tale != null)
                TaleRecorder.RecordTale(tale, presenter, awardee);

            PromotionCeremonyRegistry.Clear(jobRitual.Ritual);
        }
    }

    public class RitualOutcomeComp_PromotionPreview : RitualOutcomeComp
    {
        public override bool Applies(LordJob_Ritual ritual) =>
            PromotionCeremonyRegistry.Get(ritual?.Ritual) != null;

        public override bool DataRequired => false;

        public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data) => 0f;

        public override string GetDesc(LordJob_Ritual ritual = null, RitualOutcomeComp_Data data = null)
        {
            var state = PromotionCeremonyRegistry.Get(ritual?.Ritual);
            return state?.toRank?.RankLabel ?? "ROCKET_OutcomeDesc_Preview".Translate();
        }

        public override QualityFactor GetQualityFactor(
            Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation,
            RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            var state = PromotionCeremonyRegistry.Get(ritual);
            if (state == null) return null;
            var newRankLabel = state.toRank?.RankLabel ?? "ROCKET_OutcomePreview_RankFallback".Translate().ToString();
            var awardeeName = state.awardee?.LabelShort ?? "ROCKET_OutcomePreview_AwardeeFallback".Translate().ToString();
            return new QualityFactor
            {
                label = "ROCKET_OutcomeLabel_Preview".Translate(),
                count = newRankLabel,
                quality = 0f,
                qualityChange = "",
                positive = true,
                present = true,
                priority = 100f,
                toolTip = "ROCKET_OutcomePreview_Tooltip".Translate(awardeeName, newRankLabel)
            };
        }
    }

    public class RitualOutcomeComp_PromotionAttendance : RitualOutcomeComp
    {
        public override bool Applies(LordJob_Ritual ritual) => true;
        public override bool DataRequired => false;

        public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            if (ritual?.Map == null) return 0f;
            var attendees = ritual.assignments.Participants.Count;
            var total = ritual.Map.mapPawns.FreeColonistsSpawnedCount;
            var ratio = total > 0 ? Mathf.Clamp01((float)attendees / total) : 0f;
            return ratio * 0.4f;
        }

        public override string GetDesc(LordJob_Ritual ritual = null, RitualOutcomeComp_Data data = null) =>
            "ROCKET_OutcomeDesc_Attendance".Translate();

        public override QualityFactor GetQualityFactor(
            Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation,
            RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            var map = ritualTarget.Map ?? Find.CurrentMap;
            if (map == null) return null;
            var attendees = assignments.Participants.Count;
            var total = map.mapPawns.FreeColonistsSpawnedCount;
            var ratio = total > 0 ? Mathf.Clamp01((float)attendees / total) : 0f;
            var contribution = ratio * 0.4f;
            return new QualityFactor
            {
                label = "ROCKET_OutcomeLabel_Attendance".Translate(),
                count = $"{attendees} / {total}",
                qualityChange = $"+{contribution.ToStringPercent()}",
                quality = contribution,
                positive = ratio >= 0.25f,
                present = true,
                toolTip = "ROCKET_OutcomeAttendance_Tooltip".Translate(),
                priority = 50f
            };
        }
    }

    public class RitualOutcomeComp_PromotionRoom : RitualOutcomeComp
    {
        public override bool Applies(LordJob_Ritual ritual) => true;
        public override bool DataRequired => false;

        public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            var impressiveness = CeremonyQuality.GetRoomImpressiveness(ritual.selectedTarget);
            var roomScore = Mathf.Clamp01(impressiveness / 170f);
            return roomScore * 0.4f;
        }

        public override string GetDesc(LordJob_Ritual ritual = null, RitualOutcomeComp_Data data = null) =>
            "ROCKET_OutcomeDesc_Venue".Translate();

        public override QualityFactor GetQualityFactor(
            Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation,
            RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            var impressiveness = CeremonyQuality.GetRoomImpressiveness(ritualTarget);
            var roomScore = Mathf.Clamp01(impressiveness / 170f);
            var contribution = roomScore * 0.4f;
            var countLabel = impressiveness <= 0f
                ? "ROCKET_OutcomeVenue_Outdoors".Translate().ToString()
                : $"{impressiveness:F0}";
            return new QualityFactor
            {
                label = "ROCKET_OutcomeLabel_Venue".Translate(),
                count = countLabel,
                qualityChange = $"+{contribution.ToStringPercent()}",
                quality = contribution,
                positive = impressiveness >= 25f,
                present = true,
                toolTip = "ROCKET_OutcomeVenue_Tooltip".Translate(),
                priority = 40f
            };
        }
    }

    public class RitualOutcomeComp_PromotionCitation : RitualOutcomeComp
    {
        public override bool Applies(LordJob_Ritual ritual) =>
            PromotionCeremonyRegistry.Get(ritual?.Ritual) != null;

        public override bool DataRequired => false;

        public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            var state = PromotionCeremonyRegistry.Get(ritual?.Ritual);
            return state != null && !string.IsNullOrEmpty(state.citation) ? 0.2f : 0f;
        }

        public override string GetDesc(LordJob_Ritual ritual = null, RitualOutcomeComp_Data data = null) =>
            "ROCKET_OutcomeDesc_Citation".Translate();

        public override QualityFactor GetQualityFactor(
            Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation,
            RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            var state = PromotionCeremonyRegistry.Get(ritual);
            if (state == null) return null;
            var hasCitation = !string.IsNullOrEmpty(state.citation);
            return new QualityFactor
            {
                label = "ROCKET_OutcomeLabel_Citation".Translate(),
                count = hasCitation
                    ? "ROCKET_OutcomeCitation_Written".Translate().ToString()
                    : "ROCKET_OutcomeCitation_None".Translate().ToString(),
                qualityChange = hasCitation ? "+20%" : "+0%",
                quality = hasCitation ? 0.2f : 0f,
                positive = hasCitation,
                present = true,
                toolTip = hasCitation
                    ? $"\"{state.citation}\"\n\n{"ROCKET_OutcomeCitation_TooltipQualityNote".Translate()}"
                    : "ROCKET_OutcomeCitation_TooltipNone".Translate().ToString(),
                priority = 30f
            };
        }
    }

    public class JobGiver_PromotionSpeech : JobGiver_GiveSpeechFacingTarget
    {
        private static readonly AccessTools.FieldRef<InteractionDef, Texture2D> SymbolTexRef =
            AccessTools.FieldRefAccess<InteractionDef, Texture2D>("symbolTex");

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.GetLord()?.LordJob is not LordJob_Ritual lordJob) return null;
            var awardee = lordJob.assignments.FirstAssignedPawn("awardee");
            if (awardee is not { Spawned: true }) return null;

            var job = JobMaker.MakeJob(JobDefOf.GiveSpeech, (LocalTargetInfo)pawn.Position, (LocalTargetInfo)awardee);
            job.showSpeechBubbles = true;
            job.speechFaceSpectatorsIfPossible = faceSpectatorsIfPossible;
            var interactDef = DefDatabase<InteractionDef>.GetNamedSilentFail("ROCKET_Speech_Promote");
            if (interactDef != null)
            {
                var state = PromotionCeremonyRegistry.Get(lordJob.Ritual);
                var rankIcon = state?.toRank?.Icon;
                if (rankIcon != null)
                    SymbolTexRef(interactDef) = rankIcon;
            }
            job.interaction = interactDef;
            job.speechSoundMale = soundDefMale ?? SoundDefOf.Speech_Leader_Male;
            job.speechSoundFemale = soundDefFemale ?? SoundDefOf.Speech_Leader_Female;
            return job;
        }
    }

    [HarmonyPatch(typeof(Dialog_BeginLordJob), nameof(Dialog_BeginLordJob.DoLeftColumn))]
    public static class Patch_DrawPromotionIconInRitual
    {
        public static void Prefix(Dialog_BeginLordJob __instance, ref RectDivider layout)
        {
            if (__instance is not Dialog_BeginRitual ritualDialog) return;
            var preceptRitual = Traverse.Create(ritualDialog).Field("ritual").GetValue<Precept_Ritual>();
            if (preceptRitual == null) return;
            var state = PromotionCeremonyRegistry.Get(preceptRitual);
            if (state?.toRank?.Icon == null) return;

            var row = layout.NewRow(60f, marginOverride: 6f);
            var iconRect = new Rect(row.Rect.x, row.Rect.y, 50f, 50f);
            GUI.DrawTexture(iconRect, state.toRank.Icon);

            var labelRect = new Rect(iconRect.xMax + 10f, row.Rect.y, row.Rect.width - 60f, 50f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;
            Widgets.Label(labelRect, state.toRank.RankLabel);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }

    [HarmonyPatch]
    public static class Patch_PromotionSpeechGrammar
    {
        private static readonly AccessTools.FieldRef<PlayLogEntry_InteractionWithMany, InteractionDef> IntDefRef =
            AccessTools.FieldRefAccess<PlayLogEntry_InteractionWithMany, InteractionDef>("intDef");
        private static readonly AccessTools.FieldRef<PlayLogEntry_InteractionWithMany, Pawn> InitiatorRef =
            AccessTools.FieldRefAccess<PlayLogEntry_InteractionWithMany, Pawn>("initiator");

        private static InteractionDef _promoteSpeechDef;

        public static System.Reflection.MethodBase TargetMethod() =>
            AccessTools.Method(typeof(PlayLogEntry_InteractionWithMany), "GenerateGrammarRequest");

        public static void Postfix(LogEntry __instance, ref GrammarRequest __result)
        {
            if (__instance is not PlayLogEntry_InteractionWithMany manyLog) return;
            var intDef = IntDefRef(manyLog);
            if (intDef == null) return;
            var promoteDef = _promoteSpeechDef ??= DefDatabase<InteractionDef>.GetNamedSilentFail("ROCKET_Speech_Promote");
            if (promoteDef == null || intDef != promoteDef) return;

            var initiator = InitiatorRef(manyLog);
            var lordJob = initiator?.GetLord()?.LordJob as LordJob_Ritual;
            var awardee = lordJob?.assignments.FirstAssignedPawn("awardee");
            if (awardee == null) return;
            __result.Rules.AddRange(GrammarUtility.RulesForPawn("RECIPIENT", awardee, __result.Constants));
        }
    }
}
