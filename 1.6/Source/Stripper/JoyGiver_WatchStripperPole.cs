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
    class JoyGiver_WatchStripperPole : JoyGiver_InteractBuilding {
        protected override bool CanDoDuringGathering => true;

        protected override Job TryGivePlayJob(Pawn pawn, Thing t) {
            //Log.Message($"[StripperPole] TryGivePlayJob pawn: {pawn.LabelShort}");
            if (StripperPoleHelper.TryFindBestWatchCell(t, pawn, out var result, out var chair)) {
                Log.Message($"[StripperPole] WatchStripperPole TryGivePlayJob MakeJob pawn: {pawn.LabelShort}");
                if (chair != null)
                {
                    // といっても今のところjob側でこの椅子がどうのの処理ないので一旦
                    return JobMaker.MakeJob(def.jobDef, t, result, chair);
                }
                else
                {
                    return JobMaker.MakeJob(def.jobDef, t, result);
                }
            }
            return null;
        }

        protected override bool CanInteractWith(Pawn pawn, Thing t, bool inBed) {
            Log.Message($"[StripperPole] CanInteractWith pawn: {pawn.LabelShort} inBed: {inBed} IsForbidden: {t.IsForbidden(pawn)} IsSociallyProper: {t.IsSociallyProper(pawn)} IsPoliticallyProper: {t.IsPoliticallyProper(pawn)} ageTracker: {pawn.ageTracker.Adult}");
            if (inBed) return false;
            if (t.IsForbidden(pawn)) return false;
            if (!t.IsSociallyProper(pawn)) return false;
            if (!t.IsPoliticallyProper(pawn)) return false;
            if (!pawn.ageTracker.Adult) return false;
            var pole = t as Building_StripperPole;
            Log.Message($"[StripperPole] CanInteractWith pawn: {pawn.LabelShort} currentDancer is null: {pole.currentDancer == null}");
            if (pole.currentDancer == null) return false;
            if (pole.currentDancer == pawn) return false;
            return true;
        }
    }
}
