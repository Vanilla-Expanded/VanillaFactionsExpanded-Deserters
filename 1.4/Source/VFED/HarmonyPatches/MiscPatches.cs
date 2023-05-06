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
        foreach (var apparel in __instance.apparel.WornApparel)
            if (apparel.def == VFED_DefOf.VFED_Apparel_BombPack)
            {
                GenExplosion.DoExplosion(__instance.PositionHeld, __instance.MapHeld, 13.5f, DamageDefOf.Bomb, apparel, 60, 5f);
                if (!apparel.Destroyed) apparel.Destroy();
                break;
            }
    }
}
