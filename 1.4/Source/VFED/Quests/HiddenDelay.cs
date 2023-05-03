using RimWorld;
using RimWorld.QuestGen;

namespace VFED;

public class QuestNode_HiddenDelay : QuestNode_Delay
{
    protected override QuestPart_Delay MakeDelayQuestPart() => new QuestPart_HiddenDelay();
}

public class QuestPart_HiddenDelay : QuestPart_Delay
{
    public override void PostQuestAdded()
    {
        base.PostQuestAdded();
        quest.hidden = true;
    }

    protected override void DelayFinished()
    {
        base.DelayFinished();
        quest.hidden = false;
        QuestUtility.SendLetterQuestAvailable(quest);
    }
}
