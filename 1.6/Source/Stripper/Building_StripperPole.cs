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

namespace Stripper
{

    public class Building_StripperPole_Def : ThingDef
    {
        public int inspectOwnerDisplayCount = 3;
        public string jobName = "UseStripperPole";
        public float watchRadius = 5;
        public float joyGainFactor = 1;
    }

    public class Building_StripperPole : Building
    {

        public new Building_StripperPole_Def def => (Building_StripperPole_Def)base.def;
        public List<Pawn> owners => GetComp<CompAssignableToPawn>().AssignedPawnsForReading;

        public string lastDanceInfo = "";
        public string currentDanceInfo = "";
        public Pawn currentDancer;


        public bool IsOwner(Pawn pawn)
        {
            return owners.Contains(pawn);
        }

        public bool CanUse(Pawn pawn)
        {
            if (IsOwner(pawn)) return true;
            if (owners.Count <= 0) return true;
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref lastDanceInfo, "lastDanceInfo", "", false);
            Scribe_Values.Look<string>(ref currentDanceInfo, "currentDanceInfo", "", false);
            Scribe_References.Look(ref currentDancer, "currentDancer");
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            if (StripperMod.settings.debugLog)
            {
                Log.Message($"[StripperPole] Building_StripperPole SpawnSetup. map: {map} who?: {this}");
            }

            base.SpawnSetup(map, respawningAfterLoad);
            StripperPoleHelper.RegisterPole(map, this);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (StripperMod.settings.debugLog)
            {
                Log.Message($"[StripperPole] Building_StripperPole DeSpawn. map: {this.Map} who?: {this}");
            }

            //if (this.storageGroup != null)
            //{
            //    StorageGroup storageGroup = this.storageGroup;
            //    if (storageGroup != null)
            //    {
            //        storageGroup.RemoveMember(this, true);
            //    }
            //    this.storageGroup = null;
            //}
            //if (mode != DestroyMode.WillReplace)
            //{
            //    this.innerContainer.TryDropAll(base.Position, base.Map, ThingPlaceMode.Near, null, null, true);
            //}
            StripperPoleHelper.UnregisterPole(Map, this);

            base.DeSpawn(mode);
        }

        public override string GetInspectString()
        {
            var sb = new StringBuilder();
            sb.Append(base.GetInspectString());
            if (sb.Length > 0) sb.AppendLine();

            sb.Append((owners.Count <= 1 ? "Owner" : "Owners").Translate() + ": ");
            if (owners.Count < 1) sb.Append("Nobody".Translate());
            else
            {
                var dc = def.inspectOwnerDisplayCount;
                sb.Append(string.Join(", ", owners.Take(dc).Select(e => e.LabelShort)));
                if (owners.Count > dc)
                {
                    sb.Append($" (+{owners.Count - dc})");
                }
            }
            sb.AppendLine();

            if (!lastDanceInfo.NullOrEmpty()) sb.AppendLine(lastDanceInfo);
            else if (!currentDanceInfo.NullOrEmpty()) sb.AppendLine(currentDanceInfo);

            return sb.ToString().TrimEndNewlines();
        }

        public JobDef GetJobDef()
        {
            return DefDatabase<JobDef>.GetNamed(def.jobName, true);
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            if (selPawn.Faction != Faction.OfPlayer) yield break;

            foreach (var next in base.GetFloatMenuOptions(selPawn))
            {
                yield return next;
            }

            bool isCanReach = selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn);

            if (!selPawn.CanReserve(this, 1, -1, null, false))
            {
                yield return new FloatMenuOption("SP_Reserved".Translate(), null, MenuOptionPriority.Default, null, null, 0.0f, null, null);
            }
            else if (!isCanReach)
            {
                yield return new FloatMenuOption("SP_NoPath".Translate(), null, MenuOptionPriority.Default, null, null, 0.0f, null, null);
            }
            else if (!CanUse(selPawn))
            {
                yield return new FloatMenuOption("SP_OwnedBySomeoneElse".Translate(), null, MenuOptionPriority.Default, null, null, 0.0f, null, null);
            }
            else if (!selPawn.ageTracker.Adult)
            {
                yield return new FloatMenuOption("SP_NotOldEnough".Translate(), null, MenuOptionPriority.Default, null, null, 0.0f, null, null);
            }
            else
            {
                Action doIt = () =>
                {
                    selPawn.drafter.Drafted = false;
                    Job job = new Job(GetJobDef(), this);
                    if (job == null) return;
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.SatisfyingNeeds);
                };

                yield return new FloatMenuOption("SP_DoADance".Translate(), doIt);
            }

            // ストリップダンスするターゲットを見つけるジョブを起動する
            if (owners.Count != 1 || !IsOwner(selPawn))
            {
                // ジョブ中にうろうろするので占有状態でないと困るので
                yield return new FloatMenuOption("SP_CRM_NoPoleOwner".Translate(), null, MenuOptionPriority.Default, null, null, 0.0f, null, null);
            }
            else if (!selPawn.ageTracker.Adult)
            {
                yield return new FloatMenuOption("SP_CRM_NotOldEnough".Translate(), null, MenuOptionPriority.Default, null, null, 0.0f, null, null);
            }
            else if (!isCanReach)
            {
                // そもそも到達不可能なポールの場合
                yield return new FloatMenuOption("SP_CRM_NoPath".Translate(), null, MenuOptionPriority.Default, null, null, 0.0f, null, null);
            }
            else
            {
                //bool hasVisitGuest = Hospitality.Utilities.GuestUtility.GetAllGuests(selPawn.Map).Any();
                //bool hasVisitGuest = Hospitality.Utilities.GuestUtility.GetAllGuests(selPawn.Map)
                //    .Any(g => Hospitality.Utilities.GuestUtility.ViableGuestTarget(g));

                var targetGuest = Hospitality.Utilities.GuestUtility.GetAllGuests(selPawn.Map)
                    .Where(g => Hospitality.Utilities.GuestUtility.ViableGuestTarget(g) && selPawn.CanReserve(g))
                    .InRandomOrder()
                    .FirstOrDefault();

                if (targetGuest == null)
                {
                    yield return new FloatMenuOption("SP_CRM_NoGuests".Translate(), null);
                }
                else if (StripperPoleHelper.IsSearchingForCustomer)
                {
                    yield return new FloatMenuOption("SP_CRM_IsSearchingForCustomer".Translate(), null, MenuOptionPriority.Default, null, null, 0.0f, null, null);

                }
                else if (StripperPoleHelper.IsInviteCooldownActive(out int remainingTicks))
                {
                    yield return new FloatMenuOption("SP_CRM_IsInviteCooldownActive".Translate(remainingTicks.ToStringTicksToPeriod()), null);
                }
                else
                {
                    Log.Message($"[StripperPole] Building_StripperPole GetFloatMenuOptions Invite TargetB: {targetGuest}");
                    Action doIt = () =>
                    {
                        selPawn.drafter.Drafted = false;
                        Job job = new Job(SPJobDefOf.SP_InviteToDance, this, targetGuest);
                        if (job == null) return;
                        job.playerForced = true;
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.SatisfyingNeeds);
                    };
                    yield return new FloatMenuOption("SP_CRM_DoApproach".Translate(), doIt);
                }

                //if (!hasVisitGuest)
                //{
                //    yield return new FloatMenuOption("SP_CRM_NoGuests".Translate(), null, MenuOptionPriority.Default, null, null, 0.0f, null, null);
                //}
                //else if (StripperPoleHelper.IsSearchingForCustomer)
                //{
                //    yield return new FloatMenuOption("SP_CRM_IsSearchingForCustomer".Translate(), null, MenuOptionPriority.Default, null, null, 0.0f, null, null);

                //}
                //else if (StripperPoleHelper.IsInviteCooldownActive(out int remainingTicks))
                //{
                //    yield return new FloatMenuOption("SP_CRM_IsInviteCooldownActive".Translate(remainingTicks.ToStringTicksToPeriod()), null);
                //}
                //else
                //{
                //    Action doIt = () =>
                //    {
                //        selPawn.drafter.Drafted = false;
                //        Job job = new Job(SPJobDefOf.SP_InviteToDance, this);
                //        if (job == null) return;
                //        job.playerForced = true;
                //        selPawn.jobs.TryTakeOrderedJob(job, JobTag.SatisfyingNeeds);
                //    };
                //    yield return new FloatMenuOption("SP_CRM_DoApproach".Translate(), doIt);
                //}
            }
        }
    }
}
