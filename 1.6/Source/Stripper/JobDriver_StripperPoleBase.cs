using RimWorld;
using Rimworld_Animations;
using rjw;
using rjw.Modules.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Stripper
{
    public class Job_UseStripperPole_Def : JobDef
    {
        public int turnMin = 45;
        public int turnMax = 120;
        public int undressMin = 45;
        public int undressMax = 120;
        public string recordCountName = "";
        public LogEntry_UseStripperPole_Def logDef;
    }

    public abstract class JobDriver_StripperPoleBase : JobDriver
    {
        private const int tickerFrequency = 60; //calculations once per second
        private List<Apparel> wornApparel;
        private List<bool> forcedApparel;
        private int nextApparel;
        private int undressTick;
        private int turnTick;
        private int ticks_elapsed;

        private const string GroupAnimDefName = "GroupAnimation_Female_Stripper_UAP_Start";

        private Mote rotationMote;
        private Mote lightsMote;
        private Sustainer danceMusic;

        protected TargetIndex StripperPoleIndex = TargetIndex.A;
        protected Building_StripperPole StripperPole => (Building_StripperPole)job.GetTarget(StripperPoleIndex);

        protected Job_UseStripperPole_Def def => (Job_UseStripperPole_Def)job.def;

        protected IEnumerable<Toil> MakeDanceToils()
        {
            var gotoToil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
            gotoToil.FailOn(() => pawn.Drafted);
            gotoToil.AddFinishAction(() => {
                if (StripperMod.settings.debugLog)
                {
                    Log.Message($"[StripperPole] MakeNewToils gotoToil AddFinishAction. pawn: {pawn} curJob: {this.pawn.CurJob} this.job {this.job}");
                }

                // UAPのアニメーション強制開始によるテレポートバグを修正する。
                // UAPのPatch_StripPole_StartOnArrivalは、このgotoToilにFinishActionを追加してアニメーションを開始させる。
                // 徴兵などでこのToilが中断された場合でもFinishActionは実行されるため、到着前にアニメーションが始まってテレポートしてしまう。
                // ここでポールに到着していなければ job.targetA を無効化することで、UAP側の pole == null チェックに引っ掛けさせ処理をキャンセルさせる。
                var pole = job.GetTarget(TargetIndex.A).Thing;
                if (pole != null && pawn.Position != pole.Position && pawn.Position != pole.InteractionCell)
                {
                    job.targetA = LocalTargetInfo.Invalid;
                }
            });

            var dancing = new Toil();
            dancing.tickAction = StripperPoleTick;
            dancing.tickIntervalAction = (int delta) => {
                //TickFacing();
                //TickClothes();
                TickStats(delta);
                //ticks_elapsed++;
            };
            dancing.initAction = () => {
                Log.Message($"[StripperPole] MakeNewToils dancing initAction. pawn: {pawn}");

                var comp = pawn.TryGetComp<Rimworld_Animations.CompExtendedAnimator>();
                if (comp != null && !comp.IsAnimating)
                {
                    var anim_def = ResolveGroupDef();
                    if (anim_def != null && comp.CurrentGroupAnimation != anim_def)
                    {
                        //if (!UAP.PawnTests.GenitalChecks.HasVagina(pawn)) return;
                        //pawn.GetGenitalsList
                        if (Genital_Helper.has_vagina(pawn))
                        {
                            // Fully-qualify to avoid Verse.AnimationUtility ambiguity
                            Rimworld_Animations.AnimationUtility.StartGroupAnimation(
                                new List<Pawn> { pawn }, anim_def, StripperPole);
                        }
                    }
                }

                StripperPoleHelper.UnregisterDancer(pawn);
                wornApparel = pawn.apparel.WornApparel.ToList();
                wornApparel.Sort((Apparel a, Apparel b) => b.def.apparel.LastLayer.drawOrder.CompareTo(a.def.apparel.LastLayer.drawOrder));
                forcedApparel = wornApparel.Select(e => pawn.outfits.forcedHandler.IsForced(e)).ToList();
                nextApparel = 0;
                undressTick = GetNextUndressTick();
                turnTick = GetNextTurnTick();

                pawn.Rotation = Rot4.South;
                StripperPole.lastDanceInfo = "";
                StripperPole.currentDanceInfo = "";
                StripperPole.currentDancer = pawn;

                // ダンス開始時にソーシャルログへインタラクションを記録
                Find.PlayLog.Add(new PlayLogEntry_Interaction(SPInteractionDefOf.SP_DancePerformed, pawn, pawn, null));
                
            };
            dancing.AddFinishAction(() => {
                danceMusic?.End();
                danceMusic = null;

                if (StripperMod.settings.debugLog)
                {
                    Log.Message($"[StripperPole] MakeNewToils dancing AddFinishAction. pawn: {pawn}");
                }
                StripperPole.currentDanceInfo = "";
                StripperPole.lastDanceInfo = GetInfoString("danced");
                StripperPole.currentDancer = null;

                AddRecords();
                JoyUtility.TryGainRecRoomThought(pawn);

                if (StripperMod.settings.debugLog)
                {
                    Log.Message($"[StripperPole] MakeNewToils dancing AddFinishAction. worn apparel count: {wornApparel.Count}");
                }

                // 安全に服を復元する処理
                if (wornApparel != null)
                {
                    foreach (var wa in wornApparel)
                    {
                        if (wa == null || wa.Destroyed) continue;

                        // インベントリに移動されていた服を取り出す
                        if (pawn.inventory.innerContainer.Contains(wa))
                        {
                            pawn.inventory.innerContainer.Remove(wa);
                        }

                        // すでに着ている
                        if (!pawn.apparel.WornApparel.Contains(wa))
                        {
                            if (wa.Spawned) wa.DeSpawn();
                            // 競合で他の服を落とさないようにdropReplacedApparel: false を指定
                            pawn.apparel.Wear(wa, dropReplacedApparel: false);
                        }
                    }

                    // 強制装備状態の復元
                    for (int i = 0; i < wornApparel.Count; i++)
                    {
                        var wa = wornApparel[i];
                        if (wa != null && !wa.Destroyed)
                        {
                            pawn.outfits.forcedHandler.SetForced(wa, forcedApparel[i]);
                        }
                    }
                }

                pawn.Drawer.renderer.SetAllGraphicsDirty();
                GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);

                var comp = pawn.TryGetComp<Rimworld_Animations.CompExtendedAnimator>();
                if (comp != null && comp.IsAnimating)
                {
                    var anim_def = ResolveGroupDef();
                    if (comp.CurrentGroupAnimation == anim_def)
                    {
                        // UAPで開始されたPoleアニメーションの停止
                        Rimworld_Animations.AnimationUtility.StopGroupAnimation(pawn);
                    }
                }

                //Rimworld_Animations.AnimationUtility.StopGroupAnimation(pawn);

                AddPlayLog();
            });

            dancing.socialMode = RandomSocialMode.Off;
            dancing.handlingFacing = true;

            dancing.defaultCompleteMode = ToilCompleteMode.Delay;
            dancing.defaultDuration = def.joyDuration;

            // 売春可能なポーンはJobDriver_WatchStripperPole側(観客)で挿入される
            // ダンスが正常終了時ダイアログボックス表示用のToilを生成
            var prostitute = new Toil();
            prostitute.defaultCompleteMode = ToilCompleteMode.Never;
            prostitute.initAction = () => {
                // ダイアログボックス
                if (StripperMod.settings.debugLog)
                {
                    Log.Message($"[StripperPole] Try prostitute toil. pawn: {pawn}");
                }

                if (!pawn.IsColonist)
                {
                    pawn.jobs.curDriver.ReadyForNextToil();
                    return;
                }

                StripperPoleHelper.DistributeDancePayout(pawn);

                Pawn prostituteTarget = StripperPoleHelper.GetRandomAvailableProstitute(pawn);
                StripperPoleHelper.UnregisterDancer(pawn);

                if (prostituteTarget == null)
                {
                    pawn.jobs.curDriver.ReadyForNextToil();
                    return;
                }

                if (StripperMod.settings.debugLog)
                {
                    Log.Message($"[StripperPole] prostituteTarget: {prostituteTarget.LabelShort} pawn: {pawn.LabelShort}");
                }

                string title = "SP_Prostitute_Title".Translate();
                string text = "SP_Prostitute_Text".Translate(pawn, prostituteTarget);

                Pawn dancer = pawn;
                Pawn customer = prostituteTarget;

                // 行為選択ダイアログをポップアップ
                Find.WindowStack.Add(new Dialog_ProstitutionNegotiation(
                    title,
                    text,
                    dancer,
                    customer,
                    (SexInteractionResolved resolved) =>
                    {
                        // 選択された行為をSexPropsとしてキャッシュ
                        // JobDriver_SexBaseInitiator.Start()がpawn.GetRMBSexPropsCache() で
                        // これを拾ってくれるので、指定した体位で行為が実行される
                        if (resolved != null)
                        {
                            var SP = new SexProps(dancer, customer)
                            {
                                isWhoring = true,
                                canBeGuilty = false,
                                interaction = resolved.Interaction,
                                resolved = resolved
                            };
                            dancer.GetRJWPawnData().SexProps = SP;
                        }

                        Messages.Message("SP_Prostitute_Accept".Translate(), MessageTypeDefOf.PositiveEvent);

                        Job gettin_loved = JobMaker.MakeJob(SPJobDefOf.SP_ServingVisitor, customer);
                        dancer.jobs.StartJob(gettin_loved, JobCondition.InterruptForced);
                    },
                    () =>
                    {
                        Messages.Message("SP_Prostitute_Reject".Translate(), MessageTypeDefOf.NeutralEvent);
                        dancer.jobs.curDriver.ReadyForNextToil();
                    }
                ));
            };

            yield return gotoToil;
            yield return dancing;
            yield return prostitute;
        }


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref ticks_elapsed, "ticks_elapsed", 0, false);

            Scribe_Collections.Look(ref wornApparel, "wornApparel", LookMode.Reference);
            Scribe_Collections.Look(ref forcedApparel, "forcedApparel", LookMode.Value);
            Scribe_Values.Look<int>(ref nextApparel, "nextApparel", 0, false);
            Scribe_Values.Look<int>(ref undressTick, "undressTick", 0, false);
            Scribe_Values.Look<int>(ref turnTick, "turnTick", 0, false);


            //if (Scribe.mode == LoadSaveMode.PostLoadInit)
            //{
            //    Init(true);
            //}
        }

        public void StripperPoleTick()
        {
            TickFacing();
            TickClothes();
            //TickStats();
            ticks_elapsed++;

            if (ModsConfig.IdeologyActive)
            {
                DanceEffect();
                DanceMusic();
            }
        }
        private void TickFacing()
        {
            if (ticks_elapsed < turnTick) return;
            pawn.Rotation = Rot4.Random;
            turnTick = GetNextTurnTick();
        }

        private int GetNextTurnTick()
        {
            return turnTick + Rand.RangeInclusive(def.turnMin, def.turnMax);
        }

        private void DanceEffect()
        {
            Vector3 pos = StripperPole.DrawPos;
            pos.z += 2f;

            if (rotationMote == null || rotationMote.Destroyed)
            {


                rotationMote = MoteMaker.MakeStaticMote(
                    pos,
                    pawn.Map,
                    SPThingDefOf.Mote_StripLightBall,
                    1f
                );

                if (rotationMote == null)
                {
                    //Log.Message($"Lights={SPThingDefOf.Mote_StripLightBallLights}");
                    Log.Error("[StripperPole] rotationMote is null");
                    return;
                }
            }

            if (lightsMote == null || lightsMote.Destroyed)
            {


                lightsMote = MoteMaker.MakeStaticMote(
                    pos,
                    pawn.Map,
                    SPThingDefOf.Mote_StripLightBallLights,
                    1f
                );

                if (lightsMote == null)
                {
                    //Log.Message($"Ball={SPThingDefOf.Mote_StripLightBall}");
                    Log.Error("[StripperPole] lightsMote is null");
                    return;
                }

                lightsMote.rotationRate = -3f;
            }

            try
            {
                rotationMote?.Maintain();
                lightsMote?.Maintain();
            }
            catch (Exception e)
            {
                Log.Error($"[StripperPole] DanceEffect Maintain failed: {e}");
                rotationMote = null;
                lightsMote = null;
            }
        }

        private void DanceMusic()
        {
            //SoundDef music = DefDatabase<SoundDef>.GetNamed("Drum_Music_6");
            if (danceMusic == null || danceMusic.Ended)
            {
                danceMusic = SPSoundDefOf.StripMusicSoundDef.TrySpawnSustainer(
                    SoundInfo.InMap(
                        new TargetInfo(pawn.Position, pawn.Map),
                        MaintenanceType.PerTick
                    )
                );
            }

            try
            {
                danceMusic?.Maintain();
            }
            catch (Exception e)
            {
                Log.Error($"[StripperPole] DanceMusic Maintain failed: {e}");
                danceMusic = null;
            }

        }

        private void TickClothes()
        {
            if (nextApparel >= wornApparel.Count) return;
            if (ticks_elapsed < undressTick) return;
            pawn.apparel.TryMoveToInventory(wornApparel[nextApparel]);
            pawn.Drawer.renderer.SetAllGraphicsDirty();
            nextApparel++;
            undressTick = GetNextUndressTick();
            FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
        }

        private int GetNextUndressTick()
        {
            return undressTick + Rand.RangeInclusive(def.undressMin, def.undressMax);
        }

        private void TickStats(int delta)
        {
            //Log.Message($"joyGainFactor={StripperPole.def.joyGainFactor}");
            JoyUtility.JoyTickCheckEnd(pawn, delta, JoyTickFullJoyAction.None, StripperPole.def.joyGainFactor, StripperPole);
            if (!pawn.IsHashIntervalTick(tickerFrequency)) return;

            /* todo: make exhibitionist extra happy
			string pawn_quirks = CompRJW.Comp(pawn).quirks.ToString();
			if (pawn_quirks.Contains("Exhibitionist")) { }
			*/

            StripperPole.currentDanceInfo = GetInfoString("dancing");
        }

        private void StopSession()
        {
            ReadyForNextToil();
        }

        private void AddRecords()
        {
            var recordDef = DefDatabase<RecordDef>.GetNamed(def.recordCountName, true);
            if (recordDef != null)
            {
                pawn.records.Increment(recordDef);
            }
        }

        private string GetInfoString(string verb)
        {
            var time = new DateTime().AddMinutes((double)ticks_elapsed / 2500 * 60).ToString(@"HH\:mm\:ss");
            return $"{pawn.LabelShort} {verb} for {time}";
        }

        private void AddPlayLog()
        {
            var log = new LogEntry_UseStripperPole(def.logDef, pawn);
            Find.PlayLog.Add(log);
        }

        private static GroupAnimationDef ResolveGroupDef()
        {
            var exact = DefDatabase<GroupAnimationDef>.GetNamedSilentFail(GroupAnimDefName);
            if (exact != null) return exact;

            // Fallback: try first def that looks like a stripper/pole anim
            var cand = DefDatabase<GroupAnimationDef>.AllDefsListForReading
                .FirstOrDefault(d =>
                {
                    var n = d.defName ?? string.Empty;
                    return n.IndexOf("strip", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("pole", StringComparison.OrdinalIgnoreCase) >= 0;
                });

            if (cand == null)
                Log.Warning($"[UAP] StartOnArrival: GroupAnimationDef '{GroupAnimDefName}' not found.");
            else
                Log.Warning($"[UAP] StartOnArrival: Using fallback GroupAnimationDef '{cand.defName}'. " +
                            $"Consider renaming GroupAnimDefName constant to your exact defName.");

            return cand;
        }
    }
}
