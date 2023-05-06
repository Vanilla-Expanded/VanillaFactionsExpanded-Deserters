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
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = x => x.Thing?.TryGetComp<CompBiocodable>() is { Biocoded: true } && BaseTargetValidator(x.Thing)
        };

    public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
    {
        yield return targetChosenByPlayer;
    }
}

public class CompProperties_TargetableBiocoded : CompProperties_Targetable
{
    public CompProperties_TargetableBiocoded() => compClass = typeof(CompTargetable_Biocoded);
}

public class CompTargetEffect_Biodecode : CompTargetEffect
{
    public override void DoEffectOn(Pawn user, Thing target)
    {
        var comp = target.TryGetComp<CompBiocodable>();
        comp.UnCode();
    }
}
