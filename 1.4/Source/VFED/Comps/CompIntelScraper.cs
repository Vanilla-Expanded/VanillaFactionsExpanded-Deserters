using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFED;

public class CompIntelScraper : ThingComp
{
    private const int TicksPerPulse = 3600; // 60 seconds
    private bool active;
    private int pulsesLeft;
    private int ticksTillPulse;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (!respawningAfterLoad)
        {
            pulsesLeft = 10;
            ticksTillPulse = TicksPerPulse;
            active = false;
            if (!WorldComponent_Deserters.Instance.Active) WorldComponent_Deserters.Instance.JoinDeserters(null);
        }
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!active) return;
        ticksTillPulse--;
        if (ticksTillPulse <= 0) DoPulse();
    }

    private void DoPulse()
    {
        var effecterDef = DefDatabase<EffecterDef>.GetNamed("BurnoutMechlinkBoosterUsed");
        var effecter = new Effecter(effecterDef);
        effecter.Trigger(new(parent.Position, parent.Map), TargetInfo.Invalid);
        effecter.Cleanup();
        var visibility = WorldComponent_Deserters.Instance.Visibility;
        if (new (Action, float)[]
            {
                (ObtainIntel, 200 - visibility),
                (ObtainCriticalIntel, 10 - visibility / 10f),
                (IncreaseVisibility, 10 + visibility),
                (DoRaid, 2 + visibility)
            }.TryRandomElementByWeight(p => p.Item2, out var p)) p.Item1();
        ticksTillPulse = TicksPerPulse;
        pulsesLeft--;
        if (pulsesLeft <= 0) Expire();
    }

    private void Expire()
    {
        if (parent.TryGetComp<CompExplosive>() is { } explosive) explosive.StartWick();
        else parent.Destroy();

        active = false;
    }

    private void ObtainIntel()
    {
        var intel = ThingMaker.MakeThing(VFED_DefOf.VFED_Intel);
        var intelCount = intel.stackCount = (int)Rand.GaussianAsymmetric(1, 0, 10);
        if (GenPlace.TryPlaceThing(intel, parent.Position, parent.Map, ThingPlaceMode.Near))
            Messages.Message("VFED.BurnoutIntelGained".Translate(intelCount), intel, MessageTypeDefOf.PositiveEvent);
    }

    private void ObtainCriticalIntel()
    {
        var intel = ThingMaker.MakeThing(VFED_DefOf.VFED_CriticalIntel);
        intel.stackCount = Rand.Chance(0.1f) ? 2 : 1;
        if (GenPlace.TryPlaceThing(intel, parent.Position, parent.Map, ThingPlaceMode.Near))
            Messages.Message("VFED.BurnoutCriticalIntelGained".Translate(intel.stackCount), intel, MessageTypeDefOf.PositiveEvent);
    }

    private void IncreaseVisibility()
    {
        var visibilityGain = Rand.RangeInclusive(1, 10);
        Utilities.ChangeVisibility(visibilityGain);
        Messages.Message("VFED.BurnoutVisibilityIncreased".Translate(visibilityGain, WorldComponent_Deserters.Instance.Visibility), parent,
            MessageTypeDefOf.NegativeEvent);
    }

    private void DoRaid()
    {
        if (IncidentDefOf.RaidEnemy.Worker.TryExecute(new()
            {
                target = parent.Map,
                points = StorytellerUtility.DefaultThreatPointsNow(parent.Map) / 4f,
                faction = Faction.OfEmpire
            })) Messages.Message("VFED.BurnoutRaid".Translate(), parent, MessageTypeDefOf.NegativeEvent);
    }

    public override string CompInspectStringExtra() =>
        $"{"VFED.TimeToNextPulse".Translate(ticksTillPulse.ToStringSecondsFromTicks())}\n{"VFED.PulsesBeforeOut".Translate(pulsesLeft)}";

    public override IEnumerable<Gizmo> CompGetGizmosExtra() =>
        base.CompGetGizmosExtra()
           .Append(active
                ? new()
                {
                    defaultLabel = "VFED.DeactivateScraper".Translate(),
                    defaultDesc = "VFED.DeactivateScraper.Desc".Translate(),
                    icon = TexDeserters.IntelScraperTurnOff,
                    action = () => active = false
                }
                : new Command_Action
                {
                    defaultLabel = "VFED.ActivateScraper".Translate(),
                    defaultDesc = "VFED.ActivateScraper.Desc".Translate(),
                    icon = TexDeserters.IntelScraperTurnOn,
                    action = () => active = true,
                    disabled = pulsesLeft <= 0,
                    disabledReason = "VFED.Exhausted".Translate()
                });

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref active, nameof(active));
        Scribe_Values.Look(ref pulsesLeft, nameof(pulsesLeft));
        Scribe_Values.Look(ref ticksTillPulse, nameof(ticksTillPulse));
    }
}

public class PlaceWorker_DeserterOnly : PlaceWorker
{
    public override void PostPlace(Map map, BuildableDef def, IntVec3 loc, Rot4 rot)
    {
        base.PostPlace(map, def, loc, rot);
        if (!WorldComponent_Deserters.Instance.Active)
            Messages.Message("VFED.BurnoutWarning".Translate(), new TargetInfo(loc, map), MessageTypeDefOf.CautionInput);
    }

    public override bool IsBuildDesignatorVisible(BuildableDef def) => WorldComponent_Deserters.Instance.Active;
}
