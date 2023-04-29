using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace VFED;

public class QuestNode_JoinDeserters : QuestNode
{
    [NoTranslate] public SlateRef<string> inSignal;

    protected override void RunInt()
    {
        QuestGen.quest.AddPart(new QuestPart_JoinDeserters
        {
            inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(QuestGen.slate)) ?? QuestGen.slate.Get<string>("inSignal")
        });
    }

    protected override bool TestRunInt(Slate slate) => true;
}

public class QuestPart_JoinDeserters : QuestPart
{
    public string inSignal;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == inSignal) WorldComponent_Deserters.Instance.JoinDeserters(quest);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, nameof(inSignal));
    }
}
