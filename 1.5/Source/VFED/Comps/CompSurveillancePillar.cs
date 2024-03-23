using RimWorld;
using Verse;

namespace VFED;

public class CompSurveillancePillar : CompMotionDetector
{
    private CompPowerTrader compPower;

    public override bool Active => base.Active && compPower.PowerOn;

    protected override void Trigger(Thing initiator)
    {
        base.Trigger(initiator);
        Utilities.ChangeVisibility(DesertersMod.VisibilityFromPillar);
        Messages.Message("VFED.SurveillancePillarActivated".Translate(WorldComponent_Deserters.Instance.Visibility, DesertersMod.VisibilityFromPillar),
            initiator, MessageTypeDefOf.NegativeEvent);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        compPower = parent.TryGetComp<CompPowerTrader>();
    }
}
