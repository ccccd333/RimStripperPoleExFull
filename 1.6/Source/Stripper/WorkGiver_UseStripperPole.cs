using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;

namespace Stripper {
    public class WorkGiver_UseStripperPole : WorkGiver_Scanner {
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
            var pole = t as Building_StripperPole;
            if (pole == null) return base.JobOnThing(pawn, t, forced);
            return new Job(pole.GetJobDef(), t);
        }
    }
}
