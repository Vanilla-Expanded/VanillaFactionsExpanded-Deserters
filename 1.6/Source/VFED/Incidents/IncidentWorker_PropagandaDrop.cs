using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFED;

public class IncidentWorker_PropagandaDrop : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms) => base.CanFireNowSub(parms) && parms.target is Map map && TryFindCells(map, null);

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        var cells = new List<IntVec3>();
        if (parms.target is not Map map || !TryFindCells(map, cells)) return false;

        foreach (var cell in cells) SkyfallerMaker.SpawnSkyfaller(VFED_DefOf.VFED_DropPodIncoming_Propaganda, cell, map);

        SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, cells.MakeLookTargets(map));

        return true;
    }

    private static bool TryFindCells(Map map, List<IntVec3> cells)
    {
        if (map == null) return false;
        cells ??= new List<IntVec3>();
        cells.Clear();

        var count = Rand.Range(3, 6);

        var home = map.areaManager.Home.ActiveCells.ToList();


        for (var i = 0; i < count; i++)
        {
            var found = false;
            foreach (var cell in home.InRandomOrder())
            {
                if (cells.Any(c => c.DistanceToSquared(cell) < 50)) continue;
                if (DropCellFinder.TryFindDropSpotNear(cell, map, out var foundCell, false, false, false, new IntVec2(1, 1), false))
                {
                    if (cells.Any(c => c.DistanceToSquared(foundCell) < 50)) continue;
                    cells.Add(foundCell);
                    found = true;
                    break;
                }
            }

            if (!found) return false;
        }

        return true;
    }
}
