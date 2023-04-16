using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFED;

[HarmonyPatch]
public class Building_TurretGunBarrels : Building_TurretGun
{
    private int barrelIndex;
    private Vector3[] barrels;
    public Vector3 CastSource => DrawPos + barrels[barrelIndex].RotatedBy(top.CurRotation);

    public static Vector3 GetCastSource(Thing thing) => thing is Building_TurretGunBarrels turret ? turret.CastSource : thing.DrawPos;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        barrels = def.GetModExtension<TurretExtension_Barrels>().barrels.ToArray();
        barrelIndex = barrels.Length - 1;
    }

    private void Notify_BurstComplete()
    {
        barrelIndex.Cycle(barrels);
    }

    [HarmonyPatch(typeof(Building_TurretGun), "BurstComplete")]
    [HarmonyPostfix]
    public static void BurstComplete_Postfix(Building_TurretGun __instance)
    {
        if (__instance is Building_TurretGunBarrels turret) turret.Notify_BurstComplete();
    }

    [HarmonyPatch]
    public class CastSourceReplacer
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Verb_LaunchProjectile), "TryCastShot");
            yield return AccessTools.Method(typeof(GenDraw), nameof(GenDraw.DrawAimPie));
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ReplaceCall(IEnumerable<CodeInstruction> instructions) =>
            instructions.MethodReplacer(
                AccessTools.PropertyGetter(typeof(Thing), nameof(DrawPos)),
                AccessTools.Method(typeof(Building_TurretGunBarrels), nameof(GetCastSource)));
    }
}

// ReSharper disable InconsistentNaming
public class TurretExtension_Barrels : DefModExtension
{
    public List<Vector3> barrels;
}
