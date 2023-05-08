using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable InconsistentNaming

namespace VFED;

public class VisibilityLevelDef : Def
{
    public float contrabandIntelCostModifier = 1;
    public float contrabandSiteTimeActiveModifier = 1;
    public float contrabandTimeToReceiveModifier = 1;
    public Texture2D Icon;
    public string iconPath;
    public float imperialResponseTime;
    public ImperialResponseDef imperialResponseType;
    public List<VisibilityEffect> specialEffects;
    public IntRange visibilityRange;

    public override void PostLoad()
    {
        base.PostLoad();
        if (specialEffects != null)
            foreach (var effect in specialEffects)
                effect.PostLoadSpecial();
        LongEventHandler.ExecuteWhenFinished(delegate { Icon = ContentFinder<Texture2D>.Get(iconPath); });
    }
}

public abstract class VisibilityEffect
{
    public string label;

    public virtual void PostLoadSpecial() { }

    public virtual bool Replaces(VisibilityEffect other) => false;

    public virtual void TickRare() { }
    public virtual void TickDay() { }
    public virtual void OnActivate() { }
    public virtual void OnDeactivate() { }
    public virtual void OnLoadActive() { }
}

public class VisibilityEffect_Incident : VisibilityEffect
{
    public static HashSet<IncidentDef> ActivatedByVisibility = new();
    public static HashSet<IncidentDef> DeactivatedByVisibility = new();
    public bool becomesActive;
    public bool becomesInactive;
    public IncidentDef incident;

    public override void PostLoadSpecial()
    {
        base.PostLoadSpecial();
        LongEventHandler.ExecuteWhenFinished(delegate
        {
            if (becomesActive) ActivatedByVisibility.Add(incident);

            if (becomesInactive) DeactivatedByVisibility.Add(incident);
        });
    }

    public override bool Replaces(VisibilityEffect other) => other is VisibilityEffect_Incident { incident: var otherIncident } && incident == otherIncident;
}

public class VisibilityEffect_ArmySize : VisibilityEffect
{
    public float multiplier;

    public override bool Replaces(VisibilityEffect other) => other is VisibilityEffect_ArmySize;
}

public class VisibilityEffect_RaidChance : VisibilityEffect
{
    public float multiplier;
    public override bool Replaces(VisibilityEffect other) => other is VisibilityEffect_RaidChance;
}

public class VisibilityEffect_AerodroneBombardment : VisibilityEffect
{
    private static bool active;

    public override void OnActivate()
    {
        base.OnActivate();
        active = true;

        Utilities.Schedule(Schedule, Find.TickManager.TicksGame + Rand.Range(2 * GenDate.TicksPerDay, 3 * GenDate.TicksPerDay));
    }

    public override void OnLoadActive()
    {
        base.OnLoadActive();
        active = true;
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        active = false;
    }

    private static void Schedule()
    {
        if (!active) return;
        Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter("VFED.AerodroneBombardment".Translate(),
            "VFED.AerodroneBombardment.Desc".Translate(Faction.OfEmpire.NameColored), LetterDefOf.NegativeEvent, Faction.OfEmpire));
        Utilities.Schedule(Trigger, Find.TickManager.TicksGame + 60f.SecondsToTicks());
    }

    private static void Trigger()
    {
        if (!active) return;

        static bool IsPlayer(Thing t) => t.Faction.IsPlayerSafe();

        foreach (var map in Find.Maps.Where(map => map.IsPlayerHome))
        {
            var targets = new List<Thing>();
            targets.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.AttackTarget).OfType<Building_Turret>().Where((Func<Thing, bool>)IsPlayer));
            targets.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.PowerTrader).OfType<Building_Battery>().Where((Func<Thing, bool>)IsPlayer));
            targets.AddRange(map.mapPawns.FreeColonists);
            targets.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.Bed).Where(IsPlayer));
            var items = new List<Thing>();
            ThingOwnerUtility.GetAllThingsRecursively(map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), items, false,
                WealthWatcher.WealthItemsFilter);
            if (items.Where(item =>
                    item.SpawnedOrAnyParentSpawned && !item.PositionHeld.Fogged(map) && (item.IsInAnyStorage() || map.areaManager.Home[item.PositionHeld]))
               .TryMaxBy(item => item.MarketValue * item.stackCount, out var item))
                targets.Add(item);

            for (var i = 0; i < 5; i++)
                if (targets.TryRandomElement(out var target))
                {
                    targets.Remove(target);
                    target.Position.DoAerodroneStrike(map);
                }
        }

        Utilities.Schedule(Schedule, Find.TickManager.TicksGame + Rand.Range(2 * GenDate.TicksPerDay, 3 * GenDate.TicksPerDay));
    }
}

public class VisibilityEffect_Goodwill : VisibilityEffect
{
    public int goodwill;

    public override void TickDay()
    {
        base.TickDay();
        if (Find.FactionManager.GetFactions().Where(x => x.CanChangeGoodwillFor(Faction.OfPlayer, goodwill)).TryRandomElement(out var faction))
            if (faction.TryAffectGoodwillWith(Faction.OfPlayer, goodwill))
                Messages.Message("VFED.GoodwillDropVisibility".Translate(faction.NameColored), MessageTypeDefOf.NegativeEvent);
    }
}

public class VisibilityEffect_GameCondition : VisibilityEffect
{
    public float durationDays;
    public GameConditionDef gameCondition;

    public override void OnActivate()
    {
        base.OnActivate();
        if (!Find.World.GameConditionManager.ActiveConditions.Any(cond => cond.def == gameCondition))
            Find.World.GameConditionManager.RegisterCondition(GameConditionMaker.MakeCondition(gameCondition, durationDays.DaysToTicks()));
    }

    public override bool Replaces(VisibilityEffect other) =>
        other is VisibilityEffect_GameCondition { gameCondition: var otherCondition } && otherCondition == gameCondition;
}
