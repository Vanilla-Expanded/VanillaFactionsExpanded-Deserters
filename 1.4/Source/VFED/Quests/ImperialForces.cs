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
        var parms = new PawnGroupMakerParms
        {
            groupKind = PawnGroupKindDefOf.Settlement,
            inhabitants = true,
            points = WorldComponent_Deserters.Instance.ActiveEffects.OfType<VisibilityEffect_ArmySize>()
               .Aggregate(points.GetValue(slate), (p, effect) => p * effect.multiplier),
            faction = Faction.OfEmpire,
            generateFightersOnly = true,
            dontUseSingleUseRocketLaunchers = true
        };
        var forces = PawnGroupMakerUtility.GeneratePawns(parms).ToList();
        QuestGen.quest.AddPart(new QuestPart_SpawnForces
        {
            inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal"),
            mapParent = mapParent.GetValue(slate),
            forces = forces
        });

        QuestGen.AddQuestDescriptionRules(new List<Rule>
        {
            new Rule_String("forces_description", PawnUtility.PawnKindsToLineList(forces.Select(p => p.kindDef), "  - ", ColoredText.ThreatColor))
        });
    }

    protected override bool TestRunInt(Slate slate) => points.GetValue(slate) > 0f;
}

public class QuestPart_SpawnForces : QuestPart
{
    public List<Pawn> forces;
    public string inSignal;
    public MapParent mapParent;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == inSignal && mapParent.HasMap)
        {
            var map = mapParent.Map;
            forces.RemoveAll(pawn => pawn.Spawned);
            foreach (var pawn in forces)
                if (CellFinder.TryFindRandomCellNear(map.Center, map, 16, x => x.Standable(map), out var cell))
                    GenSpawn.Spawn(pawn, cell, map);
            LordMaker.MakeNewLord(Faction.OfEmpire, new LordJob_DefendBase(Faction.OfEmpire, map.Center), map, forces);
            forces.RemoveAll(pawn => pawn.Spawned);
            if (forces.Count > 0) Log.Error("[VFED] Failed to spawn all imperial forces.");
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, nameof(inSignal));
        Scribe_References.Look(ref mapParent, nameof(mapParent));
        Scribe_Collections.Look(ref forces, nameof(forces), LookMode.Deep);
    }
}
