using Verse;

namespace RocketsRanks
{
    public static class RankUtility
    {
        /// <summary>
        /// Check if a pawn is allowed to wear rank-restricted apparel.
        /// Returns true if the apparel has no rank restriction, or if the pawn holds the required rank.
        /// </summary>
        public static bool PawnMeetsRankRequirement(Pawn pawn, ThingDef apparelDef)
        {
            if (apparelDef == null) return true;
            var ext = apparelDef.GetModExtension<RankExtension>();
            if (ext?.requiredRank == null) return true;

            return pawn?.GetComp<CompRank>()?.HasRank(ext.requiredRank) ?? false;
        }

        /// <summary>
        /// Returns the RankExtension on an apparel def, or null if it has none.
        /// </summary>
        public static RankExtension GetRankExtension(ThingDef def)
        {
            return def?.GetModExtension<RankExtension>();
        }
    }
}
