using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RocketsRanks
{
    public class InteractionWorker_Salute : InteractionWorker
    {
        private const float SelectionWeight = 2f;

        private static readonly AccessTools.FieldRef<InteractionDef, Texture2D> SymbolTexRef =
            AccessTools.FieldRefAccess<InteractionDef, Texture2D>("symbolTex");

        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            if (!RanksMod.Settings.EnableSalutes) return 0f;

            var recvRank = recipient.GetComp<CompRank>()?.currentRank;
            if (recvRank == null || recvRank.Pack?.isOfficerPack != true) return 0f;

            var initRank = initiator.GetComp<CompRank>()?.currentRank;
            if (initRank == null) return 0f;

            if (initRank.Pack?.isOfficerPack == true)
            {
                if (!RanksMod.Settings.OfficersSaluteSeniors) return 0f;
                if (initRank.rankLevel >= recvRank.rankLevel) return 0f;
            }

            return SelectionWeight;
        }

        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks,
            out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            var rankIcon = recipient.GetComp<CompRank>()?.currentRank?.Icon;
            if (rankIcon != null) SymbolTexRef(interaction) = rankIcon;

            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;
        }
    }
}
