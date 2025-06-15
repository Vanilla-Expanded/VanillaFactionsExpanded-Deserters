using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace VFED;

public class QuestNode_GenerateShuttleCustom : QuestNode
{
    public SlateRef<bool?> acceptChildren;

    public SlateRef<bool> acceptColonists;

    public SlateRef<float?> minAge;

    public SlateRef<bool> onlyAcceptColonists;

    public SlateRef<bool> onlyAcceptHealthy;

    public SlateRef<float> overrideMass;

    public SlateRef<Faction> owningFaction;

    public SlateRef<bool> permitShuttle;

    public SlateRef<int> requireColonistCount;

    public SlateRef<IEnumerable<ThingDefCount>> requiredItems;

    public SlateRef<IEnumerable<Pawn>> requiredPawns;

    public SlateRef<ThingDef> shuttleDef;

    [NoTranslate] public SlateRef<string> storeAs;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        if (!ModLister.CheckRoyaltyOrIdeology("Shuttle")) return;
        var slate = QuestGen.slate;
        var thing = ThingMaker.MakeThing(shuttleDef.GetValue(slate) ?? ThingDefOf.Shuttle);
        if (owningFaction.GetValue(slate) != null) thing.SetFaction(owningFaction.GetValue(slate));
        var compShuttle = thing.TryGetComp<CompShuttle>();
        if (requiredPawns.GetValue(slate) != null) compShuttle.requiredPawns.AddRange(requiredPawns.GetValue(slate));
        if (requiredItems.GetValue(slate) != null) compShuttle.requiredItems.AddRange(requiredItems.GetValue(slate));
        compShuttle.acceptColonists = acceptColonists.GetValue(slate);
        compShuttle.acceptChildren = acceptChildren.GetValue(slate) ?? true;
        compShuttle.onlyAcceptColonists = onlyAcceptColonists.GetValue(slate);
        compShuttle.onlyAcceptHealthy = onlyAcceptHealthy.GetValue(slate);
        compShuttle.requiredColonistCount = requireColonistCount.GetValue(slate);
        compShuttle.permitShuttle = permitShuttle.GetValue(slate);
        compShuttle.minAge = minAge.GetValue(slate) ?? 0f;
        float num;
        if (overrideMass.TryGetValue(slate, out num) && num > 0f) compShuttle.Transporter.massCapacityOverride = num;
        QuestGen.slate.Set(storeAs.GetValue(slate), thing);
    }
}
