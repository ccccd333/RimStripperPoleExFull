
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Stripper
{
    public class JobDriver_WaitForDancer : JobDriver
    {
        //private Pawn dancer => (Pawn)job.GetTarget(TargetIndex.A);

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() =>
            {
                Pawn p = job.GetTarget(TargetIndex.A).Thing as Pawn;

                return p.CurJob.def != SPJobDefOf.SP_ServingVisitor;
            });

            Toil wait = new Toil();

            wait.initAction = delegate
            {
                pawn.pather?.StopDead();
            };

            wait.tickAction = delegate
            {
                pawn.pather?.StopDead();
            };

            wait.defaultCompleteMode = ToilCompleteMode.Never;

            yield return wait;
        }


    }
}
