using Verse;

namespace RocketsRanks
{
    /// <summary>
    /// Attach to any ThingDef (apparel) to restrict it to pawns holding a specific rank.
    /// The pawn must have exactly this rank assigned to equip the item.
    /// </summary>
    public class RankExtension : DefModExtension
    {
        public RankDef requiredRank;
    }
}
