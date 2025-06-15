using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace VFED;

public class GenStep_DownedShuttle : GenStep
{
    public override int SeedPart => 916595354;

    public override void Generate(Map map, GenStepParams parms)
    {
        if (map.Parent is not Site site) return;
        if (!WorldComponent_Deserters.Instance.DataForSites.TryGetValue(site, out var data)) return;
        if (!CellFinder.TryFindRandomCellNear(map.Center, map, 8, c => GenConstruct.CanPlaceBlueprintAt(ThingDefOf.ShuttleCrashed, c, Rot4.North, map),
                out var shuttleLoc)) return;
        var shuttle = GenSpawn.Spawn(ThingDefOf.ShuttleCrashed, shuttleLoc, map);
        var bounds = shuttle.OccupiedRect().ExpandedBy(2);
        for (var i = 0; i < 3; i++)
            if (CellFinder.TryFindRandomCellNear(shuttleLoc, map, 8,
                    c => GenConstruct.CanPlaceBlueprintAt(VFED_DefOf.VFED_CrashedAerodrone, c, Rot4.North, map),
                    out var aerodroneLoc))
            {
                var aerodrone = GenSpawn.Spawn(VFED_DefOf.VFED_CrashedAerodrone, aerodroneLoc, map);
                bounds = bounds.Encapsulate(aerodrone.OccupiedRect().ExpandedBy(2));
            }

        bounds = bounds.ExpandedBy(2);
        var forces = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
            {
                groupKind = PawnGroupKindDefOf.Settlement,
                points = data.points,
                faction = Faction.OfEmpire,
                generateFightersOnly = true,
                inhabitants = true,
                tile = site.Tile
            })
           .ToList();

        GenSpawn.Spawn(data.noble, shuttle.OccupiedRect().ExpandedBy(2).AdjacentCells.Where(c => c.Standable(map)).RandomElement(), map);

        bounds = bounds.ClipInsideMap(map);
        var possibleCells = bounds.Where(c => c.Standable(map)).ToList();
        foreach (var pawn in forces)
        {
            if (possibleCells.Count <= 0)
            {
                bounds = bounds.ExpandedBy(5).ClipInsideMap(map);
                possibleCells = bounds.Where(c => c.Standable(map) && !forces.Any(p => p.Spawned && p.Position.InHorDistOf(c, 3.9f))).ToList();
            }

            var cell = possibleCells.TakeRandom();
            GenSpawn.Spawn(pawn, cell, map);
            possibleCells.RemoveAll(c => c.InHorDistOf(cell, 3.9f));
        }

        LordMaker.MakeNewLord(Faction.OfEmpire, new LordJob_DefendPoint(shuttleLoc, bounds.Radius()), map, forces.Concat(data.noble));
    }
}
