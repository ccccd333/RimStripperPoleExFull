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
	public class LogEntry_WatchStripperPole_Def : LogEntryDef {
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

	class LogEntry_WatchStripperPole : LogEntry {
		private LogEntry_WatchStripperPole_Def def => (LogEntry_WatchStripperPole_Def)base.def;

		private Pawn watcher;
		private string WatcherName => watcher?.LabelShort ?? "null";
		private Pawn dancer;
		private string DancerName => dancer?.LabelShort ?? "null";

		private List<RulePackDef> extraSentencePacks;

		public LogEntry_WatchStripperPole() : base() { }
		public LogEntry_WatchStripperPole(LogEntry_WatchStripperPole_Def def, Pawn watcher, Pawn dancer) : base(def) {
			this.watcher = watcher;
			this.dancer = dancer;
		}

		public override bool Concerns(Thing t) {
			return t == watcher;
		}

		public override IEnumerable<Thing> GetConcerns() {
			if (watcher != null) {
				yield return watcher;
			}
		}

		public override bool CanBeClickedFromPOV(Thing pov) {
			return pov == watcher && CameraJumper.CanJump(dancer);
		}

		public override void ClickedFromPOV(Thing pov) {
			if (pov == watcher) {
				CameraJumper.TryJumpAndSelect(dancer);
				return;
			}
			throw new NotImplementedException();
		}

		public override Texture2D IconFromPOV(Thing pov) {
			return def.Symbol;
		}

		public override string GetTipString() {
			return def.LabelCap + "\n" + base.GetTipString();
		}

		protected override string ToGameStringFromPOV_Worker(Thing pov, bool forceLog) {
			if (watcher == null || dancer == null) {
				Log.ErrorOnce("LogEntry_WatchStripperPole has a null pawn reference.", 34422);
				return "[" + def.label + " error: null pawn reference]";
			}
			Rand.PushState();
			Rand.Seed = logID;
			GrammarRequest request = base.GenerateGrammarRequest();
			string text;
			if (pov == watcher) {
				request.IncludesBare.Add(def.logRules);
				request.Rules.AddRange(GrammarUtility.RulesForPawn("WATCHER", watcher, request.Constants));
				request.Rules.AddRange(GrammarUtility.RulesForPawn("DANCER", dancer, request.Constants));
				text = GrammarResolver.Resolve("r_logentry", request, "watch dance", forceLog);
			}
			else {
				Log.ErrorOnce("Cannot display LogEntry_WatchStripperPole from POV who isn't watcher.", 51251);
				text = ToString();
			}
			if (extraSentencePacks != null) {
				for (int i = 0; i < extraSentencePacks.Count; i++) {
					request.Clear();
					request.Includes.Add(extraSentencePacks[i]);
					request.Rules.AddRange(GrammarUtility.RulesForPawn("WATCHER", watcher, request.Constants));
					request.Rules.AddRange(GrammarUtility.RulesForPawn("DANCER", dancer, request.Constants));
					text = text + " " + GrammarResolver.Resolve(extraSentencePacks[i].FirstRuleKeyword, request, "extraSentencePack", forceLog, extraSentencePacks[i].FirstUntranslatedRuleKeyword);
				}
			}
			Rand.PopState();
			return text;
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_References.Look(ref watcher, "watcher", saveDestroyedThings: true);
			Scribe_References.Look(ref dancer, "dancer", saveDestroyedThings: true);
			Scribe_Collections.Look(ref extraSentencePacks, "extras", LookMode.Undefined);
		}

		public override string ToString() {
			return def.label + ": " + WatcherName + "->" + DancerName;
		}
	}
}
