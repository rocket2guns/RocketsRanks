using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RocketsRanks
{
    public static class RankUtility
    {
        private static readonly Dictionary<ThingDef, RankExtension> ExtCache = new();

        /// <summary>
        /// Recipe → pack of the rank apparel it produces, or null when the recipe
        /// doesn't produce any rank apparel. Cached because product/extension
        /// links never change after def load.
        /// </summary>
        private static readonly Dictionary<RecipeDef, RankPackDef> RecipePackCache = new();

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

        /// <summary>
        /// Is this pack currently hidden in mod settings?
        /// </summary>
        public static bool IsPackHidden(RankPackDef pack)
        {
            if (pack == null) return false;
            var hidden = RanksMod.Settings?.HiddenPacks;
            if (hidden == null || hidden.Count == 0) return false;
            return hidden.Contains(pack.defName);
        }

        public static bool IsPackHidden(RankDef rank)
        {
            if (rank == null) return false;
            return IsPackHidden(rank.Pack);
        }

        /// <summary>
        /// True when a recipe produces rank apparel whose pack is currently hidden.
        /// Hot path: called from RecipeDef.AvailableNow patch every frame the
        /// bill UI is open. Fast-exits when nothing is hidden.
        /// </summary>
        public static bool IsRecipeHidden(RecipeDef recipe)
        {
            if (recipe == null) return false;
            var hidden = RanksMod.Settings?.HiddenPacks;
            if (hidden == null || hidden.Count == 0) return false;

            if (!RecipePackCache.TryGetValue(recipe, out var pack))
            {
                pack = ResolveRecipePack(recipe);
                RecipePackCache[recipe] = pack;
            }
            return pack != null && hidden.Contains(pack.defName);
        }

        private static RankPackDef ResolveRecipePack(RecipeDef recipe)
        {
            if (recipe.products == null) return null;
            foreach (var p in recipe.products)
            {
                var ext = GetRankExtension(p.thingDef);
                if (ext?.requiredRank != null) return ext.requiredRank.Pack;
            }
            return null;
        }
    }
}
