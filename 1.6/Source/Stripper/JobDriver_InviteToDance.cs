using Hospitality;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Stripper
{
    public class JobDriver_InviteToDance : JobDriver
    {
        private InteractionDef InteractionDef => InteractionDefOf.RecruitAttempt;

        private readonly TargetIndex StripperPoleIndex = TargetIndex.A;
        private Building_StripperPole StripperPole => (Building_StripperPole)job.GetTarget(StripperPoleIndex);
        private Pawn Guest => job.targetB.Pawn;

        private bool silentFail = false;

       // private List<Pawn> randomGuests = new List<Pawn>();
        private int indexInvitedGuests = 0;
        //private Pawn approachTarget;
        //private int nextApproachTick;
        private List<Pawn> invitedGuests = new List<Pawn>();

        public override void ExposeData()
        {
            base.ExposeData();

            //Scribe_Collections.Look(ref randomGuests, "randomGuests", LookMode.Reference);
            Scribe_Collections.Look(ref invitedGuests, "invitedGuests", LookMode.Reference);
            Scribe_Values.Look<int>(ref indexInvitedGuests, "indexInvitedGuests", 0, false);
            Scribe_Values.Look<bool>(ref silentFail, "silentFail", false, false);
            //Scribe_Values.Look<int>(ref nextApproachTick, "nextApproachTick", 0, false);

            //if (randomGuests == null)
            //{
               // randomGuests = new List<Pawn>();
            //}

            if(invitedGuests == null)
            {
                invitedGuests = new List<Pawn>();
            }
        }

        public override string GetReport()
        {
            if (CurToilIndex <= 0)
                return "SP_Invite_SearchingGuest".Translate();

            if (CurToilIndex == 1)
                return "SP_Invite_SelectingGuest".Translate();

            if (CurToilIndex == 2)
                return "SP_Invite_ApproachingGuest".Translate();

            if (CurToilIndex == 3)
                return "SP_Invite_NegotiatingGuest".Translate();

            if (CurToilIndex == 4)
                return "SP_Invite_GuidingToDanceSpot".Translate();

            if (CurToilIndex == 5)
                return "SP_Invite_WaitingForGuest".Translate();

            if (CurToilIndex >= 6)
                return "SP_Invite_StartingDance".Translate();

            return base.GetReport();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(Guest, job))
            {
                
                return false;
            }
            Log.Message($"[StripperPole] JobDriver_InviteToDance TryMakePreToilReservations Invite TargetB: {Guest}");

            return pawn.Reserve(StripperPole, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //if (randomGuests.NullOrEmpty())
            //{
            //    // このチェックはセーブ→ロード時の対応
            //    // ExposeDataでrandomGuestsを復元したのにこのチェックがなければまた取ってきて
            //    // job.targetBが指しているものと異なってしまう
            //    // job.targetB = randomGuests[indexInvitedGuests]ここも
            //    // セーブ→ロード時MakeNewToilsが呼ばれるので、
            //    // target.initActionを通った後だとindexInvitedGuests = 1でjob.targetBがindexInvitedGuests = 0の箇所を指している。
            //    // これがロード時処理を復帰しようにも、このifに入れないとjob.targetBがindexInvitedGuests = 1の箇所を指してしまう
            //    randomGuests = Hospitality.Utilities.GuestUtility.GetAllGuests(pawn.Map)
            //        .Where(g => Hospitality.Utilities.GuestUtility.ViableGuestTarget(g))
            //        .InRandomOrder()
            //        .Take(StripperMod.settings.maxGuestsToInvite)
            //        .ToList();

            //    if (indexInvitedGuests >= randomGuests.Count)
            //    {
            //        EndJobWith(JobCondition.Incompletable);
            //        yield break;
            //    }

            //    //if (randomGuests.Count() > 0)
            //    //{
            //    //    job.targetB = randomGuests[indexInvitedGuests];
            //    //}
            //}

            //randomGuests = Hospitality.Utilities.GuestUtility.GetAllGuests(pawn.Map)
            //                         .InRandomOrder()
            //                         .Take(limit)
            //                         .ToList();


            //if (randomGuests.Count() > 0)
            {
                StripperPoleHelper.IsSearchingForCustomer = true;
                this.AddFinishAction(condition =>
                {
                    StripperPoleHelper.IsSearchingForCustomer = false;

                    if ((condition == JobCondition.Incompletable || condition == JobCondition.ErroredPather) && job.playerForced)
                    {
                        Messages.Message(
                            "SP_CRM_NoGuests".Translate(),
                            MessageTypeDefOf.RejectInput,
                            false);
                    }

                    if (StripperMod.settings.debugLog)
                    {
                        Log.Message(
                            $"[StripperPole] InviteToDance finished: {condition}");
                    }
                });


                if (StripperMod.settings.debugLog)
                {
                    Log.Message($"[StripperPole] JobDriver_InviteToDance MakeNewToils targetB(Guest): {job.targetB.Pawn}");
                }
                this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
                this.FailOnDowned(TargetIndex.B);
                this.FailOnNotCasualInterruptible(TargetIndex.B);
                this.FailOn(() =>
                {
                    var guest = job.targetB.Thing as Pawn;
                    var lord = guest?.GetLord();
                    //if (StripperMod.settings.debugLog)
                    //{
                    //    Log.Message($"[StripperPole] JobDriver_InviteToDance FailOn randomGuests.Count() <= indexInvitedGuests: {randomGuests.Count() <= indexInvitedGuests} guest: {guest} guest.Destroyed: {guest.Destroyed} guest.Dead: {guest.Dead} lord: {lord} LordJob_VisitColony?: {!(lord.LordJob is Hospitality.LordJob_VisitColony)}");
                    //}

                    return guest == null
                        || guest.Destroyed
                        || guest.Dead
                        || lord == null
                        || !(lord.LordJob is Hospitality.LordJob_VisitColony);
                });

                // うろうろ
                Toil wander = ToilMaker.MakeToil();
                wander.initAction = () =>
                {
                    pawn.jobs.curJob.locomotionUrgency = LocomotionUrgency.Walk;

                    if (indexInvitedGuests > 0)
                    {
                        var oldGuest = Guest;
                        if (StripperMod.settings.debugLog)
                        {
                            Log.Message($"[StripperPole] JobDriver_InviteToDance oldGuest: {oldGuest}");
                        }
                        if (pawn.Map.reservationManager.ReservedBy(oldGuest, pawn, job))
                        {
                            pawn.Map.reservationManager.Release(oldGuest, pawn, job);
                        }

                        invitedGuests.Add(oldGuest);


                        if (indexInvitedGuests >= StripperMod.settings.maxGuestsToInvite)
                        {
                            EndJobWith(JobCondition.Incompletable);
                            return;
                        }

                        var targetGuest = Hospitality.Utilities.GuestUtility.GetAllGuests(pawn.Map)
                            .Where(g => Hospitality.Utilities.GuestUtility.ViableGuestTarget(g)
                                     && !invitedGuests.Contains(g) && pawn.CanReserve(g))
                            .InRandomOrder()
                            .Take(StripperMod.settings.maxGuestsToInvite)
                            .FirstOrDefault();

                        if (targetGuest == null)
                        {
                            EndJobWith(JobCondition.Incompletable);
                            return;
                        }

                        job.targetB = targetGuest;

                        if (pawn.Reserve(Guest, job))
                        {
                            if (StripperMod.settings.debugLog)
                            {
                                Log.Message($"[StripperPole] JobDriver_InviteToDance Reserved new TargetB: {Guest}");
                            }
                        }
                        else
                        {
                            EndJobWith(JobCondition.Incompletable);
                            return;
                        }
                    }

                    ++indexInvitedGuests;


                    //    while (indexInvitedGuests < randomGuests.Count)
                    //    {
                    //        Pawn potentialGuest = randomGuests[indexInvitedGuests];

                    //        if (potentialGuest != null &&
                    //            !potentialGuest.Destroyed &&
                    //            Hospitality.Utilities.GuestUtility.ViableGuestTarget(potentialGuest) &&
                    //            pawn.CanReserve(potentialGuest))
                    //        {
                    //            job.targetB = potentialGuest;

                    //            if (pawn.Reserve(potentialGuest, job))
                    //            {
                    //                guestReserved = true;
                    //                indexInvitedGuests++;

                    //                if (StripperMod.settings.debugLog)
                    //                {
                    //                    Log.Message($"[StripperPole] JobDriver_InviteToDance Reserved new TargetB: {potentialGuest}");
                    //                }
                    //                break;
                    //            }
                    //        }

                    //        indexInvitedGuests++;
                    //    }

                    //if (!guestReserved)
                    //{
                    //    EndJobWith(JobCondition.Incompletable);
                    //    return;
                    //}
                };
                wander.tickAction = () =>
                {
                    if (!pawn.pather.Moving)
                    {
                        IntVec3 dest = CellFinder.RandomClosewalkCellNear(
                            StripperPole.Position,
                            pawn.Map,
                            3
                        );

                        pawn.pather.StartPath(dest, PathEndMode.OnCell);
                    }
                };

                wander.defaultCompleteMode = ToilCompleteMode.Delay;
                wander.defaultDuration = StripperMod.settings.InviteWanderTicks;
                yield return wander;

                // 客の対象を決定
                var target = new Toil();
                target.defaultCompleteMode = ToilCompleteMode.Instant;
                target.initAction = () =>
                {
                    pawn.jobs.curJob.locomotionUrgency = LocomotionUrgency.Jog;
                    if (StripperMod.settings.debugLog)
                    {
                        float bperchance = StripperPoleHelper.GetInviteChance(pawn) * 100.0f;
                        Log.Message($"[StripperPole] JobDriver_InviteToDance target(Toil) initAction targetB: {job.targetB.Pawn} Chance: {bperchance}");
                    }


                    //if (StripperMod.settings.debugLog)
                    //{
                    //    Log.Message($"[StripperPole] JobDriver_InviteToDance doDance(Toil) Interact ViableGuestTarget: {Hospitality.Utilities.GuestUtility.ViableGuestTarget(Guest)}");
                    //    bool notDowned = !Guest.Downed;
                    //    bool awake = false || Guest.Awake();
                    //    bool noDismissiveThought = !Guest.needs.mood.thoughts.memories.Memories.Any((Thought_Memory t) => t.def.defName == "GuestDismissiveAttitude");
                    //    bool notInTherapy = !Hospitality.Utilities.GuestUtility.IsInTherapy(Guest);

                    //    bool notTired = !Hospitality.Utilities.GuestUtility.IsTired(Guest);
                    //    bool notEating = !(Guest.CurJobDef == JobDefOf.Ingest);

                    //    Log.Message(
                    //        $"guest={Guest} " +
                    //       // $"IsArrivedGuest={arrivedGuest} " +
                    //        $"NotDowned={notDowned} " +
                    //        $"Awake={awake} " +
                    //        $"NoDismissiveThought={noDismissiveThought} " +
                    //        $"NotInTherapy={notInTherapy} " +
                    //        $"NotTired={notTired} " +
                    //        $"NotEating={notEating}");
                    //}
                };
                yield return target;

                // 客に向かっていく
                var gotoGuest = GotoGuest(pawn, Guest);
                yield return gotoGuest;
                //yield return Toils_General.Wait(10).JumpIf(() => !CanInteract(pawn, Guest), gotoGuest);

                yield return Toils_General.Wait(10);
                yield return Toils_Jump.JumpIf(gotoGuest, () => !CanInteract(pawn, Guest));

                //var gotoGuest = GotoGuest(pawn, Guest);

                //gotoGuest.AddPreInitAction(() =>
                //{
                //    Log.Message(
                //        $"[StripperPole] Start GotoGuest " +
                //        $"pawnPos={pawn.Position} " +
                //        $"guestPos={Guest.Position}");
                //});

                //gotoGuest.AddFinishAction(() =>
                //{
                //    Log.Message(
                //        $"[StripperPole] End GotoGuest " +
                //        $"pawnPos={pawn.Position} " +
                //        $"guestPos={Guest.Position}");
                //});

                //yield return gotoGuest;


                //yield return Toils_General.Wait(10).JumpIf(() =>
                //{
                //    bool canInteract = CanInteract(pawn, Guest);

                //    Log.Message(
                //        $"[StripperPole] JumpIf canInteract={canInteract} " +
                //        $"pawn={pawn} guest={Guest} " +
                //        $"pawnPos={pawn.Position} guestPos={Guest.Position}");

                //    return !canInteract;
                //}, gotoGuest);

                yield return Interact(Guest, InteractionDef, Hospitality.Utilities.GuestUtility.InteractIntervalAbsoluteMin);

                //yield return Toils_Interpersonal.SetLastInteractTime(TargetIndex.B);

                // 交渉確率未満だったらうろうろ
                //yield return Toils_General.Wait(10).JumpIf(() => !Rand.Chance(StripperPoleHelper.GetInviteChance(pawn)), wander);
                yield return Toils_General.Wait(10);

                yield return Toils_Jump.JumpIf(wander, () => !Rand.Chance(StripperPoleHelper.GetInviteChance(pawn)));

                // 客側から移動ジョブ生成
                var goDanceCell = new Toil();
                goDanceCell.defaultCompleteMode = ToilCompleteMode.Instant;
                goDanceCell.initAction = () =>
                {
                    if (StripperPoleHelper.TryFindBestWatchCell(StripperPole, pawn, out var result, out var chair))
                    {
                        job.targetC = result;
                        Guest.jobs.StartJob(JobMaker.MakeJob(
                                SPJobDefOf.SP_GoToDanceCellAndWait,
                                job.targetC.Cell,
                                pawn
                            ), JobCondition.InterruptForced);
                    }
                    else
                    {
                        Log.Warning($"[StripperPole] Failed to find dance cell. " + $"Guest={Guest} " + $"Pole={StripperPole.Position} " + $"Map={pawn.Map}");
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    //job.targetC = CellFinder.RandomClosewalkCellNear(
                    //    StripperPole.Position,
                    //    pawn.Map,
                    //    2
                    //);

                    if (StripperMod.settings.debugLog)
                    {
                        Log.Message($"[StripperPole] JobDriver_InviteToDance goDanceCell(Toil) initAction targetC(Cell): {job.targetC.Cell}");
                    }


                };

                yield return goDanceCell;

                yield return Toils_Goto.GotoCell(
                    TargetIndex.C,
                    PathEndMode.OnCell
                );

                Toil waitTogether = new Toil();
                waitTogether.defaultCompleteMode = ToilCompleteMode.Delay;
                waitTogether.initAction = delegate
                {
                    ticksLeftThisToil = 5000;
                };
                waitTogether.tickIntervalAction = delegate (int delta)
                {
                    pawn.GainComfortFromCellIfPossible(delta);
                    if (Guest.CurJobDef != SPJobDefOf.SP_GoToDanceCellAndWait)
                    {
                        Log.Message($"[StripperPole] Guest lost dance job. Current={Guest.CurJobDef}");
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }
                    if (pawn.Position.DistanceTo(Guest.Position) <= 1f)
                    {
                        if (StripperMod.settings.debugLog)
                        {
                            Log.Message($"[StripperPole] JobDriver_InviteToDance waitTogether(Toil) tickIntervalAction targetC(Cell): {job.targetC.Cell}");
                        }
                        ReadyForNextToil();
                    }
                };
                yield return waitTogether;


                var doDance = new Toil();
                doDance.defaultCompleteMode = ToilCompleteMode.Instant;
                doDance.initAction = () =>
                {
                    StripperPole.currentDancer = pawn;
                    pawn.jobs.StartJob(JobMaker.MakeJob(
                            SPJobDefOf.UseStripperPole,
                            StripperPole,Guest
                        ), JobCondition.InterruptForced);

                    Guest.jobs.StartJob(JobMaker.MakeJob(SPJobDefOf.WatchStripperPole, StripperPole, Guest.Position, pawn), JobCondition.InterruptForced);
                    StripperPoleHelper.LastInviteToDance = Find.TickManager.TicksGame;

                    if (StripperMod.settings.debugLog)
                    {
                        Log.Message($"[StripperPole] JobDriver_InviteToDance doDance(Toil) initAction targetA(Poll): {StripperPole} targetB(Guest.Pos): {Guest.Position} targetC(Dancer): {pawn}");
                    }
                };

                yield return doDance;
            }
            //else
            //{
            //    Messages.Message("SP_Invite_NoGuest".Translate(), MessageTypeDefOf.RejectInput, false);

            //    EndJobWith(JobCondition.Incompletable);
            //    yield break;
            //}


        }

        private Toil Interact(Pawn talkee, InteractionDef intDef, int duration)
        {
            var toil = new Toil
            {
                initAction = () =>
                {
                    if (StripperMod.settings.debugLog)
                    {
                        Log.Message($"[StripperPole] JobDriver_InviteToDance doDance(Toil) Interact talkee(Dancer): {talkee}");
                    }

                    PawnUtility.ForceWait(talkee, duration, pawn);
                    //TargetThingB = pawn;
                    //MoteMaker.MakeInteractionBubble(pawn, talkee, intDef.interactionMote, intDef.GetSymbol(pawn.Faction, pawn.Ideo), intDef.GetSymbolColor(pawn.Faction));
                },
                socialMode = RandomSocialMode.Off,
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = duration
            };
            return toil.WithProgressBarToilDelay(TargetIndex.B);
        }


        private Toil GotoGuest(Pawn pawn, Pawn talkee, bool mayBeSleeping = false)
        {
            var toil = Toils_Interpersonal.GotoInteractablePosition(TargetIndex.B);
            toil.AddFailCondition(() => !Hospitality.Utilities.GuestUtility.ViableGuestTarget(talkee, mayBeSleeping));
            return toil;
        }

        private bool CanInteract(Pawn pawn, Pawn talkee)
        {
            if (StripperMod.settings.debugLog)
            {
                //var reserver = talkee.Map.reservationManager.FirstRespectedReserver(talkee, pawn);
                //Log.Message(
                //    $"talkee:{talkee} reserver:{reserver?.Name?.ToStringShort ?? "null"}");
                Log.Message($"[StripperPole] JobDriver_InviteToDance CanInteract talkee(Dancer): {talkee} IsBusy: {IsBusy(talkee)} FirstRespectedReserver: {talkee.Map.reservationManager.FirstRespectedReserver(talkee, pawn) == pawn}");
            }

            // Log.Message($"{pawn.NameShortColored} can interact with {talkee.NameShortColored}? IsBusy = {IsBusy(talkee)} FirstRespectedReserver = {talkee.Map.reservationManager.FirstRespectedReserver(talkee, pawn)?.NameShortColored}");
            if (IsBusy(talkee)) return false;
            if (talkee.Map.reservationManager.FirstRespectedReserver(talkee, pawn) == pawn) return true;
            return false;
        }

        private bool IsBusy(Pawn p)
        {
            // Non-suspendable job? We're busy!
            if (p.CurJob?.def.suspendable == false) return true;
            if (p.CurJob?.def.casualInterruptible == false) return true;

            return p.interactions.InteractedTooRecentlyToInteract() || Hospitality.Utilities.GuestUtility.IsInTherapy(p);
        }
    }
}
