using System.Collections.Generic;
using RimWorld;
using Verse;
using VFECore;

namespace VFED;

public class DropPodIncoming_Propaganda : DropPodIncoming
{
    protected override void SpawnThings()
    {
        var usedCells = new HashSet<IntVec3>();
        for (var i = 0; i < 7; i++)
        {
            var cell = GenAdjFast.AdjacentCells8Way(Position).Exclude(usedCells).RandomElement();
            usedCells.Add(cell);
            FilthMaker.TryMakeFilth(cell, Map, VFED_DefOf.VFED_Filth_Propaganda, additionalFlags: FilthSourceFlags.Unnatural);
        }
    }
}
