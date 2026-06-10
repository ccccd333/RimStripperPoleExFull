using rjw;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static rjw.xxx;

namespace Stripper
{
    public class StripperMod : Mod
    {
        public static StripperSettings settings;
        private Vector2 scrollPosition = Vector2.zero;
        private Dictionary<rjwSextype, string> _partsBuffers = new Dictionary<rjwSextype, string>();
        public StripperMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<StripperSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect scrollOuterRect = new Rect(0, 0, inRect.width, inRect.height);
            float totalHeight = 1500f;
            Rect scrollInnerRect = new Rect(0, 0, inRect.width - 20f, totalHeight);

            Widgets.BeginScrollView(scrollOuterRect, ref scrollPosition, scrollInnerRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(scrollInnerRect);

            listing.Gap(30f);

            listing.Label("SP_Settings_Dance_Title".Translate());
            listing.Gap();
            listing.Label("SP_Settings_Dance_BasePrice".Translate());
            Rect sdt_rect = listing.GetRect(30f);
            Widgets.TextFieldNumeric(sdt_rect, ref settings.baseDancePrice, ref _bufferDancePrice,1.0f);
            if (float.TryParse(_bufferDancePrice, out float result))
            {
                settings.baseDancePrice = result;
            }

            listing.Gap();

            listing.Label("SP_Settings_Dance_BeautyMult".Translate());
            Rect sdb_rect = listing.GetRect(30f);
            Widgets.TextFieldNumeric(sdb_rect, ref settings.danceBeautyMultiplier, ref _bufferDanceBeauty, 1.0f);
            if (float.TryParse(_bufferDanceBeauty, out float result2))
            {
                settings.danceBeautyMultiplier = result2;
            }

            listing.GapLine();


            listing.Label("SP_Settings_Prostitute_Title".Translate());
            listing.Gap();

            listing.Label("SP_Settings_Prostitute_BasePrice".Translate());
            Rect spbb_rect = listing.GetRect(30f);
            Widgets.TextFieldNumeric(spbb_rect, ref settings.baseProstitutionPrice, ref _bufferProstPrice, 1.0f);
            if (float.TryParse(_bufferProstPrice, out float result3))
            {
                settings.baseProstitutionPrice = result3;
            }

            listing.Gap();

            listing.Label("SP_Settings_Prostitute_BeautyMult".Translate());
            Rect spbm_rect = listing.GetRect(30f);
            Widgets.TextFieldNumeric(spbm_rect, ref settings.prostitutionBeautyMultiplier, ref _bufferProstBeauty, 1.0f);
            if (float.TryParse(_bufferProstBeauty, out float result4))
            {
                settings.prostitutionBeautyMultiplier = result4;
            }

            listing.GapLine();

            listing.Label("SP_Settings_SexType_Title".Translate());
            listing.Gap();

            var validTypes = Enum.GetValues(typeof(rjwSextype))
                     .Cast<rjw.xxx.rjwSextype>()
                     .Where(type => type != rjwSextype.None && type != rjwSextype.Masturbation);

            foreach (rjwSextype sextype in validTypes)
            {

                Rect rect = listing.GetRect(30f);
                Widgets.Label(rect.LeftHalf(), ("SP_Settings_SexType_" + sextype.ToString()).Translate());
                float ival = 1.0f;

                if (!_partsBuffers.ContainsKey(sextype))
                {
                    ival = StripperPoleHelper.GetProstitutePartsMult(sextype);
                    _partsBuffers[sextype] = ival.ToString();
                }

                string buffer = _partsBuffers[sextype];
                Widgets.TextFieldNumeric(rect.RightHalf(), ref ival, ref buffer, 1.0f);

                if (float.TryParse(buffer, out float result_p))
                {
                    _partsBuffers[sextype] = buffer;
                    StripperPoleHelper.SetProstitutePartsMult(sextype, result_p);
                }
            }


            listing.Gap();

            listing.GapLine();

            listing.Label("SP_Settings_Payment_Title".Translate());
            listing.Gap();
            listing.CheckboxLabeled("SP_Settings_Payment_DanceAutoSpawn".Translate(), ref settings.spawnSilverForDance);
            listing.Gap();
            listing.CheckboxLabeled("SP_Settings_Payment_ProstituteAutoSpawn".Translate(), ref settings.spawnSilverForProstitution);
            listing.GapLine();

            listing.Label("SP_Settings_InviteCooldown".Translate());
            Rect spic_rect = listing.GetRect(30f);
            Widgets.TextFieldNumeric(spic_rect, ref settings.inviteCooldownSeconds, ref _bufferInviteCooldown, 1.0f,1000000.0f);
            if (int.TryParse(_bufferInviteCooldown, out int result5))
            {
                settings.inviteCooldownSeconds = result5;
            }
            listing.Gap();

            listing.Label("SP_Settings_Invite_Wander".Translate());
            Rect spiw_rect = listing.GetRect(30f);
            Widgets.TextFieldNumeric(spiw_rect, ref settings.inviteWanderSeconds, ref _bufferInviteWander, 1.0f, 1000.0f);
            if (int.TryParse(_bufferInviteWander, out int result6))
            {
                settings.inviteWanderSeconds = result6;
            }
            listing.Gap();

            listing.Label("SP_Settings_Invite_BaseChance".Translate());
            Rect spibc_rect = listing.GetRect(30f);
            float ibc = settings.inviteBaseChance * 100.0f;
            Widgets.TextFieldNumeric(spibc_rect, ref ibc, ref _bufferInviteChanceBase, 1.0f, 100.0f);
            if (float.TryParse(_bufferInviteChanceBase, out float result7))
            {
                settings.inviteBaseChance = result7 / 100.0f;
                    
            }
            listing.Gap();

            listing.Label("SP_Settings_Invite_BeautyMult".Translate());
            Rect spibm_rect = listing.GetRect(30f);
            Widgets.TextFieldNumeric(spibm_rect, ref settings.inviteBeautyMultiplier, ref _bufferInviteChanceBeautyMult, 1.0f, 100.0f);
            if (float.TryParse(_bufferInviteChanceBeautyMult, out float result8))
            {
                settings.inviteBeautyMultiplier = result8;
            }
            listing.Gap();

            listing.Label("SP_Settings_Invite_MaxInviteGuests".Translate());
            Rect spimig_rect = listing.GetRect(30f);
            Widgets.TextFieldNumeric(spimig_rect, ref settings.maxGuestsToInvite, ref _bufferMaxGuestsToInvite, 1, 100);
            if (int.TryParse(_bufferMaxGuestsToInvite, out int result9))
            {
                settings.maxGuestsToInvite = result9;
            }
            listing.Gap();

            listing.GapLine();
            listing.Label("SP_Settings_WorkGiver".Translate());
            listing.Gap();
            listing.Label("SP_Settings_Dance_CooldownTicks".Translate());
            Rect spsdct_rect = listing.GetRect(30f);
            Widgets.TextFieldNumeric(spsdct_rect, ref settings.danceCooldownSeconds, ref _bufferDanceCooldownSeconds, 0, 1000000);
            if (int.TryParse(_bufferDanceCooldownSeconds, out int result10))
            {
                settings.danceCooldownSeconds = result10;
            }

            listing.GapLine();

            listing.CheckboxLabeled("DebugLog", ref settings.debugLog);

            listing.End();
            Widgets.EndScrollView();
            settings.Write();
        }

        // 数値入力用のバッファ（RimWorldの仕様で必要）
        private string _bufferDancePrice;
        private string _bufferDanceBeauty;
        private string _bufferProstPrice;
        private string _bufferProstBeauty;
        private string _bufferInviteCooldown;
        private string _bufferInviteWander;
        private string _bufferInviteChanceBase;
        private string _bufferInviteChanceBeautyMult;
        private string _bufferMaxGuestsToInvite;
        private string _bufferDanceCooldownSeconds;
        private string _bufferPartMult;

        public override string SettingsCategory() => "Stripper Mod Settings";
    }
}