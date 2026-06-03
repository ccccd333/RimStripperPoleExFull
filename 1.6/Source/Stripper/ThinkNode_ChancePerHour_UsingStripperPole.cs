using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;

namespace Stripper {
    public class ThinkNode_ChancePerHour_UsingStripperPole : ThinkNode_ChancePerHour {

        protected override float MtbHours(Pawn pawn) {
            //if (pawn_quirks.Contains("Exhibitionist"))
            return .1f;
        }

    }
}
