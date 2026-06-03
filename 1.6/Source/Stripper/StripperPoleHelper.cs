using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Stripper {
    public static class StripperPoleHelper {

		public static HashSet<ThingDef> registeredDefs = new HashSet<ThingDef>();
        private static Dictionary<Pawn, HashSet<Pawn>> dancerWatcherMap = new Dictionary<Pawn, HashSet<Pawn>>();

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
                var validWatchers = watchers
                    .Where(w => w != null && w.Spawned && !w.Dead && !w.Downed)
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

            float dancerBeauty = dancer.GetStatValue(StatDefOf.Beauty, true);

            int basePayout = StripperPaymentHelper.PriceOfPerformance(dancer);
            
            int totalSilverToDrop = 0;

            foreach (Pawn watcher in watchers)
            {
                if (watcher == null || watcher.Dead || !watcher.Spawned) continue;

                if(watcher.IsColonist) continue;

                totalSilverToDrop += StripperPaymentHelper.PayClientToPerformer(watcher, dancer, basePayout, watchers);
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

        public static void Register(Building_StripperPole pole) {
			registeredDefs.Add(pole.def);
		}


        public static List<Thing> GetStripperPoles(Map map) {
			var poles = registeredDefs.SelectMany(e => map.listerThings.ThingsOfDef(e)).ToList();
			return poles.Count > 0 ? poles : null;
		}

		public static Building_StripperPole GetStripperPoleForPawn(Pawn pawn) {
			if (!pawn.ageTracker.Adult) return null;
			var poles = GetStripperPoles(pawn.Map);
			if (poles == null) return null;

			bool validator(Thing t) {
				if (!(t as Building_StripperPole).CanUse(pawn)) return false;
				if (!pawn.CanReserve(t, 1, -1, null, false)) return false;
				if (t.IsForbidden(pawn)) return false;
				if (!t.IsSociallyProper(pawn)) return false;
				return true;
			}

			Func<Thing, float> priorityGetter = (t => {
				float p = 1f;

				if ((t as Building_StripperPole).IsOwner(pawn)) {
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


		public static IEnumerable<IntVec3> WatchCells(ThingDef def, IntVec3 center, Map map) {
			if (def == null) throw new Exception("missing def");
			var poleDef = def as Building_StripperPole_Def;
			if (poleDef == null) throw new Exception("def is not a pole");
			return GenRadial.RadialCellsAround(center, poleDef.watchRadius, false)
				.Where(e => EverPossibleToWatchFrom(e, center, map));
		}

		private static bool EverPossibleToWatchFrom(IntVec3 watchCell, IntVec3 buildingCenter, Map map) {
			if (!watchCell.InBounds(map)) return false;
			if (!watchCell.Standable(map)) return false;
			Room room = buildingCenter.GetRoom(map);
			if (room != null && !room.ContainsCell(watchCell)) return false;
			return GenSight.LineOfSight(buildingCenter, watchCell, map, skipFirstCell: true);
		}


		public static bool TryFindBestWatchCell(Thing t, Pawn pawn, out IntVec3 result, out Building chair) {
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
	}
}
