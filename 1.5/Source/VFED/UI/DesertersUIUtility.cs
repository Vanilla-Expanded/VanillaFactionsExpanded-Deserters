using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using VFECore.UItils;

namespace VFED;

public static class DesertersUIUtility
{
    public static void DrawIntelCost(ref Rect inRect, string header, int intelCost, int criticalIntelCost)
    {
        using (new TextBlock(GameFont.Medium))
            Widgets.Label(inRect.TakeTopPart(30), header);

        Widgets.DrawLineHorizontal(inRect.x, inRect.yMin, inRect.width);

        var intelRect = inRect.TakeTopPart(30);
        Widgets.DrawLightHighlight(intelRect);
        if (Mouse.IsOver(intelRect)) Widgets.DrawHighlight(intelRect);
        Widgets.DefIcon(intelRect.TakeLeftPart(30).ContractedBy(1.5f), VFED_DefOf.VFED_Intel);
        Widgets.InfoCardButton(intelRect.TakeLeftPart(30).ContractedBy(3), VFED_DefOf.VFED_Intel);
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Widgets.Label(intelRect.TakeRightPart(60), intelCost.ToString());
            intelRect.TakeLeftPart(20);
            Widgets.Label(intelRect, VFED_DefOf.VFED_Intel.LabelCap);
        }

        intelRect = inRect.TakeTopPart(30);
        if (Mouse.IsOver(intelRect)) Widgets.DrawHighlight(intelRect);
        Widgets.DefIcon(intelRect.TakeLeftPart(30).ContractedBy(1.5f), VFED_DefOf.VFED_CriticalIntel);
        Widgets.InfoCardButton(intelRect.TakeLeftPart(30).ContractedBy(3), VFED_DefOf.VFED_CriticalIntel);
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Widgets.Label(intelRect.TakeRightPart(60), criticalIntelCost.ToString());
            intelRect.TakeLeftPart(20);
            Widgets.Label(intelRect, VFED_DefOf.VFED_CriticalIntel.LabelCap);
        }
    }

    public static bool DoPurchaseButton(Rect inRect, string text, int intelCost, int criticalIntelCost, Dialog_DeserterNetwork parent)
    {
        var cartEmpty = ContrabandManager.ShoppingCart.NullOrEmpty();
        if (!parent.HasIntel(intelCost, criticalIntelCost) || cartEmpty) GUI.color = Color.grey;
        if (Widgets.ButtonText(inRect, text) && !cartEmpty && parent.TrySpendIntel(intelCost, criticalIntelCost))
        {
            SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
            GUI.color = Color.white;
            return true;
        }

        GUI.color = Color.white;
        return false;
    }
}
