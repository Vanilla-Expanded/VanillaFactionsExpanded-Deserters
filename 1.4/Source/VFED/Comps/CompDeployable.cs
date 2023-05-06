using System.Collections.Generic;
using System.Linq;
using Verse;

namespace VFED;

public class CompDeployable : ThingComp
{
    public CompProperties_Deployable Props => props as CompProperties_Deployable;

    public override IEnumerable<Gizmo> CompGetWornGizmosExtra() =>
        base.CompGetWornGizmosExtra()
           .Append(new Command_Action
            {
                defaultLabel = "VFED.Deploy".Translate(),
                defaultDesc = "VFED.Deploy.Desc".Translate(parent.def.LabelCap, Props.deployedThing.LabelCap),
                action = delegate
                {
                    GenPlace.TryPlaceThing(ThingMaker.MakeThing(Props.deployedThing), parent.PositionHeld, parent.MapHeld, ThingPlaceMode.Direct);
                    parent.Destroy();
                }
            });
}

public class CompProperties_Deployable : CompProperties
{
    public ThingDef deployedThing;

    public CompProperties_Deployable() => compClass = typeof(CompDeployable);
}
