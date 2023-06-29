using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace VFED;

public class QuestNode_DeserterRewards : QuestNode
{
    [NoTranslate] public SlateRef<string> inSignal;
    protected override bool TestRunInt(Slate slate) => WorldComponent_Deserters.Instance.Active;

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var restoreInfo = slate.GetRestoreInfo("inSignal");
        var deserters = slate.Get<Faction>("deserters");
        var givenSignal = inSignal.GetValue(QuestGen.slate);
        if (!givenSignal.NullOrEmpty())
            slate.Set("inSignal", QuestGenUtility.HardcodedSignalWithQuestID(givenSignal));

        var rewardValue = slate.Get<float>("rewardValue") * Find.Storyteller.difficulty.EffectiveQuestRewardValueFactor;

        var questPartChoice = new QuestPart_Choice
        {
            inSignalChoiceUsed = slate.Get<string>("inSignal")
        };

        var totalWorth = 0f;

        var choice = new QuestPart_Choice.Choice();
        var rewardItems = new Reward_Items();
        var intel = ThingMaker.MakeThing(VFED_DefOf.VFED_Intel);
        intel.stackCount = Mathf.FloorToInt(rewardValue / VFED_DefOf.VFED_Intel.BaseMarketValue);
        rewardItems.items.Add(intel);
        choice.rewards.Add(rewardItems);
        choice.rewards.Add(GetVisibilityReward(rewardItems.TotalMarketValue, true));
        AddAndProcessChoice(questPartChoice, choice, rewardValue, deserters);
        totalWorth += rewardItems.TotalMarketValue;

        choice = new QuestPart_Choice.Choice();
        rewardItems = new Reward_Items();
        var value = rewardValue * new FloatRange(0.7f, 1.3f);
        var makerParams = default(ThingSetMakerParams);
        makerParams.totalMarketValueRange = value;
        makerParams.makingFaction = deserters;
        rewardItems.items.AddRange(VFED_DefOf.VFED_Reward_ItemsSpecial.root.Generate(makerParams));
        choice.rewards.Add(rewardItems);
        choice.rewards.Add(GetVisibilityReward(rewardItems.TotalMarketValue, true));
        AddAndProcessChoice(questPartChoice, choice, rewardValue, deserters);
        totalWorth += rewardItems.TotalMarketValue;

        choice = new QuestPart_Choice.Choice();
        choice.rewards.Add(GetVisibilityReward(totalWorth / 2f, false));
        AddAndProcessChoice(questPartChoice, choice, rewardValue, deserters);

        QuestGen.quest.AddPart(questPartChoice);

        slate.Restore(restoreInfo);
    }

    private static Reward_Visibility GetVisibilityReward(float rewardValue, bool increase)
    {
        var reward = new Reward_Visibility();
        reward.InitFromValue(rewardValue, increase);
        return reward;
    }

    private static void AddAndProcessChoice(QuestPart_Choice questPartChoice, QuestPart_Choice.Choice choice, float rewardValue, Faction deserters)
    {
        var parms = default(RewardsGeneratorParams);
        parms.rewardValue = rewardValue;
        parms.giverFaction = deserters;
        for (var index = 0; index < choice.rewards.Count; index++)
            foreach (var questPart in choice.rewards[index].GenerateQuestParts(index, parms, null, null, null, null))
            {
                choice.questParts.Add(questPart);
                QuestGen.quest.AddPart(questPart);
            }

        questPartChoice.choices.Add(choice);
    }
}

public class Reward_Visibility : Reward
{
    private int visibilityChange;
    private bool ChangesVisibility => visibilityChange != 0;
    private int VisibilityChangeAbs => Math.Abs(visibilityChange);
    private bool IncreasesVisibility => visibilityChange > 0;

    public override float TotalMarketValue => ChangesVisibility ? VisibilityChangeAbs * (IncreasesVisibility ? 300 : 200) : 0;

    public override IEnumerable<GenUI.AnonymousStackElement> StackElements
    {
        get
        {
            if (!ChangesVisibility) yield break;

            yield return GetVisibilityStackElement(visibilityChange);
        }
    }

    public static GenUI.AnonymousStackElement GetVisibilityStackElement(float visibilityChange)
    {
        var increasesVisibility = visibilityChange > 0;
        var visibilityChangeAbs = Math.Abs(visibilityChange);
        return QuestPartUtility.GetStandardRewardStackElement(
            $"VFED.Visibility{(increasesVisibility ? "In" : "De")}creases".Translate(visibilityChangeAbs),
            increasesVisibility ? TexDeserters.VisibilityIncreaseTex : TexDeserters.VisibilityDecreaseTex, () =>
                $"VFED.Visibility{(increasesVisibility ? "In" : "De")}creases.Desc".Translate(visibilityChangeAbs));
    }

    public void InitFromValue(float rewardValue, bool increase)
    {
        if (increase)
            visibilityChange = Mathf.FloorToInt(rewardValue / 300f);
        else
            visibilityChange = -Mathf.FloorToInt(rewardValue / 200f);
    }

    public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText,
        RulePack customLetterLabelRules,
        RulePack customLetterTextRules)
    {
        if (!ChangesVisibility) yield break;

        yield return new QuestPart_ChangeVisibility
        {
            inSignal = QuestGen.slate.Get<string>("inSignal"),
            visibilityChange = visibilityChange
        };
    }

    public override string GetDescription(RewardsGeneratorParams parms) =>
        $"VFED.Visibility{(IncreasesVisibility ? "In" : "De")}creases".Translate(VisibilityChangeAbs);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref visibilityChange, nameof(visibilityChange));
    }
}

public class QuestPart_ChangeVisibility : QuestPart
{
    public string inSignal;
    public int visibilityChange;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == inSignal) Utilities.ChangeVisibility(visibilityChange);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, nameof(inSignal));
        Scribe_Values.Look(ref visibilityChange, nameof(visibilityChange));
    }
}
