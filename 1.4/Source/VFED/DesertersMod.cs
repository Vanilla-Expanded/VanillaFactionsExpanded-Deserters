using HarmonyLib;
using Verse;

namespace VFED
{
    public class DesertersMod : Mod
    {
        public static Harmony Harm;

        public DesertersMod(ModContentPack content) : base(content)
        {
            Harm = new Harmony("vanillaexpanded.factions.deserters");
            Harm.PatchAll();
        }
    }
}
