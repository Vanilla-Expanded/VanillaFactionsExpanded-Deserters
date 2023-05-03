using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFED.HarmonyPatches;

[HarmonyPatch]
public static class QuestWindowPatches
{
    [HarmonyPatch(typeof(MainTabWindow_Quests), "DoCharityIcon")]
    public static void DoCharityIcon_Postfix(Rect innerRect, Quest ___selected)
    {
        if (___selected != null && ___selected.root.HasModExtension<QuestExtension_Deserter>())
        {
            var rect = new Rect(innerRect.xMax - 32f - 26f - 32f - 4f, innerRect.y, 32f, 32f);
            GUI.DrawTexture(rect, TexDeserters.DeserterQuestTex);
            if (Mouse.IsOver(rect)) TooltipHandler.TipRegion(rect, "VFED.DeserterQuestDesc".Translate());
        }
    }
}
