using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.AI.Group;
using static VFED.QuestNode_ApproachChoices.Choice;

namespace VFED;

public class QuestNode_ApproachChoices : QuestNode
{
    public List<Choice> choices = new();
    public SlateRef<string> inSignalFailure;
    public SlateRef<string> inSignalSuccess;

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var part = new QuestPart_ApproachChoices
        {
            noble = slate.Get<Pawn>("noble")
        };
        var rewardValue = slate.Get<float>("rewardValue") * Find.Storyteller.difficulty.EffectiveQuestRewardValueFactor;
        var deserters = slate.Get<Faction>("deserters");
        foreach (var (name, root, info) in choices)
        {
            var choice = new QuestPart_ApproachChoices.Choice
            {
                name = name,
                info = info
            };
            var parts = QuestGen.quest.PartsListForReading.ListFullCopy();
            slate.PushPrefix(name, true);
            QuestGenUtility.RunInnerNode(root, slate.Get<string>("inSignal"));
            info.intelCost = slate.Get<int>("intelCost");
            info.visibilityGain = slate.Get<int>("visibilityGain");
            QuestGen.AddTextRequest("approach_label", label => choice.label = label);
            QuestGen.AddTextRequest("approach_desc", desc => choice.description = desc);
            slate.PopPrefix();
            choice.questParts.AddRange(QuestGen.quest.PartsListForReading.Except(parts));
            var rewardsSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignalSuccess.GetValue(slate));
            var rewards = new QuestPart_Choice
            {
                inSignalChoiceUsed = rewardsSignal
            };
            var reward = new QuestPart_Choice.Choice();
            var visibility = new Reward_Visibility();
            visibility.InitFromValue(info.visibilityGain * 300, true);
            reward.rewards.Add(visibility);
            var rewardItems = new Reward_Items();
            var value = rewardValue * new FloatRange(0.7f, 1.3f);
            var makerParams = default(ThingSetMakerParams);
            makerParams.totalMarketValueRange = value;
            makerParams.makingFaction = deserters;
            rewardItems.items.AddRange(VFED_DefOf.VFED_Reward_ItemsSpecial.root.Generate(makerParams));
            reward.rewards.Add(rewardItems);


            var restoreInfo = slate.GetRestoreInfo("inSignal");
            slate.Set("inSignal", rewardsSignal);
            foreach (var rewardReward in reward.rewards)
            {
                var parms = default(RewardsGeneratorParams);
                parms.rewardValue = rewardValue;
                parms.giverFaction = deserters;
                foreach (var rewardQuestPart in rewardReward.GenerateQuestParts(0, parms, null, null, null, null))
                {
                    choice.questParts.Add(rewardQuestPart);
                    reward.questParts.Add(rewardQuestPart);
                    QuestGen.quest.AddPart(rewardQuestPart);
                }
            }

            slate.Restore(restoreInfo);

            rewards.choices.Add(reward);
            choice.questParts.Add(rewards);
            QuestGen.quest.AddPart(rewards);

            var changeVisibility = new QuestPart_ChangeVisibility
            {
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignalFailure.GetValue(slate)),
                visibilityChange = info.visibilityGain
            };
            choice.questParts.Add(changeVisibility);
            QuestGen.quest.AddPart(changeVisibility);

            part.choices.Add(choice);
        }

        QuestGen.quest.AddPart(part);
    }

    protected override bool TestRunInt(Slate slate) =>
        choices.Any(choice =>
        {
            slate.PushPrefix(choice.name, true);
            var result = choice.node.TestRun(slate);
            slate.PopPrefix();
            return result;
        });


    public class Choice
    {
        public enum CombatLevel
        {
            Low, Medium, High
        }

        public ChoiceInfo info;
        public string name;
        public QuestNode node;

        public void Deconstruct(out string name, out QuestNode node, out ChoiceInfo info)
        {
            name = this.name;
            node = this.node;
            info = this.info;
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            name = xmlRoot.Name;
            info = new ChoiceInfo
            {
                combatLevel = (CombatLevel)Enum.Parse(typeof(CombatLevel), xmlRoot.Attributes!["CombatLevel"].Value),
                useCriticalIntel = ParseHelper.FromString<bool>(xmlRoot.Attributes["UseCriticalIntel"]?.Value ?? "false")
            };
            node = new QuestNode_Sequence
            {
                nodes = DirectXmlToObject.ObjectFromXml<List<QuestNode>>(xmlRoot["nodes"], true)
            };
        }

        public class ChoiceInfo : IExposable
        {
            public CombatLevel combatLevel;
            public int intelCost;
            public bool useCriticalIntel;
            public int visibilityGain;

            public void ExposeData()
            {
                Scribe_Values.Look(ref intelCost, nameof(intelCost));
                Scribe_Values.Look(ref combatLevel, nameof(combatLevel));
                Scribe_Values.Look(ref visibilityGain, nameof(visibilityGain));
                Scribe_Values.Look(ref useCriticalIntel, nameof(useCriticalIntel));
            }

            public override string ToString() =>
                $"combatLevel={combatLevel}, intelCost={intelCost}, useCriticalIntel={useCriticalIntel}, visibilityGain={visibilityGain}";
        }
    }
}

public class QuestPart_ApproachChoices : QuestPart
{
    public List<Choice> choices = new();
    public Pawn noble;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref noble, nameof(noble));
        Scribe_Collections.Look(ref choices, nameof(choices), LookMode.Deep);
    }

    public void Choose(Choice choice)
    {
        for (var i = choices.Count - 1; i >= 0; i--)
            if (choices[i] != choice)
            {
                foreach (var part in choices[i].questParts.Except(choice.questParts))
                {
                    if (part is QuestPart_SpawnWorldObject { worldObject: Site site })
                    {
                        site.Destroy();
                        WorldComponent_Deserters.Instance.DataForSites.Remove(site);
                    }
                    else if (part is QuestPart_SetupTransportShip { pawns: var pawns })
                        foreach (var pawn in pawns.Except(noble))
                        {
                            if (pawn.GetLord() is { } lord)
                            {
                                lord.RemovePawn(noble);
                                lord.Notify_PawnLost(pawn, PawnLostCondition.Vanished);
                            }

                            if (pawn.IsWorldPawn()) Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
                            else
                            {
                                pawn.Destroy();
                                pawn.Discard(true);
                            }
                        }

                    part.Notify_PreCleanup();
                    part.Cleanup();
                    quest.RemovePart(part);
                }

                choices.RemoveAt(i);
            }

        quest.description += "\n\n" + choice.description;
    }

    public class Choice : IExposable
    {
        public string description;
        public ChoiceInfo info;
        public string label;
        public string name;
        public List<QuestPart> questParts = new();


        public void ExposeData()
        {
            Scribe_Deep.Look(ref info, nameof(info));
            Scribe_Values.Look(ref name, nameof(name));
            Scribe_Values.Look(ref label, nameof(label));
            Scribe_Values.Look(ref description, nameof(description));
            Scribe_Collections.Look(ref questParts, nameof(questParts), LookMode.Reference);
        }
    }
}
