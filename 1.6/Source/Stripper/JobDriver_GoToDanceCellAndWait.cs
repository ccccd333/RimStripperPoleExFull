using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace Stripper
{
    public class JobDriver_GoToDanceCellAndWait : JobDriver
    {
        private Pawn Dancer => job.targetB.Pawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Dancer.CurJob?.def != SPJobDefOf.SP_InviteToDance);

            yield return Toils_Goto.GotoCell(
                TargetIndex.A,
                PathEndMode.OnCell
            );

            yield return Toils_General.Wait(5000);
        }
    }
}
