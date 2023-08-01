using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace VFED;

public class QuestNode_MarkObjectives : QuestNode
{
    public SlateRef<string> inSignal;
    public SlateRef<MapParent> mapParent;
    public SlateRef<string> objectiveCompleteSignal;
    public SlateRef<ThingDef> objectiveDef;

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var handlerTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("objectives");
        var objectiveTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("objective");

        QuestGen.quest.AddPart(new QuestPart_MarkObjectives
        {
            inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal"),
            completeSignal = QuestGenUtility.HardcodedSignalWithQuestID(objectiveTag + "." + objectiveCompleteSignal.GetValue(slate)),
            handlerTag = handlerTag,
            objectiveTag = objectiveTag,
            mapParent = mapParent.GetValue(slate),
            objectiveDef = objectiveDef.GetValue(slate)
        });
    }

    protected override bool TestRunInt(Slate slate) => true;
}

public class QuestPart_MarkObjectives : QuestPart
{
    public string completeSignal;
    public string handlerTag;
    public string inSignal;
    public MapParent mapParent;
    public ThingDef objectiveDef;
    public string objectiveTag;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == inSignal && mapParent.HasMap)
        {
            var objectives = mapParent.Map.listerThings.ThingsOfDef(objectiveDef);
            var objectiveHandler = mapParent.Map.GetComponent<MapComponent_ObjectiveHighlighter>();
            objectiveHandler.Activate(completeSignal, handlerTag);
            for (var i = objectives.Count; i-- > 0;)
            {
                QuestUtility.AddQuestTag(objectives[i], objectiveTag);
                objectiveHandler.AddObjective(objectives[i]);
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, nameof(inSignal));
        Scribe_Values.Look(ref handlerTag, nameof(handlerTag));
        Scribe_Defs.Look(ref objectiveDef, nameof(objectiveDef));
        Scribe_References.Look(ref mapParent, nameof(mapParent));
        Scribe_Values.Look(ref objectiveTag, nameof(objectiveTag));
        Scribe_Values.Look(ref completeSignal, nameof(completeSignal));
    }
}
