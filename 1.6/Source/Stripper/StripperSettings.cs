using UnityEngine;
using Verse;

namespace Stripper
{
    public class StripperSettings : ModSettings
    {
        // 設定値の変数
        public float baseDancePrice = 20f;
        public float danceBeautyMultiplier = 1.0f;
        public float baseProstitutionPrice = 50f;
        public float prostitutionBeautyMultiplier = 1.0f;
        public float partMultiplier = 1.0f;
        public bool spawnSilverForDance = false;
        public bool spawnSilverForProstitution = false;
        public bool debugLog = false;

        public float prostitutePartsVaginal = 1.0f;
        public float prostitutePartsAnal = 1.0f;
        public float prostitutePartsOral = 1.0f;
        public float prostitutePartsDoublePenetration = 1.0f;
        public float prostitutePartsBoobjob = 1.0f;
        public float prostitutePartsHandjob = 1.0f;
        public float prostitutePartsFootjob = 1.0f;
        public float prostitutePartsFingering = 1.0f;
        public float prostitutePartsScissoring = 1.0f;
        public float prostitutePartsFisting = 1.0f;
        public float prostitutePartsRimming = 1.0f;
        public float prostitutePartsFellatio = 1.0f;
        public float prostitutePartsCunnilingus = 1.0f;
        public float prostitutePartsSixtynine = 1.0f;

        public int inviteCooldownSeconds = 21600;
        public int inviteWanderSeconds = 10;

        public float inviteBaseChance = 0.3f;
        public float inviteBeautyMultiplier = 1.0f;

        public int maxGuestsToInvite = 5;

        public int danceCooldownSeconds = 7200;

        public int InviteCooldownTicks => Mathf.RoundToInt(inviteCooldownSeconds * (2500f / 3600f));

        public int InviteWanderTicks => Mathf.RoundToInt(inviteWanderSeconds * (2500f / 3600f));

        public int DanceCooldownTicks => Mathf.RoundToInt(danceCooldownSeconds * (2500f / 3600f));

        // 設定の保存・読み込み
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref baseDancePrice, "baseDancePrice", 20f);
            Scribe_Values.Look(ref danceBeautyMultiplier, "danceBeautyMultiplier", 1.0f);
            Scribe_Values.Look(ref baseProstitutionPrice, "baseProstitutionPrice", 50f);
            Scribe_Values.Look(ref prostitutionBeautyMultiplier, "prostitutionBeautyMultiplier", 1.0f);
            Scribe_Values.Look(ref partMultiplier, "partMultiplier", 1.0f);
            Scribe_Values.Look(ref spawnSilverForDance, "spawnSilverForDance", false);
            Scribe_Values.Look(ref spawnSilverForProstitution, "spawnSilverForProstitution", false);
            Scribe_Values.Look(ref debugLog, "debugLog", false);

            Scribe_Values.Look(ref prostitutePartsVaginal, "prostitutePartsVaginal", 1.0f);
            Scribe_Values.Look(ref prostitutePartsAnal, "prostitutePartsAnal", 1.0f);
            Scribe_Values.Look(ref prostitutePartsOral, "prostitutePartsOral", 1.0f);
            Scribe_Values.Look(ref prostitutePartsDoublePenetration, "prostitutePartsDoublePenetration", 1.0f);
            Scribe_Values.Look(ref prostitutePartsBoobjob, "prostitutePartsBoobjob", 1.0f);
            Scribe_Values.Look(ref prostitutePartsHandjob, "prostitutePartsHandjob", 1.0f);
            Scribe_Values.Look(ref prostitutePartsFootjob, "prostitutePartsFootjob", 1.0f);
            Scribe_Values.Look(ref prostitutePartsFingering, "prostitutePartsFingering", 1.0f);
            Scribe_Values.Look(ref prostitutePartsScissoring, "prostitutePartsScissoring", 1.0f);
            Scribe_Values.Look(ref prostitutePartsFisting, "prostitutePartsFisting", 1.0f);
            Scribe_Values.Look(ref prostitutePartsRimming, "prostitutePartsRimming", 1.0f);
            Scribe_Values.Look(ref prostitutePartsFellatio, "prostitutePartsFellatio", 1.0f);
            Scribe_Values.Look(ref prostitutePartsCunnilingus, "prostitutePartsCunnilingus", 1.0f);
            Scribe_Values.Look(ref prostitutePartsSixtynine, "prostitutePartsSixtynine", 1.0f);

            Scribe_Values.Look(ref inviteCooldownSeconds, "inviteCooldownSeconds", 21600);
            Scribe_Values.Look(ref inviteWanderSeconds, "inviteWanderSeconds", 10);

            Scribe_Values.Look(ref inviteBaseChance, "inviteBaseChance", 0.3f);
            Scribe_Values.Look(ref inviteBeautyMultiplier, "inviteBeautyMultiplier", 1.0f);
            Scribe_Values.Look(ref maxGuestsToInvite, "maxGuestsToInvite", 10);

            Scribe_Values.Look(ref danceCooldownSeconds, "danceCooldownSeconds", 7200);

        }
    }
}