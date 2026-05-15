using System.Collections.Generic;
using RimWorld;
using UnityEngine;
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
        /// <summary>
        /// Optional. Rank whose icon represents this pack in the settings UI.
        /// When null, falls back to the lowest-level rank in the pack that has an icon.
        /// </summary>
        public RankDef previewRank;

        /// <summary>
        /// True if pawns in this pack are commissioned officers. Read by
        /// InteractionWorker_Salute to decide who salutes whom.
        /// </summary>
        public bool isOfficerPack = false;

        [Unsaved] private Texture2D cachedPreviewIcon;
        [Unsaved] private bool previewIconResolved;

        public Texture2D PreviewIcon
        {
            get
            {
                if (previewIconResolved) return cachedPreviewIcon;
                previewIconResolved = true;

                if (previewRank != null)
                {
                    cachedPreviewIcon = previewRank.Icon;
                    if (cachedPreviewIcon != null) return cachedPreviewIcon;
                }

                var packRanks = new List<RankDef>();
                var defs = DefDatabase<RankDef>.AllDefsListForReading;
                for (var i = 0; i < defs.Count; i++)
                {
                    var r = defs[i];
                    if (r.Pack != this) continue;
                    if (r.Icon == null) continue;
                    packRanks.Add(r);
                }
                packRanks.Sort((a, b) => a.rankLevel.CompareTo(b.rankLevel));

                if (packRanks.Count > 0)
                    cachedPreviewIcon = packRanks[packRanks.Count / 2].Icon;
                return cachedPreviewIcon;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var err in base.ConfigErrors())
                yield return err;
            if (previewRank != null && previewRank.Pack != this)
                yield return $"RankPackDef {defName} previewRank '{previewRank.defName}' belongs to pack '{previewRank.Pack?.defName ?? "<none>"}'.";
        }
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
