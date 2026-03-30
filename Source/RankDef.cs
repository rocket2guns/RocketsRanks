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

        [Unsaved] private Texture2D cachedIcon;
        
        public string RankLabel => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(LabelCap);

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
