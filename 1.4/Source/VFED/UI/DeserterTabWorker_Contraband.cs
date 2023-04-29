using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using VFECore.UItils;
using static VFED.ContrabandManager;

namespace VFED;

public class DeserterTabWorker_Contraband : DeserterTabWorker
{
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;

    public override void DoLeftPart(Rect inRect)
    {
        var headerRect = inRect.TakeTopPart(15);
        headerRect.TakeLeftPart(80);
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft, null))
        {
            Widgets.Label(headerRect.TakeLeftPart(160), "VFED.ItemName".Translate());
            Widgets.Label(headerRect, "VFED.Cost".Translate());
        }

        var height = 0f;
        foreach (var (category, items) in ContrabandByCategory)
        {
            height += 25;
            if (OpenCategories[category]) height += items.Count * 35;
        }

        var viewRect = new Rect(0, 0, inRect.width - 20, height);
        Widgets.BeginScrollView(inRect, ref leftScrollPos, viewRect);
        foreach (var (category, items) in ContrabandByCategory)
        {
            var categoryRect = viewRect.TakeTopPart(25);
            var open = OpenCategories[category];
            if (Widgets.ButtonImage(categoryRect.TakeLeftPart(25), open ? TexButton.Collapse : TexButton.Reveal))
            {
                (open ? SoundDefOf.TabClose : SoundDefOf.TabOpen).PlayOneShotOnCamera();
                open = OpenCategories[category] = !open;
            }

            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(categoryRect, category.LabelCap);

            if (open)
                foreach (var (item, ext) in items)
                {
                    var itemRect = viewRect.TakeTopPart(35).ContractedBy(2.5f);
                    Widgets.DefIcon(itemRect.TakeLeftPart(30), item);
                    Widgets.InfoCardButton(itemRect.TakeLeftPart(30).ContractedBy(1.5f), item);
                    itemRect.TakeLeftPart(20);
                    if (Widgets.ButtonText(itemRect.TakeRightPart(30), ">")) AddToCart(item, ext);

                    itemRect.TakeRightPart(10);
                    using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(itemRect.TakeRightPart(50), ext.TotalIntelCost().ToString());
                    Widgets.DefIcon(itemRect.TakeRightPart(30), ext.useCriticalIntel ? VFED_DefOf.VFED_CriticalIntel : VFED_DefOf.VFED_Intel);
                    var text = item.LabelCap;
                    using (new TextBlock(Text.CalcSize(text).x > itemRect.width ? GameFont.Tiny : GameFont.Small, TextAnchor.MiddleLeft, null))
                        Widgets.Label(itemRect, text);
                }
        }

        Widgets.EndScrollView();
    }

    public override void DoMainPart(Rect inRect)
    {
        using (new TextBlock(GameFont.Medium)) Widgets.Label(inRect.TakeTopPart(30), "VFED.Basket".Translate());

        var basketRect = inRect.TakeTopPart(350);
        Widgets.DrawMenuSection(basketRect);
        basketRect = basketRect.ContractedBy(10);
        var items = ShoppingCart.ToList();
        var viewRect = new Rect(0, 0, basketRect.width - 20, items.Count * 35);
        Widgets.BeginScrollView(basketRect, ref rightScrollPos, viewRect);
        foreach (var ((item, ext), count) in items)
        {
            var itemRect = viewRect.TakeTopPart(35).ContractedBy(2.5f);
            Widgets.DefIcon(itemRect.TakeLeftPart(30), item);
            Widgets.InfoCardButton(itemRect.TakeLeftPart(30).ContractedBy(1.5f), item);
            itemRect.TakeLeftPart(20);
            var buttonRect = itemRect.TakeRightPart(30);
            if (count > 1 && Widgets.ButtonText(buttonRect, "0")) RemoveFromCart(item, ext, true);

            buttonRect = itemRect.TakeRightPart(30);
            if (Widgets.ButtonText(buttonRect, "<")) RemoveFromCart(item, ext);

            itemRect.TakeRightPart(10);
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(itemRect.TakeRightPart(50), ext.TotalIntelCost().ToString());
            Widgets.DefIcon(itemRect.TakeRightPart(30), ext.useCriticalIntel ? VFED_DefOf.VFED_CriticalIntel : VFED_DefOf.VFED_Intel);
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(itemRect, item.LabelCap + " x" + count);
        }

        Widgets.EndScrollView();

        using (new TextBlock(GameFont.Tiny))
            Widgets.Label(inRect.TakeTopPart(50), "VFED.ContrabandRecieveDesc".Translate().Colorize(ColoredText.SubtleGrayColor));

        basketRect = inRect.TakeTopPart(150);

        var buttonsRect = basketRect.TakeRightPart(150);
        buttonsRect.yMin -= 5;
        if (Widgets.ButtonText(buttonsRect.TakeTopPart(100).ContractedBy(25, 5), "VFED.Purchase".Translate())) { }

        if (Widgets.ButtonText(buttonsRect.ContractedBy(25, 0), "VFED.RushDelivery".Translate())) { }


        using (new TextBlock(GameFont.Medium))
            Widgets.Label(basketRect.TakeTopPart(30), "VFED.TotalCost".Translate());

        Widgets.DrawLineHorizontal(basketRect.x, basketRect.yMin, basketRect.width);

        var intelRect = basketRect.TakeTopPart(30);
        Widgets.DrawLightHighlight(intelRect);
        if (Mouse.IsOver(intelRect)) Widgets.DrawHighlight(intelRect);
        Widgets.DefIcon(intelRect.TakeLeftPart(30).ContractedBy(1.5f), VFED_DefOf.VFED_Intel);
        Widgets.InfoCardButton(intelRect.TakeLeftPart(30).ContractedBy(3), VFED_DefOf.VFED_Intel);
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Widgets.Label(intelRect.TakeRightPart(60), TotalCostIntel.ToString());
            intelRect.TakeLeftPart(20);
            Widgets.Label(intelRect, VFED_DefOf.VFED_Intel.LabelCap);
        }

        intelRect = basketRect.TakeTopPart(30);
        if (Mouse.IsOver(intelRect)) Widgets.DrawHighlight(intelRect);
        Widgets.DefIcon(intelRect.TakeLeftPart(30).ContractedBy(1.5f), VFED_DefOf.VFED_CriticalIntel);
        Widgets.InfoCardButton(intelRect.TakeLeftPart(30).ContractedBy(3), VFED_DefOf.VFED_CriticalIntel);
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Widgets.Label(intelRect.TakeRightPart(60), TotalCostCriticalIntel.ToString());
            intelRect.TakeLeftPart(20);
            Widgets.Label(intelRect, VFED_DefOf.VFED_CriticalIntel.LabelCap);
        }

        using (new TextBlock(GameFont.Tiny))
            Widgets.Label(basketRect, "VFED.RushDeliveryDesc".Translate().Colorize(ColoredText.SubtleGrayColor));

        inRect.yMin -= 20;

        using (new TextBlock(GameFont.Medium))
        {
            Widgets.Label(inRect.TakeTopPart(30), "VFED.TimeToReceive".Translate(
                    TotalAmount * 7500 * WorldComponent_Deserters.Instance.VisibilityLevel.contrabandTimeToReceiveModifier / 60000f,
                    TotalAmount * 15000 * WorldComponent_Deserters.Instance.VisibilityLevel.contrabandTimeToReceiveModifier / 60000f)
               .Resolve()
               .ResolveTags());
            Widgets.Label(inRect.TakeTopPart(30), "VFED.TimeToPickup".Translate((900000 - TotalAmount * 60000)
                  * WorldComponent_Deserters.Instance.VisibilityLevel.contrabandSiteTimeActiveModifier / 60000f)
               .Resolve()
               .ResolveTags());
        }

        using (new TextBlock(GameFont.Tiny)) Widgets.Label(inRect, "VFED.TimeLimitDesc".Translate().Colorize(ColoredText.SubtleGrayColor));
    }

    public override void Notify_Open()
    {
        ClearCart();
    }
}

public class ContrabandExtension : DefModExtension
{
    public ContrabandCategoryDef category;
    public int intelCost = -1;
    public bool useCriticalIntel;
}

public class ContrabandCategoryDef : Def
{
    public ContrabandCategoryDef() => description = label;
}
