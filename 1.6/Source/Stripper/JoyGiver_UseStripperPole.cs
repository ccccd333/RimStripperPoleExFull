using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Stripper {
    public class JoyGiver_UseStripperPole : JoyGiver_InteractBuilding {
        protected override bool CanDoDuringGathering => true;

        protected override Job TryGivePlayJob(Pawn pawn, Thing t) {
            return JobMaker.MakeJob(def.jobDef, t);
        }

        protected override bool CanInteractWith(Pawn pawn, Thing t, bool inBed) {
            if (inBed) return false;
            if (!pawn.ageTracker.Adult) return false;
            if (!(t as Building_StripperPole).CanUse(pawn)) return false;
            return base.CanInteractWith(pawn, t, inBed);
        }
    }
}
