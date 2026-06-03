using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Stripper {

	public class Building_StripperPole_Def : ThingDef {
        public int inspectOwnerDisplayCount = 3;
        public string jobName = "UseStripperPole";
        public float watchRadius = 5;
        public float joyGainFactor = 1;
    }

    public class Building_StripperPole : Building {

        public new Building_StripperPole_Def def => (Building_StripperPole_Def)base.def;
        public List<Pawn> owners => GetComp<CompAssignableToPawn>().AssignedPawnsForReading;

        public string lastDanceInfo = "";
        public string currentDanceInfo = "";
        public Pawn currentDancer;


        public bool IsOwner(Pawn pawn) {
			return owners.Contains(pawn);
		}

		public bool CanUse(Pawn pawn) {
			if (IsOwner(pawn)) return true;
			if (owners.Count <= 0) return true;
			return false;
		}

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look<string>(ref lastDanceInfo, "lastDanceInfo", "", false);
            Scribe_Values.Look<string>(ref currentDanceInfo, "currentDanceInfo", "", false);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad) {
            base.SpawnSetup(map, respawningAfterLoad);
            StripperPoleHelper.Register(this);
        }

        public override string GetInspectString() {
            var sb = new StringBuilder();
            sb.Append(base.GetInspectString());
            if(sb.Length > 0) sb.AppendLine();

            sb.Append((owners.Count <= 1 ? "Owner" : "Owners").Translate() + ": ");
            if (owners.Count < 1) sb.Append("Nobody".Translate());
            else {
                var dc = def.inspectOwnerDisplayCount;
                sb.Append(string.Join(", ", owners.Take(dc).Select(e => e.LabelShort)));
                if (owners.Count > dc) {
                    sb.Append($" (+{owners.Count - dc})");
                }
            }
            sb.AppendLine();

            if (!lastDanceInfo.NullOrEmpty()) sb.AppendLine(lastDanceInfo);
            else if (!currentDanceInfo.NullOrEmpty()) sb.AppendLine(currentDanceInfo);

            return sb.ToString().TrimEndNewlines();
        }

        public JobDef GetJobDef() {
            return DefDatabase<JobDef>.GetNamed(def.jobName, true);
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn) {
            if (selPawn.Faction != Faction.OfPlayer) yield break;

            foreach (var next in base.GetFloatMenuOptions(selPawn)) {
                yield return next;
            }
            if (!selPawn.CanReserve(this, 1, -1, null, false)) {
                yield return new FloatMenuOption("Reserved", null, MenuOptionPriority.Default, null, null, 0.0f, null, null);
            }
            else if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn)) {
                yield return new FloatMenuOption("No path", null, MenuOptionPriority.Default, null, null, 0.0f, null, null);
            }
            else if (!CanUse(selPawn)) {
                yield return new FloatMenuOption("Owned by someone else", null, MenuOptionPriority.Default, null, null, 0.0f, null, null);
            }
            else if (!selPawn.ageTracker.Adult) {
                yield return new FloatMenuOption("Not old enough", null, MenuOptionPriority.Default, null, null, 0.0f, null, null);
            }
            else {
                Action doIt = () => {
                    selPawn.drafter.Drafted = false;
                    Job job = new Job(GetJobDef(), this);
                    if (job == null) return;
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.SatisfyingNeeds);
                };

                yield return new FloatMenuOption("Do a dance", doIt);
            }
        }
    }
}
