using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFED;

public class StorytellerComp_CategoryMTB_ByVisibility : StorytellerComp_ByVisibility
{
    protected StorytellerCompProperties_CategoryMTB_ByVisibility Props => (StorytellerCompProperties_CategoryMTB_ByVisibility)props;

    public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
    {
        if (!AllowGoodEvents) yield break;
        var num = Props.mtbDays;
        if (Props.mtbDaysFactorByDaysPassedCurve != null) num *= Props.mtbDaysFactorByDaysPassedCurve.Evaluate(GenDate.DaysPassedSinceSettleFloat);
        num *= Mathf.Lerp(1, 0.1f, VisibilityFactor);

        if (Rand.MTBEventOccurs(num, 60000f, 1000f))
        {
            var parms = GenerateParms(Props.category, target);
            if (TrySelectRandomIncident(UsableIncidentsInCategory(Props.category, parms), out var def)) yield return new FiringIncident(def, this, parms);
        }
    }

    public override string ToString() => base.ToString() + " " + Props.category;
}

public class StorytellerCompProperties_CategoryMTB_ByVisibility : StorytellerCompProperties
{
    public IncidentCategoryDef category;

    public float mtbDays = -1f;

    public SimpleCurve mtbDaysFactorByDaysPassedCurve;

    public StorytellerCompProperties_CategoryMTB_ByVisibility() => compClass = typeof(StorytellerComp_CategoryMTB_ByVisibility);
}
