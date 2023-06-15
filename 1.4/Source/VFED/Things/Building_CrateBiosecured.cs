using RimWorld;
using UnityEngine;
using Verse;

namespace VFED;

public class Building_CrateBiosecured : Building_SupplyCrate
{
    protected override void GenerateContents()
    {
        for (var i = 0; i < 3; i++)
        {
            var (def, ext) = ContrabandManager.AllContraband.RandomElement();
            var thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
            thing.stackCount = ext.useCriticalIntel ? Mathf.CeilToInt(3f / ext.intelCost) : Mathf.CeilToInt(10f / ext.intelCost);
            innerContainer.TryAdd(thing);
        }

        if (Rand.Chance(0.25f))
        {
            var intel = ThingMaker.MakeThing(VFED_DefOf.VFED_CriticalIntel);
            intel.stackCount = Rand.Range(1, 4);
            innerContainer.TryAdd(intel);
        }
    }
}
