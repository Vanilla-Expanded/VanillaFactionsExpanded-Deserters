using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace VFED;

public class IncidentWorker_ImperialPatrol : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms) =>
        base.CanFireNowSub(parms) &&
        CaravanIncidentUtility.CanFireIncidentWhichWantsToGenerateMapAt(parms.target.Tile);

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (parms.target is not Caravan caravan) return false;
        var empire = parms.faction = Faction.OfEmpire;
        var leader = caravan.GetLeader();
        Find.Maps.Where(m => m.IsPlayerHome).TryRandomElement(out var colony);
        caravan.PawnsListForReading.Except(leader).TryRandomElement(out var random);
        parms.points = Mathf.Max(parms.points, 130);
        var makerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, parms);
        makerParms.generateFightersOnly = true;
        makerParms.dontUseSingleUseRocketLaunchers = true;
        var title = "VFED.ImperialPatrol".Translate(empire.NameColored);
        var text = "VFED.ImperialPatrol.Desc".Translate(leader.NameFullColored, empire.NameColored,
            (colony?.Parent as Settlement)?.Name ?? Faction.OfPlayer.NameColored,
            PawnUtility.PawnKindsToLineList(PawnGroupMakerUtility.GeneratePawnKindsExample(makerParms), "  - ", ColoredText.ThreatColor));
        var node = new DiaNode(text);
        node.options.Add(new DiaOption("VFED.ImperialPatrol.Escape".Translate())
        {
            action = delegate { Utilities.ChangeVisibility(10); },
            resolveTree = true
        });
        node.options.Add(new DiaOption("VFED.ImperialPatrol.Attack".Translate())
        {
            action = delegate
            {
                LongEventHandler.QueueLongEvent(delegate
                {
                    var enemies = PawnGroupMakerUtility.GeneratePawns(makerParms).ToList();
                    var map = CaravanIncidentUtility.SetupCaravanAttackMap(caravan, enemies, true);
                    LordMaker.MakeNewLord(parms.faction, new LordJob_AssaultColony(parms.faction, true, false), map, enemies);
                    Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
                }, "GeneratingMapForNewEncounter", false, null);
            },
            resolveTree = true
        });
        if (random != null)
            node.options.Add(new DiaOption("VFED.ImperialPatrol.GiveUp".Translate(random.NameFullColored))
            {
                action = delegate
                {
                    caravan.RemovePawn(random);
                    var list = new List<Thing>();
                    foreach (var thing in ThingOwnerUtility.GetAllThingsRecursively(random, false))
                    {
                        list.Add(thing);
                        thing.holdingOwner.Take(thing);
                    }

                    empire.kidnapped.Kidnap(random, null);
                    foreach (var t in list.Where(t => !t.Destroyed))
                        CaravanInventoryUtility.GiveThing(caravan, t);

                    Messages.Message("VFED.ImperialPatrol.GaveUp".Translate(leader.NameFullColored, random.NameFullColored), caravan,
                        MessageTypeDefOf.NegativeEvent);
                },
                resolveTree = true
            });
        Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(node, empire, false, false, title));
        return true;
    }
}
