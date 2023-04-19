using System;
using RimWorld;
using Verse;

namespace VFED;

public static class Utilities
{
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
}
