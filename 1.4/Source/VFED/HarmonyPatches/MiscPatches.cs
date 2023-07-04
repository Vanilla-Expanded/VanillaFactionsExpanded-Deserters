using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VFED.HarmonyPatches;

[HarmonyPatch]
public static class MiscPatches
{
    [HarmonyDelegate(typeof(AvoidGrid), "IncrementAvoidGrid")]
    public delegate void IncrementAvoidGrid(IntVec3 c, int num);

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    [HarmonyPostfix]
    public static void ExplodePackOnDeath(Pawn __instance)
    {
        if (__instance.SpawnedOrAnyParentSpawned)
        {
            var apparel = __instance.apparel?.WornApparel.FirstOrDefault(ap => ap.def == VFED_DefOf.VFED_Apparel_BombPack);
            if (apparel != null)
            {
                GenExplosion.DoExplosion(__instance.PositionHeld, __instance.MapHeld, 13.5f, DamageDefOf.Bomb, apparel, 60, 5f);
                if (!apparel.Destroyed) apparel.Destroy();
            }
        }
    }

    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.CanFireNow))]
    [HarmonyPrefix]
    public static bool CheckForVisibility(IncidentWorker __instance, ref bool __result)
    {
        if (VisibilityEffect_Incident.ActivatedByVisibility.Contains(__instance.def)
         && !WorldComponent_Deserters.Instance.ActiveEffects
               .OfType<VisibilityEffect_Incident>()
               .Any(effect => effect.becomesActive && effect.incident == __instance.def))
        {
            __result = false;
            return false;
        }

        if (VisibilityEffect_Incident.DeactivatedByVisibility.Contains(__instance.def)
         && WorldComponent_Deserters.Instance.ActiveEffects
               .OfType<VisibilityEffect_Incident>()
               .Any(effect => effect.becomesInactive && effect.incident == __instance.def))
        {
            __result = false;
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(FactionDef), nameof(FactionDef.RaidCommonalityFromPoints))]
    [HarmonyPostfix]
    public static void EmpireRaidChance(FactionDef __instance, ref float __result)
    {
        if (__instance == FactionDefOf.Empire
         && WorldComponent_Deserters.Instance.Active
         && WorldComponent_Deserters.Instance.ActiveEffects.OfType<VisibilityEffect_RaidChance>().FirstOrDefault() is { multiplier: var multiplier })
            __result *= multiplier;
    }

    [HarmonyPatch(typeof(Designation), nameof(Designation.Notify_Added))]
    [HarmonyPostfix]
    public static void BiosecurityWarning(Designation __instance)
    {
        if (__instance.def == DesignationDefOf.Open && __instance.target.Thing is Building_CrateBiosecured)
            Messages.Message("VFED.BiosecuredCrateWarning".Translate(), __instance.target.Thing, MessageTypeDefOf.CautionInput);
    }

    [HarmonyPatch(typeof(WorkGiver_Open), nameof(WorkGiver_Open.HasJobOnThing))]
    [HarmonyPostfix]
    public static void CheckBiosecurity(Pawn pawn, Thing t, ref bool __result)
    {
        if (__result && t is Building_CrateBiosecured && !pawn.story.AllBackstories.Any(backstory => backstory.spawnCategories.Contains("ImperialRoyal")))
        {
            __result = false;
            JobFailReason.Is("VFED.CantOpenBiosecured".Translate());
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> CheckBiosecurity(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var info1 = AccessTools.Field(typeof(DesignationDefOf), nameof(DesignationDefOf.Open));
        var idx1 = codes.FindIndex(ins => ins.LoadsField(info1));
        Label? label1 = null;
        var idx2 = codes.FindLastIndex(idx1, ins => ins.Branches(out label1));
        var idx3 = codes.FindIndex(idx2, ins => ins.opcode == OpCodes.Ldflda);
        var idx4 = codes.FindIndex(idx2, ins => ins.opcode == OpCodes.Ldloc_S);
        var info2 = codes[idx3].operand;
        var info3 = codes[idx4].operand;
        var label2 = generator.DefineLabel();
        var labels = codes[idx4].ExtractLabels();
        codes[idx4].labels.Add(label2);
        codes.InsertRange(idx4, new[]
        {
            new CodeInstruction(OpCodes.Ldloc, info3).WithLabels(labels),
            new CodeInstruction(OpCodes.Ldflda, info2),
            CodeInstruction.Call(typeof(LocalTargetInfo), "get_Thing"),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_2),
            CodeInstruction.Call(typeof(MiscPatches), nameof(CheckBiosecurity), new[] { typeof(Thing), typeof(Pawn), typeof(List<FloatMenuOption>) }),
            new CodeInstruction(OpCodes.Brfalse, label2),
            new CodeInstruction(OpCodes.Br, label1!.Value)
        });
        return codes;
    }

    public static bool CheckBiosecurity(Thing t, Pawn p, List<FloatMenuOption> opts)
    {
        if (t is Building_CrateBiosecured && !p.story.AllBackstories.Any(backstory => backstory.spawnCategories.Contains("ImperialRoyal")))
        {
            opts.Add(new FloatMenuOption("CannotOpen".Translate(t) + ": " + "VFED.CantOpenBiosecured".Translate().CapitalizeFirst(), null));
            return true;
        }

        return false;
    }

    [HarmonyPatch(typeof(MainTabWindow_Quests), "DoCharityIcon")]
    [HarmonyPostfix]
    public static void AddDeserterIcon(ref Rect innerRect, Quest ___selected)
    {
        if (___selected != null && ___selected.root.IsDeserterQuest())
        {
            var rect = new Rect(innerRect.xMax - 32f - 26f - 32f - 4f, innerRect.y, 32f, 32f);
            GUI.DrawTexture(rect, TexDeserters.DeserterQuestTex);
            if (Mouse.IsOver(rect)) TooltipHandler.TipRegion(rect, "VFED.DeserterQuestDesc".Translate());
        }
    }

    [HarmonyPatch(typeof(Site), nameof(Site.ShouldRemoveMapNow))]
    [HarmonyPostfix]
    public static void NoRemoveObjectiveMaps(bool __result, ref bool alsoRemoveWorldObject, Site __instance)
    {
        if (__result && alsoRemoveWorldObject && __instance.parts
               .Any(part => part.def.Worker is SitePartWorker_Objectives objectives && objectives.ShouldKeepSiteForObjectives(part)))
            alsoRemoveWorldObject = false;
    }

    [HarmonyPatch(typeof(AvoidGrid), "PrintAvoidGridAroundTurret")]
    [HarmonyPrefix]
    public static bool NoAvoidGridForLargeRange(Building_TurretGun tur) => tur.GunCompEq.PrimaryVerb.verbProps.range < GenRadial.MaxRadialPatternRadius;

    [HarmonyPatch(typeof(ThingListGroupHelper), nameof(ThingListGroupHelper.Includes))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> FixLudeonsStupidHardcoding(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var idx1 = codes.FindIndex(ins => ins.opcode == OpCodes.Ldtoken && (Type)ins.operand == typeof(CompAffectsSky));
        var idx2 = codes.FindIndex(idx1, ins => ins.opcode == OpCodes.Callvirt);
        codes[idx2].operand = AccessTools.Method(typeof(ThingDef), nameof(ThingDef.HasAssignableCompFrom));
        return codes;
    }

    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> APAlwaysHits(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var info1 = AccessTools.PropertyGetter(typeof(VerbProperties), nameof(VerbProperties.ForcedMissRadius));
        var idx1 = codes.FindIndex(ins => ins.Calls(info1));
        var idx2 = codes.FindIndex(idx1, ins => ins.Branches(out _));
        var label1 = generator.DefineLabel();
        var label2 = generator.DefineLabel();
        codes[idx2 + 1].labels.Add(label1);
        codes.InsertRange(idx2 + 1, new[]
        {
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFED_DefOf), nameof(VFED_DefOf.VFED_Bullet_Shell_ArmorPiercing))),
            new CodeInstruction(OpCodes.Beq, label2),
            new CodeInstruction(OpCodes.Br, label1),
            new CodeInstruction(OpCodes.Nop).WithLabels(label2),
            new CodeInstruction(OpCodes.Ldloc, 7),
            new CodeInstruction(OpCodes.Ldloc_3),
            new CodeInstruction(OpCodes.Ldloc, 6),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Verb), "currentTarget")),
            new CodeInstruction(OpCodes.Dup),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Verb), "preventFriendlyFire")),
            new CodeInstruction(OpCodes.Ldloc, 4),
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Callvirt,
                AccessTools.Method(typeof(Projectile), nameof(Projectile.Launch),
                    new[]
                    {
                        typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(ProjectileHitFlags), typeof(bool),
                        typeof(Thing), typeof(ThingDef)
                    })),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ret)
        });
        return codes;
    }

    [HarmonyPatch(typeof(PlaceWorker_ShowTurretRadius), nameof(PlaceWorker_ShowTurretRadius.AllowsPlacing))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> FixPlaceworkerForLargeRadius(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        var info1 = AccessTools.Field(typeof(VerbProperties), nameof(VerbProperties.range));
        var idx1 = codes.FindIndex(ins => ins.LoadsField(info1));
        Label? label1 = null;
        var idx2 = codes.FindIndex(idx1, ins => ins.Branches(out label1));
        if (idx2 == -1 || label1 == null)
            Log.Error("[VFED] Failed to find jump label in PlaceWorker_ShowTurretRadius");
        else
            codes.InsertRange(idx2 + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldfld, info1),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(GenRadial), nameof(GenRadial.MaxRadialPatternRadius))),
                new CodeInstruction(OpCodes.Bge, label1.Value)
            });

        info1 = AccessTools.Field(typeof(VerbProperties), nameof(VerbProperties.minRange));
        idx1 = codes.FindIndex(ins => ins.LoadsField(info1));
        label1 = null;
        idx2 = codes.FindIndex(idx1, ins => ins.Branches(out label1));
        if (idx2 == -1 || label1 == null)
            Log.Error("[VFED] Failed to find jump label in PlaceWorker_ShowTurretRadius");
        else
            codes.InsertRange(idx2 + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldfld, info1),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(GenRadial), nameof(GenRadial.MaxRadialPatternRadius))),
                new CodeInstruction(OpCodes.Bge, label1.Value)
            });

        return codes;
    }

    [HarmonyPatch(typeof(Quest), nameof(Quest.End))]
    [HarmonyPostfix]
    public static void CheckForPlotEnd(Quest __instance)
    {
        if (WorldComponent_Deserters.Instance.PlotMissions.Any(plotMission => plotMission.quest == __instance))
            WorldComponent_Deserters.Instance.Notify_PlotQuestEnded(__instance);
    }
}
