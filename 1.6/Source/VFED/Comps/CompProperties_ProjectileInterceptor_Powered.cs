using HarmonyLib;
using RimWorld;
using Verse;

namespace VFED;

[HarmonyPatch]
public class CompProperties_ProjectileInterceptor_Powered : CompProperties_ProjectileInterceptor
{
    public bool requiresPower;

    [HarmonyPatch(typeof(CompProjectileInterceptor), nameof(CompProjectileInterceptor.Active), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Active_Postfix(ref bool __result, CompProjectileInterceptor __instance)
    {
        if (__result && __instance.Props is CompProperties_ProjectileInterceptor_Powered { requiresPower: true }
                     && __instance.parent.TryGetComp<CompPowerTrader>() is null or { PowerOn: false }) __result = false;
    }
}
