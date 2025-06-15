using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using VFEEmpire;

namespace VFED;

public class MapComponent_FlagshipFight : MapComponent
{
    public bool Active;
    public List<Building_ZeusCannon> Cannons;
    public Building_CannonControl Controller;

    public float FlagshipHealth;
    private string shipDamagedSignal;
    private string shipDestroyedSignal;
    public MapComponent_FlagshipFight(Map map) : base(map) { }
    public bool CannonsShouldFire => Controller is { Controlled: true } && FlagshipHealth > 0;

    public void Initiate(string shipDamaged, string shipDestroyed)
    {
        Active = true;
        FlagshipHealth = 1f;
        shipDamagedSignal = shipDamaged;
        shipDestroyedSignal = shipDestroyed;
        Cannons = new();
        foreach (var thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            switch (thing)
            {
                case Building_CannonControl controller:
                    Controller = controller;
                    break;
                case Building_ZeusCannon cannon:
                    Cannons.Add(cannon);
                    break;
            }

        for (var i = 0; i < 2; i++)
        {
            if (!CellFinder.TryFindRandomEdgeCellWith(c => !c.Fogged(map) && c.Standable(map), map, CellFinder.EdgeRoadChance_Friendly, out var cell))
                cell = CellFinder.RandomEdgeCell(map);
            if (!DropCellFinder.TryFindDropSpotNear(cell, map, out var landingSpot, false, false, false, VFED_DefOf.VFED_DeserterShuttle.size))
                landingSpot = DropCellFinder.GetBestShuttleLandingSpot(map, EmpireUtility.Deserters);
            var lord = LordMaker.MakeNewLord(EmpireUtility.Deserters, new LordJob_AssistColony(EmpireUtility.Deserters, map.Center), map);
            var pawns = new List<Pawn>();
            for (var j = 0; j < 8; j++)
                pawns.Add(PawnGenerator.GeneratePawn(new(VFEE_DefOf.VFEE_Deserter, EmpireUtility.Deserters, mustBeCapableOfViolence: true,
                    biocodeWeaponChance: 1, biocodeApparelChance: 1)));
            lord.AddPawns(pawns);
            Utilities.DropShuttleCustom(pawns, map, landingSpot, EmpireUtility.Deserters, VFED_DefOf.VFED_DeserterShuttle,
                VFED_DefOf.VFED_Ship_DeserterShuttle);
        }
    }

    public void DamageFlagship(float damage)
    {
        FlagshipHealth -= damage;
        Find.SignalManager.SendSignal(new(shipDamagedSignal));
        for (var i = 0; i < 4; i++)
            if (CellFinderLoose.TryFindSkyfallerCell(ThingDefOf.ShipChunkIncoming, map,TerrainAffordanceDefOf.Walkable, out var cell, 10, map.Center, 999999))
                SkyfallerMaker.SpawnSkyfaller(ThingDefOf.ShipChunkIncoming, VFED_DefOf.VFED_FlagshipChunk, cell, map);
        if (FlagshipHealth <= 0f)
        {
            var empire = Faction.OfEmpire;
            foreach (var lord in map.lordManager.lords)
                if (lord.faction == empire)
                    try
                    {
                        var toil = lord.Graph.lordToils.OfType<LordToil_PanicFlee>().FirstOrDefault();
                        if (toil == null)
                        {
                            toil = new()
                            {
                                useAvoidGrid = true
                            };
                            lord.Graph.AddToil(toil);
                        }

                        Messages.Message("MessageFightersFleeing".Translate(lord.faction.def.pawnsPlural.CapitalizeFirst(), lord.faction.Name),
                            MessageTypeDefOf.NeutralEvent);
                        lord.GotoToil(toil);
                    }
                    catch (Exception e) { Log.Error($"[VFE - Deserters] Failed to cleanup lord {lord}: {e}"); }

            var deserterFaction = EmpireUtility.Deserters;
            try
            {
                var deserters = map.mapPawns.PawnsInFaction(deserterFaction);
                foreach (var deserter in deserters) deserter.GetLord()?.RemovePawn(deserter);
                LordMaker.MakeNewLord(deserterFaction, new LordJob_ExitMapBest(LocomotionUrgency.Walk, true, true), map, deserters);
            }
            catch (Exception e) { Log.Error($"[VFE - Deserters] Failed to cleanup deserters: {e}"); }

            ShipCountdown.InitiateCountdown("VFED.EndgameText".Translate(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists
               .Select(p => p.Name.ToStringFull)
               .ToLineList("  - ", true)));

            Find.SignalManager.SendSignal(new(shipDestroyedSignal));

            empire.defeated = true;
            empire.hidden = true;
            deserterFaction.defeated = true;

            WorldComponent_Deserters.Instance.Active = false;
            WorldComponent_Deserters.Instance.Locked = true;

            foreach (var settlement in Find.WorldObjects.Settlements)
                try
                {
                    if (settlement.Faction == empire && Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out var newFaction, true))
                        settlement.SetFaction(newFaction);
                }
                catch (Exception e) { Log.Error($"[VFE - Deserters] Failed to reassign faction of Empire settlement {settlement}: {e}"); }

            foreach (var pawn in PawnsFinder.All_AliveOrDead)
            {
                if (!pawn.Dead)
                    try
                    {
                        if (pawn.Faction == empire)
                        {
                            if (!pawn.Spawned) pawn.Kill(new DamageInfo(DamageDefOf.Bomb, 999));
                            else if (Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out var newFaction, true)) pawn.SetFaction(newFaction);
                        }
                        else if (pawn.Faction == deserterFaction && Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out var newFaction, true))
                            pawn.SetFaction(newFaction);
                    }
                    catch (Exception e) { Log.Error($"[VFE - Deserters] Failed to clean up pawn {pawn}: {e}"); }

                if (pawn.royalty != null)
                    try
                    {
                        pawn.royalty.AllTitlesForReading.RemoveAll(royalTitle => royalTitle.faction == empire);
                        pawn.royalty.AllFactionPermits.RemoveAll(permit => permit.Faction == empire);

                        pawn.royalty.UpdateAvailableAbilities();
                        pawn.abilities.Notify_TemporaryAbilitiesChanged();
                        pawn.Notify_DisabledWorkTypesChanged();
                        pawn.needs?.AddOrRemoveNeedsAsAppropriate();
                        pawn.apparel?.Notify_TitleChanged();
                        QuestUtility.SendQuestTargetSignals(pawn.questTags, "TitleChanged", pawn.Named("SUBJECT"));
                        MeditationFocusTypeAvailabilityCache.ClearFor(pawn);
                    }
                    catch (Exception e) { Log.Error($"[VFE - Deserters] Failed to remove titles and permits from {pawn}: {e}"); }
            }

            foreach (var quest in Find.QuestManager.QuestsListForReading)
                if ((quest.root.IsDeserterQuest() || quest.InvolvedFactions.Any(f => f == empire || f == deserterFaction))
                 && quest.root != VFED_DefOf.VFED_DeserterEndgame
                 && quest.State is QuestState.NotYetAccepted or QuestState.Ongoing)
                    try { quest.End(quest.EverAccepted ? QuestEndOutcome.Fail : QuestEndOutcome.InvalidPreAcceptance); }
                    catch (Exception e) { Log.Error($"[VFE - Deserters] Failed to end quest {quest}: {e}"); }

            QuestUtility.SendLetterQuestAvailable(QuestUtility.GenerateQuestAndMakeAvailable(VFED_DefOf.VFED_EmpireRuins,
                StorytellerUtility.DefaultSiteThreatPointsNow()));
        }
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (!Active || FlagshipHealth <= 0 || FlagshipHealth >= 1) return;

        if (Find.TickManager.TicksGame % 30 == 1)
        {
            if (Rand.MTBEventOccurs(20, 60, 30))
                if (map.attackTargetsCache.TargetsHostileToFaction(Faction.OfEmpire).OfType<Pawn>().TryRandomElement(out var target))
                    target.Position.DoAerodroneStrike(map);

            if (Rand.MTBEventOccurs(120, 60, 30))
            {
                var cell1 = CellFinder.RandomEdgeCell(map);
                var cell2 = map.Center + GenRadial.RadialPattern[Rand.RangeInclusive(GenRadial.NumCellsInRadius(4.9f), GenRadial.NumCellsInRadius(14.9f))];

                OrbitalSlicer.DoSlice(cell1, cell2, map, WorldComponent_Hierarchy.Instance.TitleHolders.Last());
            }

            if (Rand.MTBEventOccurs(140, 60, 30))
            {
                var pods = new List<ActiveTransporterInfo>();
                var lord = LordMaker.MakeNewLord(Faction.OfEmpire, new LordJob_AssaultColony(Faction.OfEmpire, false, false, false, false, false), map);
                var pawns = PawnGroupMakerUtility.GeneratePawns(new()
                    {
                        dontUseSingleUseRocketLaunchers = true,
                        faction = Faction.OfEmpire,
                        generateFightersOnly = true,
                        inhabitants = true,
                        tile = map.Tile,
                        points = StorytellerUtility.DefaultThreatPointsNow(Find.World),
                        groupKind = PawnGroupKindDefOf.Combat
                    })
                   .ToList();
                var pod = new ActiveTransporterInfo();
                pod.innerContainer.TryAddRangeOrTransfer(pawns, false);
                lord.AddPawns(pawns);
                pods.Add(pod);
                foreach (var shuttle in pods)
                {
                    TransportersArrivalActionUtility.DropShuttle(shuttle, map, DropCellFinder.GetBestShuttleLandingSpot(map, Faction.OfEmpire));
                }
            }

            if (Rand.MTBEventOccurs(100, 60, 30))
            {
                var pawn = PawnGenerator.GeneratePawn(VFEE_DefOf.VFEE_Deserter, EmpireUtility.Deserters);
                var lord = map.lordManager.lords.FirstOrDefault(lord => lord.faction == EmpireUtility.Deserters) ?? LordMaker.MakeNewLord(
                    EmpireUtility.Deserters, new LordJob_AssistColony(EmpireUtility.Deserters, map.mapPawns.FreeColonistsSpawned.RandomElement().Position),
                    map);
                lord.AddPawn(pawn);
                DropPodUtility.DropThingsNear(CellFinder.RandomEdgeCell(map), map, Gen.YieldSingle(pawn));
            }
        }
    }

    public override void MapComponentOnGUI()
    {
        base.MapComponentOnGUI();
        if (!Active || FlagshipHealth <= 0 || FlagshipHealth >= 1) return;
        var emperor = WorldComponent_Hierarchy.Instance.TitleHolders.Last();
        var rect = new Rect(new(UI.screenWidth - 116, 30), ColonistBar.BaseSize * 2);
        GUI.DrawTexture(rect, TexDeserters.BossBackground);
        GUI.DrawTexture(
            new(new(rect.x, rect.y - ColonistBarColonistDrawer.PawnTextureSize.y), ColonistBarColonistDrawer.PawnTextureSize * 2),
            PortraitsCache.Get(emperor, ColonistBarColonistDrawer.PawnTextureSize * 2, Rot4.South, ColonistBarColonistDrawer.PawnTextureCameraOffset,
                1.28205f));
        var innerRect = rect.BottomHalf();
        innerRect.width *= 0.7f;
        GUI.DrawTexture(innerRect, TexDeserters.BossFlagship);
        innerRect = rect.BottomPartPixels(Text.LineHeight);
        using (new TextBlock(TextAnchor.MiddleCenter)) Widgets.Label(innerRect, "VFED.Flagship".Translate());
        rect = new(rect.x - 210, rect.y, 200, 30);
        Widgets.FillableBar(rect, FlagshipHealth, TexDeserters.BossHealthTex, BaseContent.BlackTex, false);
        using (new TextBlock(TextAnchor.MiddleCenter)) Widgets.Label(rect, "VFED.HullIntegrity".Translate() + ":" + FlagshipHealth.ToStringPercentEmptyZero());
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Active, "active");
        Scribe_References.Look(ref Controller, "controller");
        Scribe_Values.Look(ref FlagshipHealth, "flagshipHealth");
        Scribe_Collections.Look(ref Cannons, "cannons", LookMode.Reference);
        Scribe_Values.Look(ref shipDamagedSignal, nameof(shipDamagedSignal));
        Scribe_Values.Look(ref shipDestroyedSignal, nameof(shipDestroyedSignal));
    }
}
