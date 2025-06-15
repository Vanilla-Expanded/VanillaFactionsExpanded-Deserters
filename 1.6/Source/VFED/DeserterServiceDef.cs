using System;
using System.Linq;
using System.Xml;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VFEEmpire;

namespace VFED;

public class DeserterServiceDef : Def
{
    public Texture2D Icon;
    public string iconPath;
    public int intelCost;
    public bool useCriticalIntel;
    public MethodReference<Action> worker;

    public override void PostLoad()
    {
        base.PostLoad();
        LongEventHandler.ExecuteWhenFinished(() => Icon = ContentFinder<Texture2D>.Get(iconPath));
    }
}

public class MethodReference<T> where T : Delegate
{
    public T Call;

    public MethodReference() { }

    public MethodReference(string name) => Call = (T)AccessTools.Method(name).CreateDelegate(typeof(T));

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        Call = (T)AccessTools.Method(xmlRoot.InnerText).CreateDelegate(typeof(T));
    }
}

public static class DeserterServiceWorkers
{
    public static void LowerVisibility()
    {
        Utilities.ChangeVisibility(-1);
    }

    public static void CallShuttle()
    {
        var thing = ThingMaker.MakeThing(VFED_DefOf.VFED_DeserterShuttle);
        var compShuttle = thing.TryGetComp<CompShuttle>();
        compShuttle.permitShuttle = true;
        compShuttle.acceptChildren = true;
        var transportShip = TransportShipMaker.MakeTransportShip(VFED_DefOf.VFED_Ship_DeserterShuttle, null, thing);
        var map = Find.CurrentMap;
        transportShip.ArriveAt(DropCellFinder.GetBestShuttleLandingSpot(map, EmpireUtility.Deserters), map.Parent);
        transportShip.AddJobs(ShipJobDefOf.WaitForever, ShipJobDefOf.Unload_Destination, ShipJobDefOf.FlyAway);
    }

    public static void IncreaseGoodwill()
    {
        if (Find.FactionManager.GetFactions().Where(f => f.CanChangeGoodwillFor(Faction.OfPlayer, 5)).TryRandomElement(out var faction) &&
            faction.TryAffectGoodwillWith(Faction.OfPlayer, 5))
            Messages.Message("VFED.ServiceIncreasedGoodwill".Translate(faction.NameColored), MessageTypeDefOf.PositiveEvent);
    }

    public static void DelayResponse()
    {
        foreach (var part in Find.QuestManager.QuestsListForReading.SelectMany(quest => quest.PartsListForReading))
            if (part is QuestPart_ImperialResponse { responseDef: not null } response)
                response.responseTick += 60f.SecondsToTicks();
    }

    public static void TauntImperials()
    {
        var map = Find.CurrentMap;
        if (IncidentDefOf.RaidEnemy.Worker.TryExecute(new()
            {
                points = StorytellerUtility.DefaultThreatPointsNow(map),
                faction = Faction.OfEmpire,
                target = map
            })) { }
    }

    public static void ChangeCritical()
    {
        var map = Find.CurrentMap;
        var intel = ThingMaker.MakeThing(VFED_DefOf.VFED_Intel);
        intel.stackCount = 10;
        DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(map), map, Gen.YieldSingle(intel), canRoofPunch: false, forbid: false, allowFogged: false,
            faction: EmpireUtility.Deserters);
    }
}
