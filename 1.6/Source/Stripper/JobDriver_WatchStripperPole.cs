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

		public Pawn dancer;

		private bool isWatching = false;



        public override bool TryMakePreToilReservations(bool errorOnFailed) {
            return pawn.ReserveSittableOrSpot(Cell, job, errorOnFailed);
        }
        public override bool CanBeginNowWhileLyingDown() {
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<bool>(ref isWatching, "isWatching", false, false);
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Building_StripperPole pole = job.targetA.Thing as Building_StripperPole;

                if (isWatching && pole != null)
                {
                    dancer = pole.currentDancer;

					if (StripperMod.settings.debugLog)
					{
						Log.Message($"[StripperPole] WatchStripperPole ExposeData isWatching: {isWatching} currentDancer: {dancer} watcher: {pawn} mode: {Scribe.mode}");
					}
                    if (dancer != null && !pawn.IsColonist)
                    {
                        // セーブ時この観客のjobが生きている場合、セーブロード後観客リストを復元する必要があるので
                        // セーブロード時のみ挿入しなおす
                        StripperPoleHelper.RegisterAvailableProstitute(dancer, pawn);
                    }
                }
            }
        }

        protected override IEnumerable<Toil> MakeNewToils() {
			this.EndOnDespawnedOrNull(TargetIndex.A);
            this.AddEndCondition(() => {

				//if (StripperMod.settings.debugLog)
				//{
				//	Log.Message($"[StripperPole] WatchStripperPole MakeNewToils AddEndCondition currentDancer: {StripperPole.currentDancer} mode: {Scribe.mode}");
				//}

                if (StripperPole.currentDancer == null)
                {
                    if (Scribe.mode != LoadSaveMode.Inactive) return JobCondition.Ongoing;
                    return JobCondition.Incompletable;
                }
                return JobCondition.Ongoing;
            });

            //this.AddEndCondition(() => StripperPole.currentDancer == null ? JobCondition.Incompletable : JobCondition.Ongoing);
			yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
			var watch = ToilMaker.MakeToil("MakeNewToils");
			watch.initAction = () => {
				//if (StripperMod.settings.debugLog)
				//{
				//	Log.Message($"[StripperPole] WatchStripperPole MakeNewToils initAction currentDancer: {StripperPole.currentDancer}");
				//}
                dancer = StripperPole.currentDancer;
				// 到達前にポーンがダンスを終了した場合は観客数に加えない
				if (dancer != null && !pawn.IsColonist)
				{
                    isWatching = true;
                    StripperPoleHelper.RegisterAvailableProstitute(dancer, pawn);
				}

            };
			//watch.AddPreTickAction(() => {
   //             //Log.Message($"[StripperPole] WatchStripperPole MakeNewToils AddPreTickAction currentDancer: {StripperPole.currentDancer}");
   //             //WatchTickAction();
			//});
            watch.tickIntervalAction = (int delta) => {
                //Log.Message($"[StripperPole] WatchStripperPole MakeNewToils tickIntervalAction currentDancer: {StripperPole.currentDancer}");
                WatchTickAction();
                JoyUtility.JoyTickCheckEnd(pawn, delta, JoyTickFullJoyAction.EndJob, StripperPole.def.joyGainFactor, StripperPole);
            };
            watch.AddFinishAction(() => {
                //Log.Message($"[StripperPole] WatchStripperPole MakeNewToils AddFinishAction currentDancer: {StripperPole.currentDancer}");
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
			//JoyUtility.JoyTickCheckEnd(pawn, 0, JoyTickFullJoyAction.EndJob, StripperPole.def.joyGainFactor, StripperPole);
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
