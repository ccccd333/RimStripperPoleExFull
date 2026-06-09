using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;

namespace Stripper {
    public class WorkGiver_UseStripperPole : WorkGiver {
        // WorkGiver_Scannerである必要ない
        // PotentialWorkThingRequest
        // PotentialWorkThingsGlobalを使う必要ないので

        public override Job NonScanJob(Pawn pawn)
        {
            if (StripperMod.settings.debugLog)
            {
                Log.Message($"[StripperPole] WorkGiver_UseStripperPole NonScanJob. pawn: {pawn}");
            }
            if (!pawn.ageTracker.Adult) return null;

            var randomGuests = Hospitality.Utilities.GuestUtility.GetAllGuests(pawn.Map)
                .Where(g => Hospitality.Utilities.GuestUtility.ViableGuestTarget(g))
                .InRandomOrder()
                .Take(StripperMod.settings.maxGuestsToInvite)
                .ToList();
            if (randomGuests.NullOrEmpty()) return null;
            var pole = StripperPoleHelper.GetStripperPoleForPawn(pawn);
            if (pole == null) return null;
            if (!pole.CanUse(pawn)) return null;

            return new Job(pole.GetJobDef(), pole);
        }
        //public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
        //    if (StripperMod.settings.debugLog)
        //    {
        //        Log.Message($"[StripperPole] WorkGiver_UseStripperPole JobOnThing. pawn: {pawn}");
        //    }
        //    var pole = t as Building_StripperPole;
        //    if (pole == null) return base.JobOnThing(pawn, t, forced);
        //    return new Job(pole.GetJobDef(), t);
        //}
    }
}
