using RimWorld;
using Verse;
using Verse.Sound;

namespace VFED;

public class CompSurveillancePillar : CompMotionDetector
{
    private CompPowerTrader compPower;
    private Sustainer sustainer;

    public override bool Active => base.Active && compPower.PowerOn;

    protected override void Trigger(Thing initiator)
    {
        base.Trigger(initiator);
        Messages.Message("VFED.SurveillancePillarActivated".Translate(100), initiator, MessageTypeDefOf.NegativeEvent);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        compPower = parent.TryGetComp<CompPowerTrader>();
    }

    public override void CompTick()
    {
        base.CompTick();
//            if (Activated)
//            {
//                if (sustainer == null || sustainer.Ended) sustainer = ;
//                sustainer.Maintain();
//            }
//            else
//            {
//                sustainer.End();
//            }
    }
}
