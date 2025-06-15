using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFED;

public class CompTargetable_Biocoded : CompTargetable
{
    protected override bool PlayerChoosesTarget => true;

    protected override TargetingParameters GetTargetingParameters() =>
        new()
        {
            canTargetPawns = false,
            canTargetBuildings = true,
            canTargetItems = true,
            mapObjectTargetsMustBeAutoAttackable = false
        };

    public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
    {
        yield return targetChosenByPlayer;
    }

    public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true) =>
        (target.Thing?.TryGetComp<CompBiocodable>() is { Biocoded: true } || target.Thing is Building_CrateBiosecured)
     && base.ValidateTarget(target, showMessages);
}

public class CompProperties_TargetableBiocoded : CompProperties_Targetable
{
    public CompProperties_TargetableBiocoded() => compClass = typeof(CompTargetable_Biocoded);
}

public class CompTargetEffect_Biodecode : CompTargetEffect
{
    public override void DoEffectOn(Pawn user, Thing target)
    {
        target.TryGetComp<CompBiocodable>()?.UnCode();
        if (target is Building_CrateBiosecured crate) crate.Open();
    }
}
