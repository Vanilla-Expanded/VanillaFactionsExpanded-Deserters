using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using VFEEmpire;

namespace VFED;

public class QuestNode_FlagshipFight : QuestNode
{
    public SlateRef<string> inSignal;
    public SlateRef<MapParent> mapParent;

    protected override void RunInt()
    {
        var shipDamaged = QuestGenUtility.HardcodedSignalWithQuestID("ship.Damaged");
        var shipDestroyed = QuestGenUtility.HardcodedSignalWithQuestID("ship.Destroyed");
        var slate = QuestGen.slate;

        QuestGen.quest.AddPart(new QuestPart_FlagshipFight
        {
            inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal"),
            mapParent = mapParent.GetValue(slate),
            shipDamaged = shipDamaged,
            shipDestroyed = shipDestroyed
        });
    }

    protected override bool TestRunInt(Slate slate) => true;
}

public class QuestPart_FlagshipFight : QuestPart
{
    public string inSignal;
    public MapParent mapParent;
    public string shipDamaged;
    public string shipDestroyed;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == inSignal && mapParent.HasMap) mapParent.Map.GetComponent<MapComponent_FlagshipFight>().Initiate(shipDamaged, shipDestroyed);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, nameof(inSignal));
        Scribe_References.Look(ref mapParent, nameof(mapParent));
        Scribe_Values.Look(ref shipDamaged, nameof(shipDamaged));
        Scribe_Values.Look(ref shipDestroyed, nameof(shipDestroyed));
    }
}

public class QuestNode_GetEmperor : QuestNode
{
    public SlateRef<string> storeAs;

    private bool DoIt(Slate slate)
    {
        var emperor = WorldComponent_Hierarchy.Instance.TitleHolders.Last();
        if (emperor?.royalty == null || emperor.royalty.GetCurrentTitle(Faction.OfEmpire) != VFEE_DefOf.Emperor
                                     || storeAs.GetValue(slate).NullOrEmpty()) return false;
        slate.Set(storeAs.GetValue(slate), emperor);
        return true;
    }

    protected override void RunInt() => DoIt(QuestGen.slate);

    protected override bool TestRunInt(Slate slate) => DoIt(slate);
}
