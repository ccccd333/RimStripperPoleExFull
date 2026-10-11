using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.Grammar;

namespace Stripper
{
    /// <summary>
    /// RimTalk連携用のセンシティブなPlayLogEntry_Interaction。
    /// PlayLogEntry_Interactionを継承しているため、RimTalkの監視フックを透過し、
    /// RJWの胸サイズ・相手のペニスサイズ・特性に応じた詳細なシチュエーション文をGrammarに動的注入します。
    /// </summary>
    public class PlayLogEntry_StripperSensitive : PlayLogEntry_Interaction
    {
        public PlayLogEntry_StripperSensitive() : base() { }

        public PlayLogEntry_StripperSensitive(InteractionDef intDef, Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks)
            : base(intDef, initiator, recipient, extraSentencePacks)
        {
        }

        protected override GrammarRequest GenerateGrammarRequest()
        {
            GrammarRequest request = base.GenerateGrammarRequest();
            InjectDynamicRules(ref request);
            return request;
        }

        protected override string ToGameStringFromPOV_Worker(Thing pov, bool forceLog)
        {
            if (initiator == null || recipient == null)
            {
                return base.ToGameStringFromPOV_Worker(pov, forceLog);
            }

            Rand.PushState();
            Rand.Seed = logID;
            GrammarRequest request = GenerateGrammarRequest();

            string text;
            if (pov == initiator)
            {
                request.IncludesBare.Add(intDef.logRulesInitiator);
                request.Rules.AddRange(GrammarUtility.RulesForPawn("INITIATOR", initiator, request.Constants, true, true));
                request.Rules.AddRange(GrammarUtility.RulesForPawn("RECIPIENT", recipient, request.Constants, true, true));
                text = GrammarResolver.Resolve("r_logentry", request, "interaction from initiator", forceLog, null, null, null, true);
            }
            else if (pov == recipient)
            {
                if (intDef.logRulesRecipient != null)
                {
                    request.IncludesBare.Add(intDef.logRulesRecipient);
                }
                else
                {
                    request.IncludesBare.Add(intDef.logRulesInitiator);
                }
                request.Rules.AddRange(GrammarUtility.RulesForPawn("INITIATOR", initiator, request.Constants, true, true));
                request.Rules.AddRange(GrammarUtility.RulesForPawn("RECIPIENT", recipient, request.Constants, true, true));
                text = GrammarResolver.Resolve("r_logentry", request, "interaction from recipient", forceLog, null, null, null, true);
            }
            else
            {
                request.IncludesBare.Add(intDef.logRulesInitiator);
                request.Rules.AddRange(GrammarUtility.RulesForPawn("INITIATOR", initiator, request.Constants, true, true));
                request.Rules.AddRange(GrammarUtility.RulesForPawn("RECIPIENT", recipient, request.Constants, true, true));
                text = GrammarResolver.Resolve("r_logentry", request, "interaction from other", forceLog, null, null, null, true);
            }

            if (extraSentencePacks != null)
            {
                for (int i = 0; i < extraSentencePacks.Count; i++)
                {
                    request.Clear();
                    request.Includes.Add(extraSentencePacks[i]);
                    request.Rules.AddRange(GrammarUtility.RulesForPawn("INITIATOR", initiator, request.Constants, true, true));
                    request.Rules.AddRange(GrammarUtility.RulesForPawn("RECIPIENT", recipient, request.Constants, true, true));
                    InjectDynamicRules(ref request); // Re-inject constants after clearing request
                    text = text + " " + GrammarResolver.Resolve(extraSentencePacks[i].FirstRuleKeyword, request, "extraSentencePack", forceLog, extraSentencePacks[i].FirstUntranslatedRuleKeyword, null, null, true);
                }
            }

            Rand.PopState();

            if (StripperMod.settings?.debugLog == true)
            {
                Log.Message($"[StripperPole] Sensitive interaction resolved ({pov?.LabelShort ?? "Other"} POV): {text}");
            }

            return text;
        }

        private void InjectDynamicRules(ref GrammarRequest request)
        {
            if (initiator == null) return;
            
            // Ensure Constants dictionary is initialized
            var constants = request.Constants; 

            // 1. ダンサー（Initiator）の胸サイズ定数
            var breastCat = StripperRJWHelper.GetBreastCategory(initiator);
            string breastStr = breastCat.ToString();
            request.Constants["initiator_breastSize"] = breastStr;
            request.Constants["INITIATOR_breastSize"] = breastStr;

            // 2. 相手（Recipient）の性別およびペニスサイズ定数
            bool isSelf = (recipient == null || recipient == initiator);
            string isSelfStr = isSelf ? "True" : "False";
            request.Constants["recipient_isSelf"] = isSelfStr;
            request.Constants["RECIPIENT_isSelf"] = isSelfStr;

            if (!isSelf && recipient != null)
            {
                string genderStr = recipient.gender.ToString();
                request.Constants["recipient_gender"] = genderStr;
                request.Constants["RECIPIENT_gender"] = genderStr;

                var penisCat = StripperRJWHelper.GetPenisCategory(recipient);
                string penisStr = penisCat.ToString();
                request.Constants["recipient_penisSize"] = penisStr;
                request.Constants["RECIPIENT_penisSize"] = penisStr;
            }
            else
            {
                request.Constants["recipient_gender"] = "None";
                request.Constants["RECIPIENT_gender"] = "None";
                request.Constants["recipient_penisSize"] = "None";
                request.Constants["RECIPIENT_penisSize"] = "None";
            }

            // 3. ダンサーの露出狂・好色特性定数
            bool hasExhibitionist = StripperRJWHelper.HasExhibitionistTrait(initiator);
            string exhStr = hasExhibitionist ? "True" : "False";
            request.Constants["initiator_isExhibitionist"] = exhStr;
            request.Constants["INITIATOR_isExhibitionist"] = exhStr;

            if (StripperMod.settings?.debugLog == true)
            {
                Log.Message($"[StripperPole] Injected dynamic rules for {initiator?.LabelShort}: breast={breastStr}, recipient={recipient?.LabelShort}, isSelf={isSelfStr}, recipient_gender={request.Constants["recipient_gender"]}, recipient_penisSize={request.Constants["recipient_penisSize"]}");
            }
        }
    }
}
