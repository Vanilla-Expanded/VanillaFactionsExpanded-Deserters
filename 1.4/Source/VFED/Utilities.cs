using System;
using System.Collections.Generic;
using HarmonyLib;
using MonoMod.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFED;

public static class Utilities
{
    private static readonly Func<Projectile, float> getArcHeightFactor =
        AccessTools.PropertyGetter(typeof(Projectile), "ArcHeightFactor").CreateDelegate<Func<Projectile, float>>();

    private static readonly Func<Projectile, float> getDistanceCoveredFraction =
        AccessTools.PropertyGetter(typeof(Projectile), "DistanceCoveredFraction").CreateDelegate<Func<Projectile, float>>();

    public static int Wrap(int value, int min, int max)
    {
        if (min == max) return min;
        while (value < min) value = max - (min - value);
        while (value > max) value = min + (value - max);
        return value;
    }

    public static void Cycle(this ref int value, int min, int max) => value = Wrap(value + 1, min, max);
    public static void Cycle(this ref int value, Array arr) => Cycle(ref value, 0, arr.Length - 1);

    public static void DoAerodroneStrike(this IntVec3 cell, Map map)
    {
        (MoteMaker.MakeStaticMote(cell.ToVector3Shifted(), map, VFED_DefOf.VFED_Mote_AerodroneStrike, 1f, true) as Mote_Strike)?.LaunchStrike(
            VFED_DefOf.VFED_AerodroneStrikeIncoming, 10);
    }

    public static Vector3 ActualDrawPos(this Thing thing) =>
        thing is Projectile proj
            ? proj.DrawPos + new Vector3(0f, 0f, 1f) * (getArcHeightFactor(proj) * GenMath.InverseParabola(getDistanceCoveredFraction(proj)))
            : thing.DrawPos;

    public static bool Contains(this IntRange range, int count) => count >= range.TrueMin && count <= range.TrueMax;

    public static int TotalIntelCost(this ContrabandExtension ext) =>
        Mathf.FloorToInt(ext.intelCost * WorldComponent_Deserters.Instance.VisibilityLevel.contrabandIntelCostModifier);

    public static FloatRange ReceiveTimeRange(int totalAmount) =>
        new(
            totalAmount * 7500 * WorldComponent_Deserters.Instance.VisibilityLevel.contrabandTimeToReceiveModifier / 60000f,
            totalAmount * 15000 * WorldComponent_Deserters.Instance.VisibilityLevel.contrabandTimeToReceiveModifier / 60000f);

    public static float SiteExistTime(int totalAmount) =>
        (totalAmount >= 25 ? -Mathf.Log(totalAmount - 24) + 5 : 30 - totalAmount)
      * WorldComponent_Deserters.Instance.VisibilityLevel.contrabandSiteTimeActiveModifier;

    public static int DaysToTicks(this float days) => Mathf.RoundToInt(days * GenDate.TicksPerDay);

    public static IEnumerable<T> LogInline<T>(this IEnumerable<T> source, Func<T, string> toString = null)
    {
        foreach (var item in source)
        {
            Log.Message(toString == null ? item.ToString() : toString(item));
            yield return item;
        }
    }
}
