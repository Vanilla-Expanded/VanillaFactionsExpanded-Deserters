﻿using RimWorld;
using Verse;

namespace VFED;

[DefOf]
public class VFED_DefOf
{
    public static ThingDef VFED_AerodroneStrikeIncoming;
    public static ThingDef VFED_Mote_AerodroneStrike;
    public static ThingDef VFED_Spark;

    public static ThingDef VFED_Intel;
    public static ThingDef VFED_CriticalIntel;

    public static ContrabandCategoryDef VFED_Imperial;

    public static QuestScriptDef VFED_DeadDrop;

    public static DesignationDef VFED_ExtractIntel;

    [DefAlias("VFED_ExtractIntel")] public static JobDef VFED_ExtractIntelJob;

    static VFED_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(VFED_DefOf));
    }
}