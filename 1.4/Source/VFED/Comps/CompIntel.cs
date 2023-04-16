using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VFED;

public class CompIntel : ThingComp
{
    private int tickTillOutOfDate;
    public CompProperties_Intel Props => props as CompProperties_Intel;

    public override void PostPostMake()
    {
        base.PostPostMake();
        tickTillOutOfDate = Props.outOfDateDays * GenDate.TicksPerDay;
    }

    public override void CompTick()
    {
        Tick(1);
    }

    public override void CompTickRare()
    {
        Tick(250);
    }

    public override void CompTickLong()
    {
        Tick(2000);
    }

    private void Tick(int interval)
    {
        tickTillOutOfDate -= interval;
        if (tickTillOutOfDate <= 0)
        {
            Messages.Message("VFED.IntelOutOfDateNow".Translate(parent.LabelCap), new GlobalTargetInfo(parent.PositionHeld, parent.MapHeld),
                MessageTypeDefOf.NegativeEvent);
            parent.Destroy();
        }
    }

    public override void PreAbsorbStack(Thing otherStack, int count)
    {
        tickTillOutOfDate = (tickTillOutOfDate * parent.stackCount + otherStack.TryGetComp<CompIntel>().tickTillOutOfDate * count)
                          / (parent.stackCount + count);
    }

    public override void PostSplitOff(Thing piece)
    {
        piece.TryGetComp<CompIntel>().tickTillOutOfDate = tickTillOutOfDate;
    }

    public override string CompInspectStringExtra() => "VFED.IntelOutOfDate".Translate(tickTillOutOfDate.ToStringTicksToPeriodVerbose());
}

public class CompProperties_Intel : CompProperties
{
    public int outOfDateDays;

    public CompProperties_Intel() => compClass = typeof(CompIntel);
}
