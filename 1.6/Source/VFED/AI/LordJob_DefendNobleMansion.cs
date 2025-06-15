using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFED;

public class LordJob_DefendNobleMansion : LordJob
{
    private LordToilData_DefendNobleMansion data;

    public LordJob_DefendNobleMansion() { }

    public LordJob_DefendNobleMansion(Pawn noble, IEnumerable<Pawn> guards, IEnumerable<Pawn> onCall, IEnumerable<Pawn> patrollers, CellRect patrolArea,
        List<CellRect> bunkerAreas) =>
        data = new LordToilData_DefendNobleMansion(noble, guards, onCall, patrollers, patrolArea, bunkerAreas);

    public override bool AddFleeToil => false;

    public override StateGraph CreateGraph()
    {
        var graph = new StateGraph();
        var passive = graph.StartingToil = new LordToil_DefendNobleMansion_Passive(data);
        var active = new LordToil_DefendNobleMansion_Active(data);
        graph.AddToil(active);
        var activate = new Transition(passive, active);
        activate.AddTrigger(new Trigger_PawnHarmed(requireInstigatorWithSpecificFaction: Faction.OfPlayer));
        activate.AddTrigger(new Trigger_MotionDetected());
        activate.AddPreAction(new TransitionAction_Message("VFED.DetectedWarning".Translate(), MessageTypeDefOf.ThreatBig));
        graph.AddTransition(activate);
        var highAlert = new LordToil_DefendNobleMansion_HighAlert(data);
        graph.AddToil(highAlert);
        var moveToHighAlert = new Transition(active, highAlert);
        moveToHighAlert.AddSource(passive);
        moveToHighAlert.AddTrigger(new Trigger_FractionPawnsLost(0.3f));
        moveToHighAlert.AddPreAction(new TransitionAction_Message("VFED.HighAlertWarning".Translate(), MessageTypeDefOf.ThreatBig));
        graph.AddTransition(moveToHighAlert);
        var emergency = new LordToil_DefendNobleMansion_Emergency(data);
        graph.AddToil(emergency);
        var nowEmergency = new Transition(highAlert, emergency);
        nowEmergency.AddSources(passive, active);
        nowEmergency.AddTrigger(new Trigger_FractionPawnsLost(0.6f));
        nowEmergency.AddPostAction(new TransitionAction_CheckForJobOverride());
        graph.AddTransition(nowEmergency);
        var flee = new LordToil_DefendNobleMansion_Flee(data);
        graph.AddToil(flee);
        var startFlee = new Transition(emergency, flee);
        startFlee.AddSources(passive, active, highAlert);
        startFlee.AddTrigger(new Trigger_FractionPawnsLost(0.8f));
        startFlee.AddPreAction(new TransitionAction_Message("VFED.FleeWarning".Translate(data.noble.NameFullColored), MessageTypeDefOf.NegativeEvent));
        startFlee.AddPostAction(new TransitionAction_CheckForJobOverride());
        graph.AddTransition(startFlee, true);
        var assault = new LordToil_AssaultColony(false, true);
        graph.AddToil(assault);
        var attack = new Transition(passive, assault);
        attack.AddSources(active, highAlert, emergency, flee);
        attack.AddTrigger(new Trigger_SpecificPawnKilled(data.noble));
        attack.AddPostAction(new TransitionAction_CheckForJobOverride());
        attack.AddPreAction(new TransitionAction_Message(
            "VFED.NobleKilledRevenge".Translate(lord.faction.def.pawnsPlural, lord.faction.Name, data.noble.NameFullColored).CapitalizeFirst(),
            MessageTypeDefOf.ThreatBig, data.noble));
        graph.AddTransition(attack, true);
        return graph;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref data, nameof(data));
    }
}

public class Trigger_MotionDetected : Trigger
{
    public override bool ActivateOn(Lord lord, TriggerSignal signal) =>
        signal is { type: TriggerSignalType.Signal, signal: { tag: "MotionDetected" } inSignal }
     && inSignal.args.GetArg<Thing>("DETECTOR")?.TryGetComp<CompSurveillancePillar>() != null
     && inSignal.args.GetArg<Thing>("DETECTED")?.Faction is { IsPlayer: true };
}

public class Trigger_SpecificPawnKilled : Trigger
{
    private readonly Pawn pawn;

    public Trigger_SpecificPawnKilled() { }

    public Trigger_SpecificPawnKilled(Pawn pawn) => this.pawn = pawn;

    public override bool ActivateOn(Lord lord, TriggerSignal signal) =>
        signal is { type: TriggerSignalType.PawnLost, condition: PawnLostCondition.Killed, Pawn: { Dead: true } signalPawn } && signalPawn == pawn;
}
