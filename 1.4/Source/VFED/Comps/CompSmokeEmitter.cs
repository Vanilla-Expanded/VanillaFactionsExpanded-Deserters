using RimWorld;
using Verse;

namespace VFED;

public class CompSmokeEmitter : ThingComp
{
    public override void CompTick()
    {
        FleckMaker.ThrowSmoke(parent.ActualDrawPos(), parent.MapHeld, 2f);
    }
}
