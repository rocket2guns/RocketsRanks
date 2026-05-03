using System.Collections.Generic;
using Verse;

namespace RocketsRanks
{
    public static class RankUtility
    {
        private static readonly Dictionary<ThingDef, RankExtension> ExtCache = new();

        /// <summary>
        /// Check if a pawn is allowed to wear rank-restricted apparel.
        /// Returns true if the apparel has no rank restriction, or if the pawn holds the required rank.
        /// </summary>
        public static bool PawnMeetsRankRequirement(Pawn pawn, ThingDef apparelDef)
        {
            if (apparelDef == null) return true;
            var ext = GetRankExtension(apparelDef);
            if (ext?.requiredRank == null) return true;

            return pawn?.GetComp<CompRank>()?.HasRank(ext.requiredRank) ?? false;
        }

        /// <summary>
        /// Returns the RankExtension on an apparel def, or null if it has none.
        /// Cached per ThingDef since modExtensions never change after load.
        /// </summary>
        public static RankExtension GetRankExtension(ThingDef def)
        {
            if (def == null) return null;
            if (ExtCache.TryGetValue(def, out var cached)) return cached;
            var ext = def.GetModExtension<RankExtension>();
            ExtCache[def] = ext;
            return ext;
        }
    }
}
