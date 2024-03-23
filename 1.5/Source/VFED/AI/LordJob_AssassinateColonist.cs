using Verse.AI.Group;

namespace VFED;

public class LordJob_AssassinateColonist : LordJob
{
    public override bool AddFleeToil => false;

    public override StateGraph CreateGraph()
    {
        var graph = new StateGraph();
        var attack = graph.StartingToil = new LordToil_AttackClosest();
        var exit = new LordToil_ExitMap();
        graph.AddToil(exit);
        var leave = new Transition(attack, exit);
        leave.AddTrigger(new Trigger_Memo("TargetDead"));
        leave.AddPostAction(new TransitionAction_Custom(() =>
        {
            exit.UpdateAllDuties();
            foreach (var pawn in lord.ownedPawns) pawn.jobs.StopAll();
        }));
        graph.AddTransition(leave);
        return graph;
    }
}
