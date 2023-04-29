using HarmonyLib;
using RimWorld;

namespace VFED.HarmonyPatches;

[HarmonyPatch]
public static class GoodwillPatches
{
    public static bool IsEnemyBecauseDeserter(Faction a, Faction b) =>
        WorldComponent_Deserters.Instance.Active && ((a.IsPlayer && b.def == FactionDefOf.Empire) || (a.def == FactionDefOf.Empire && b.IsPlayer));

    [HarmonyPatch(typeof(Faction), nameof(Faction.CanChangeGoodwillFor))]
    [HarmonyPostfix]
    public static void CanChangeGoodwillFor_Postfix(Faction __instance, Faction other, ref bool __result)
    {
        __result = __result && !IsEnemyBecauseDeserter(__instance, other);
    }

    [HarmonyPatch(typeof(Faction), nameof(Faction.TryMakeInitialRelationsWith))]
    [HarmonyPostfix]
    public static void TryMakeInitialRelationsWith_Postfix(Faction __instance, Faction other)
    {
        if (IsEnemyBecauseDeserter(__instance, other))
            __instance.SetRelation(new FactionRelation { baseGoodwill = -100, other = other, kind = FactionRelationKind.Hostile });
    }

    [HarmonyPatch(typeof(GoodwillSituationWorker_PermanentEnemy), nameof(GoodwillSituationWorker_PermanentEnemy.ArePermanentEnemies))]
    [HarmonyPostfix]
    public static void ArePermanentEnemies_Postfix(Faction a, Faction b, ref bool __result)
    {
        __result = __result && !IsEnemyBecauseDeserter(a, b);
    }
}
