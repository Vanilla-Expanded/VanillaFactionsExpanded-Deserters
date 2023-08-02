using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFED.StorytellerComps;

public class StorytellerComp_FactionInteraction_ByVisibility : StorytellerComp_ByVisibility
{
    private StorytellerCompProperties_FactionInteraction_ByVisibility Props => (StorytellerCompProperties_FactionInteraction_ByVisibility)props;

    public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
    {
        var map = target as Map;
        if (Props.minDanger != StoryDanger.None && (map == null || map.dangerWatcher.DangerRating < Props.minDanger)) yield break;

        if (Props.minWealth > 0f && (map == null || map.wealthWatcher.WealthTotal < Props.minWealth)) yield break;

        if (!AllowGoodEvents) yield break;

        var num = StorytellerUtility.AllyIncidentFraction(Props.fullAlliesOnly);
        if (num <= 0f) yield break;

        var incidentsPerYear = Props.baseIncidentsPerYear;
        incidentsPerYear *= Mathf.Lerp(0, 20, VisibilityFactor);

        num *= VisibilityFactor + 0.5f;

        var incCount = IncidentCycleUtility.IncidentCountThisInterval(target, Find.Storyteller.storytellerComps.IndexOf(this), Props.minDaysPassed, 60f,
            0f, Props.minSpacingDays, incidentsPerYear, incidentsPerYear, num);
        int num2;
        for (var i = 0; i < incCount; i = num2 + 1)
        {
            var parms = GenerateParms(Props.incident.category, target);
            if (Props.incident.Worker.CanFireNow(parms)) yield return new FiringIncident(Props.incident, this, parms);

            num2 = i;
        }
    }

    public override string ToString() => base.ToString() + " (" + Props.incident.defName + ")";
}

public class StorytellerCompProperties_FactionInteraction_ByVisibility : StorytellerCompProperties
{
    public float baseIncidentsPerYear;

    public bool fullAlliesOnly;

    public IncidentDef incident;

    public StoryDanger minDanger;

    public float minSpacingDays;

    public float minWealth;

    public StorytellerCompProperties_FactionInteraction_ByVisibility() => compClass = typeof(StorytellerComp_FactionInteraction);
}
