using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;

namespace Stripper {
    public class ThinkNode_ConditionalCanUseStripperPole : ThinkNode_Conditional {
        protected override bool Satisfied(Pawn pawn) {
            if (pawn.Map == null) return false;
            if (pawn.HostileTo(Faction.OfPlayer)) return false;
            if (pawn.Drafted) return false;
            return true;
        }
    }
}
