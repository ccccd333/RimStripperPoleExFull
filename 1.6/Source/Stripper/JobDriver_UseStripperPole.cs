using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using rjw;

namespace Stripper {
	public class Job_UseStripperPole_Def : JobDef {
		public int turnMin = 45;
		public int turnMax = 120;
		public int undressMin = 45;
		public int undressMax = 120;
		public string recordCountName = "";
		public LogEntry_UseStripperPole_Def logDef;
	}

    public class JobDriver_UseStripperPole : JobDriver {
		//consts
		private const int tickerFrequency = 60; //calculations once per second

		private readonly TargetIndex StripperPoleIndex = TargetIndex.A;
		private Building_StripperPole StripperPole => (Building_StripperPole)job.GetTarget(StripperPoleIndex);
		private Job_UseStripperPole_Def def => (Job_UseStripperPole_Def)job.def;

		private int ticks_elapsed = 0; //main time counter

		private List<Apparel> wornApparel;
		private List<bool> forcedApparel;
		private int nextApparel = 0; //next piece of clothing to remove
		private int undressTick = 0; //tick when to remove next piece of clothing
		private int turnTick = 0; //tick when to turn body



		private void Init(bool loadingSave = false) {
			this.FailOnDespawnedOrNull(StripperPoleIndex);
			this.FailOn(() => pawn.Drafted);
			this.FailOn(() => pawn.IsFighting());

			if (loadingSave) return;
            StripperPoleHelper.UnregisterDancer(pawn);
            wornApparel = pawn.apparel.WornApparel.ToList();
			wornApparel.Sort((Apparel a, Apparel b) => b.def.apparel.LastLayer.drawOrder.CompareTo(a.def.apparel.LastLayer.drawOrder));
			forcedApparel = wornApparel.Select(e => pawn.outfits.forcedHandler.IsForced(e)).ToList();
			nextApparel = 0;
			undressTick = GetNextUndressTick();
			turnTick = GetNextTurnTick();
		}

        public override bool TryMakePreToilReservations(bool errorOnFailed) {
            return pawn.Reserve(StripperPole, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils() {
            Init();
            var dancing = new Toil();

            dancing.tickAction = StripperPoleTick;
            dancing.initAction = () => {
				pawn.Rotation = Rot4.South;
				StripperPole.lastDanceInfo = "";
				StripperPole.currentDanceInfo = "";
				StripperPole.currentDancer = pawn;
                
            };
            dancing.AddFinishAction(() => {
				StripperPole.currentDanceInfo = "";
				StripperPole.lastDanceInfo = GetInfoString("danced");
				StripperPole.currentDancer = null;

				AddRecords();
				JoyUtility.TryGainRecRoomThought(pawn);

				pawn.inventory.innerContainer.RemoveAll(wornApparel.Contains);
                for (int i = 0; i < wornApparel.Count; i++) {
					pawn.apparel.Wear(wornApparel[i]);
					pawn.outfits.forcedHandler.SetForced(wornApparel[i], forcedApparel[i]);
				}
				pawn.Drawer.renderer.SetAllGraphicsDirty();
				GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);

				AddPlayLog();
			});

            dancing.socialMode = RandomSocialMode.Off;
            dancing.handlingFacing = true;

			dancing.defaultCompleteMode = ToilCompleteMode.Delay;
            dancing.defaultDuration = def.joyDuration;

			// 売春可能なポーンはJobDriver_WatchStripperPole側(観客)で挿入される
			// ダンスが正常終了時ダイアログボックス表示用のToilを生成
            var prostitute = new Toil();
			prostitute.defaultCompleteMode = ToilCompleteMode.Instant;
            prostitute.initAction = () => {
                // ダイアログボックス
                //Log.Message($"[StripperPole] Try prostitute toil");

                StripperPoleHelper.DistributeDancePayout(pawn);

                Pawn prostituteTarget = StripperPoleHelper.GetRandomAvailableProstitute(pawn);

                if (prostituteTarget == null)
                {
                    return;
                }

                Log.Message($"[StripperPole] prostituteTarget: {prostituteTarget.LabelShort} pawn: {pawn.LabelShort}");

                string title = "SP_Prostitute_Title".Translate(); // ダイアログのタイトル
                
                string text = "SP_Prostitute_Text".Translate(pawn, prostituteTarget);

                // RimWorld標準の確認ダイアログをポップアップ
                Find.WindowStack.Add(new Dialog_ProstitutionNegotiation(
                    title,
                    text,
                    pawn,
                    prostituteTarget,
                    () => {
                        Messages.Message("SP_Prostitute_Accept".Translate(pawn, prostituteTarget), MessageTypeDefOf.PositiveEvent);
                        Job gettin_loved = JobMaker.MakeJob(SPJobDefOf.SP_ServingVisitor, prostituteTarget);
                        pawn.jobs.StartJob(gettin_loved, JobCondition.InterruptForced);
                        //ReadyForNextToil();
                    },
                    () => {
                        Messages.Message("SP_Prostitute_Reject".Translate(), MessageTypeDefOf.NeutralEvent);
                        //ReadyForNextToil();
                    }
                ));
            };

            yield return Toils_Goto.GotoThing(StripperPoleIndex, PathEndMode.OnCell);
            yield return dancing;
			yield return prostitute;
        }

        public void StripperPoleTick() {
			TickFacing();
			TickClothes();
			TickStats();
			ticks_elapsed++;
		}

		private void TickFacing() {
			if (ticks_elapsed < turnTick) return;
			pawn.Rotation = Rot4.Random;
			turnTick = GetNextTurnTick();
		}

		private int GetNextTurnTick() {
			return turnTick + Rand.RangeInclusive(def.turnMin, def.turnMax);
		}

		private void TickClothes() {
			if (nextApparel >= wornApparel.Count) return;
			if (ticks_elapsed < undressTick) return;
			pawn.apparel.TryMoveToInventory(wornApparel[nextApparel]);
			pawn.Drawer.renderer.SetAllGraphicsDirty();
			nextApparel++;
			undressTick = GetNextUndressTick();
			FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
        }

		private int GetNextUndressTick() {
			return undressTick + Rand.RangeInclusive(def.undressMin, def.undressMax);
		}

		private void TickStats() {
			JoyUtility.JoyTickCheckEnd(pawn, 0, JoyTickFullJoyAction.None, StripperPole.def.joyGainFactor, StripperPole);
			if (!pawn.IsHashIntervalTick(tickerFrequency)) return;

			/* todo: make exhibitionist extra happy
			string pawn_quirks = CompRJW.Comp(pawn).quirks.ToString();
			if (pawn_quirks.Contains("Exhibitionist")) { }
			*/

			StripperPole.currentDanceInfo = GetInfoString("dancing");
		}

        private void StopSession() {
            ReadyForNextToil();
        }

		private void AddRecords() {
			var recordDef = DefDatabase<RecordDef>.GetNamed(def.recordCountName, true);
			if (recordDef != null) {
				pawn.records.Increment(recordDef);
			}
		}

        public override void ExposeData() {
            base.ExposeData();

			Scribe_Values.Look<int>(ref ticks_elapsed, "ticks_elapsed", 0, false);

			Scribe_Collections.Look(ref wornApparel, "wornApparel", LookMode.Reference);
			Scribe_Collections.Look(ref forcedApparel, "forcedApparel", LookMode.Value);
			Scribe_Values.Look<int>(ref nextApparel, "nextApparel", 0, false);
			Scribe_Values.Look<int>(ref undressTick, "undressTick", 0, false);
			Scribe_Values.Look<int>(ref turnTick, "turnTick", 0, false);

			if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                Init(true);
            }
        }

		private string GetInfoString(string verb) {
			var time = new DateTime().AddMinutes((double)ticks_elapsed / 2500 * 60).ToString(@"HH\:mm\:ss");
			return $"{pawn.LabelShort} {verb} for {time}";
		}

		private void AddPlayLog() {
            var log = new LogEntry_UseStripperPole(def.logDef, pawn);
			Find.PlayLog.Add(log);
		}
	}
}
