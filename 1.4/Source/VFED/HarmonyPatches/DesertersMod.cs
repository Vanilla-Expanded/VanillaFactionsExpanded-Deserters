using HarmonyLib;
using Verse;

namespace VFED.HarmonyPatches;

public class DesertersMod : Mod
{
    public static Harmony Harm;

    public DesertersMod(ModContentPack content) : base(content)
    {
        Harm = new Harmony("ve.faction.deserters");
        Harm.PatchAll();
    }
}
