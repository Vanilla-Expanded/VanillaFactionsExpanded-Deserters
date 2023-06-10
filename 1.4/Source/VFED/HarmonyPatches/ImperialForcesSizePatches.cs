using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MonoMod.Utils;
using RimWorld;

namespace VFED.HarmonyPatches;

[HarmonyPatch]
public static class ImperialForcesSizePatches
{
    private static readonly Func<PawnGroupMakerParms, PawnGroupMakerParms> copyParms;

    static ImperialForcesSizePatches()
    {
        var dm = new DynamicMethod("__COPYPARMS__", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
            CallingConventions.Standard, typeof(PawnGroupMakerParms),
            new[] { typeof(PawnGroupMakerParms) }, typeof(ImperialForcesSizePatches), true);
        var il = dm.GetILGenerator();
        il.Emit(OpCodes.Newobj, typeof(PawnGroupMakerParms).GetConstructor(Type.EmptyTypes)!);
        foreach (var fieldInfo in typeof(PawnGroupMakerParms).GetFields())
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fieldInfo);
            il.Emit(OpCodes.Stfld, fieldInfo);
        }

        il.Emit(OpCodes.Ret);

        copyParms = dm.CreateDelegate<Func<PawnGroupMakerParms, PawnGroupMakerParms>>();
    }

    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.GeneratePawns));
        yield return AccessTools.Method(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.GeneratePawnKindsExample));
    }

    [HarmonyPrefix]
    public static void Prefix(ref PawnGroupMakerParms parms)
    {
        parms = copyParms(parms);
        if (parms.faction == Faction.OfEmpire)
            parms.points = WorldComponent_Deserters.Instance.ActiveEffects.OfType<VisibilityEffect_ArmySize>()
               .Aggregate(parms.points, (p, effect) => p * effect.multiplier);
    }
}
