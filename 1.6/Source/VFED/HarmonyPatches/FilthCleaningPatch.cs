using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFED.HarmonyPatches;

[HarmonyPatch]
public static class FilthCleaningPatch
{
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod() =>
        AccessTools.Method(AccessTools.Inner(typeof(JobDriver_CleanFilth), "<>c__DisplayClass7_0"), "<MakeNewToils>b__1");

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var info = AccessTools.Method(typeof(Filth), nameof(Filth.ThinFilth));
        var pawn = generator.DeclareLocal(typeof(Pawn));
        var info2 = AccessTools.Field(typeof(JobDriver), nameof(JobDriver.pawn));
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (instruction.Calls(info))
            {
                yield return new CodeInstruction(OpCodes.Ldloc_0);
                yield return new CodeInstruction(OpCodes.Ldloc, pawn);
                yield return CodeInstruction.Call(typeof(FilthExtension_OnClean), nameof(FilthExtension_OnClean.Notify_FilthCleaned));
            }
            else if (instruction.LoadsField(info2))
            {
                yield return new CodeInstruction(OpCodes.Dup);
                yield return new CodeInstruction(OpCodes.Stloc, pawn);
            }
        }
    }
}
