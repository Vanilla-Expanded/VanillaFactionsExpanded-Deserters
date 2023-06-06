using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFED.HarmonyPatches;

[HarmonyPatch]
public static class MiscPatches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    [HarmonyPostfix]
    public static void ExplodePackOnDeath(Pawn __instance)
    {
        if (__instance.SpawnedOrAnyParentSpawned)
        {
            var apparel = __instance.apparel?.WornApparel.FirstOrDefault(ap => ap.def == VFED_DefOf.VFED_Apparel_BombPack);
            if (apparel != null)
            {
                GenExplosion.DoExplosion(__instance.PositionHeld, __instance.MapHeld, 13.5f, DamageDefOf.Bomb, apparel, 60, 5f);
                if (!apparel.Destroyed) apparel.Destroy();
            }
        }
    }

    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.CanFireNow))]
    [HarmonyPrefix]
    public static bool CheckForVisibility(IncidentWorker __instance, ref bool __result)
    {
        if (VisibilityEffect_Incident.ActivatedByVisibility.Contains(__instance.def)
         && !WorldComponent_Deserters.Instance.ActiveEffects
               .OfType<VisibilityEffect_Incident>()
               .Any(effect => effect.becomesActive && effect.incident == __instance.def))
        {
            __result = false;
            return false;
        }

        if (VisibilityEffect_Incident.DeactivatedByVisibility.Contains(__instance.def)
         && WorldComponent_Deserters.Instance.ActiveEffects
               .OfType<VisibilityEffect_Incident>()
               .Any(effect => effect.becomesInactive && effect.incident == __instance.def))
        {
            __result = false;
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(FactionDef), nameof(FactionDef.RaidCommonalityFromPoints))]
    [HarmonyPostfix]
    public static void EmpireRaidChance(FactionDef __instance, ref float __result)
    {
        if (__instance == FactionDefOf.Empire
         && WorldComponent_Deserters.Instance.Active
         && WorldComponent_Deserters.Instance.ActiveEffects.OfType<VisibilityEffect_RaidChance>().FirstOrDefault() is { multiplier: var multiplier })
            __result *= multiplier;
    }

    [HarmonyPatch(typeof(Designation), nameof(Designation.Notify_Added))]
    [HarmonyPostfix]
    public static void BiosecurityWarning(Designation __instance)
    {
        if (__instance.def == DesignationDefOf.Open && __instance.target.Thing is Building_CrateBiosecured)
            Messages.Message("VFED.BiosecuredCrateWarning".Translate(), __instance.target.Thing, MessageTypeDefOf.CautionInput);
    }

    [HarmonyPatch(typeof(WorkGiver_Open), nameof(WorkGiver_Open.HasJobOnThing))]
    [HarmonyPostfix]
    public static void CheckBiosecurity(Pawn pawn, Thing t, ref bool __result)
    {
        if (__result && t is Building_CrateBiosecured && !pawn.story.AllBackstories.Any(backstory => backstory.spawnCategories.Contains("ImperialRoyal")))
        {
            __result = false;
            JobFailReason.Is("VFED.CantOpenBiosecured".Translate());
        }
    }
}
