using System.Collections.Generic;
using System.Xml;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace VFED;

public class ImperialResponseDef : Def
{
    public GameConditionDef gameCondition;
    public List<PawnKindDefRange> reinforcements;
    public ImperialResponseDef() => description = label;
}

public class PawnKindDefRange
{
    public PawnKindDef kindDef;

    public IntRange range;

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "kindDef", xmlRoot.Name);
        range = ParseHelper.FromString<IntRange>(xmlRoot.FirstChild.Value);
    }
}

public class QuestNode_ImperialResponse : QuestNode
{
    public SlateRef<string> siteName;

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var name = siteName.GetValue(slate);
        QuestGen.quest.AddPart(new QuestPart_ImperialResponse
        {
            mapParent = slate.Get<MapParent>(name),
            inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(name + ".MapGenerated"),
            inSignalDisable = QuestGenUtility.HardcodedSignalWithQuestID(name + ".MapRemoved")
        });
    }

    protected override bool TestRunInt(Slate slate) => WorldComponent_Deserters.Instance.Active;
}

public class QuestPart_ImperialResponse : QuestPartActivable
{
    public MapParent mapParent;
    public ImperialResponseDef responseDef;
    public int responseTick;

    public override bool AlertCritical => true;

    public override string AlertLabel => "VFED.ResponseComing".Translate();

    public override string AlertExplanation =>
        "VFED.ResponseComingDesc".Translate(responseDef?.label.Colorize(ColoredText.ThreatColor),
            (responseTick - Find.TickManager.TicksGame).TicksToSeconds().ToStringDecimalIfSmall().Colorize(ColoredText.DateTimeColor));

    public override AlertReport AlertReport => responseDef == null ? AlertReport.Inactive : AlertReport.CulpritIs(mapParent);

    protected override void Enable(SignalArgs receivedArgs)
    {
        base.Enable(receivedArgs);
        responseTick = Find.TickManager.TicksGame
                     + (WorldComponent_Deserters.Instance.VisibilityLevel.imperialResponseTime * DesertersMod.ResponseTimeMultiplier).SecondsToTicks();
        responseDef = WorldComponent_Deserters.Instance.VisibilityLevel.imperialResponseType;
    }

    public override void QuestPartTick()
    {
        if (Find.TickManager.TicksGame >= responseTick) Complete();
    }

    protected override void Complete(SignalArgs signalArgs)
    {
        base.Complete(signalArgs);
        if (quest.State != QuestState.Ongoing || !mapParent.HasMap || responseDef == null) return;
        var visibility = WorldComponent_Deserters.Instance.Visibility;
        var visibilityLevel = WorldComponent_Deserters.Instance.VisibilityLevel;
        var map = mapParent.Map;
        if (responseDef.reinforcements != null)
        {
            var pods = new List<ActiveTransporterInfo>();
            var lord = LordMaker.MakeNewLord(Faction.OfEmpire, new LordJob_AssaultColony(Faction.OfEmpire, false, false, false, false, false), map);
            foreach (var pawnKind in responseDef.reinforcements)
            {
                var count = pawnKind.range.Lerped(Mathf.InverseLerp(visibilityLevel.visibilityRange.TrueMin, visibilityLevel.visibilityRange.TrueMax,
                    visibility));
                var pawns = new List<Pawn>();
                for (var i = 0; i < count; i++)
                {
                    var pawn = PawnGenerator.GeneratePawn(pawnKind.kindDef, Faction.OfEmpire);
                    pawns.Add(pawn);
                }

                var pod = new ActiveTransporterInfo();
                pod.innerContainer.TryAddRangeOrTransfer(pawns, false);
                lord.AddPawns(pawns);

                pods.Add(pod);
            }

            TransportersArrivalActionUtility.DropShuttle(pods, map, DropCellFinder.GetBestShuttleLandingSpot(map, Faction.OfEmpire), Faction.OfEmpire);
        }

        if (responseDef.gameCondition != null) map.gameConditionManager.RegisterCondition(GameConditionMaker.MakeConditionPermanent(responseDef.gameCondition));

        WorldComponent_Deserters.Instance.Visibility += Mathf.CeilToInt(visibility * 0.1f);

        Messages.Message("VFED.ResponseMessage".Translate(Mathf.CeilToInt(visibility * 0.1f), WorldComponent_Deserters.Instance.Visibility),
            MessageTypeDefOf.NegativeEvent);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref mapParent, nameof(mapParent));
        Scribe_Defs.Look(ref responseDef, nameof(responseDef));
        Scribe_Values.Look(ref responseTick, nameof(responseTick));
    }
}
