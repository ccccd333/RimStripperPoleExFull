using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Stripper {
	public class Job_WatchStripperPole_Def : JobDef {
		public string recordCountName = "";
		public LogEntry_WatchStripperPole_Def logDef;
		public ThoughtDef thoughtDef;
	}

	class JobDriver_WatchStripperPole : JobDriver {
		private Job_WatchStripperPole_Def def => (Job_WatchStripperPole_Def)job.def;
		private Building_StripperPole StripperPole => job.targetA.Thing as Building_StripperPole;
        private IntVec3 Cell => job.targetB.Cell;
        private Building Chair => job.targetC.HasThing ? job.targetC.Thing as Building : null;

		private Pawn dancer;


        public override bool TryMakePreToilReservations(bool errorOnFailed) {
            return pawn.ReserveSittableOrSpot(Cell, job, errorOnFailed);
        }
        public override bool CanBeginNowWhileLyingDown() {
            return false;
        }

		protected override IEnumerable<Toil> MakeNewToils() {
			this.EndOnDespawnedOrNull(TargetIndex.A);
			this.AddEndCondition(() => StripperPole.currentDancer == null ? JobCondition.Incompletable : JobCondition.Ongoing);
			yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
			var watch = ToilMaker.MakeToil("MakeNewToils");
			watch.initAction = () => {
				dancer = StripperPole.currentDancer;
				StripperPoleHelper.RegisterAvailableProstitute(dancer,pawn);

            };
			watch.AddPreTickAction(() => {
				WatchTickAction();
			});
			watch.AddFinishAction(() => {
				AddRecords();
				JoyUtility.TryGainRecRoomThought(pawn);
				AddPlayLog();
				AddThought();
			});
			watch.defaultCompleteMode = ToilCompleteMode.Delay;
			watch.defaultDuration = job.def.joyDuration;
			watch.handlingFacing = true;
			yield return watch;
		}

		protected virtual void WatchTickAction() {
			pawn.rotationTracker.FaceCell(base.TargetA.Cell);
			pawn.GainComfortFromCellIfPossible(0);
			JoyUtility.JoyTickCheckEnd(pawn, 0, JoyTickFullJoyAction.EndJob, StripperPole.def.joyGainFactor, StripperPole);
		}

		private void AddRecords() {
			var recordDef = DefDatabase<RecordDef>.GetNamed(def.recordCountName, true);
			if (recordDef != null) {
				pawn.records.Increment(recordDef);
			}
		}

		private void AddPlayLog() {
			if (dancer == null) return;
			var log = new LogEntry_WatchStripperPole(def.logDef, pawn, dancer);
			Find.PlayLog.Add(log);
		}

		private void AddThought() {
			if (dancer == null) return;
			float statValue = dancer.GetStatValue(StatDefOf.PawnBeauty);
			var thought = (Thought_MemorySocial)ThoughtMaker.MakeThought(def.thoughtDef);
			thought.moodPowerFactor = statValue;
			thought.opinionOffset *= statValue;
			pawn.needs.mood.thoughts.memories.TryGainMemory(thought, dancer);
		}
	}
}
