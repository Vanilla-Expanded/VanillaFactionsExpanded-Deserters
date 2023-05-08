using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFED.HarmonyPatches;

[HarmonyPatch]
public static class MiscPatches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    [HarmonyPostfix]
    public static void ExplodePackOnDeath(Pawn __instance)
    {
        if (__instance.SpawnedOrAnyParentSpawned)
            foreach (var apparel in __instance.apparel.WornApparel)
                if (apparel.def == VFED_DefOf.VFED_Apparel_BombPack)
                {
                    GenExplosion.DoExplosion(__instance.PositionHeld, __instance.MapHeld, 13.5f, DamageDefOf.Bomb, apparel, 60, 5f);
                    if (!apparel.Destroyed) apparel.Destroy();
                    break;
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
}
