using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFED.StorytellerComps;

public class StorytellerComp_OnOffCycle_ByVisibility : StorytellerComp_ByVisibility
{
    protected StorytellerCompProperties_OnOffCycle_ByVisibility Props => (StorytellerCompProperties_OnOffCycle_ByVisibility)props;

    public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
    {
        var num = 1f;
        if (Props.acceptFractionByDaysPassedCurve != null) num *= Props.acceptFractionByDaysPassedCurve.Evaluate(GenDate.DaysPassedSinceSettleFloat);
        if (Props.acceptPercentFactorPerThreatPointsCurve != null)
            num *= Props.acceptPercentFactorPerThreatPointsCurve.Evaluate(StorytellerUtility.DefaultThreatPointsNow(target));
        if (Props.acceptPercentFactorPerProgressScoreCurve != null)
            num *= Props.acceptPercentFactorPerProgressScoreCurve.Evaluate(StorytellerUtility.GetProgressScore(target));
        var onDays = Props.onDays;
        var offDays = Props.offDays;
        if ((Props.onDaysNoTreeConnectors != 0f || Props.offDaysNoTreeConnectors != 0f) && Faction.OfPlayer.ideos != null
                                                                                        && !Faction.OfPlayer.ideos.AllIdeos.Any(ideo =>
                                                                                               ideo.HasMeme(MemeDefOf.TreeConnection)))
        {
            if (Props.onDaysNoTreeConnectors != 0f) onDays = Props.onDaysNoTreeConnectors;
            if (Props.offDaysNoTreeConnectors != 0f) offDays = Props.offDaysNoTreeConnectors;
        }

        num *= VisibilityFactor + 0.5f;

        var incCount = IncidentCycleUtility.IncidentCountThisInterval(target, Find.Storyteller.storytellerComps.IndexOf(this), Props.minDaysPassed, onDays,
            offDays, Props.minSpacingDays, Props.numIncidentsRange.min * Mathf.Lerp(0, 20, VisibilityFactor), Props.numIncidentsRange.max
          * Mathf.Lerp(0, 20, VisibilityFactor), num);
        int num2;
        for (var i = 0; i < incCount; i = num2 + 1)
        {
            var firingIncident = GenerateIncident(target);
            if (firingIncident != null) yield return firingIncident;
            num2 = i;
        }
    }

    private FiringIncident GenerateIncident(IIncidentTarget target)
    {
        if (Props.IncidentCategory == null) return null;
        var parms = GenerateParms(Props.IncidentCategory, target);
        IncidentDef def;
        if (GenDate.DaysPassedSinceSettle < Props.forceRaidEnemyBeforeDaysPassed)
        {
            if (!IncidentDefOf.RaidEnemy.Worker.CanFireNow(parms)) return null;
            def = IncidentDefOf.RaidEnemy;
        }
        else if (Props.incident != null)
        {
            if (!Props.incident.Worker.CanFireNow(parms)) return null;
            def = Props.incident;
        }
        else if (!UsableIncidentsInCategory(Props.IncidentCategory, parms).TryRandomElementByWeight(IncidentChanceFinal, out def)) return null;

        return new FiringIncident(def, this, parms);
    }

    public override string ToString()
    {
        if (Props.incident == null && Props.IncidentCategory == null) return "";
        return base.ToString() + " (" + (Props.incident != null ? Props.incident.defName : Props.IncidentCategory.defName) + ")";
    }
}

public class StorytellerCompProperties_OnOffCycle_ByVisibility : StorytellerCompProperties
{
    public SimpleCurve acceptFractionByDaysPassedCurve;

    public SimpleCurve acceptPercentFactorPerProgressScoreCurve;

    public SimpleCurve acceptPercentFactorPerThreatPointsCurve;

    public float forceRaidEnemyBeforeDaysPassed;

    public IncidentDef incident;

    public float minSpacingDays;

    public FloatRange numIncidentsRange = FloatRange.Zero;

    public float offDays;

    public float offDaysNoTreeConnectors;

    public float onDays;

    public float onDaysNoTreeConnectors;

    private IncidentCategoryDef category;

    public StorytellerCompProperties_OnOffCycle_ByVisibility() => compClass = typeof(StorytellerComp_OnOffCycle_ByVisibility);

    public IncidentCategoryDef IncidentCategory
    {
        get
        {
            if (incident != null) return incident.category;

            return category;
        }
    }

    public override IEnumerable<string> ConfigErrors(StorytellerDef parentDef)
    {
        if (incident != null && category != null) yield return "incident and category should not both be defined";

        if (onDays <= 0f) yield return "onDays must be above zero";

        if (numIncidentsRange.TrueMax <= 0f) yield return "numIncidentRange not configured";

        if (minSpacingDays * numIncidentsRange.TrueMax > onDays * 0.9f) yield return "minSpacingDays too high compared to max number of incidents.";
    }
}
