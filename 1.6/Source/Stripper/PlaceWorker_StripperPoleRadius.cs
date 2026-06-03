using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Stripper {
    public class PlaceWorker_StripperPoleRadius : PlaceWorker {

		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null) {
			Map currentMap = Find.CurrentMap;
            GenDraw.DrawFieldEdges(StripperPoleHelper.WatchCells(def, center, currentMap).ToList());
		}

    }
}
