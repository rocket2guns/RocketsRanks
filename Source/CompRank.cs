using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RocketsRanks
{
    public class PromotionRecord : IExposable
    {
        public RankDef rank;
        public RankDef previousRank;
        public string citation;
        public int tick = -1;
        public Pawn presentedBy;
        public int ceremonyQuality = -1;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref rank, "rank");
            Scribe_Defs.Look(ref previousRank, "previousRank");
            Scribe_Values.Look(ref citation, "citation");
            Scribe_Values.Look(ref tick, "tick", -1);
            Scribe_References.Look(ref presentedBy, "presentedBy");
            Scribe_Values.Look(ref ceremonyQuality, "ceremonyQuality", -1);
        }
    }

    public class CompRank : ThingComp
    {
        public RankDef currentRank;
        public List<PromotionRecord> history = new();
        private Command_Action cachedGizmo;

        public Command_Action GetGizmo()
        {
            if (cachedGizmo == null)
            {
                var pawn = (Pawn)parent;
                cachedGizmo = new Command_Action
                {
                    defaultDesc = "ROCKET_PromoteDesc".Translate(),
                    action = () => Find.WindowStack.Add(new Dialog_Promote(pawn))
                };
            }
            cachedGizmo.defaultLabel = currentRank?.RankLabel
                ?? "ROCKET_Promote".Translate().ToString();
            cachedGizmo.icon = currentRank?.Icon ?? RankTextures.PromoteIcon;
            return cachedGizmo;
        }

        public override void PostExposeData()
        {
            Scribe_Defs.Look(ref currentRank, "currentRank");
            Scribe_Collections.Look(ref history, "history", LookMode.Deep);
            history ??= new List<PromotionRecord>();
        }

        public void SetRank(RankDef newRank, string citation = null, Pawn presentedBy = null, int ceremonyQuality = -1)
        {
            var pawn = (Pawn)parent;
            var previousRank = currentRank;
            currentRank = newRank;

            if (previousRank == newRank) return;

            history.Add(new PromotionRecord
            {
                rank = newRank,
                previousRank = previousRank,
                citation = citation.NullOrEmpty() ? null : citation.Trim(),
                tick = Find.TickManager.TicksGame,
                presentedBy = presentedBy,
                ceremonyQuality = ceremonyQuality
            });

            RankApparelRefresh.RefreshApparelFor(pawn);
        }

        public bool HasRank(RankDef rank)
        {
            if (rank == null || currentRank == null) return false;
            return currentRank.defNameHash == rank.defNameHash;
        }
    }
}