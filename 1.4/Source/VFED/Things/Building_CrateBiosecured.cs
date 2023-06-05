using Verse;

namespace VFED;

public class Building_CrateBiosecured : Building_SupplyCrate
{
    protected override void GenerateContents()
    {
        for (var i = 0; i < 3; i++) innerContainer.TryAdd(ThingMaker.MakeThing(ContrabandManager.AllContraband.RandomElement().Item1));
        if (Rand.Chance(0.25f))
        {
            var intel = ThingMaker.MakeThing(VFED_DefOf.VFED_CriticalIntel);
            intel.stackCount = Rand.Range(1, 4);
            innerContainer.TryAdd(intel);
        }
    }
}
