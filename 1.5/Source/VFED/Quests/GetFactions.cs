using RimWorld;
using RimWorld.QuestGen;
using Verse;
using VFEEmpire;

namespace VFED;

public class QuestNode_GetDeserters : QuestNode
{
    [NoTranslate] public SlateRef<string> storeAs;

    protected override void RunInt()
    {
        QuestGen.slate.Set(storeAs.GetValue(QuestGen.slate), EmpireUtility.Deserters);
    }

    protected override bool TestRunInt(Slate slate) => EmpireUtility.Deserters != null;
}

public class QuestNode_GetEmpire : QuestNode
{
    [NoTranslate] public SlateRef<string> storeAs;

    protected override void RunInt()
    {
        QuestGen.slate.Set(storeAs.GetValue(QuestGen.slate), Faction.OfEmpire);
    }

    protected override bool TestRunInt(Slate slate) => Faction.OfEmpire != null;
}
