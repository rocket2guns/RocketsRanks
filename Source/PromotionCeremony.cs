using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace RocketsRanks
{
    public class PromotionCeremonyState
    {
        public Pawn awardee;
        public RankDef toRank;
        public string citation;
        public bool prompted;
    }

    public static class PromotionCeremonyRegistry
    {
        private static readonly ConditionalWeakTable<Precept_Ritual, PromotionCeremonyState> states = new();

        public static void Register(Precept_Ritual ritual, PromotionCeremonyState state)
        {
            if (ritual == null || state == null) return;
            states.Remove(ritual);
            states.Add(ritual, state);
        }

        public static PromotionCeremonyState Get(Precept_Ritual ritual)
        {
            if (ritual == null) return null;
            return states.TryGetValue(ritual, out var s) ? s : null;
        }

        public static void Clear(Precept_Ritual ritual)
        {
            if (ritual == null) return;
            states.Remove(ritual);
        }
    }

    public static class PromotionCeremony
    {
        public static bool CanStart(Pawn awardee, out string reason)
        {
            reason = null;
            if (!ModsConfig.IdeologyActive)
            {
                reason = "ROCKET_CeremonyRequiresIdeology".Translate();
                return false;
            }
            if (awardee?.Map == null && awardee?.MapHeld == null)
            {
                reason = "ROCKET_CeremonyAwardeeNeedsMap".Translate();
                return false;
            }
            if (!ColonyHasOtherFreeColonist(awardee))
            {
                reason = "ROCKET_CeremonyNeedsOtherColonist".Translate();
                return false;
            }
            return true;
        }

        public static void Start(Pawn awardee, RankDef toRank, string citation)
        {
            if (!CanStart(awardee, out var why))
            {
                Messages.Message(why, awardee, MessageTypeDefOf.RejectInput, false);
                return;
            }

            var currentRank = awardee.GetComp<CompRank>()?.currentRank;
            if (currentRank != null && (toRank == null || toRank.rankLevel < currentRank.rankLevel))
            {
                Log.Warning("[RocketsRanks] Refusing to start promotion ceremony for a demotion.");
                return;
            }

            var pattern = DefDatabase<RitualPatternDef>.GetNamedSilentFail("ROCKET_PromotionPattern");
            var dummyPreceptDef = DefDatabase<PreceptDef>.GetNamedSilentFail("ROCKET_PromotionCeremonyPrecept");
            if (pattern == null || dummyPreceptDef == null)
            {
                Log.Error("[RocketsRanks] Missing ritual defs ROCKET_PromotionPattern / ROCKET_PromotionCeremonyPrecept.");
                return;
            }

            var safeMap = awardee.Map ?? awardee.MapHeld;
            if (safeMap == null) return;

            if (!CeremonyLocationPicker.Pick(awardee, out var ritualTarget, out var locationLabel))
            {
                Messages.Message("ROCKET_CeremonyLocationPickFailed".Translate(), awardee, MessageTypeDefOf.RejectInput, false);
                return;
            }

            var fakeRitual = (Precept_Ritual)PreceptMaker.MakePrecept(dummyPreceptDef);
            fakeRitual.ideo = awardee.Ideo;
            fakeRitual.sourcePattern = pattern;
            var ceremonyName = "ROCKET_CeremonyName".Translate().ToString();
            fakeRitual.SetName(ceremonyName);
            fakeRitual.behavior = pattern.ritualBehavior.GetInstance();
            fakeRitual.behavior.def = pattern.ritualBehavior;
            fakeRitual.outcomeEffect = pattern.ritualOutcomeEffect.GetInstance();
            fakeRitual.outcomeEffect.def = pattern.ritualOutcomeEffect;
            fakeRitual.outcomeEffect.compDatas ??= new();

            PromotionCeremonyRegistry.Register(fakeRitual, new PromotionCeremonyState
            {
                awardee = awardee,
                toRank = toRank,
                citation = string.IsNullOrEmpty(citation) ? null : citation.Trim()
            });

            var preferredPresenter = PickPreferredPresenter(awardee);

            Dialog_BeginRitual.ActionCallback startAction = delegate (RitualRoleAssignments assignments)
            {
                var lordJob = new LordJob_Ritual(
                    selectedTarget: ritualTarget,
                    ritual: fakeRitual,
                    obligation: null,
                    allStages: pattern.ritualBehavior.stages,
                    assignments: assignments,
                    organizer: null,
                    spotOverride: null);
                LordMaker.MakeNewLord(Faction.OfPlayer, lordJob, safeMap, assignments.Participants);
                return true;
            };

            var extraInfo = new List<string>
            {
                "ROCKET_CeremonyLocationLine".Translate(locationLabel)
            };

            // Lock the awardee role to the source pawn so the player cannot
            // swap who gets promoted from the ritual dialog.
            var forcedRoles = new Dictionary<string, Pawn> { { "awardee", awardee } };

            Find.WindowStack.Add(new Dialog_BeginRitual(
                ritualLabel: ceremonyName,
                ritual: fakeRitual,
                target: ritualTarget,
                map: safeMap,
                action: startAction,
                organizer: null,
                obligation: null,
                filter: null,
                okButtonText: "ROCKET_BeginCeremonyButton".Translate(),
                requiredPawns: null,
                forcedForRole: forcedRoles,
                outcome: pattern.ritualOutcomeEffect,
                extraInfoText: extraInfo,
                selectedPawn: preferredPresenter
            ));
        }

        private static bool ColonyHasOtherFreeColonist(Pawn awardee)
        {
            var map = awardee?.Map ?? awardee?.MapHeld;
            if (map == null) return false;
            foreach (var p in map.mapPawns.FreeColonistsSpawned)
            {
                if (p != awardee) return true;
            }
            return false;
        }

        private static Pawn PickPreferredPresenter(Pawn awardee)
        {
            var map = awardee.Map ?? awardee.MapHeld;
            if (map == null) return null;
            Pawn leader = null;
            Pawn bestRanked = null;
            var bestRankLevel = -1;
            var awardeeRankLevel = awardee.GetComp<CompRank>()?.currentRank?.rankLevel ?? -1;

            foreach (var p in map.mapPawns.FreeColonistsSpawned)
            {
                if (p == awardee) continue;
                if (leader == null && p.Ideo?.GetRole(p)?.def == PreceptDefOf.IdeoRole_Leader)
                    leader = p;
                var lvl = p.GetComp<CompRank>()?.currentRank?.rankLevel ?? -1;
                if (lvl > awardeeRankLevel && lvl > bestRankLevel)
                {
                    bestRankLevel = lvl;
                    bestRanked = p;
                }
            }

            if (leader != null) return leader;
            if (bestRanked != null) return bestRanked;
            return map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p => p != awardee);
        }
    }

    public class RitualRole_Promoter : RitualRoleColonist
    {
        public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget,
            LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null,
            Precept_Ritual precept = null, bool skipReason = false)
        {
            if (!base.AppliesToPawn(p, out reason, selectedTarget, ritual, assignments, precept, skipReason))
                return false;

            var awardee = assignments?.FirstAssignedPawn("awardee")
                          ?? PromotionCeremonyRegistry.Get(precept)?.awardee;
            if (awardee != null && p == awardee)
            {
                if (!skipReason) reason = "The awardee cannot present their own promotion.";
                return false;
            }

            reason = null;
            return true;
        }
    }
}
