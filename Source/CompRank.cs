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

        public void ExposeData()
        {
            Scribe_Defs.Look(ref rank, "rank");
            Scribe_Defs.Look(ref previousRank, "previousRank");
            Scribe_Values.Look(ref citation, "citation");
            Scribe_Values.Look(ref tick, "tick", -1);
        }
    }

    public class CompRank : ThingComp
    {
        public RankDef currentRank;
        public List<PromotionRecord> history = new();

        public override void PostExposeData()
        {
            Scribe_Defs.Look(ref currentRank, "currentRank");
            Scribe_Collections.Look(ref history, "history", LookMode.Deep);
            history ??= new List<PromotionRecord>();
        }

        public void SetRank(RankDef newRank, string citation = null)
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
                tick = Find.TickManager.TicksGame
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