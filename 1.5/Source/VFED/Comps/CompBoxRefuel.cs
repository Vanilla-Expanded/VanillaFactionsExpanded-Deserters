using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFED;

[HarmonyPatch]
public class CompBoxRefuel : ThingComp
{
    public static HashSet<string> MustUseBoxes = new();
    private static readonly List<Thing> checkThings = new();
    private CompRefuelable compRefuelable;
    public CompProperties_BoxRefuel Props => props as CompProperties_BoxRefuel;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        compRefuelable = parent.TryGetComp<CompRefuelable>();
    }

    public override void CompTick()
    {
        base.CompTick();
        if (parent.IsHashIntervalTick(30) && !compRefuelable.HasFuel) AttemptRefuel();
    }

    public override void ReceiveCompSignal(string signal)
    {
        base.ReceiveCompSignal(signal);
        if (signal == "RanOutOfFuel") AttemptRefuel();
    }

    private void AttemptRefuel()
    {
        GenAdjFast.AdjacentThings8Way(parent, checkThings);
        for (var i = 0; i < checkThings.Count; i++)
            if (checkThings[i].def == Props.refuelWith)
            {
                var refuelAmount = Props.refuelAmount > 0 ? Props.refuelAmount : compRefuelable.Props.fuelCapacity;
                compRefuelable.Refuel(refuelAmount / compRefuelable.Props.FuelMultiplierCurrentDifficulty);
                checkThings[i].Destroy();

                // Use the vanilla method that handles auto rebuilding and pass the only DestroyMode that allows it.
                // Also don't use checkThings[i].Map, as it'll be null after the Destroy call.
                ThingUtility.CheckAutoRebuildOnDestroyed_NewTemp(checkThings[i], DestroyMode.KillFinalize, parent.Map, checkThings[i].def);
                break;
            }
    }

    [HarmonyPatch(typeof(RefuelWorkGiverUtility), nameof(RefuelWorkGiverUtility.CanRefuel))]
    [HarmonyPrefix]
    public static bool CanRefuel_Prefix(Thing t, ref bool __result)
    {
        if (MustUseBoxes.Contains(t.def.defName))
        {
            __result = false;
            return false;
        }

        return true;
    }
}

// ReSharper disable InconsistentNaming
public class CompProperties_BoxRefuel : CompProperties
{
    public bool mustUseBoxes;
    public ThingDef refuelWith;
    public int refuelAmount = -1;

    public CompProperties_BoxRefuel() => compClass = typeof(CompBoxRefuel);

    public override void PostLoadSpecial(ThingDef parent)
    {
        base.PostLoadSpecial(parent);
        if (mustUseBoxes) CompBoxRefuel.MustUseBoxes.Add(parent.defName);
    }
}
