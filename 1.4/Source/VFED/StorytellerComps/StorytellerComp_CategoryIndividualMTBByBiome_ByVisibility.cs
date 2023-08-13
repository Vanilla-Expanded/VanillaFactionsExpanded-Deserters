using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VFED;

public class StorytellerComp_CategoryIndividualMTBByBiome_ByVisibility : StorytellerComp_ByVisibility
{
    protected StorytellerCompProperties_CategoryIndividualMTBByBiome_ByVisibility Props =>
        (StorytellerCompProperties_CategoryIndividualMTBByBiome_ByVisibility)props;

    public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
    {
        if (target is World) yield break;
        if (!AllowGoodEvents) yield break;

        var allIncidents = DefDatabase<IncidentDef>.AllDefsListForReading;
        int num2;
        for (var i = 0; i < allIncidents.Count; i = num2 + 1)
        {
            var incidentDef = allIncidents[i];
            if (incidentDef.category == Props.category)
            {
                var biome = Find.WorldGrid[target.Tile].biome;
                var mtbbyBiome = incidentDef.mtbDaysByBiome?.Find(x => x.biome == biome);
                if (mtbbyBiome != null)
                {
                    var num = mtbbyBiome.mtbDays;
                    if (Props.applyCaravanVisibility)
                    {
                        if (target is Caravan caravan)
                            num /= caravan.Visibility;
                        else
                        {
                            if (target is Map map && map.Parent.def.isTempIncidentMapOwner)
                            {
                                var pawns = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
                                   .Concat(map.mapPawns.PrisonersOfColonySpawned);
                                num /= CaravanVisibilityCalculator.Visibility(pawns, false);
                            }
                        }
                    }

                    num *= Mathf.Lerp(1, 0.1f, VisibilityFactor);

                    if (Rand.MTBEventOccurs(num, 60000f, 1000f))
                    {
                        var parms = GenerateParms(incidentDef.category, target);
                        if (incidentDef.Worker.CanFireNow(parms)) yield return new FiringIncident(incidentDef, this, parms);
                    }
                }
            }

            num2 = i;
        }
    }

    public override string ToString() => base.ToString() + " " + Props.category;
}

public class StorytellerCompProperties_CategoryIndividualMTBByBiome_ByVisibility : StorytellerCompProperties
{
    public bool applyCaravanVisibility;

    public IncidentCategoryDef category;

    public StorytellerCompProperties_CategoryIndividualMTBByBiome_ByVisibility() =>
        compClass = typeof(StorytellerComp_CategoryIndividualMTBByBiome_ByVisibility);
}
