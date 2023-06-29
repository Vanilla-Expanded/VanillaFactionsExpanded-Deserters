using System.Collections.Generic;
using KCSG;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace VFED;

public class SitePartWorker_WithQuestStructure : SitePartWorker
{
    public override void Notify_GeneratedByQuestGen(SitePart part, Slate slate, List<Rule> outExtraDescriptionRules,
        Dictionary<string, string> outExtraDescriptionConstants)
    {
        base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
        WorldComponent_Deserters.Instance.DataForSites.SetOrAdd(part.site, new WorldComponent_Deserters.SiteExtraData
        {
            structure = slate.Get<TiledStructureDef>("structureDef"),
            points = slate.Get<float>("points"),
            noble = slate.Get<Pawn>("noble")
        });
    }
}

public class SitePartWorker_Objectives : SitePartWorker
{
    public bool ShouldKeepSiteForObjectives(SitePart part) => part.site?.Map?.GetComponent<MapComponent_ObjectiveHighlighter>()?.HasObjectives ?? false;
}
