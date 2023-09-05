using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using HarmonyLib;
using KCSG;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.Grammar;
using VFEEmpire;

namespace VFED;

[HarmonyPatch]
public class WorldComponent_Deserters : WorldComponent, ICommunicable
{
    public static WorldComponent_Deserters Instance;

    public static PlotMissionInfo GeneratingPlot;

    public bool Active;
    public List<VisibilityEffect> ActiveEffects = new();
    public Dictionary<Site, SiteExtraData> DataForSites = new();
    public PriorityQueue<Action, int> EventQueue = new();
    public bool Locked;

    public List<PlotMissionInfo> PlotMissions = new();

    public List<Quest> ServiceQuests = new(10);
    public int Visibility;
    public VisibilityLevelDef VisibilityLevel;
    private List<SiteExtraData> tempExtraDataValues;

    private List<Site> tempSiteKeys;

    public WorldComponent_Deserters(World world) : base(world) => Instance = this;
    public string GetCallLabel() => null;

    public string GetInfoText() => null;
    public Faction GetFaction() => null;

    public void TryOpenComms(Pawn negotiator)
    {
        Find.WindowStack.Add(new Dialog_DeserterNetwork(negotiator.MapHeld));
    }

    public FloatMenuOption CommFloatMenuOption(Building_CommsConsole console, Pawn negotiator) =>
        FloatMenuUtility.DecoratePrioritizedTask(new("VFED.ContactDeserters".Translate(), delegate { console.GiveUseCommsJob(negotiator, this); },
            VFEE_DefOf.VFEE_Deserters.FactionIcon,
            EmpireUtility.Deserters.Color, MenuOptionPriority.InitiateSocial), negotiator, console);

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        if (Active)
        {
            if (Find.TickManager.TicksGame % 2500 == 0)
                foreach (var effect in ActiveEffects)
                    effect.TickRare();

            if (Find.TickManager.TicksGame % 60000 == 0)
            {
                Visibility -= DesertersMod.VisibilityChangePerDay;
                Utilities.StripTitles();
                Notify_VisibilityChanged();
                foreach (var effect in ActiveEffects)
                    effect.TickDay();
            }
        }

        while (EventQueue.TryPeek(out _, out var tick) && tick <= Find.TickManager.TicksGame) EventQueue.Dequeue()();
    }

    public void JoinDeserters(Quest fromQuest)
    {
        Faction.OfEmpire.TryAffectGoodwillWith(Faction.OfPlayer, Faction.OfEmpire.GoodwillToMakeHostile(Faction.OfPlayer), false, false);

        EmpireUtility.Deserters.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Ally, false);

        Active = true;
        Locked = false;

        Utilities.StripTitles(fromQuest);

        Find.FactionManager.goodwillSituationManager.RecalculateAll(true);
        Notify_VisibilityChanged(false, fromQuest);
    }

    public void BetrayDeserters(Quest fromQuest)
    {
        Active = false;
        Locked = true;
        Visibility = 0;
        VisibilityLevel = null;

        Faction.OfEmpire.TryAffectGoodwillWith(Faction.OfPlayer, 200, false, false);

        EmpireUtility.Deserters.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, false);

        foreach (var quest in Find.QuestManager.QuestsListForReading)
            if (quest.root.IsDeserterQuest() && quest.State is QuestState.NotYetAccepted or QuestState.Ongoing && quest != fromQuest)
                quest.End(quest.EverAccepted ? QuestEndOutcome.Fail : QuestEndOutcome.InvalidPreAcceptance);

        Find.FactionManager.goodwillSituationManager.RecalculateAll(true);
    }

    public void Notify_VisibilityChanged(bool fromLoad = false, Quest fromQuest = null)
    {
        Visibility = Mathf.Clamp(Visibility, 0, 100);
        var oldEffects = ActiveEffects.ListFullCopy();
        ActiveEffects.Clear();
        if (!Active) return;
        var oldLevel = VisibilityLevel;
        foreach (var def in DefDatabase<VisibilityLevelDef>.AllDefs.OrderBy(def => def.visibilityRange.max))
        {
            if (def.visibilityRange.min <= Visibility)
                if (def.specialEffects != null)
                    foreach (var effect in def.specialEffects)
                    {
                        ActiveEffects.RemoveAll(effect.Replaces);
                        ActiveEffects.Add(effect);
                    }

            if (def.visibilityRange.max >= Visibility)
            {
                VisibilityLevel = def;
                break;
            }
        }

        if (fromLoad)
            foreach (var effect in ActiveEffects)
                effect.OnLoadActive();
        else
        {
            foreach (var effect in oldEffects.Except(ActiveEffects)) effect.OnDeactivate();

            foreach (var effect in ActiveEffects.Except(oldEffects)) effect.OnActivate();
        }

        if (oldLevel != VisibilityLevel && !fromLoad)
        {
            var label = "VFED.VisibilityChange".Translate(VisibilityLevel.LabelCap);
            var desc = "VFED.VisibilityChange.Desc".Translate(VisibilityLevel.LabelCap);
            var deactivated = oldEffects.Except(ActiveEffects).ToList();
            var activated = ActiveEffects.Except(oldEffects).ToList();

            if (activated.Count > 0)
            {
                desc += "\n\n" + "VFED.VisibilityEffectsActive".Translate() + "\n";
                desc += activated.Select(effect => effect.label).ToLineList("  - ", true);
            }

            if (deactivated.Count > 0)
            {
                desc += "\n\n" + "VFED.VisibilityEffectsInactive".Translate() + "\n";
                desc += deactivated.Select(effect => effect.label).ToLineList("  - ", true);
            }

            var letter = new Letter_VisibilityChange
            {
                def = (oldLevel?.visibilityRange.max ?? 0) <= VisibilityLevel.visibilityRange.min
                    ? VisibilityLevel.visibilityRange.max == 100 ? LetterDefOf.ThreatBig : LetterDefOf.NegativeEvent
                    : LetterDefOf.PositiveEvent,
                Label = label,
                Text = desc,
                ID = Find.UniqueIDsManager.GetNextLetterID(),
                relatedFaction = EmpireUtility.Deserters,
                quest = fromQuest,
                visibilityLevel = VisibilityLevel
            };

            Find.LetterStack.ReceiveLetter(letter);
        }
    }

    public void EnsureQuestListFilled()
    {
        var points = StorytellerUtility.DefaultThreatPointsNow(Find.World);
        var storyState = Find.World.StoryState;
        while (ServiceQuests.Count < 10 && Utilities.DeserterQuests.Where(root => root.CanRun(points))
                  .TryRandomElementByWeight(root => NaturalRandomQuestChooser.GetNaturalRandomSelectionWeight(root, points, storyState),
                       out var questScript))
        {
            var slate = new Slate();
            slate.Set("points", points);
            slate.Set("purchasable", true);
            var quest = QuestGen.Generate(questScript, slate);
            quest.hidden = true;
            quest.hiddenInUI = true;
            quest.ticksUntilAcceptanceExpiry = -1;
            Find.QuestManager.Add(quest);
            ServiceQuests.Add(quest);
            var choice = quest.PartsListForReading.OfType<QuestPart_Choice>().First();
            choice.Choose(choice.choices.RandomElement());
        }
    }

    public void InitializePlots()
    {
        foreach (var def in WorldComponent_Hierarchy.Titles)
        {
            if (def.seniority < RoyalTitleDefOf.Knight.seniority) continue;

            var request = default(GrammarRequest);
            request.Includes.Add(VFED_DefOf.VFED_PlotName);
            request.Constants.Add("nobleTitle", def.defName);
            PlotMissions.Add(new()
            {
                royalTitle = def,
                name = NameGenerator.GenerateName(VFED_DefOf.VFED_OperationName, rootKeyword: "operationName"),
                title = NameGenerator.GenerateName(request, rootKeyword: "plotName"),
                quest = null
            });
        }

        GeneratePlotQuest(PlotMissions[0]);
    }

    public void Notify_PlotQuestEnded(Quest quest)
    {
        var plot = PlotMissions.Find(info => info.quest == quest);
        if (quest.State == QuestState.EndedSuccess)
        {
            plot.completedTick = Find.TickManager.TicksGame;
            var target = plot.target;
            plot.target = null;
            var targetTitle = plot.royalTitle;
            var idx = PlotMissions.IndexOf(plot) + 1;
            if (idx >= PlotMissions.Count) return;
            var bargainChance = targetTitle.seniority switch
            {
                700 => 1,
                701 => 0.1f,
                800 => 0.3f,
                801 => 0.5f,
                802 => 0.7f,
                900 => 0.9f,
                901 => 1,
                _ => 0
            };
            if (Rand.Chance(bargainChance))
            {
                var slate = new Slate();
                slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(Find.World));
                slate.Set("lastTarget", target);
                slate.Set("lastTarget_title", targetTitle);
                var bargainQuest = QuestGen.Generate(VFED_DefOf.VFED_EmpireBargain, slate);
                Find.QuestManager.Add(bargainQuest);
            }

            GeneratePlotQuest(PlotMissions[idx]);
        }
        else if (quest.State is QuestState.EndedFailed or QuestState.EndedInvalid) GeneratePlotQuest(plot);
    }

    private static void GeneratePlotQuest(PlotMissionInfo plot)
    {
        GeneratingPlot = plot;
        var points = StorytellerUtility.DefaultThreatPointsNow(Find.World);
        var slate = new Slate();
        slate.Set("points", points);
        slate.Set("nobleTitle", plot.royalTitle);
        var quest = QuestGen.Generate(plot.royalTitle == VFEE_DefOf.Emperor ? VFED_DefOf.VFED_DeserterEndgame : VFED_DefOf.VFED_PlotMission, slate);
        quest.hidden = true;
        quest.hiddenInUI = true;
        quest.ticksUntilAcceptanceExpiry = -1;
        Find.QuestManager.Add(quest);
        plot.quest = quest;
        GeneratingPlot = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Active, "active");
        Scribe_Values.Look(ref Locked, "locked");
        Scribe_Values.Look(ref Visibility, "visibility");
        Scribe_Collections.Look(ref ServiceQuests, "serviceQuests", LookMode.Reference);
        Scribe_Collections.Look(ref PlotMissions, "plotMissions", LookMode.Deep);
        if (Scribe.EnterNode("eventQueue"))
            try
            {
                if (Scribe.mode == LoadSaveMode.Saving)
                    foreach (var (action, priority) in EventQueue.ToList())
                    {
                        if (Scribe.EnterNode("li"))
                            try
                            {
                                Scribe.saver.WriteElement("action", action.Serialize());
                                Scribe.saver.WriteElement("triggerTick", priority.ToString());
                            }
                            finally { Scribe.ExitNode(); }
                    }
                else if (Scribe.mode == LoadSaveMode.LoadingVars)
                {
                    var curXmlParent = Scribe.loader.curXmlParent;
                    EventQueue ??= new();
                    EventQueue.Clear();
                    EventQueue.EnsureCapacity(curXmlParent.ChildNodes.Count);

                    foreach (var node in curXmlParent.ChildNodes.Cast<XmlNode>())
                    {
                        if (node.Name != "li" || node["triggerTick"] == null || node["action"] == null) continue;
                        var priority = ParseHelper.FromString<int>(node["triggerTick"].InnerText);
                        var action = node["action"].InnerText.DeserializeAction();

                        EventQueue.Enqueue(action, priority);
                    }
                }
            }
            finally { Scribe.ExitNode(); }

        if (Scribe.mode == LoadSaveMode.Saving) DataForSites.RemoveAll(kv => kv.Key.Destroyed);

        Scribe_Collections.Look(ref DataForSites, "extraSiteData", LookMode.Reference, LookMode.Deep, ref tempSiteKeys, ref tempExtraDataValues);

        ServiceQuests ??= new();
        DataForSites ??= new();
        PlotMissions ??= new();

        Notify_VisibilityChanged(true);
    }

    [HarmonyPatch(typeof(Building_CommsConsole), nameof(Building_CommsConsole.GetCommTargets))]
    [HarmonyPostfix]
    public static IEnumerable<ICommunicable> GetCommTargets_Postfix(IEnumerable<ICommunicable> targets) => Instance.Active ? targets.Append(Instance) : targets;

    [DebugAction("World", "Toggle Desertion", allowedGameStates = AllowedGameStates.Playing)]
    public static void ToggleDeserters()
    {
        if (Instance.Active)
        {
            Faction.OfEmpire.TryAffectGoodwillWith(Faction.OfPlayer, 75);
            Instance.Active = false;
        }
        else Instance.JoinDeserters(null);
    }

    [DebugAction("Quests", "Generate Plot Mission", allowedGameStates = AllowedGameStates.Playing)]
    public static List<DebugActionNode> GeneratePlotMission() =>
        Instance.PlotMissions.Where(info => !info.Available)
           .Select(info => new DebugActionNode(info.title, DebugActionType.Action, () => GeneratePlotQuest(info)))
           .ToList();


    public class PlotMissionInfo : IExposable
    {
        public int completedTick;
        public string name;
        public Quest quest;
        public RoyalTitleDef royalTitle;
        public Pawn target;
        public string title;

        public bool Available => quest != null;
        public bool Complete => quest is { State: QuestState.EndedSuccess };

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, nameof(name));
            Scribe_Values.Look(ref title, nameof(title));
            Scribe_References.Look(ref quest, nameof(quest));
            Scribe_References.Look(ref target, nameof(target));
            Scribe_Defs.Look(ref royalTitle, nameof(royalTitle));
            Scribe_Values.Look(ref completedTick, nameof(completedTick));
        }
    }

    public class SiteExtraData : IExposable
    {
        public Pawn noble;
        public float points;
        public TiledStructureDef structure;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref structure, nameof(structure));
            Scribe_Values.Look(ref points, nameof(points));
            Scribe_References.Look(ref noble, nameof(noble));
        }
    }
}
