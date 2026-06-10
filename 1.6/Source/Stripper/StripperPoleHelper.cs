using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using static rjw.xxx;

namespace Stripper
{
    public static class StripperPoleHelper
    {

        //public static HashSet<ThingDef> registeredDefs = new HashSet<ThingDef>();
        private static Dictionary<Map, HashSet<Building_StripperPole>> registeredPoles = new Dictionary<Map, HashSet<Building_StripperPole>>();
        private static Dictionary<Pawn, HashSet<Pawn>> dancerWatcherMap = new Dictionary<Pawn, HashSet<Pawn>>();
        private static bool isSearchingForCustomer = false;
        private static int lastInviteToDance = 0;

        public static bool IsSearchingForCustomer
        {
            get { return isSearchingForCustomer; }
            set { isSearchingForCustomer = value; }
        }

        public static int LastInviteToDance
        {
            get { return lastInviteToDance; }
            set { lastInviteToDance = value; }
        }

        public static void ExposeData()
        {
            Scribe_Values.Look(ref isSearchingForCustomer, "isSearchingForCustomer", false);
            Scribe_Values.Look(ref lastInviteToDance, "lastInviteToDance", 0);
        }

        public static int GetRemainingTicks()
        {
            return Math.Max(0, (lastInviteToDance + StripperMod.settings.inviteCooldownSeconds) - Find.TickManager.TicksGame);
        }

        public static bool IsInviteCooldownActive(out int remainingTicks)
        {
            remainingTicks = GetRemainingTicks();

            return remainingTicks > 0;
        }

        public static void ClearAvailableProstitutes()
        {
            dancerWatcherMap.Clear();
        }

        public static void RegisterAvailableProstitute(Pawn dancer, Pawn watcher)
        {

            if (dancer == null || watcher == null) return;

            if (!dancerWatcherMap.ContainsKey(dancer))
            {
                dancerWatcherMap[dancer] = new HashSet<Pawn>();
            }

            dancerWatcherMap[dancer].Add(watcher);
        }

        public static void UnregisterDancer(Pawn dancer)
        {
            if (dancer == null) return;

            if (dancerWatcherMap.ContainsKey(dancer))
            {
                dancerWatcherMap.Remove(dancer);
            }
        }

        public static Pawn GetRandomAvailableProstitute(Pawn dancer)
        {
            if (dancer == null) return null;

            if (dancerWatcherMap.TryGetValue(dancer, out var watchers) && watchers != null)
            {
                if (StripperMod.settings.debugLog)
                {
                    foreach (var a in watchers)
                    {
                        Log.Message($"[StripperPole] StripperPoleHelper.GetRandomAvailableProstitute. pawn: {a} curJob {a.jobs?.curDriver}");
                    }
                }

                var validWatchers = watchers
                            .Where(w => w != null && w.Spawned && !w.Dead && !w.Downed)
                            .Where(w =>
                            {
                                var watchDriver = w.jobs?.curDriver as JobDriver_WatchStripperPole;

                                // 観客がダンス終了後、予約したポーンを見続けているか(別ジョブ中だといやなので)
                                // 以下エラー対応
                                //Could not reserve Thing_Human32341 (layer: null) for Amelia for job SP_ServingVisitor (Job_14061) A = Thing_Human32341 (now doing job SP_ServingVisitor (Job_14061) A = Thing_Human32341(curToil=-1)) for maxPawns 1 and stackCount 0.
                                //Existing reservers:
                                //   [0] Natsuki (job: SP_ServingVisitor (Job_13998) A = Thing_Human32341, toil: 1, maxPawns: 1, stackCount: 0)
                                return watchDriver != null && watchDriver.dancer == dancer;
                            })
                            .ToList();

                if (validWatchers.Count > 0)
                {
                    return validWatchers.RandomElement();
                }
            }
            return null;
        }

        public static void DistributeDancePayout(Pawn dancer)
        {
            if (dancer == null || !dancer.Spawned) return;

            if (!dancerWatcherMap.TryGetValue(dancer, out HashSet<Pawn> watchers) || watchers == null || watchers.Count == 0)
            {
                return;
            }

            int basePayout = StripperPaymentHelper.PriceOfPerformance(dancer, StripperMod.settings.baseDancePrice, StripperMod.settings.danceBeautyMultiplier);

            int totalSilverToDrop = 0;

            foreach (Pawn watcher in watchers)
            {
                if (watcher == null || watcher.Dead || !watcher.Spawned) continue;

                if (watcher.IsColonist) continue;

                var watchDriver = watcher.jobs?.curDriver as JobDriver_WatchStripperPole;
                if (watchDriver == null) continue;
                if (watchDriver.dancer != dancer) continue;

                totalSilverToDrop += StripperPaymentHelper.PayClientToPerformer(watcher, dancer, basePayout, watchers, StripperMod.settings.spawnSilverForDance);
            }

            if (totalSilverToDrop > 0)
            {
                Messages.Message("SP_DistributeDancePayout".Translate(dancer, watchers.Count, totalSilverToDrop), dancer, MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                Messages.Message("SP_MessageDancePayout_None".Translate(dancer), dancer, MessageTypeDefOf.NeutralEvent);

            }
        }

        public static void RegisterPole(Map m, Building_StripperPole pole)
        {
            if (!registeredPoles.ContainsKey(m))
                registeredPoles[m] = new HashSet<Building_StripperPole>();

            registeredPoles[m].Add(pole);
        }

        public static void UnregisterPole(Map m, Building_StripperPole pole)
        {
            if (registeredPoles.TryGetValue(m, out var poles))
            {
                poles.Remove(pole);
                if (poles.Count == 0) registeredPoles.Remove(m);
            }
        }

        public static void ClearAllPoles()
        {
            registeredPoles.Clear();
        }

        public static float GetInviteChance(Pawn pawn)
        {
            float beauty = pawn.GetStatValue(StatDefOf.PawnBeauty);

            float chance = StripperMod.settings.inviteBaseChance *
                           Mathf.Pow(StripperMod.settings.inviteBeautyMultiplier, beauty);

            return Mathf.Clamp01(chance);
        }

        //public static List<Thing> GetStripperPoles(Map map) {


        //    if (map != null && registeredDefs.TryGetValue(map, out var poles))
        //    {
        //        return poles.Count > 0 ? poles : null;
        //    }
        //    return null;
        //}

        public static IEnumerable<Thing> GetStripperPoles(Map map)
        {
            if (map != null && registeredPoles.TryGetValue(map, out var poles))
            {
                if (StripperMod.settings.debugLog)
                {
                    Log.Message($"[StripperPole] Found {poles.Count} managed poles on map: {map}");
                }
                return poles.Count > 0 ? poles : null;
            }
            return null;
        }

        public static Building_StripperPole GetStripperPoleForPawn(Pawn pawn)
        {
            if (!pawn.ageTracker.Adult) return null;
            var poles = GetStripperPoles(pawn.Map);
            if (poles == null) return null;

            bool validator(Thing t)
            {
                if (!(t as Building_StripperPole).CanUse(pawn)) return false;
                if (!pawn.CanReserve(t, 1, -1, null, false)) return false;
                if (t.IsForbidden(pawn)) return false;
                if (!t.IsSociallyProper(pawn)) return false;
                return true;
            }

            Func<Thing, float> priorityGetter = (t =>
            {
                float p = 1f;

                if ((t as Building_StripperPole).IsOwner(pawn))
                {
                    p *= 1.15f;
                }

                return p;
            });

            return (Building_StripperPole)GenClosest.ClosestThing_Global_Reachable(
                pawn.Position,
                pawn.Map,
                poles,
                PathEndMode.OnCell,
                TraverseParms.For(pawn, Danger.Some, TraverseMode.ByPawn, false),
                300f,
                validator,
                priorityGetter);
        }


        public static IEnumerable<IntVec3> WatchCells(ThingDef def, IntVec3 center, Map map)
        {
            if (def == null) throw new Exception("missing def");
            var poleDef = def as Building_StripperPole_Def;
            if (poleDef == null) throw new Exception("def is not a pole");
            return GenRadial.RadialCellsAround(center, poleDef.watchRadius, false)
                .Where(e => EverPossibleToWatchFrom(e, center, map));
        }

        private static bool EverPossibleToWatchFrom(IntVec3 watchCell, IntVec3 buildingCenter, Map map)
        {
            if (!watchCell.InBounds(map)) return false;
            if (!watchCell.Standable(map)) return false;
            Room room = buildingCenter.GetRoom(map);
            if (room != null && !room.ContainsCell(watchCell)) return false;
            return GenSight.LineOfSight(buildingCenter, watchCell, map, skipFirstCell: true);
        }

        public static bool TryFindBestWatchCell(Thing t, Pawn pawn, out IntVec3 result, out Building chair)
        {
            var cells = WatchCells(t.def, t.Position, t.Map).ToList();
            cells.Shuffle();
            // 元仕様だと椅子がない場合はJobが起動しないため立見も追加する
            foreach (var next in cells)
            {
                if (next.IsForbidden(pawn)) continue;
                if (!pawn.CanReserveSittableOrSpot(next)) continue;
                if (!pawn.Map.pawnDestinationReservationManager.CanReserve(next, pawn)) continue;

                var building = next.GetEdifice(pawn.Map);

                if (building != null && building.def.building.isSittable)
                {
                    if (pawn.CanReserve(building))
                    {
                        result = next;
                        chair = building;
                        return true;
                    }
                }
                else
                {
                    if (building == null || building.def.passability != Traversability.Impassable)
                    {
                        result = next;
                        chair = null;
                        return true;
                    }
                }
            }
            result = IntVec3.Invalid;
            chair = null;
            return false;
        }

        public static float GetProstitutePartsMult(rjwSextype sextype)
        {
            switch (sextype)
            {
                case rjwSextype.Vaginal:
                    return StripperMod.settings.prostitutePartsVaginal;
                case rjwSextype.Anal:
                    return StripperMod.settings.prostitutePartsAnal;
                case rjwSextype.Oral:
                    return StripperMod.settings.prostitutePartsOral;
                case rjwSextype.DoublePenetration:
                    return StripperMod.settings.prostitutePartsDoublePenetration;
                case rjwSextype.Boobjob:
                    return StripperMod.settings.prostitutePartsBoobjob;
                case rjwSextype.Handjob:
                    return StripperMod.settings.prostitutePartsHandjob;
                case rjwSextype.Footjob:
                    return StripperMod.settings.prostitutePartsFootjob;
                case rjwSextype.Fingering:
                    return StripperMod.settings.prostitutePartsFingering;
                case rjwSextype.Scissoring:
                    return StripperMod.settings.prostitutePartsScissoring;
                case rjwSextype.Fisting:
                    return StripperMod.settings.prostitutePartsFisting;
                case rjwSextype.Rimming:
                    return StripperMod.settings.prostitutePartsRimming;
                case rjwSextype.Fellatio:
                    return StripperMod.settings.prostitutePartsFellatio;
                case rjwSextype.Cunnilingus:
                    return StripperMod.settings.prostitutePartsCunnilingus;
                case rjwSextype.Sixtynine:
                    return StripperMod.settings.prostitutePartsSixtynine;
                default:
                    return 1.0f;
            }
        }

        public static void SetProstitutePartsMult(rjwSextype sextype, float value)
        {
            switch (sextype)
            {
                case rjwSextype.Vaginal:
                    StripperMod.settings.prostitutePartsVaginal = value;
                    break;
                case rjwSextype.Anal:
                    StripperMod.settings.prostitutePartsAnal = value;
                    break;
                case rjwSextype.Oral:
                    StripperMod.settings.prostitutePartsOral = value;
                    break;
                case rjwSextype.DoublePenetration:
                    StripperMod.settings.prostitutePartsDoublePenetration = value;
                    break;
                case rjwSextype.Boobjob:
                    StripperMod.settings.prostitutePartsBoobjob = value;
                    break;
                case rjwSextype.Handjob:
                    StripperMod.settings.prostitutePartsHandjob = value;
                    break;
                case rjwSextype.Footjob:
                    StripperMod.settings.prostitutePartsFootjob = value;
                    break;
                case rjwSextype.Fingering:
                    StripperMod.settings.prostitutePartsFingering = value;
                    break;
                case rjwSextype.Scissoring:
                    StripperMod.settings.prostitutePartsScissoring = value;
                    break;
                case rjwSextype.Fisting:
                    StripperMod.settings.prostitutePartsFisting = value;
                    break;
                case rjwSextype.Rimming:
                    StripperMod.settings.prostitutePartsRimming = value;
                    break;
                case rjwSextype.Fellatio:
                    StripperMod.settings.prostitutePartsFellatio = value;
                    break;
                case rjwSextype.Cunnilingus:
                    StripperMod.settings.prostitutePartsCunnilingus = value;
                    break;
                case rjwSextype.Sixtynine:
                    StripperMod.settings.prostitutePartsSixtynine = value;
                    break;
                default:
                    break;
            }
        }
    }
}
