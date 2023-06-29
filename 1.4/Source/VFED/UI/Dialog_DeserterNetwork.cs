using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using VFECore.UItils;

namespace VFED;

public class Dialog_DeserterNetwork : Window
{
    private readonly List<TabRecord> tabs;
    public Map Map;
    public int TotalCriticalIntel;
    public int TotalIntel;
    private DeserterTabDef curTab;

    public Dialog_DeserterNetwork(Map map)
    {
        forcePause = true;
        doCloseButton = false;
        doCloseX = true;
        closeOnAccept = false;
        closeOnCancel = true;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = false;
        soundAppear = SoundDefOf.CommsWindow_Open;
        soundClose = SoundDefOf.CommsWindow_Close;
        soundAmbient = SoundDefOf.RadioComms_Ambience;
        tabs = DefDatabase<DeserterTabDef>.AllDefs.Select(def => new TabRecord(def.LabelCap, () => curTab = def, () => curTab == def)).ToList();
        Map = map;
    }

    public override Vector2 InitialSize => new(1000, 750);

    public bool HasIntel(int normal, int critical) => normal <= TotalIntel && critical <= TotalCriticalIntel;

    public bool HasIntel(int num, bool useCritical) => useCritical ? num <= TotalCriticalIntel : num <= TotalIntel;

    public bool TrySpendIntel(int normal, int critical)
    {
        if (normal > TotalIntel)
        {
            Messages.Message("VFED.NotEnough".Translate(VFED_DefOf.VFED_Intel.LabelCap, normal, TotalIntel), MessageTypeDefOf.RejectInput,
                false);
            return false;
        }

        if (critical > TotalCriticalIntel)
        {
            Messages.Message("VFED.NotEnough".Translate(VFED_DefOf.VFED_CriticalIntel.LabelCap, critical, TotalCriticalIntel),
                MessageTypeDefOf.RejectInput, false);
            return false;
        }

        TradeUtility.LaunchThingsOfType(VFED_DefOf.VFED_Intel, normal, Map, null);
        TradeUtility.LaunchThingsOfType(VFED_DefOf.VFED_CriticalIntel, critical, Map, null);

        TotalIntel -= normal;
        TotalCriticalIntel -= critical;

        return true;
    }

    public bool TrySpendIntel(int num, bool useCritical)
    {
        int normal = 0, critical = 0;
        if (useCritical) critical = num;
        else normal = num;
        return TrySpendIntel(normal, critical);
    }

    public override void PostOpen()
    {
        base.PostOpen();
        foreach (var beacon in Building_OrbitalTradeBeacon.AllPowered(Map))
        foreach (var cell in beacon.TradeableCells)
        foreach (var thing in cell.GetThingList(Map))
        {
            if (thing.def == VFED_DefOf.VFED_Intel) TotalIntel += thing.stackCount;
            if (thing.def == VFED_DefOf.VFED_CriticalIntel) TotalCriticalIntel += thing.stackCount;
        }

        foreach (var tab in DefDatabase<DeserterTabDef>.AllDefs) tab.Worker.Notify_Open(this);
        curTab = DefDatabase<DeserterTabDef>.AllDefs.First();
    }

    public override void DoWindowContents(Rect inRect)
    {
        var deserters = WorldComponent_Deserters.Instance;
        var left = inRect.TakeLeftPart(inRect.width / 5 * 2);
        var title = left.TakeTopPart(30f);
        using (new TextBlock(GameFont.Medium)) Widgets.Label(title, Faction.OfPlayer.Name);
        var visibilityLevel = deserters.VisibilityLevel;
        var text = "VFED.Visibility".Translate(visibilityLevel.LabelCap).Resolve();
        var width = Text.CalcSize(text).x + 35;
        var visibilityRect = title.TakeRightPart(width);
        visibilityRect = visibilityRect.ContractedBy(2f, 4.5f);
        Widgets.DrawHighlight(visibilityRect);
        if (Mouse.IsOver(visibilityRect))
        {
            Widgets.DrawHighlight(visibilityRect);
            var builder = new StringBuilder();
            builder.Append("VFED.CurrentVisibility".Translate().Colorize(ColoredText.TipSectionTitleColor));
            builder.AppendLine(deserters.Visibility.ToString()
               .Colorize(Color.Lerp(ColorLibrary.BrightGreen, ColorLibrary.RedReadable, Mathf.InverseLerp(0, 100, deserters.Visibility))));
            builder.AppendLine();
            builder.AppendLine(visibilityLevel.description);
            builder.AppendLine();
            builder.AppendLine("VFED.Effects".Translate().Colorize(ColoredText.TipSectionTitleColor));
            builder.Append("VFED.IntelCostModifier".Translate());
            builder.AppendLine(visibilityLevel.intelCostModifier.ToStringByStyle(ToStringStyle.FloatMaxOne, ToStringNumberSense.Factor)
               .Colorize(ColoredText.ImpactColor));
            builder.Append("VFED.ContrabandRecieveTimeModifier".Translate());
            builder.AppendLine(visibilityLevel.contrabandTimeToReceiveModifier.ToStringByStyle(ToStringStyle.FloatMaxOne, ToStringNumberSense.Factor)
               .Colorize(ColoredText.ImpactColor));
            builder.Append("VFED.ContrabandSiteTimeModifier".Translate());
            builder.AppendLine(visibilityLevel.contrabandSiteTimeActiveModifier.ToStringByStyle(ToStringStyle.FloatMaxOne, ToStringNumberSense.Factor)
               .Colorize(ColoredText.ImpactColor));
            builder.Append("VFED.ImperialResponseTime".Translate());
            builder.AppendLine(visibilityLevel.imperialResponseTime.SecondsToTicks().ToStringTicksToPeriodVerbose().Colorize(ColoredText.DateTimeColor));
            builder.Append("VFED.ImperialResponseType".Translate());
            builder.AppendLine(visibilityLevel.imperialResponseType != null
                ? visibilityLevel.imperialResponseType.LabelCap.Colorize(ColoredText.ThreatColor)
                : "VFED.NoResponse".Translate().Colorize(ColorLibrary.BrightGreen));
            builder.Append("VFED.SpecialEffects".Translate().Colorize(ColoredText.TipSectionTitleColor));
            if (WorldComponent_Deserters.Instance.ActiveEffects.NullOrEmpty())
                builder.Append("VFED.None".Translate().Colorize(ColorLibrary.BrightGreen));
            else
                builder.AppendLine().Append(WorldComponent_Deserters.Instance.ActiveEffects.Select(def => def.label).ToLineList("  - ", true));
            TooltipHandler.TipRegion(visibilityRect, builder.ToString());
        }

        GUI.DrawTexture(visibilityRect.TakeLeftPart(20), visibilityLevel.Icon);
        visibilityRect.TakeLeftPart(3);
        using (new TextBlock(TextAnchor.MiddleCenter))
            Widgets.Label(visibilityRect, text);

        Widgets.DrawLineHorizontal(left.x, left.yMin, left.width);

        Text.Font = GameFont.Small;

        var intelRect = left.TakeTopPart(30);
        Widgets.DrawLightHighlight(intelRect);
        if (Mouse.IsOver(intelRect)) Widgets.DrawHighlight(intelRect);
        Widgets.DefIcon(intelRect.TakeLeftPart(30).ContractedBy(1.5f), VFED_DefOf.VFED_Intel);
        Widgets.InfoCardButton(intelRect.TakeLeftPart(30).ContractedBy(3), VFED_DefOf.VFED_Intel);
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Widgets.Label(intelRect.TakeRightPart(80), TotalIntel.ToString());
            intelRect.TakeLeftPart(20);
            Widgets.Label(intelRect, VFED_DefOf.VFED_Intel.LabelCap);
        }

        intelRect = left.TakeTopPart(30);
        if (Mouse.IsOver(intelRect)) Widgets.DrawHighlight(intelRect);
        Widgets.DefIcon(intelRect.TakeLeftPart(30).ContractedBy(1.5f), VFED_DefOf.VFED_CriticalIntel);
        Widgets.InfoCardButton(intelRect.TakeLeftPart(30).ContractedBy(3), VFED_DefOf.VFED_CriticalIntel);
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Widgets.Label(intelRect.TakeRightPart(80), TotalCriticalIntel.ToString());
            intelRect.TakeLeftPart(20);
            Widgets.Label(intelRect, VFED_DefOf.VFED_CriticalIntel.LabelCap);
        }

        left.TakeTopPart(40);
        Widgets.DrawMenuSection(left);
        TabDrawer.DrawTabs(left, tabs, 1);
        curTab.Worker.DoLeftPart(left.ContractedBy(2));
        inRect.TakeLeftPart(3);
        curTab.Worker.DoMainPart(inRect.ContractedBy(2));
    }
}
