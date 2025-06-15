using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld.QuestGen;
using Verse;

namespace VFED.HarmonyPatches;

[StaticConstructorOnStartup]
public static class DebugPatches
{
    private static int level;

    static DebugPatches()
    {
        var prefix = new HarmonyMethod(typeof(DebugPatches), nameof(Debug_Prefix));
        var postfix = new HarmonyMethod(typeof(DebugPatches), nameof(Debug_Postfix));
        var postfixVoid = new HarmonyMethod(typeof(DebugPatches), nameof(Debug_Postfix_Void));
        foreach (var methodBase in GetToPatch())
            DesertersMod.Harm.Patch(methodBase, prefix, methodBase is not MethodInfo { ReturnType: var t } || t == typeof(void) ? postfixVoid : postfix);
    }

    public static IEnumerable<MethodBase> GetToPatch()
    {
        yield break;
        foreach (var type in typeof(QuestNode).AllSubclasses())
        {
            var method = AccessTools.Method(type, "TestRunInt");
            if (method.ReflectedType == method.DeclaringType && !method.IsAbstract) yield return method;
        }
    }

    public static void Debug_Prefix(object[] __args, MethodBase __originalMethod)
    {
        Log.Message(Indent() + FullName(__originalMethod) + $"({__args.Join(arg => arg?.ToString() ?? "null")})");
        level++;
    }

    public static void Debug_Postfix(object[] __args, MethodBase __originalMethod, object __result, bool __runOriginal)
    {
        level--;
        Log.Message(Indent() + FullName(__originalMethod) + $"({__args.Join(arg => arg?.ToString() ?? "null")})" +
                    $" -> {__result} ({(__runOriginal ? "original ran" : "original was skipped")})");
    }

    public static void Debug_Postfix_Void(object[] __args, MethodBase __originalMethod, bool __runOriginal)
    {
        level--;
        Log.Message(Indent() + FullName(__originalMethod) + $"({__args.Join(arg => arg?.ToString() ?? "null")})" +
                    $" -> void ({(__runOriginal ? "original ran" : "original was skipped")})");
    }

    private static string FullName(MethodBase method) => $"{method.DeclaringType?.Namespace}.{method.DeclaringType?.Name}.{method.Name}";

    private static string Indent() => string.Concat(Enumerable.Repeat("    ", level));
}
