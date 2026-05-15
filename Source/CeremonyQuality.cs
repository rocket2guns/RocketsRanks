using RimWorld;
using UnityEngine;
using Verse;

namespace RocketsRanks
{
    public static class CeremonyQuality
    {
        private const float ATTENDANCE_WEIGHT = 0.4f;
        private const float ROOM_WEIGHT = 0.4f;
        private const float CITATION_WEIGHT = 0.2f;

        public static float GetQualityScore(int attendees, int totalColonists, float roomImpressiveness, bool hasCitation)
        {
            var attendanceRatio = totalColonists > 0
                ? Mathf.Clamp01((float)attendees / totalColonists)
                : 0f;
            var roomScore = Mathf.Clamp01(roomImpressiveness / 170f);
            var citationScore = hasCitation ? 1f : 0f;
            return (attendanceRatio * ATTENDANCE_WEIGHT)
                 + (roomScore * ROOM_WEIGHT)
                 + (citationScore * CITATION_WEIGHT);
        }

        public static int GetStageIndex(float qualityScore)
        {
            if (qualityScore >= 0.8f) return 3;
            if (qualityScore >= 0.5f) return 2;
            if (qualityScore >= 0.25f) return 1;
            return 0;
        }

        public static string GetQualityLabel(int stageIndex) =>
            stageIndex switch
            {
                3 => "ROCKET_CeremonyQuality_Legendary".Translate(),
                2 => "ROCKET_CeremonyQuality_Grand".Translate(),
                1 => "ROCKET_CeremonyQuality_Decent".Translate(),
                _ => "ROCKET_CeremonyQuality_Poor".Translate()
            };

        public static float GetRoomImpressiveness(TargetInfo target)
        {
            if (!target.HasThing && !target.Cell.IsValid) return 0f;
            var map = target.Map;
            if (map == null) return 0f;
            var room = target.Cell.GetRoom(map);
            if (room == null || room.PsychologicallyOutdoors) return 0f;
            return room.GetStat(RoomStatDefOf.Impressiveness);
        }
    }
}
