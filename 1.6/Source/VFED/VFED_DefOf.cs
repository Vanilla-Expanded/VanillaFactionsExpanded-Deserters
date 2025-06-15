﻿using RimWorld;
using Verse;
using Verse.AI;

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
    public static ThingDef VFED_DeserterShuttle;
    public static ThingDef VFED_Bullet_Shell_ArmorPiercing;
    public static ThingDef VFED_CrashedAerodrone;
    public static ThingDef Mote_ProximityScannerRadius;
    public static ThingDef Mote_ActivatorProximityGlow;
    public static ThingDef VFED_Mote_ProximityScannerRadius_Green;
    public static ThingDef VFED_Mote_ActivatorProximityGlow_Green;
    public static ThingDef VFED_FlagshipChunk;
    public static ThingDef VFED_ImperialMegaHighShield;

    public static ThingDef VFED_Intel;
    public static ThingDef VFED_CriticalIntel;

    public static ContrabandCategoryDef VFED_Imperial;

    public static QuestScriptDef VFED_DeadDrop;
    public static QuestScriptDef VFED_PlotMission;
    public static QuestScriptDef VFED_EmpireBargain;
    public static QuestScriptDef VFED_DeserterEndgame;
    public static QuestScriptDef VFED_EmpireRuins;

    public static DesignationDef VFED_ExtractIntel;

    public static HediffDef VFED_Invisibility;

    public static FleckDef VFED_BloodMist;

    public static SoundDef DispensePaste;
    public static SoundDef VFE_ZeusCannon_Aiming;
    public static SoundDef VFE_ZeusCannon_Shot;
    public static SoundDef VFE_ZeusCannon_DistantExplosion;
    public static SoundDef VFE_ZeusCannon_Recharging;

    public static PawnKindDef VFEE_Empire_Fighter_Absolver;

    [DefAlias("VFED_ExtractIntel")] public static JobDef VFED_ExtractIntelJob;
    public static JobDef VFED_ExtractIntelPawn;
    public static JobDef VFED_FleeEnemies;

    public static ThingSetMakerDef VFED_Reward_ItemsSpecial;

    public static SongDef VFED_DivineInferno;

    public static TransportShipDef VFED_Ship_DeserterShuttle;

    public static DutyDef VFED_StandGuard;
    public static DutyDef VFED_SitOnThrone;
    public static DutyDef VFED_Patrol;

    public static RulePackDef VFED_OperationName;
    public static RulePackDef VFED_PlotName;

    public static SitePartDef VFED_ZeusCannonComplex;

    public static ThoughtDef VFED_JoinedDeserters;
    public static ThoughtDef VFED_UsedDeclassifier;

    static VFED_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(VFED_DefOf));
    }
}
