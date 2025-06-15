using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFED;

public class Letter_VisibilityChange : ChoiceLetter
{
    public VisibilityLevelDef visibilityLevel;

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            yield return Option_Close;

            if (quest is { hidden: false }) yield return Option_ViewInQuestsTab();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref visibilityLevel, nameof(visibilityLevel));
    }

    public override void OpenLetter()
    {
        var diaNode = new DiaNode(Text);
        diaNode.options.AddRange(Choices);
        var window = new Dialog_NodeTreeWithFactionInfoAndVisibility(diaNode, relatedFaction, visibilityLevel, false, radioMode, title);
        Find.WindowStack.Add(window);
    }
}

public class Dialog_NodeTreeWithFactionInfoAndVisibility : Dialog_NodeTreeWithFactionInfo
{
    private readonly VisibilityLevelDef visibleLevel;

    public Dialog_NodeTreeWithFactionInfoAndVisibility(DiaNode nodeRoot, Faction faction, VisibilityLevelDef visibilityLevel, bool delayInteractivity = false,
        bool radioMode = false,
        string title = null) : base(nodeRoot, faction, delayInteractivity, radioMode, title) =>
        visibleLevel = visibilityLevel;

    public override void DoWindowContents(Rect inRect)
    {
        base.DoWindowContents(inRect);
        GUI.DrawTexture(inRect.RightPartPixels(64).TopPartPixels(64), visibleLevel.Icon);
    }
}
