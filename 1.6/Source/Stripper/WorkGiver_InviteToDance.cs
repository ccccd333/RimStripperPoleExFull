using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace Stripper
{
    public class WorkGiver_InviteToDance : WorkGiver
    {
        public override Job NonScanJob(Pawn pawn)
        {
            if (StripperMod.settings.debugLog)
            {
                Log.Message($"[StripperPole] WorkGiver_InviteToDance NonScanJob. pawn: {pawn}");
            }
            if (!pawn.ageTracker.Adult) return null;

            if (StripperPoleHelper.IsInviteCooldownActive(out int remainingTicks))
            {
                return null;
            }

            var poles = StripperPoleHelper.GetStripperPoles(pawn.Map);
            if (poles == null) return null;
            bool owner_pole = false;
            Building_StripperPole my_pole = null;
            foreach (var p in poles) {
                var stripperPole = p as Building_StripperPole;
                if (stripperPole != null) {
                    if(stripperPole.owners.Count == 1 && stripperPole.IsOwner(pawn) && pawn.CanReserve(stripperPole))
                    {
                        owner_pole = true;
                        my_pole = stripperPole;
                    }
                }
            }

            if (!owner_pole) { return null; }

            bool isCanReach = pawn.CanReach(my_pole, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn);
            if(!isCanReach) { return null; }

            var targetGuest = Hospitality.Utilities.GuestUtility.GetAllGuests(pawn.Map)
                .Where(g => Hospitality.Utilities.GuestUtility.ViableGuestTarget(g) && pawn.CanReserve(g))
                .InRandomOrder()
                .FirstOrDefault();
            if (targetGuest == null) return null;


            return new Job(SPJobDefOf.SP_InviteToDance, my_pole, targetGuest);
        }
    }
}
