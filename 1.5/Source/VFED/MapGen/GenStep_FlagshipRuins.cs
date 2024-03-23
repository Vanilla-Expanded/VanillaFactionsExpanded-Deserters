using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using VFEEmpire;

namespace VFED;

public class GenStep_FlagshipRuins : GenStep
{
    public override int SeedPart => 916595355;

    public override void Generate(Map map, GenStepParams parms)
    {
        var chunks = map.listerThings.ThingsOfDef(VFED_DefOf.VFED_FlagshipChunk);
        var count = chunks.Count;
        Log.Message($"Found {count} chunks");
        {
            var emperor = WorldComponent_Hierarchy.Instance.TitleHolders.Last();
            if (chunks.TryRandomElement(out var chunk) && CellFinder.TryFindRandomCellNear(chunk.Position, map, 15, c => c.Standable(map), out var loc))
            {
                if (emperor.IsWorldPawn()) Find.WorldPawns.RemovePawn(emperor);
                GenPlace.TryPlaceThing(emperor.Corpse ?? emperor.MakeCorpse(null, null), loc, map, ThingPlaceMode.Near);
            }
        }

        var pawns = Find.WorldPawns.AllPawnsDead.Where(p => p.Faction == Faction.OfEmpire).ToList();
        var wantedCount = Rand.RangeInclusive(count / 5, count / 2);
        var pawnKinds = FactionDefOf.Empire.pawnGroupMakers.SelectMany(pgm => Utilities.CombineEnumerable(pgm.guards, pgm.carriers, pgm.options, pgm.traders))
           .Select(pgo => pgo.kind)
           .ToHashSet();
        pawnKinds.AddRange(DefDatabase<RoyalTitleDef>.AllDefs.Select(title => title.GetModExtension<RoyalTitleDefExtension>().kindForHierarchy));
        pawnKinds.Remove(VFEE_DefOf.Emperor.GetModExtension<RoyalTitleDefExtension>().kindForHierarchy);
        pawnKinds.RemoveWhere(pk => pk == null || !pk.RaceProps.Humanlike);
        while (pawns.Count < wantedCount)
        {
            var pawnKindDef = pawnKinds.RandomElement();
            var pawn = PawnGenerator.GeneratePawn(new(pawnKindDef, Faction.OfEmpire, forceDead: true));
            if (!pawn.IsWorldPawn()) Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            pawns.Add(pawn);
        }

        foreach (var pawn in pawns.InRandomOrder())
            if (chunks.TryRandomElement(out var chunk) && CellFinder.TryFindRandomCellNear(chunk.Position, map, 7, c => c.Standable(map), out var loc))
            {
                Find.WorldPawns.RemovePawn(pawn);
                GenPlace.TryPlaceThing(pawn.Corpse ?? pawn.MakeCorpse(null, null), loc, map, ThingPlaceMode.Near);
            }

        var techprints = DefDatabase<ResearchProjectDef>.AllDefs
           .Where(project => project.TechprintCount > 0 && project.heldByFactionCategoryTags.Contains(FactionDefOf.Empire.categoryTag))
           .Select(project => project.Techprint)
           .ToList();
        foreach (var techprint in techprints)
            for (var i = 0; i < 3; i++)
                if (chunks.TryRandomElement(out var chunk)
                 && CellFinder.TryFindRandomCellNear(chunk.Position, map, 10, c => c.GetEdifice(map) == null, out var loc))
                    GenPlace.TryPlaceThing(ThingMaker.MakeThing(techprint), loc, map, ThingPlaceMode.Near);

        var trader = DefDatabase<TraderKindDef>.GetNamed("Base_Empire_Standard");
        var items = new List<Thing>();
        foreach (var chunk in chunks.InRandomOrder())
        {
            if (items.Count == 0) items.AddRange(trader.stockGenerators.SelectMany(sg => sg.GenerateThings(map.Tile, Faction.OfEmpire)));
            {
                if (CellFinder.TryFindRandomCellNear(chunk.Position, map, 5, c => FireUtility.ChanceToStartFireIn(c, map) > 0, out var loc)
                 && items.TryRandomElement(out var thing))
                {
                    GenPlace.TryPlaceThing(thing, loc, map, ThingPlaceMode.Near);
                    items.Remove(thing);
                }
            }

            for (var i = 0; i < 4; i++)
                if (CellFinder.TryFindRandomCellNear(chunk.Position, map, 5, c => FireUtility.ChanceToStartFireIn(c, map) > 0, out var loc))
                    FireUtility.TryStartFireIn(loc, map, Rand.Range(0.1f, 0.925f), null);
        }
    }
}
