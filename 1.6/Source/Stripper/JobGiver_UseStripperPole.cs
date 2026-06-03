using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;

namespace Stripper {
    public class JobGiver_UseStripperPole : ThinkNode_JobGiver {
        protected override Job TryGiveJob(Pawn pawn) {
            return this.TryGiveJob(pawn, null);
        }

        public Job TryGiveJob(Pawn pawn, Building_StripperPole pole) {
            if (pawn.Drafted) return null;
            if (!(pawn.jobs.curJob == null || pawn.jobs.curJob.def == JobDefOf.LayDown)) return null;
            if (!(pawn.Faction?.IsPlayer ?? false)) return null;

            if(pole == null) pole = StripperPoleHelper.GetStripperPoleForPawn(pawn);
            return pole == null ? null : new Job(pole.GetJobDef(), pole);
        }
    }
}
