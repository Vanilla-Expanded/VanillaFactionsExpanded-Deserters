using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.Grammar;
using VFEEmpire;
using VFEEmpire.HarmonyPatches;

namespace VFED;

public class QuestNode_BetrayalRewards : QuestNode
{
    [NoTranslate] public SlateRef<string> inSignal;
    protected override bool TestRunInt(Slate slate) => WorldComponent_Deserters.Instance.Active;

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var restoreInfo = slate.GetRestoreInfo("inSignal");
        var empire = slate.Get<Faction>("empire");
        var map = slate.Get<Map>("map");
        var lastTargetTitle = slate.Get<RoyalTitleDef>("lastTarget_title");
        var givenSignal = inSignal.GetValue(QuestGen.slate);
        if (!givenSignal.NullOrEmpty())
            slate.Set("inSignal", QuestGenUtility.HardcodedSignalWithQuestID(givenSignal));
        var questPartChoice = new QuestPart_Choice
        {
            inSignalChoiceUsed = slate.Get<string>("inSignal")
        };
        var choice = new QuestPart_Choice.Choice();
        var honor = lastTargetTitle.seniority switch
        {
            700 => 21,
            701 => 31,
            800 => 45,
            801 => 65,
            802 => 93,
            900 => 133,
            901 => 189,
            _ => 0
        };
        if (honor > 0)
        {
            var honorReward = new Reward_RoyalFavor
            {
                faction = empire,
                amount = honor
            };
            choice.rewards.Add(honorReward);
        }

        var wealthPercent = lastTargetTitle.seniority switch
        {
            700 => 0.001f,
            701 => 0.0035f,
            800 => 0.01f,
            801 => 0.015f,
            802 => 0.02f,
            900 => 0.03f,
            901 => 0.05f,
            _ => 0f
        };
        if (wealthPercent > 0)
        {
            var wealth = map.PlayerWealthForStoryteller;
            var silverReward = new Reward_Items();
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = Mathf.FloorToInt(wealth * wealthPercent);
            silverReward.items.Add(silver);
            choice.rewards.Add(silverReward);
        }

        var rewardValueRange = lastTargetTitle.seniority switch
        {
            800 => new FloatRange(1000, 2000),
            801 => new FloatRange(1500, 3000),
            802 => new FloatRange(2000, 4000),
            900 => new FloatRange(3000, 6000),
            901 => new FloatRange(5000, 10000),
            _ => FloatRange.Zero
        };
        if (rewardValueRange.Span > 0)
        {
            var makerParams = default(ThingSetMakerParams);
            makerParams.totalMarketValueRange = rewardValueRange;
            makerParams.makingFaction = empire;
            var itemsReward = new Reward_Items();
            itemsReward.items.AddRange(VFED_DefOf.VFED_Reward_ItemsSpecial.root.Generate(makerParams));
            choice.rewards.Add(itemsReward);
        }

        var numHonors = lastTargetTitle.seniority switch
        {
            802 => 1,
            900 => 3,
            901 => 3,
            _ => 0
        };
        if (numHonors > 0)
            for (var i = 0; i < numHonors; i++)
            {
                var honorReward = new Reward_Honor();
                var value = 2000f;
                honorReward.Honor = HonorUtility.Generate(ref value);
                choice.rewards.Add(honorReward);
            }

        var numTechPrints = lastTargetTitle.seniority switch
        {
            901 => 5,
            _ => 0
        };
        if (numTechPrints > 0)
        {
            var techprintReward = new Reward_Items();
            for (var i = 0; i < numTechPrints; i++)
                if (TechprintUtility.TryGetTechprintDefToGenerate(empire, out var techprint))
                    techprintReward.items.Add(ThingMaker.MakeThing(techprint));
            choice.rewards.Add(techprintReward);
        }

        var soldierKind = lastTargetTitle.seniority switch
        {
            802 => PawnKindDefOf.Empire_Fighter_Janissary,
            900 => PawnKindDefOf.Empire_Fighter_Cataphract,
            901 => VFEE_DefOf.Empire_Fighter_StellicGuardRanged,
            _ => null
        };
        if (soldierKind != null)
            for (var i = 0; i < 2; i++)
            {
                var pawnReward = new Reward_Pawn();
                var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(soldierKind, empire, mustBeCapableOfViolence: true));
                QuestGen.AddToGeneratedPawns(pawn);
                pawnReward.pawn = pawn;
                pawnReward.arrivalMode = Reward_Pawn.ArrivalMode.DropPod;
                choice.rewards.Add(pawnReward);
            }

        var parms = default(RewardsGeneratorParams);
        parms.rewardValue = map.PlayerWealthForStoryteller * wealthPercent + rewardValueRange.Average + numHonors * 500 + numTechPrints * 1000
                          + (soldierKind?.combatPower ?? 0f) * 2 + RewardsGenerator.RewardValueToRoyalFavorCurve.EvaluateInverted(honor);
        parms.giverFaction = empire;
        var builder = new StringBuilder();

        for (var i = 0; i < choice.rewards.Count; i++)
        {
            var reward = choice.rewards[i];
            builder.AppendLine(reward.GetDescription(parms));
            foreach (var questPart in reward.GenerateQuestParts(i, parms, null, null, null, null))
            {
                choice.questParts.Add(questPart);
                QuestGen.quest.AddPart(questPart);
            }
        }

        var lines = builder.ToString().Split('\n');
        builder.Clear();
        foreach (var line in lines)
        {
            var str = line.Trim();
            if (str.Length == 0) continue;
            if (str.StartsWith("-"))
                builder.AppendLine("    " + line.TrimEnd());
            else
                builder.AppendLine("  - " + str);
        }

        QuestGen.AddQuestDescriptionRules(new List<Rule>
        {
            new Rule_String("reward_description", builder.ToString().TrimEndNewlines())
        });

        questPartChoice.choices.Add(choice);
        QuestGen.quest.AddPart(questPartChoice);
        slate.Restore(restoreInfo);
    }
}

public class QuestNode_BetrayDeserters : QuestNode
{
    [NoTranslate] public SlateRef<string> inSignal;

    protected override void RunInt()
    {
        QuestGen.quest.AddPart(new QuestPart_BetrayDeserters
        {
            inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(QuestGen.slate)) ?? QuestGen.slate.Get<string>("inSignal")
        });
    }

    protected override bool TestRunInt(Slate slate) => !WorldComponent_Deserters.Instance.Active;
}

public class QuestPart_BetrayDeserters : QuestPart
{
    public string inSignal;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == inSignal) WorldComponent_Deserters.Instance.BetrayDeserters(quest);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, nameof(inSignal));
    }
}
