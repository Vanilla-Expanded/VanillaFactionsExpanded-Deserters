using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using VFEEmpire;

namespace VFED;

[HarmonyPatch]
public class WorldComponent_Deserters : WorldComponent, ICommunicable
{
    public static WorldComponent_Deserters Instance;

    public bool Active;
    public List<string> ActiveEffects = new();
    public int Visibility;
    public VisibilityLevelDef VisibilityLevel;

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
    }

    public void Notify_VisibilityChanged()
    {
        Visibility = Mathf.Clamp(Visibility, 0, 100);
        ActiveEffects.Clear();
        if (!Active) return;
        foreach (var def in DefDatabase<VisibilityLevelDef>.AllDefs.OrderBy(def => def.visibilityRange.max))
            if (def.visibilityRange.min < Visibility)
                ActiveEffects.AddRange(def.specialEffects);
            else if (def.visibilityRange.max > Visibility)
            {
                VisibilityLevel = def;
                break;
            }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Active, "active");
        Scribe_Values.Look(ref Visibility, "visibility");
        Notify_VisibilityChanged();
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
    public static IEnumerable<ICommunicable> GetCommTargets_Postfix(IEnumerable<ICommunicable> targets)
    {
        // For some reason this gets called twice, so ensure we don't add an extra one to it
        var found = false;
        foreach (var target in targets)
        {
            if (target == Instance) found = true;
            yield return target;
        }

        if (Instance.Active && !found) yield return Instance;
    }

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
}
