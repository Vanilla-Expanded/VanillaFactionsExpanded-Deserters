using RimWorld;
using Verse;

namespace VFED;

public class CompEMPEffect : ThingComp
{
    private Effecter empEffecter;

    private int ticksEffectLeft;

    public override void CompTick()
    {
        if (ticksEffectLeft > 0)
        {
            empEffecter ??= EffecterDefOf.DisabledByEMP.Spawn();
            empEffecter.EffectTick(parent, parent);
            ticksEffectLeft--;
        }
        else if (parent.IsHashIntervalTick(500)) ticksEffectLeft = Rand.Range(200, 400);
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref ticksEffectLeft, nameof(ticksEffectLeft));
    }
}
