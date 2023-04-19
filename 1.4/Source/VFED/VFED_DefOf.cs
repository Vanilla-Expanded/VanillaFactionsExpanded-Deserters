using RimWorld;
using Verse;

namespace VFED;

[DefOf]
public class VFED_DefOf
{
    public static ThingDef VFED_AerodroneStrikeIncoming;
    public static ThingDef VFED_Mote_AerodroneStrike;

    static VFED_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(VFED_DefOf));
    }
}
