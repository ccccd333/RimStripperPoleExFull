using RimWorld;
using rjw;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Stripper
{
    class JobDriver_ServingVisitor : JobDriver_SexBaseInitiator
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Target, job, 1, 0, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            setup_ticks();

            this.FailOnDespawnedOrNull(iTarget);
            this.FailOn(() => pawn.Drafted);
            this.FailOn(() => Partner.IsFighting());

            yield return Toils_Reserve.Reserve(iTarget, 1, 0);

            int basePrice = StripperPaymentHelper.PriceOfPerformance(pawn);

            yield return Toils_Goto.GotoThing(iTarget, PathEndMode.OnCell);

            Toil waitTogether = new Toil();
            waitTogether.defaultCompleteMode = ToilCompleteMode.Delay;
            waitTogether.initAction = delegate
            {
                ticksLeftThisToil = 5000;
            };
            waitTogether.tickIntervalAction = delegate (int delta)
            {
                pawn.GainComfortFromCellIfPossible(delta);
                if (pawn.Position.DistanceTo(Partner.Position) <= 1f)
                {
                    ReadyForNextToil();
                }
            };
            yield return waitTogether;

            Toil startPartnerJob = new Toil();
            startPartnerJob.defaultCompleteMode = ToilCompleteMode.Instant;
            startPartnerJob.socialMode = RandomSocialMode.Off;
            startPartnerJob.initAction = delegate
            {
                Job gettin_loved = JobMaker.MakeJob(SPJobDefOf.SP_SexClient, pawn);
                Partner.jobs.StartJob(gettin_loved, JobCondition.InterruptForced);
            };
            yield return startPartnerJob;

            Toil sexToil = new Toil();
            sexToil.defaultCompleteMode = ToilCompleteMode.Never;
            sexToil.socialMode = RandomSocialMode.Off;
            sexToil.handlingFacing = true;
            sexToil.FailOn(() => Partner.CurJob?.def != SPJobDefOf.SP_SexClient);
            sexToil.initAction = delegate
            {
                // その場にとどまらせるためStopDead
                Partner.pather.StopDead();

                // Todo:寝るToil調べてるときの残り香、テスト用なので一応残しておく
                // 疲れ果てたとか出来たらいいな
                //Partner.jobs.curDriver.asleep = true;

                Start();

                var receiverDriver = Partner.jobs.curDriver as JobDriver_SexBaseReciever;
                if (receiverDriver != null)
                {
                    receiverDriver.Sexprops = Sexprops.GetForPartner();
                }
            };
            sexToil.AddPreTickAction(delegate
            {
                if (pawn.IsHashIntervalTick(ticks_between_hearts))
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, xxx.mote_noheart);
                }
                SexTick(pawn, Partner);

                SexUtility.reduce_rest(Partner, 1);
                SexUtility.reduce_rest(pawn, 2);
                if (ticks_left <= 0)
                    ReadyForNextToil();
            });
            sexToil.AddFinishAction(End);
            yield return sexToil;

            yield return new Toil
            {
                initAction = delegate
                {
                    SexUtility.ProcessSex(Sexprops);

                    int paid = StripperPaymentHelper.PayClientToPerformer(Partner, pawn, basePrice, null);
                    if (paid >= basePrice)
                    {
                        Messages.Message("SP_PaymentSilver".Translate(Partner, paid, pawn, basePrice), pawn, MessageTypeDefOf.NeutralEvent);
                    }
                    else if (paid > 0)
                    {
                        Messages.Message("SP_PaymentSilverPartial".Translate(Partner, paid, basePrice, pawn), pawn, MessageTypeDefOf.NeutralEvent);
                    }
                    else
                    {
                        Messages.Message("SP_PaymentSilverNone".Translate(Partner, basePrice, pawn), pawn, MessageTypeDefOf.NegativeEvent);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
