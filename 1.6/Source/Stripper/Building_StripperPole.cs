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
using rjw;
using static rjw.xxx;
using rjw.Modules.Interactions;

namespace Stripper
{

    public class Building_StripperPole_Def : ThingDef
    {
        public int inspectOwnerDisplayCount = 3;
        public string jobName = "UseStripperPole";
        public float watchRadius = 5;
        public float joyGainFactor = 1;
    }

    [StaticConstructorOnStartup]
    public class Building_StripperPole : Building
    {
        private static readonly Texture2D ServiceOnIcon = ContentFinder<Texture2D>.Get("UI/Commands/Stripper_ServiceAuto", false) ?? TexCommand.ForbidOff;
        private static readonly Texture2D ServiceSettingsIcon = ContentFinder<Texture2D>.Get("UI/Commands/Stripper_ServiceMenu", false) ?? TexButton.OpenStatsReport;

        public new Building_StripperPole_Def def => (Building_StripperPole_Def)base.def;
        public List<Pawn> owners => GetComp<CompAssignableToPawn>().AssignedPawnsForReading;

        public string lastDanceInfo = "";
        public string currentDanceInfo = "";
        public Pawn currentDancer;

        public bool autoProstitutionEnabled = false;
        public bool autoProstitutionRandom = false;
        public List<string> autoMaleInteractions = new List<string> { null, null, null };
        public List<string> autoFemaleInteractions = new List<string> { null, null, null };

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

            Scribe_Values.Look(ref autoProstitutionEnabled, "autoProstitutionEnabled", false);
            Scribe_Values.Look(ref autoProstitutionRandom, "autoProstitutionRandom", false);
            Scribe_Collections.Look(ref autoMaleInteractions, "autoMaleInteractions", LookMode.Value);
            Scribe_Collections.Look(ref autoFemaleInteractions, "autoFemaleInteractions", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (autoMaleInteractions == null) autoMaleInteractions = new List<string>();
                while (autoMaleInteractions.Count < 3) autoMaleInteractions.Add(null);
                if (autoFemaleInteractions == null) autoFemaleInteractions = new List<string>();
                while (autoFemaleInteractions.Count < 3) autoFemaleInteractions.Add(null);
            }
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
                    //Log.Message($"[StripperPole] Building_StripperPole GetFloatMenuOptions Invite TargetB: {targetGuest}");
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

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            // 売春自動化 ON/OFF
            yield return new Command_Toggle
            {
                defaultLabel = "SP_AutoProstitution_Label".Translate(),
                defaultDesc = "SP_AutoProstitution_Desc".Translate(),
                icon = ServiceOnIcon,
                isActive = () => autoProstitutionEnabled,
                toggleAction = () => autoProstitutionEnabled = !autoProstitutionEnabled
            };

            // 自動売春設定ウィンドウを開く
            yield return new Command_Action
            {
                defaultLabel = "SP_AutoProstitutionSettings_Label".Translate(),
                defaultDesc = "SP_AutoProstitutionSettings_Desc".Translate(),
                icon = ServiceSettingsIcon,
                action = () => Find.WindowStack.Add(new Dialog_AutoProstitutionSettings(this))
            };
        }

        public SexInteractionResolved TryGetAutoInteraction(Pawn dancer, Pawn customer, List<SexInteractionResolved> available)
        {
            bool debug = StripperMod.settings != null && StripperMod.settings.debugLog;

            if (debug)
            {
                string malePrefsStr = autoMaleInteractions != null ? string.Join(", ", autoMaleInteractions) : "(null)";
                string femalePrefsStr = autoFemaleInteractions != null ? string.Join(", ", autoFemaleInteractions) : "(null)";
                string availableActsStr = available != null ? string.Join(", ", available.Select(a => $"{a.Interaction?.Def?.defName}[{a.Interaction?.Sextype}]")) : "(null)";

                Log.Message($"[StripperPole AutoDiag] === Prostitution Auto Check ===");
                Log.Message($"[StripperPole AutoDiag] Pole: {this.ThingID}, Dancer: {dancer?.LabelShort}, Customer: {customer?.LabelShort} ({customer?.gender}), AutoEnabled: {autoProstitutionEnabled}");
                Log.Message($"[StripperPole AutoDiag] autoMaleInteractions: [{malePrefsStr}]");
                Log.Message($"[StripperPole AutoDiag] autoFemaleInteractions: [{femalePrefsStr}]");
                Log.Message($"[StripperPole AutoDiag] available list ({available?.Count ?? 0}): [{availableActsStr}]");
            }

            if (!autoProstitutionEnabled)
            {
                if (debug) Log.Message("[StripperPole AutoDiag] Pole auto-prostitution is OFF.");
                return null;
            }

            if (available == null || available.Count == 0)
            {
                if (debug) Log.Message("[StripperPole AutoDiag] Available list is empty.");
                return null;
            }

            List<string> prefs = null;
            if (customer.gender == Gender.Male)
            {
                prefs = autoMaleInteractions;
            }
            else if (customer.gender == Gender.Female)
            {
                prefs = autoFemaleInteractions;
            }
            else
            {
                if (debug) Log.Message($"[StripperPole AutoDiag] Unsupported customer gender: {customer.gender}");
                return null;
            }

            if (prefs == null || prefs.Count == 0)
            {
                if (debug) Log.Message("[StripperPole AutoDiag] Configured preferences list is empty.");
                return null;
            }

            if (autoProstitutionRandom)
            {
                var configuredSextypes = new HashSet<rjwSextype>();
                for (int i = 0; i < prefs.Count; i++)
                {
                    if (!string.IsNullOrEmpty(prefs[i]) && Dialog_AutoProstitutionSettings.TryResolveSextype(prefs[i], out var st))
                    {
                        configuredSextypes.Add(st);
                    }
                }

                if (configuredSextypes.Count == 0)
                {
                    if (debug) Log.Message("[StripperPole AutoDiag] Configured preferences list is empty.");
                    return null;
                }

                // 現場の利用可能リストから、設定されたジャンルに合致するものを全抽出
                var candidates = available
                    .Where(r => r.Interaction != null && configuredSextypes.Contains(r.Interaction.Sextype))
                    .ToList();

                if (candidates.Count == 0)
                {
                    if (debug) Log.Message("[StripperPole AutoDiag] None of the configured random preferences matched available acts. Falling back to dialog.");
                    return null;
                }

                // 候補が1件しかない場合はそのまま、2件以上ある場合のみランダムに1件選択
                SexInteractionResolved chosen = (candidates.Count == 1)
                    ? candidates[0]
                    : candidates.RandomElement();

                if (debug) Log.Message($"[StripperPole AutoDiag] Random match selected from {candidates.Count} candidate(s)! (TargetSextype: {chosen.Interaction?.Sextype}, MatchedDef: {chosen.Interaction?.Def?.defName})");
                return chosen;
            }

            for (int i = 0; i < prefs.Count; i++)
            {
                string rawVal = prefs[i];
                if (string.IsNullOrEmpty(rawVal))
                {
                    if (debug) Log.Message($"[StripperPole AutoDiag] Slot {i + 1} is empty.");
                    continue;
                }

                if (!Dialog_AutoProstitutionSettings.TryResolveSextype(rawVal, out var targetSextype))
                {
                    if (debug) Log.Message($"[StripperPole AutoDiag] Slot {i + 1} '{rawVal}' could not be resolved to a valid rjwSextype.");
                    continue;
                }

                // 現場の利用可能リストから、Sextype が一致するものを探す
                // RJWスコアが存在する場合は最高スコアのもの、そうでなければ先頭
                var match = available
                    .Where(r => r.Interaction?.Sextype == targetSextype)
                    .OrderByDescending(r => r.Score?.totalScore ?? 0f)
                    .FirstOrDefault();

                if (match != null)
                {
                    if (debug) Log.Message($"[StripperPole AutoDiag] Match found at Slot {i + 1}! (TargetSextype: {targetSextype}, MatchedDef: {match.Interaction?.Def?.defName})");
                    return match;
                }
                else
                {
                    if (debug) Log.Message($"[StripperPole AutoDiag] Slot {i + 1} [{targetSextype}] was NOT found in available acts.");
                }
            }

            if (debug) Log.Message("[StripperPole AutoDiag] None of the configured preferences matched available acts. Falling back to dialog.");
            return null;
        }
    }
}
