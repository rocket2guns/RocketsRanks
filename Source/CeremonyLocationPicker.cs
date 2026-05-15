using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RocketsRanks
{
    public static class CeremonyLocationPicker
    {
        public static bool Pick(Pawn awardee, out TargetInfo target, out string locationLabel)
        {
            target = default;
            locationLabel = null;

            var map = awardee?.Map ?? awardee?.MapHeld;
            if (map == null) return false;

            if (ModsConfig.RoyaltyActive)
            {
                foreach (var t in map.listerThings.AllThings)
                {
                    if (t.def?.building == null || !t.def.building.isSittable) continue;
                    if (t.Faction != Faction.OfPlayer) continue;
                    var room = t.GetRoom();
                    if (room == null || room.PsychologicallyOutdoors) continue;
                    if (room.Role != RoomRoleDefOf.ThroneRoom) continue;
                    if (!awardee.CanReach(t, PathEndMode.Touch, Danger.Deadly)) continue;
                    target = new TargetInfo(t);
                    locationLabel = "the throne room";
                    return true;
                }
            }

            var lecternDef = ThingDefOf.Lectern;
            if (lecternDef != null)
            {
                var lecterns = map.listerThings.ThingsOfDef(lecternDef);
                for (var i = 0; i < lecterns.Count; i++)
                {
                    var l = lecterns[i];
                    var room = l.GetRoom();
                    if (room == null || room.PsychologicallyOutdoors) continue;
                    if (!awardee.CanReach(l, PathEndMode.Touch, Danger.Deadly)) continue;
                    target = new TargetInfo(l);
                    locationLabel = "the lectern";
                    return true;
                }
            }

            target = new TargetInfo(awardee);
            locationLabel = "wherever the awardee is standing";
            return true;
        }
    }
}
