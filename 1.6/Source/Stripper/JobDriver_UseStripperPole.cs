using RimWorld;
using Rimworld_Animations;
using rjw;
using rjw.Modules.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;
using static UnityEngine.GraphicsBuffer;

namespace Stripper {
	//public class Job_UseStripperPole_Def : JobDef {
	//	public int turnMin = 45;
	//	public int turnMax = 120;
	//	public int undressMin = 45;
	//	public int undressMax = 120;
	//	public string recordCountName = "";
	//	public LogEntry_UseStripperPole_Def logDef;
	//}

    public class JobDriver_UseStripperPole : JobDriver_StripperPoleBase
    {

        private void Init(bool loadingSave = false) {
			this.FailOnDespawnedOrNull(StripperPoleIndex);
			this.FailOn(() => pawn.Drafted);
			this.FailOn(() => pawn.IsFighting());

        }

        public override bool TryMakePreToilReservations(bool errorOnFailed) {

            if (job.targetB.IsValid)
            {
                if (StripperMod.settings.debugLog)
                {
                    Log.Message($"[StripperPole] JobDriver_InviteToDance => JobDriver_UseStripperPole");
                }

                if (!pawn.Reserve(job.targetB.Pawn, job))
                {
                    return false;
                }
            }

            if(!pawn.Reserve(StripperPole, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            //Log.Message($"UseStripperPole TryMakePreToilReservations {pawn.Reserve(StripperPole, job, 1, -1, null, errorOnFailed)}");
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils() {
            if (StripperMod.settings.debugLog)
            {
                Log.Message($"[StripperPole] JobDriver_UseStripperPole MakeNewToils. pawn: {pawn}");
            }
            Init();

            foreach (var toil in MakeDanceToils())
            {
                yield return toil;
            }
        }

    }
}
