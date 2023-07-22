using System;
using System.Collections.Generic;
using System.Xml;
using KCSG;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.AI.Group;
using VFEEmpire;

namespace VFED;

public class QuestNode_GetChallengeRating : QuestNode
{
    public SlateRef<int> defaultValue;
    public SlateRef<string> storeAs;
    protected override void RunInt() => SetVars(QuestGen.slate);

    protected override bool TestRunInt(Slate slate)
    {
        SetVars(slate);
        return true;
    }

    private void SetVars(Slate slate)
    {
        slate.Set(storeAs.GetValue(slate), QuestGen.quest?.challengeRating ?? defaultValue.GetValue(slate));
    }
}

public class QuestNode_GetNoble : QuestNode
{
    public SlateRef<string> storeAs;
    public SlateRef<RoyalTitleDef> title;

    protected override void RunInt()
    {
        var empire = Faction.OfEmpire;
        var slate = QuestGen.slate;
        var royalTitle = title.GetValue(slate);
        QuestGen.AddQuestNameConstants(new Dictionary<string, string> { { "nobleTitle", royalTitle.defName } });
        QuestGen.AddQuestDescriptionConstants(new Dictionary<string, string> { { "nobleTitle", royalTitle.defName } });
        var kind = royalTitle.GetModExtension<RoyalTitleDefExtension>().kindForHierarchy;
        var noble = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, empire, forceGenerateNewPawn: true, fixedTitle: royalTitle,
            canGeneratePawnRelations: false));
        slate.Set(storeAs.GetValue(slate), noble);
        QuestGen.AddToGeneratedPawns(noble);
        if (!noble.IsWorldPawn()) Find.WorldPawns.PassToWorld(noble);
        if (WorldComponent_Deserters.GeneratingPlot != null) WorldComponent_Deserters.GeneratingPlot.target = noble;
    }

    protected override bool TestRunInt(Slate slate) => true;
}

public class QuestNode_MergeLists : QuestNode
{
    public SlateRef<object> item1;
    public SlateRef<object> item2;
    public SlateRef<List<object>> list1;
    public SlateRef<List<object>> list2;
    public SlateRef<string> storeAs;

    protected override void RunInt()
    {
        DoIt(QuestGen.slate);
    }

    protected override bool TestRunInt(Slate slate)
    {
        DoIt(slate);
        return true;
    }

    private void DoIt(Slate slate)
    {
        var list = new List<object>();
        if (list1.TryGetValue(slate, out var list1Val) && !list1Val.NullOrEmpty()) list.AddRange(list1Val);
        if (list2.TryGetValue(slate, out var list2Val) && !list2Val.NullOrEmpty()) list.AddRange(list2Val);
        if (item1.TryGetValue(slate, out var item1Val) && item1Val != null) list.Add(item1Val);
        if (item2.TryGetValue(slate, out var item2Val) && item2Val != null) list.Add(item2Val);
        slate.Set(storeAs.GetValue(slate), list);
    }
}

public class QuestNode_GetStructure : QuestNode_ByTitle
{
    public SlateRef<List<StructureForTitle>> structures;

    protected override void RunInt()
    {
        DoIt(QuestGen.slate);
    }

    protected override bool TestRunInt(Slate slate)
    {
        DoIt(slate);
        return true;
    }

    protected override void DoIt(Slate slate)
    {
        if (value.TryGetValue(slate, out var val) && structures.GetValue(slate).FirstOrDefault(item => item.title == val) is { structure: var result })
            slate.Set(storeAs.GetValue(slate), result);
    }

    public class StructureForTitle
    {
        public TiledStructureDef structure;
        public RoyalTitleDef title;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "title", xmlRoot.Name);
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "structure", xmlRoot.FirstChild.Value);
        }
    }
}

public abstract class QuestNode_ByTitle : QuestNode
{
    public SlateRef<string> storeAs;
    public SlateRef<RoyalTitleDef> value;

    protected override void RunInt()
    {
        DoIt(QuestGen.slate);
    }

    protected override bool TestRunInt(Slate slate)
    {
        DoIt(slate);
        return true;
    }

    protected abstract void DoIt(Slate slate);
}

public class QuestNode_ValueFromTitle : QuestNode_ByTitle
{
    public SlateRef<List<ValueForTitle>> values;

    protected override void DoIt(Slate slate)
    {
        if (value.TryGetValue(slate, out var val) && values.GetValue(slate).FirstOrDefault(item => item.title == val) is { value: var result })
            slate.Set(storeAs.GetValue(slate), result);
    }

    public class ValueForTitle
    {
        public RoyalTitleDef title;
        public float value;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "title", xmlRoot.Name);
            value = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
        }
    }
}

public class QuestNode_MakeLord : QuestNode
{
    public enum LordJobType
    {
        Assault, Defend, Assist
    }

    public SlateRef<bool> canFlee;
    public SlateRef<bool> canSteal;
    public SlateRef<Faction> faction;
    public SlateRef<LordJobType> lordJob;
    public SlateRef<List<Pawn>> pawns;

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var fac = faction.GetValue(slate);
        var map = slate.Get<Map>("map");
        LordMaker.MakeNewLord(fac, lordJob.GetValue(slate) switch
        {
            LordJobType.Assault => new LordJob_AssaultColony(fac, false, canFlee.GetValue(slate), false, true, canSteal.GetValue(slate)),
            LordJobType.Defend => new LordJob_DefendBase(fac, map.Center, true),
            LordJobType.Assist => new LordJob_AssistColony(fac, CellFinder.RandomSpawnCellForPawnNear(CellFinder.RandomEdgeCell(map), map)),
            _ => throw new ArgumentOutOfRangeException()
        }, map, pawns.GetValue(slate));
    }

    protected override bool TestRunInt(Slate slate) => true;
}

public class QuestNode_RevealDelay : QuestNode
{
    public SlateRef<IntRange> delayTicksRange;

    protected override void RunInt()
    {
        var part = new QuestPart_RevealDelay
        {
            delayTicksRange = delayTicksRange.GetValue(QuestGen.slate),
            inSignalEnable = QuestGen.quest.AddedSignal,
            inSignalDisable = QuestGen.quest.InitiateSignal,
            signalListenMode = QuestPart.SignalListenMode.OngoingOrNotYetAccepted
        };
        QuestGen.quest.AddPart(part);
    }

    protected override bool TestRunInt(Slate slate) => true;
}

public class QuestPart_RevealDelay : QuestPart_DelayRandom
{
    public override bool PreventsAutoAccept => true;

    public override AlertReport AlertReport => AlertReport.Inactive;

    protected override void Enable(SignalArgs receivedArgs)
    {
        base.Enable(receivedArgs);
        quest.hidden = true;
        quest.hiddenInUI = true;
        quest.ticksUntilAcceptanceExpiry = -1;
    }

    protected override void Complete(SignalArgs signalArgs)
    {
        base.Complete(signalArgs);
        quest.hidden = false;
        quest.hiddenInUI = false;
        quest.ticksUntilAcceptanceExpiry = quest.root.expireDaysRange.RandomInRange.DaysToTicks();
        quest.appearanceTick = Find.TickManager.TicksGame;
        QuestUtility.SendLetterQuestAvailable(quest);
    }
}
