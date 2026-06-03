using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using Verse.Grammar;

namespace Stripper {
	public class LogEntry_UseStripperPole_Def : LogEntryDef {
		[NoTranslate]
		private string symbol;
		public InteractionSymbolSource symbolSource;
		[Unsaved(false)]
		private Texture2D symbolTex;
		public Texture2D Symbol {
			get {
				if (symbolTex == null) {
					symbolTex = ContentFinder<Texture2D>.Get(symbol);
				}
				return symbolTex;
			}
		}

		public RulePack logRules;
	}

	class LogEntry_UseStripperPole : LogEntry {
		private LogEntry_UseStripperPole_Def def => (LogEntry_UseStripperPole_Def)base.def;

		private Pawn dancer;
		private string DancerName => dancer?.LabelShort ?? "null";
		private List<RulePackDef> extraSentencePacks;

		public LogEntry_UseStripperPole() : base() { }
		public LogEntry_UseStripperPole(LogEntry_UseStripperPole_Def def, Pawn dancer) : base(def) {
			this.dancer = dancer;
		}

		public override bool Concerns(Thing t) {
			return t == dancer;
		}

		public override IEnumerable<Thing> GetConcerns() {
			if (dancer != null) {
				yield return dancer;
			}
		}

		public override Texture2D IconFromPOV(Thing pov) {
			return def.Symbol;
		}

		public override string GetTipString() {
			return def.LabelCap + "\n" + base.GetTipString();
		}

		protected override string ToGameStringFromPOV_Worker(Thing pov, bool forceLog) {
			if (dancer == null) {
				Log.ErrorOnce("LogEntry_UseStripperPole has a null pawn reference.", 34422);
				return "[" + def.label + " error: null pawn reference]";
			}
			Rand.PushState();
			Rand.Seed = logID;
			GrammarRequest request = base.GenerateGrammarRequest();
			string text;
			if (pov == dancer) {
				request.IncludesBare.Add(def.logRules);
				request.Rules.AddRange(GrammarUtility.RulesForPawn("DANCER", dancer, request.Constants));
				text = GrammarResolver.Resolve("r_logentry", request, "dance from dancer", forceLog);
			}
			else {
				Log.ErrorOnce("Cannot display LogEntry_UseStripperPole from POV who isn't dancer.", 51251);
				text = ToString();
			}
			if (extraSentencePacks != null) {
				for (int i = 0; i < extraSentencePacks.Count; i++) {
					request.Clear();
					request.Includes.Add(extraSentencePacks[i]);
					request.Rules.AddRange(GrammarUtility.RulesForPawn("DANCER", dancer, request.Constants));
					text = text + " " + GrammarResolver.Resolve(extraSentencePacks[i].FirstRuleKeyword, request, "extraSentencePack", forceLog, extraSentencePacks[i].FirstUntranslatedRuleKeyword);
				}
			}
			Rand.PopState();
			return text;
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_References.Look(ref dancer, "dancer", saveDestroyedThings: true);
			Scribe_Collections.Look(ref extraSentencePacks, "extras", LookMode.Undefined);
		}

		public override string ToString() {
			return def.label + ": " + DancerName;
		}
	}
}
