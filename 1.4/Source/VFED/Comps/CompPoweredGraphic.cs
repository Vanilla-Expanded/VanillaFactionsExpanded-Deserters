using RimWorld;
using UnityEngine;
using Verse;

namespace VFED;

public class CompPoweredGraphic : ThingComp
{
    private CompPowerTrader compPower;

    private CompProperties_PoweredGraphic Props => (CompProperties_PoweredGraphic)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        compPower = parent.TryGetComp<CompPowerTrader>();
    }

    public override void PostDraw()
    {
        base.PostDraw();
        if (compPower.PowerOn)
        {
            var mesh = Props.graphicData.Graphic.MeshAt(parent.Rotation);
            var drawPos = parent.DrawPos;
            drawPos.y = AltitudeLayer.BuildingOnTop.AltitudeFor();
            Graphics.DrawMesh(mesh, drawPos + Props.graphicData.drawOffset.RotatedBy(parent.Rotation), Quaternion.identity,
                Props.graphicData.Graphic.MatAt(parent.Rotation), 0);
        }
    }
}

public class CompProperties_PoweredGraphic : CompProperties
{
    public GraphicData graphicData;

    public CompProperties_PoweredGraphic() => compClass = typeof(CompPoweredGraphic);
}
