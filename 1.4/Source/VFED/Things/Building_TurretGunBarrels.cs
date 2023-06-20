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

    private float curAngle;
    private float rotationSpeed;

    private float rotationVelocity;
    public Vector3 CastSource => DrawPos + barrels[barrelIndex].RotatedBy(top.CurRotation);

    public static Vector3 GetCastSource(Thing thing) => thing is Building_TurretGunBarrels turret ? turret.CastSource : thing.DrawPos;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        var ext = def.GetModExtension<TurretExtension_Barrels>();
        barrels = ext.barrels.ToArray();
        rotationSpeed = ext.rotationSpeed;
        barrelIndex = barrels.Length - 1;
    }

    public override void Tick()
    {
        if (CurrentTarget.IsValid)
        {
            var targetAngle = (CurrentTarget.Cell.ToVector3Shifted() - DrawPos).AngleFlat();
            if (!Mathf.Approximately(targetAngle, curAngle))
            {
                curAngle = top.CurRotation = Mathf.SmoothDampAngle(curAngle, targetAngle, ref rotationVelocity, 0.01f, rotationSpeed,
                    1f / GenTicks.TicksPerRealSecond);
                return;
            }
        }
        else curAngle = top.CurRotation;

        base.Tick();
    }

    private void Notify_BurstComplete()
    {
        barrelIndex.Cycle(barrels);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref rotationVelocity, nameof(rotationVelocity));
    }

    public override string GetInspectString() =>
        Prefs.DevMode && CurrentTarget.IsValid
            ? base.GetInspectString() + $"\nCurrent Angle: {curAngle}, Target Angle: {(CurrentTarget.Cell.ToVector3Shifted() - DrawPos).AngleFlat()}"
            : base.GetInspectString();

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
    public float rotationSpeed;
}
