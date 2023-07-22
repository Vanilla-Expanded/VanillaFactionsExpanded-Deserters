using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using VFECore;
using VFECore.UItils;
using VFEEmpire;

namespace VFED;

[HotSwappable]
public class DeserterTabWorker_Plots : DeserterTabWorker
{
    private readonly List<GenUI.AnonymousStackElement> stackElements = new();
    private Vector2 approachesScrollPos;
    private Vector2 descScrollPos;
    private float extraInfoRectLastHeight = 24;
    private Vector2 leftScrollPos;
    private WorldComponent_Deserters.PlotMissionInfo selectedPlot;

    public override void Notify_Open(Dialog_DeserterNetwork parent)
    {
        base.Notify_Open(parent);
        if (WorldComponent_Deserters.Instance.PlotMissions.NullOrEmpty()) WorldComponent_Deserters.Instance.InitializePlots();
        selectedPlot ??= WorldComponent_Deserters.Instance.PlotMissions[0];
    }

    public override void DoLeftPart(Rect inRect)
    {
        var plots = WorldComponent_Deserters.Instance.PlotMissions;
        var viewRect = new Rect(0, 0, inRect.width - 20, plots.Count * 80);
        Widgets.BeginScrollView(inRect, ref leftScrollPos, viewRect);
        for (var i = 0; i < plots.Count; i++)
        {
            var plot = plots[i];
            var rect = viewRect.TakeTopPart(80).ContractedBy(2, 0);
            var upperRect = rect.TakeTopPart(5);
            var lowerRect = rect.TakeBottomPart(5);
            upperRect.width = 4;
            upperRect.x = 33;
            lowerRect.width = 4;
            lowerRect.x = 33;
            if (i > 0)
            {
                GUI.color = Widgets.WindowBGFillColor;
                GUI.DrawTexture(upperRect, BaseContent.WhiteTex);
                GUI.color = Color.white;
            }

            if (i < plots.Count - 1)
            {
                GUI.color = Widgets.WindowBGFillColor;
                GUI.DrawTexture(lowerRect, BaseContent.WhiteTex);
                GUI.color = Color.white;
            }

            if (plot == selectedPlot) Widgets.DrawHighlightSelected(rect.ExpandedBy(2));

            if (plot.Available && Widgets.ButtonInvisible(rect)) selectedPlot = plot;

            var iconRect = rect.TakeLeftPart(70);

            GUI.color = Widgets.WindowBGFillColor;
            GUI.DrawTexture(iconRect, BaseContent.WhiteTex);
            GUI.color = Color.white;

            var titleExt = plot.royalTitle.GetModExtension<RoyalTitleDefExtension>();
            GUI.DrawTexture(iconRect, plot.Available ? titleExt.Icon : titleExt.GreyIcon);

            if (plot.Complete) GUI.DrawTexture(iconRect, TexDeserters.PlotCompletedTex);

            rect = rect.ContractedBy(2, 0);
            Widgets.Label(rect.TakeTopPart(25), plot.name);
            Widgets.Label(rect.TakeTopPart(25), "VFED.PlotTarget".Translate(plot.title));
            if (plot.Complete)
            {
                var location = Find.CurrentMap != null ? Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile) : default;
                Widgets.Label(rect.TakeTopPart(20),
                    "VFED.CompletedOn".Translate(GenDate.DateShortStringAt(GenDate.TickGameToAbs(plot.completedTick), location)));
            }
            else if (plot.Available)
            {
                var starRect = rect.TakeTopPart(20);
                for (var j = 0; j < plot.quest.challengeRating; j++) GUI.DrawTexture(starRect.TakeLeftPart(20), TexDeserters.RatingIcon);
            }
        }

        Widgets.EndScrollView();
    }

    public override void DoMainPart(Rect inRect)
    {
        using (new TextBlock(GameFont.Medium))
            Widgets.Label(inRect.TakeTopPart(30).ContractedBy(3, 0), "VFED.PlotDesc".Translate());
        if (selectedPlot.quest.State == QuestState.NotYetAccepted)
        {
            var descRect = inRect.TakeTopPart(174);
            Widgets.DrawMenuSection(descRect);
            descRect = descRect.ContractedBy(7);
            descRect.xMax += 5;
            var desc = selectedPlot.quest.description.Resolve();
            var viewRect = new Rect(0, 0, descRect.width - 20, Text.CalcHeight(desc, descRect.width - 20));
            Widgets.BeginScrollView(descRect, ref descScrollPos, viewRect);
            Widgets.Label(viewRect, desc);
            Widgets.EndScrollView();

            QuestPart_ApproachChoices approachChoices = null;
            var parts = selectedPlot.quest.PartsListForReading;
            for (var i = 0; i < parts.Count; i++)
            {
                approachChoices = parts[i] as QuestPart_ApproachChoices;
                if (approachChoices != null) break;
            }

            if (approachChoices != null)
            {
                var approaches = approachChoices.choices;
                viewRect = new Rect(0, 0, inRect.width - 20, approaches.Count * 150);
                Widgets.BeginScrollView(inRect, ref approachesScrollPos, viewRect);
                foreach (var approach in approaches)
                {
                    var rect = viewRect.TakeTopPart(150).ContractedBy(0, 1);
                    rect.xMax += 20;
                    Widgets.DrawMenuSection(rect);
                    rect = rect.ContractedBy(7);
                    using (new TextBlock(GameFont.Medium))
                        Widgets.Label(rect, approach.label);
                    if (Widgets.ButtonText(rect.TakeRightPart(60).ContractedBy(1, 3), "VFED.Select".Translate())
                     && Parent.TrySpendIntel(approach.info.intelCost, approach.info.useCriticalIntel))
                    {
                        selectedPlot.quest.hidden = false;
                        selectedPlot.quest.hiddenInUI = false;
                        approachChoices.Choose(approach);
                        selectedPlot.quest.Accept(null);
                        Parent.Close();
                        MainButtonDefOf.Quests.Worker.Activate();
                        ((MainTabWindow_Quests)MainButtonDefOf.Quests.TabWindow).Select(selectedPlot.quest);
                        break;
                    }

                    rect.yMin += 30;
                    rect = rect.ContractedBy(5, 0);
                    stackElements.Clear();
                    var extraInfoRect = rect.TakeBottomPart(extraInfoRectLastHeight);

                    stackElements.Add(Reward_Visibility.GetVisibilityStackElement(approach.info.visibilityGain));
                    stackElements.Add(QuestPartUtility.GetStandardRewardStackElement($"VFED.Combat{approach.info.combatLevel}".Translate(),
                        approach.info.combatLevel switch
                        {
                            QuestNode_ApproachChoices.Choice.CombatLevel.High => TexDeserters.CombatHighIcon,
                            QuestNode_ApproachChoices.Choice.CombatLevel.Medium => TexDeserters.CombatMediumIcon,
                            QuestNode_ApproachChoices.Choice.CombatLevel.Low => TexDeserters.CombatLowIcon,
                            _ => throw new ArgumentOutOfRangeException()
                        }, () => $"VFED.Combat{approach.info.combatLevel}Desc".Translate()));
                    var intelDef = approach.info.useCriticalIntel ? VFED_DefOf.VFED_CriticalIntel : VFED_DefOf.VFED_Intel;
                    stackElements.Add(QuestPartUtility.GetStandardRewardStackElement("VFED.Cost".Translate() + ": " + approach.info.intelCost,
                        intelDef.uiIcon, () => intelDef.DescriptionDetailed + "\n\n" + "ClickForMoreInfo".Translate().Colorize(ColoredText.SubtleGrayColor),
                        () =>
                            Find.WindowStack.Add(new Dialog_InfoCard(intelDef))));

                    extraInfoRectLastHeight = GenUI.DrawElementStack(extraInfoRect, 24f, stackElements,
                            delegate(Rect r, GenUI.AnonymousStackElement obj) { obj.drawer(r); },
                            obj => obj.width, 4f, 5f, false)
                       .height;
                    stackElements.Clear();

                    Widgets.Label(rect, approach.description);
                }

                Widgets.EndScrollView();
            }
        }
        else
        {
            if (selectedPlot.quest.State == QuestState.Ongoing
             && Widgets.ButtonText(inRect.TakeBottomPart(25).ContractedBy(10, 0), "VFED.ShowActiveQuest".Translate()))
            {
                Parent.Close();
                MainButtonDefOf.Quests.Worker.Activate();
                ((MainTabWindow_Quests)MainButtonDefOf.Quests.TabWindow).Select(selectedPlot.quest);
            }

            inRect.yMax -= 4;

            var rewardsRect = inRect.TakeBottomPart(15 + Text.LineHeight + extraInfoRectLastHeight);
            Widgets.DrawMenuSection(rewardsRect);
            rewardsRect = rewardsRect.ContractedBy(5f);
            Widgets.Label(rewardsRect.TakeTopPart(Text.LineHeight), "VFED.Rewards".Translate());
            Widgets.DrawLineHorizontal(rewardsRect.x + 2.5f, rewardsRect.y, rewardsRect.width - 5);
            rewardsRect.yMin += 5;
            QuestPart_ApproachChoices approachChoices = null;
            QuestPart_Choice choice = null;
            var parts = selectedPlot.quest.PartsListForReading;
            for (var i = 0; i < parts.Count; i++)
            {
                approachChoices ??= parts[i] as QuestPart_ApproachChoices;
                choice ??= parts[i] as QuestPart_Choice;
                if (approachChoices != null && choice != null) break;
            }

            if (approachChoices != null)
            {
                var approach = approachChoices.choices[0];
                stackElements.Add(QuestPartUtility.GetStandardRewardStackElement($"VFED.Combat{approach.info.combatLevel}".Translate(),
                    approach.info.combatLevel switch
                    {
                        QuestNode_ApproachChoices.Choice.CombatLevel.High => TexDeserters.CombatHighIcon,
                        QuestNode_ApproachChoices.Choice.CombatLevel.Medium => TexDeserters.CombatMediumIcon,
                        QuestNode_ApproachChoices.Choice.CombatLevel.Low => TexDeserters.CombatLowIcon,
                        _ => throw new ArgumentOutOfRangeException()
                    }, () => $"VFED.Combat{approach.info.combatLevel}Desc".Translate()));
            }

            if (choice != null)
            {
                var rewards = choice.choices[0].rewards;

                for (var i = 0; i < rewards.Count; i++) stackElements.AddRange(rewards[i].StackElements);
            }

            extraInfoRectLastHeight = GenUI.DrawElementStack(rewardsRect, 24f, stackElements,
                    delegate(Rect r, GenUI.AnonymousStackElement obj) { obj.drawer(r); },
                    obj => obj.width, 4f, 5f, false)
               .height;
            stackElements.Clear();
            inRect.yMax -= 4;

            var descRect = inRect;
            Widgets.DrawMenuSection(descRect);
            descRect = descRect.ContractedBy(7);
            descRect.xMax += 5;
            var desc = selectedPlot.quest.description.Resolve();
            var viewRect = new Rect(0, 0, descRect.width - 20, Text.CalcHeight(desc, descRect.width - 20));
            Widgets.BeginScrollView(descRect, ref descScrollPos, viewRect);
            Widgets.Label(viewRect, desc);
            Widgets.EndScrollView();
        }
    }
}
