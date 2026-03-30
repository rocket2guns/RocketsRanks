using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RocketsRanks
{
    public enum RankBodyType
    {
        Male = 0,
        Female = 1,
        Thin = 2,
        Fat = 3,
        Hulk = 4,
        Count = 5
    }

    // ── XML DefModExtension for apparel render positioning ──

    public class RankRenderBodyEntry
    {
        public string bodyType;
        public float northOffsetX;
        public float northOffsetZ;
        public float northScale = 1f;
        public float eastOffsetX;
        public float eastOffsetZ;
        public float eastScale = 1f;
    }

    /// <summary>
    /// DefModExtension holding per-body-type render positioning.
    /// Place on the abstract base ThingDef for rank apparel.
    /// Children use &lt;modExtensions Inherit="True"&gt; to keep this
    /// while adding their own RankExtension.
    /// </summary>
    public class RankRenderExtension : DefModExtension
    {
        public List<RankRenderBodyEntry> bodyTypes;
    }

    // ── Mod settings (debug override only) ──

    public class RankDirectionSettings : IExposable
    {
        public float offsetX;
        public float offsetZ;
        public float scale = 1f;

        public void ExposeData()
        {
            Scribe_Values.Look(ref offsetX, "offsetX");
            Scribe_Values.Look(ref offsetZ, "offsetZ");
            Scribe_Values.Look(ref scale, "scale", 1f);
        }
    }

    public class RankBodyTypeSettings : IExposable
    {
        public RankDirectionSettings north = new();
        public RankDirectionSettings east = new();

        public void ExposeData()
        {
            Scribe_Deep.Look(ref north, "north");
            Scribe_Deep.Look(ref east, "east");
            north ??= new RankDirectionSettings();
            east ??= new RankDirectionSettings();
        }
    }

    // ── Resolution logic ──

    public static class RankRenderSettings
    {
        private static Dictionary<string, RankBodyType> _bodyTypeMap;

        public static readonly string[] BodyTypeLabels =
        {
            "Male", "Female", "Thin", "Fat", "Hulk"
        };

        private static Dictionary<string, RankBodyType> BodyTypeMap
        {
            get
            {
                if (_bodyTypeMap == null)
                {
                    _bodyTypeMap = new Dictionary<string, RankBodyType>(5)
                    {
                        ["Male"] = RankBodyType.Male,
                        ["Female"] = RankBodyType.Female,
                        ["Thin"] = RankBodyType.Thin,
                        ["Fat"] = RankBodyType.Fat,
                        ["Hulk"] = RankBodyType.Hulk
                    };
                }
                return _bodyTypeMap;
            }
        }

        /// <summary>
        /// Gets offset and scale for a given apparel def, body type, and facing.
        /// Uses mod settings if debug is enabled, otherwise reads from the
        /// RankRenderExtension on the apparel def.
        /// </summary>
        public static void GetOffsetAndScale(ThingDef apparelDef, BodyTypeDef bodyType, Rot4 facing,
            out float offsetX, out float offsetZ, out float scale)
        {
            offsetX = 0f;
            offsetZ = 0f;
            scale = 1f;

            if (bodyType == null) return;

            var isNorthSouth = facing.AsInt == 0 || facing.AsInt == 2;

            // If debug settings enabled, use mod settings
            if (RanksMod.Settings.ShowBodyTypeDebug)
            {
                var arr = RanksMod.Settings?.BodySettings;
                if (arr != null && BodyTypeMap.TryGetValue(bodyType.defName, out var idx))
                {
                    var bs = arr[(int)idx];
                    var ds = isNorthSouth ? bs?.north : bs?.east;
                    if (ds != null)
                    {
                        offsetX = ds.offsetX;
                        offsetZ = ds.offsetZ;
                        scale = ds.scale;
                        return;
                    }
                }
            }

            // Otherwise use the DefModExtension on the apparel
            var renderExt = apparelDef?.GetModExtension<RankRenderExtension>();
            if (renderExt?.bodyTypes == null) return;

            var bodyName = bodyType.defName;
            foreach (var entry in renderExt.bodyTypes)
            {
                if (entry.bodyType != bodyName) continue;
                if (isNorthSouth)
                {
                    offsetX = entry.northOffsetX;
                    offsetZ = entry.northOffsetZ;
                    scale = entry.northScale;
                }
                else
                {
                    offsetX = entry.eastOffsetX;
                    offsetZ = entry.eastOffsetZ;
                    scale = entry.eastScale;
                }
                return;
            }
        }

        public static void EnsureDefaults(RankBodyTypeSettings[] arr)
        {
            for (var i = 0; i < (int)RankBodyType.Count; i++)
                arr[i] ??= new RankBodyTypeSettings();
        }
    }
}