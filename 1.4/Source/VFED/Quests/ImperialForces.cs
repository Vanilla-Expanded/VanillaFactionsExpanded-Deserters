using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.AI.Group;
using Verse.Grammar;

namespace VFED;

public class QuestNode_ImperialForces : QuestNode
{
    public SlateRef<string> inSignal;
    public SlateRef<MapParent> mapParent;
    public SlateRef<float> points;

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var site = mapParent.GetValue(slate);
        var tile = site.Tile;
        var parms = new PawnGroupMakerParms_Saveable
        {
            groupKind = PawnGroupKindDefOf.Settlement,
            points = points.GetValue(slate),
            faction = Faction.OfEmpire,
            generateFightersOnly = true,
            dontUseSingleUseRocketLaunchers = true,
            inhabitants = true,
            tile = tile,
            seed = Gen.HashCombineInt(Find.World.info.Seed, tile)
        };

        QuestGen.quest.AddPart(new QuestPart_SpawnForces
        {
            inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal"),
            mapParent = site,
            parms = parms
        });

        QuestGen.AddQuestDescriptionRules(new List<Rule>
        {
            new Rule_String("forces_description",
                PawnUtility.PawnKindsToLineList(PawnGroupMakerUtility.GeneratePawnKindsExample(parms), "  - ", ColoredText.ThreatColor))
        });
    }

    protected override bool TestRunInt(Slate slate) => points.GetValue(slate) > 0f;
}

public class QuestPart_SpawnForces : QuestPart
{
    public string inSignal;
    public MapParent mapParent;
    public PawnGroupMakerParms_Saveable parms;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == inSignal && mapParent.HasMap)
        {
            var map = mapParent.Map;
            var forces = PawnGroupMakerUtility.GeneratePawns(parms).ToList();
            Rand.PushState(Gen.HashCombineInt(Find.World.info.Seed, mapParent.Tile));
            foreach (var pawn in forces)
                if (CellFinder.TryFindRandomCellNear(map.Center, map, 16, x => x.Standable(map), out var cell))
                    GenSpawn.Spawn(pawn, cell, map);
            LordMaker.MakeNewLord(Faction.OfEmpire, new LordJob_DefendBase(Faction.OfEmpire, map.Center), map, forces);
            Rand.PopState();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, nameof(inSignal));
        Scribe_References.Look(ref mapParent, nameof(mapParent));
        Scribe_Deep.Look(ref parms, nameof(parms));
    }
}

public class PawnGroupMakerParms_Saveable : PawnGroupMakerParms, IExposable
{
    public void ExposeData()
    {
        Scribe_Defs.Look(ref groupKind, nameof(groupKind));
        Scribe_Values.Look(ref tile, nameof(tile));
        Scribe_Values.Look(ref inhabitants, nameof(inhabitants));
        Scribe_Values.Look(ref points, nameof(points));
        Scribe_References.Look(ref faction, nameof(faction));
        Scribe_References.Look(ref ideo, nameof(ideo));
        Scribe_Defs.Look(ref traderKind, nameof(traderKind));
        Scribe_Values.Look(ref generateFightersOnly, nameof(generateFightersOnly));
        Scribe_Values.Look(ref dontUseSingleUseRocketLaunchers, nameof(dontUseSingleUseRocketLaunchers));
        Scribe_Defs.Look(ref raidStrategy, nameof(raidStrategy));
        Scribe_Values.Look(ref forceOneDowned, nameof(forceOneDowned));
        Scribe_Values.Look(ref seed, nameof(seed));
        Scribe_Defs.Look(ref raidAgeRestriction, nameof(raidAgeRestriction));
    }
}

public class QuestNode_GenerateForces : QuestNode
{
    public SlateRef<float> points;
    public SlateRef<float> pointsMult;
    public SlateRef<string> storeAs;

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var forces = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
            {
                groupKind = PawnGroupKindDefOf.Combat,
                points = points.GetValue(slate) * (pointsMult.TryGetValue(slate, out var mult) ? mult : 1),
                faction = Faction.OfEmpire,
                generateFightersOnly = true
            })
           .Select(pawn =>
            {
                if (!pawn.IsWorldPawn()) Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                QuestGen.AddToGeneratedPawns(pawn);
                return pawn;
            })
           .ToList();
        slate.Set(storeAs.GetValue(slate), forces);
    }

    protected override bool TestRunInt(Slate slate) => true;
}
