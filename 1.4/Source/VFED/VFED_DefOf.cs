using RimWorld;
using Verse;

namespace VFED;

[DefOf]
public class VFED_DefOf
{
    public static ThingDef VFED_AerodroneStrikeIncoming;
    public static ThingDef VFED_Mote_AerodroneStrike;
    public static ThingDef VFED_Spark;
    public static ThingDef VFED_Apparel_BombPack;
    public static ThingDef VFED_Filth_Propaganda;
    public static ThingDef VFED_DropPodIncoming_Propaganda;

    public static ThingDef VFED_Intel;
    public static ThingDef VFED_CriticalIntel;

    public static ContrabandCategoryDef VFED_Imperial;

    public static QuestScriptDef VFED_DeadDrop;

    public static DesignationDef VFED_ExtractIntel;

    public static HediffDef VFED_Invisibility;

    public static FleckDef VFED_BloodMist;

    public static SoundDef DispensePaste;

    public static PawnKindDef VFEE_Empire_Fighter_Absolver;

    [DefAlias("VFED_ExtractIntel")] public static JobDef VFED_ExtractIntelJob;
    public static JobDef VFED_ExtractIntelPawn;

    public static ThingSetMakerDef VFED_Reward_ItemsSpecial;

    static VFED_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(VFED_DefOf));
    }
}
