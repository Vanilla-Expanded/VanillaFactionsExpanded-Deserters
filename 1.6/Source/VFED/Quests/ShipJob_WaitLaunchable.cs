using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace VFED;

public class ShipJob_WaitLaunchable : ShipJob_WaitSendable
{
    public override IEnumerable<Gizmo> GetJobGizmos()
    {
        var send = new Command_Action
        {
            defaultLabel = "CommandSendShuttle".Translate(),
            defaultDesc = "CommandSendShuttleDesc".Translate(),
            icon = SendCommandTex,
            alsoClickIfOtherInGroupClicked = false,
            action = SendAway
        };
        if (!transportShip.ShuttleComp.AllRequiredThingsLoaded) send.Disable("CommandSendShuttleFailMissingRequiredThing".Translate());
        yield return send;
    }
}

public class QuestNode_AddShipJob_WaitSendable : QuestNode_AddShipJob_Wait
{
    public SlateRef<MapParent> destination;
    public SlateRef<bool> targetPlayerSettlement;
    protected override ShipJobDef DefaultShipJobDef => ShipJobDefOf.WaitSendable;

    protected override void AddJobVars(ShipJob shipJob, Slate slate)
    {
        base.AddJobVars(shipJob, slate);
        if (shipJob is ShipJob_WaitSendable waitSendable)
        {
            waitSendable.destination = destination.GetValue(slate);
            waitSendable.targetPlayerSettlement = targetPlayerSettlement.GetValue(slate);
        }
    }
}
