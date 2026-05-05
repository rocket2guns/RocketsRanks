using RimWorld;
using Verse;

namespace RocketsRanks
{
    /// <summary>
    /// A pack groups RankDefs (and their associated apparel) so the player can
    /// hide a whole bundle from UI surfaces without removing the underlying defs.
    /// Other mods can ship a RankPackDef of their own to register a new pack.
    /// </summary>
    public class RankPackDef : Def
    {
    }

    [DefOf]
    public static class RankPackDefOf
    {
        public static RankPackDef CoreArmy;
        public static RankPackDef CoreOfficers;

        static RankPackDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RankPackDefOf));
        }
    }
}
