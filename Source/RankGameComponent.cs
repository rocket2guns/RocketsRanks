using System.Collections.Generic;
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

    public class PawnRankData : IExposable
    {
        public RankDef currentRank;
        public List<PromotionRecord> history = new();

        public void ExposeData()
        {
            Scribe_Defs.Look(ref currentRank, "currentRank");
            Scribe_Collections.Look(ref history, "history", LookMode.Deep);
            history ??= new List<PromotionRecord>();
        }
    }

    public class RankGameComponent : GameComponent
    {
        private Dictionary<Pawn, PawnRankData> rankData = new();

        // Scribe helper lists
        private List<Pawn> pawnKeys = new();
        private List<PawnRankData> dataValues = new();

        public RankGameComponent(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref rankData, "rankData",
                LookMode.Reference, LookMode.Deep,
                ref pawnKeys, ref dataValues);
            rankData ??= new Dictionary<Pawn, PawnRankData>();
        }

        public static RankGameComponent Instance =>
            Verse.Current.Game?.GetComponent<RankGameComponent>();

        /// <summary>
        /// Get the rank data for a pawn, or null if the pawn has never been ranked.
        /// </summary>
        public PawnRankData GetData(Pawn pawn)
        {
            return pawn != null && rankData.TryGetValue(pawn, out var data) ? data : null;
        }

        /// <summary>
        /// Get the current RankDef for a pawn, or null if unranked.
        /// </summary>
        public RankDef GetRank(Pawn pawn)
        {
            return GetData(pawn)?.currentRank;
        }

        /// <summary>
        /// Set a pawn's rank, recording a promotion history entry.
        /// Pass null for newRank to strip rank entirely.
        /// </summary>
        public void SetRank(Pawn pawn, RankDef newRank, string citation = null)
        {
            if (pawn == null) return;

            if (!rankData.TryGetValue(pawn, out var data))
            {
                data = new PawnRankData();
                rankData[pawn] = data;
            }

            var previousRank = data.currentRank;
            data.currentRank = newRank;

            // Only record history if something actually changed
            if (previousRank != newRank)
            {
                data.history.Add(new PromotionRecord
                {
                    rank = newRank,
                    previousRank = previousRank,
                    citation = citation.NullOrEmpty() ? null : citation.Trim(),
                    tick = Find.TickManager.TicksGame
                });

                // Force apparel re-evaluation so rank gear swaps immediately
                RankApparelRefresh.RefreshApparelFor(pawn);
            }
        }

        /// <summary>
        /// Check if a pawn holds exactly the given rank.
        /// </summary>
        public bool HasRank(Pawn pawn, RankDef rank)
        {
            if (rank == null || pawn == null) return false;
            return GetRank(pawn) == rank;
        }
    }
}
