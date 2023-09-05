using System.Collections.Generic;
using System.Linq;
using KCSG;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace VFED;

public class SitePartWorker_WithQuestStructure : SitePartWorker_Objectives
{
    public override void Notify_GeneratedByQuestGen(SitePart part, Slate slate, List<Rule> outExtraDescriptionRules,
        Dictionary<string, string> outExtraDescriptionConstants)
    {
        base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
        WorldComponent_Deserters.Instance.DataForSites.SetOrAdd(part.site, new()
        {
            structure = slate.Get<TiledStructureDef>("structureDef"),
            points = slate.Get<float>("points"),
            noble = slate.Get<Pawn>("noble")
        });
    }

    public override bool ShouldKeepSiteForObjectives(SitePart part) =>
        WorldComponent_Deserters.Instance.DataForSites.TryGetValue(part.site, out var data) && data?.noble is { Spawned: true };

    public override void Notify_SiteMapAboutToBeRemoved(SitePart sitePart)
    {
        base.Notify_SiteMapAboutToBeRemoved(sitePart);
        WorldComponent_Deserters.Instance.DataForSites.Remove(sitePart.site);
    }
}

public class SitePartWorker_Objectives : SitePartWorker
{
    public virtual bool ShouldKeepSiteForObjectives(SitePart part) => part.site?.Map?.GetComponent<MapComponent_ObjectiveHighlighter>()?.HasObjectives ?? false;
}

public class SitePartWorker_DeadDrop : SitePartWorker_ItemStash
{
    public override void Notify_GeneratedByQuestGen(SitePart part, Slate slate, List<Rule> outExtraDescriptionRules,
        Dictionary<string, string> outExtraDescriptionConstants)
    {
        outExtraDescriptionRules.AddRange(GrammarUtility.RulesForDef("", part.def));
        outExtraDescriptionConstants.Add("sitePart", part.def.defName);
        var thingDef = slate.Get<ThingDef>("itemStashSingleThing");
        var enumerable = slate.Get<IEnumerable<ThingDef>>("itemStashThings");
        List<Thing> list;
        if (thingDef != null)
            list = new() { ThingMaker.MakeThing(thingDef) };
        else
        {
            if (enumerable != null)
                list = enumerable.Select(def => ThingMaker.MakeThing(def)).ToList();
            else
            {
                var x = slate.Get<float>("points");
                var parms = default(ThingSetMakerParams);
                parms.totalMarketValueRange = new FloatRange(0.7f, 1.3f) * QuestTuning.PointsToRewardMarketValueCurve.Evaluate(x);
                list = ThingSetMakerDefOf.Reward_ItemsStandard.root.Generate(parms);
            }
        }

        part.things = new ThingOwner<Thing>(part, false);
        part.things.TryAddRangeOrTransfer(list);
        slate.Set("generatedItemStashThings", list);
        outExtraDescriptionRules.Add(new Rule_String("itemStashContents", GenLabel.ThingsLabel(list)));
        outExtraDescriptionRules.Add(new Rule_String("itemStashContentsValue", GenThing.GetMarketValue(list).ToStringMoney()));
    }
}
