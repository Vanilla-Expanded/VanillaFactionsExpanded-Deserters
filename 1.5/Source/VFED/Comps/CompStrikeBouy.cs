using RimWorld;
using Verse;

namespace VFED;

public class CompStrikeBouy : CompMotionDetector
{
    public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
    {
        base.PostPostApplyDamage(dinfo, totalDamageDealt);
        if (dinfo.Def == DamageDefOf.EMP) parent.Destroy();
    }

    protected override void Trigger(Thing initiator)
    {
        base.Trigger(initiator);
        initiator.Position.DoAerodroneStrike(initiator.Map);
    }
}
