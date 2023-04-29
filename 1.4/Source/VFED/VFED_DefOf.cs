using RimWorld;
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

    static VFED_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(VFED_DefOf));
    }
}
