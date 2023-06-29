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

    public bool Active;
    public List<VisibilityEffect> ActiveEffects = new();
    public Dictionary<Site, SiteExtraData> DataForSites = new();
    public PriorityQueue<Action, int> EventQueue = new();

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
        FloatMenuUtility.DecoratePrioritizedTask(new
            FloatMenuOption("VFED.ContactDeserters".Translate(), delegate { console.GiveUseCommsJob(negotiator, this); }, VFEE_DefOf.VFEE_Deserters.FactionIcon,
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
                Notify_VisibilityChanged();
                foreach (var effect in ActiveEffects)
                    effect.TickDay();
            }
        }

        while (EventQueue.TryPeek(out _, out var tick) && tick <= Find.TickManager.TicksGame) EventQueue.Dequeue()();
    }

    public void JoinDeserters(Quest fromQuest)
    {
        Active = true;

        foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
            if (pawn?.royalty?.GetCurrentTitle(Faction.OfEmpire) is { } title)
            {
                var intelAmount = IntelForTitle(title);
                var intel = ThingMaker.MakeThing(VFED_DefOf.VFED_Intel);
                intel.stackCount = intelAmount;
                if (pawn.GetCaravan() is { } caravan)
                    caravan.AddPawnOrItem(intel, true);
                else if (pawn.MapHeld is { } pawnMap)
                    GenPlace.TryPlaceThing(intel, pawn.PositionHeld, pawnMap, ThingPlaceMode.Near);
                else if (Find.Maps.Where(map => map.IsPlayerHome).TryRandomElement(out var playerMap))
                {
                    var cell = DropCellFinder.TryFindSafeLandingSpotCloseToColony(playerMap, IntVec2.One, Faction.OfPlayer, 1);
                    DropPodUtility.DropThingsNear(cell, playerMap, Gen.YieldSingle(intel), canRoofPunch: false, allowFogged: false, faction: Faction.OfPlayer);
                }

                Messages.Message("VFED.IntelFromTitle".Translate(pawn.NameFullColored, title.GetLabelCapFor(pawn), intelAmount), intel,
                    MessageTypeDefOf.PositiveEvent, fromQuest);

                pawn.royalty.AllTitlesForReading.RemoveAll(royalTitle => royalTitle.def == title);
                pawn.royalty.AllFactionPermits.RemoveAll(permit => permit.Title == title);

                pawn.royalty.UpdateAvailableAbilities();
                pawn.Notify_DisabledWorkTypesChanged();
                pawn.needs?.AddOrRemoveNeedsAsAppropriate();
                pawn.apparel?.Notify_TitleChanged();

                QuestUtility.SendQuestTargetSignals(pawn.questTags, "TitleChanged", pawn.Named("SUBJECT"));
                MeditationFocusTypeAvailabilityCache.ClearFor(pawn);
            }

        Notify_VisibilityChanged();
    }

    public void Notify_VisibilityChanged(bool fromLoad = false)
    {
        Visibility = Mathf.Clamp(Visibility, 0, 100);
        var oldEffects = ActiveEffects.ListFullCopy();
        ActiveEffects.Clear();
        if (!Active) return;
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
    }

    public void EnsureQuestListFilled()
    {
        var points = StorytellerUtility.DefaultThreatPointsNow(Find.World);
        if (points < 300) points = 300;
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
            if (def == VFEE_DefOf.Emperor) continue;

            var request = default(GrammarRequest);
            request.Includes.Add(VFED_DefOf.VFED_PlotName);
            request.Constants.Add("nobleTitle", def.defName);
            PlotMissions.Add(new PlotMissionInfo
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
            var idx = PlotMissions.IndexOf(plot);
            GeneratePlotQuest(PlotMissions[idx + 1]);
        }
        else if (quest.State is QuestState.EndedFailed or QuestState.EndedInvalid) GeneratePlotQuest(plot);
    }

    private void GeneratePlotQuest(PlotMissionInfo plot)
    {
        var points = StorytellerUtility.DefaultThreatPointsNow(Find.World);
        if (points < 300) points = 300;
        var slate = new Slate();
        slate.Set("points", points);
        slate.Set("nobleTitle", plot.royalTitle);
        var quest = QuestGen.Generate(VFED_DefOf.VFED_PlotMission, slate);
        quest.hidden = true;
        quest.hiddenInUI = true;
        Find.QuestManager.Add(quest);
        plot.quest = quest;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Active, "active");
        Scribe_Values.Look(ref Visibility, "visibility");
        Scribe_Collections.Look(ref ServiceQuests, "serviceQuests", LookMode.Reference);
        Scribe_Collections.Look(ref DataForSites, "extraSiteData", LookMode.Reference, LookMode.Deep, ref tempSiteKeys, ref tempExtraDataValues);
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
                    EventQueue ??= new PriorityQueue<Action, int>();
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

        Notify_VisibilityChanged(true);
    }

    private static int IntelForTitle(RoyalTitleDef title) =>
        title.seniority switch
        {
            0 => 1, // Freeholder
            100 => 2, // Yeoman
            200 => 4, // Acolyte
            300 => 8, // Knight
            400 => 16, // Praetor
            500 => 24, // Baron
            600 => 36, // Count
            601 => 50, // Archcount
            602 => 66, // Marquess
            700 => 84, // Duke
            701 => 110, // Archduke
            800 => 140, // Consul
            801 => 180, // Magister
            802 => 225, // Despot
            900 => 280, // Stellarch
            901 => 350, // High Stellarch
            1000 => 400 // Emperor
        };

    [HarmonyPatch(typeof(Building_CommsConsole), nameof(Building_CommsConsole.GetCommTargets))]
    [HarmonyPostfix]
    public static IEnumerable<ICommunicable> GetCommTargets_Postfix(IEnumerable<ICommunicable> targets) => Instance.Active ? targets.Append(Instance) : targets;

    [DebugAction("World", "Increase Visibility By 10", allowedGameStates = AllowedGameStates.Playing)]
    public static void IncreaseVisibility()
    {
        Instance.Visibility += 10;
        Instance.Notify_VisibilityChanged();
    }

    [DebugAction("World", "Decrease Visibility By 10", allowedGameStates = AllowedGameStates.Playing)]
    public static void DecreaseVisibility()
    {
        Instance.Visibility -= 10;
        Instance.Notify_VisibilityChanged();
    }

    [DebugAction("World", "Toggle Desertion", allowedGameStates = AllowedGameStates.Playing)]
    public static void ToggleDeserters()
    {
        if (!Instance.Active)
            Faction.OfEmpire.TryAffectGoodwillWith(Faction.OfPlayer, Faction.OfEmpire.GoodwillToMakeHostile(Faction.OfPlayer));
        Instance.Active = !Instance.Active;
        if (!Instance.Active)
            Faction.OfEmpire.TryAffectGoodwillWith(Faction.OfPlayer, 75);
        Instance.Notify_VisibilityChanged();
    }

    public class PlotMissionInfo : IExposable
    {
        public int completedTick;
        public string name;
        public Quest quest;
        public RoyalTitleDef royalTitle;
        public string title;

        public bool Available => quest != null;
        public bool Complete => quest is { State: QuestState.EndedSuccess };

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, nameof(name));
            Scribe_Values.Look(ref title, nameof(title));
            Scribe_References.Look(ref quest, nameof(quest));
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
