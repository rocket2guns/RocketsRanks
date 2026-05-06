using System.Collections.Generic;
using System.Globalization;
using RimWorld;
using UnityEngine;
using Verse;

namespace RocketsRanks
{
    public class RankDef : Def
    {
        /// <summary>
        /// Integer level for sorting/display. Not used for hierarchy enforcement
        /// since promotion is freeform, but useful for ordering the rank list
        /// and for the apparel requirement check (exact match on defName).
        /// </summary>
        public int rankLevel = 0;

        /// <summary>
        /// Texture path for the rank icon, relative to Textures/.
        /// e.g. "UI/Rank_Corporal"
        /// </summary>
        public string iconPath;

        /// <summary>
        /// Pack this rank belongs to. Required — validated in ConfigErrors so
        /// every rank surfaces in exactly one promote-menu tab.
        /// </summary>
        public RankPackDef pack;

        /// <summary>
        /// Multiplier applied to the map icon size when this rank is drawn
        /// next to a pawn's floating name label.
        /// </summary>
        public float mapScale = 1f;

        public RankPackDef Pack => pack;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var err in base.ConfigErrors())
                yield return err;
            if (pack == null)
                yield return $"RankDef {defName} is missing required <pack>.";
        }

        [Unsaved] private Texture2D cachedIcon;
        [Unsaved] private string cachedRankLabel;

        public string RankLabel =>
            cachedRankLabel ??= CultureInfo.CurrentCulture.TextInfo.ToTitleCase(LabelCap);

        public Texture2D Icon
        {
            get
            {
                if (cachedIcon != null) return cachedIcon;
                if (!iconPath.NullOrEmpty())
                    cachedIcon = ContentFinder<Texture2D>.Get(iconPath, false);
                return cachedIcon;
            }
        }
    }
}
