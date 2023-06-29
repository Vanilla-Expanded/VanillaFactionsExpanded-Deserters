using System.Collections.Generic;
using System.Linq;
using KCSG;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace VFED;

public class GenStep_PlotRaid : GenStep
{
    private readonly List<Pawn> forces = new();
    private readonly List<Pawn> guards = new();
    private readonly List<Pawn> onCall = new();
    private readonly List<Pawn> patrollers = new();
    public override int SeedPart => 916595356;

    public override void Generate(Map map, GenStepParams parms)
    {
        if (map.Parent is not Site site) return;
        if (!WorldComponent_Deserters.Instance.DataForSites.TryGetValue(site, out var data)) return;
        data.structure.Generate(map.Center, map, CustomGenOption.GetRelatedQuest(map));
        forces.Clear();
        forces.AddRange(PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
        {
            groupKind = PawnGroupKindDefOf.Settlement,
            points = data.points * 3,
            faction = Faction.OfEmpire,
            generateFightersOnly = true,
            inhabitants = true,
            tile = site.Tile
        }));
        var bunkerRects = new List<CellRect>();
        var bounds = CellRect.Empty;
        guards.Clear();
        patrollers.Clear();
        onCall.Clear();
        foreach (var (rect, tile) in GenOption.tiledRects)
        {
            bounds = bounds.IsEmpty ? rect : bounds.Encapsulate(rect);
            if (tile.defName.Contains("NobleThroneRoom"))
            {
                foreach (var thing in rect.AllThings(map).ToList())
                    if (thing is Building_Throne && !data.noble.Spawned)
                        GenSpawn.Spawn(data.noble, thing.Position, map, thing.Rotation);
                    else if (thing.def.IsDoor) SpawnDoorGaurds(thing);
            }
            else if (tile.defName.Contains("Bunker"))
                bunkerRects.Add(rect);
            else if (tile.defName.Contains("StockpileDepot") || tile.defName.Contains("NobleBedroom"))
                foreach (var thing in rect.AllThings(map).ToList())
                    if (thing.def.IsDoor)
                        SpawnDoorGaurds(thing);
        }

        var countPerBunker = forces.Count / 2 / bunkerRects.Count;
        List<IntVec3> possibleCells;
        foreach (var rect in bunkerRects)
        {
            possibleCells = rect.Where(c => c.Standable(map)).ToList();
            for (var i = 0; i < countPerBunker; i++)
            {
                if (possibleCells.Count == 0) break;
                var cell = possibleCells.TakeRandom();
                var pawn = forces.TakeRandom();
                GenSpawn.Spawn(pawn, cell, map);
                possibleCells.RemoveAll(c => c.InHorDistOf(cell, 1.9f));
                onCall.Add(pawn);
            }
        }

        bounds = bounds.ExpandedBy(12).ClipInsideMap(map).ContractedBy(2);

        possibleCells = bounds.EdgeCells.Where(c => c.Standable(map)).ToList();
        while (forces.Count > 0 && possibleCells.Count > 0)
        {
            var pawn1 = forces.TakeRandom();
            var pawn2 = forces.Count > 0 ? forces.TakeRandom() : null;
            var cell = possibleCells.TakeRandom();

            GenSpawn.Spawn(pawn1, cell.RandomAdjacentCell8Way(), map);
            patrollers.Add(pawn1);
            if (pawn2 != null)
            {
                GenSpawn.Spawn(pawn2, cell.RandomAdjacentCell8Way(), map);
                patrollers.Add(pawn2);
            }

            possibleCells.RemoveAll(c => c.InHorDistOf(cell, 11.9f));
        }

        foreach (var pawn in forces)
        {
            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(data.noble.Position, map, 5), map);
            guards.Add(pawn);
        }

        LordMaker.MakeNewLord(Faction.OfEmpire, new LordJob_DefendNobleMansion(data.noble,
                guards.ListFullCopy(),
                onCall.ListFullCopy(),
                patrollers.ListFullCopy(), bounds, bunkerRects),
            map, guards.Concat(data.noble).Concat(onCall).Concat(patrollers));

        forces.Clear();
        guards.Clear();
        patrollers.Clear();
        onCall.Clear();
    }

    private void SpawnDoorGaurds(Thing door)
    {
        var outsideDirection = Rot4.Invalid;
        var map = door.Map;
        foreach (var c in GenAdjFast.AdjacentCellsCardinal(door.Position))
            if (c.Standable(map) && c.GetRoom(map).PsychologicallyOutdoors)
                outsideDirection = Rot4.FromIntVec3(door.Position - c);

        var pos = door.Position;
        var guardLocations = (IntVec3.Invalid, IntVec3.Invalid);

        while (!guardLocations.Item1.IsValid || !guardLocations.Item1.Standable(map) || !guardLocations.Item2.IsValid || !guardLocations.Item2.Standable(map))
        {
            pos += outsideDirection.FacingCell;
            guardLocations = (pos + outsideDirection.Rotated(RotationDirection.Clockwise).FacingCell,
                pos + outsideDirection.Rotated(RotationDirection.Counterclockwise).FacingCell);
            if (!guardLocations.Item1.InBounds(map) || !guardLocations.Item2.InBounds(map)) break;
        }

        if (guardLocations.Item1.IsValid && guardLocations.Item1.InBounds(map) && guardLocations.Item1.Standable(map))
        {
            var pawn = forces.TakeRandom();
            GenSpawn.Spawn(pawn, guardLocations.Item1, map);
            guards.Add(pawn);
        }

        if (guardLocations.Item2.IsValid && guardLocations.Item2.InBounds(map) && guardLocations.Item2.Standable(map))
        {
            var pawn = forces.TakeRandom();
            GenSpawn.Spawn(pawn, guardLocations.Item2, map);
            guards.Add(pawn);
        }
    }
}
