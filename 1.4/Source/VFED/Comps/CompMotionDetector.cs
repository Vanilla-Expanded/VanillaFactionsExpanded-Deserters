using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace VFED;

public abstract class CompMotionDetector : ThingComp
{
    protected bool Activated;

    private Mote moteGlow;
    private Mote moteScan;
    private Sustainer sustainer;

    public virtual bool Active => !Activated;

    public CompProperties_MotionDetection Props => (CompProperties_MotionDetection)props;

    protected virtual void Trigger(Thing initiator)
    {
        Activated = true;
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!parent.Spawned || !Active) return;
        if (parent.IsHashIntervalTick(250)) CompTickRare();

        if (moteGlow == null || moteGlow.Destroyed) moteGlow = ThrowMote(Props.moteGlow);
        if (moteGlow is { Destroyed: false }) moteGlow.Maintain();

        if (!Props.soundEmitting.NullOrUndefined())
        {
            if (Active)
            {
                if (sustainer == null || sustainer.Ended) sustainer = Props.soundEmitting.TrySpawnSustainer(SoundInfo.InMap(parent));

                sustainer.Maintain();
            }
            else if (sustainer is { Ended: false }) sustainer.End();
        }

        if (moteScan == null || moteScan.Destroyed) moteScan = ThrowMote(Props.moteScan);
        if (moteScan is { Destroyed: false }) moteScan.Maintain();
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (!parent.Spawned || !Active) return;

        Thing thing = null;
        if (Props.triggerOnPawnInRoom)
            thing = parent.GetRoom().ContainedAndAdjacentThings.FirstOrDefault(TriggerOn);

        if (thing == null && Props.radius > 0f)
            thing = GenClosest.ClosestThingReachable(parent.Position, parent.Map, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell,
                TraverseParms.For(TraverseMode.NoPassClosedDoors), Props.radius, TriggerOn);

        if (thing != null) Trigger(thing);
    }

    private bool TriggerOn(Thing t) => !Props.onlyHumanlike || t is Pawn { RaceProps.Humanlike: true };

    public override string CompInspectStringExtra()
    {
        if (Active) return "radius".Translate().CapitalizeFirst() + ": " + Props.radius.ToString("F0");

        return "expired".Translate().CapitalizeFirst();
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref Activated, "activated");
    }

    private Mote ThrowMote(ThingDef thingDef)
    {
        var scale = Props.radius / 12.9f;
        if (typeof(MoteAttached).IsAssignableFrom(thingDef.thingClass))
            return MoteMaker.MakeAttachedOverlay(parent, thingDef, Vector3.zero, scale);
        var drawPos = parent.DrawPos;
        return drawPos.InBounds(parent.Map) ? MoteMaker.MakeStaticMote(drawPos, parent.Map, thingDef, scale) : null;
    }

    public override void PostDraw()
    {
        base.PostDraw();
        if (Props.activeGraphic != null && Active)
        {
            var mesh = Props.activeGraphic.Graphic.MeshAt(parent.Rotation);
            var drawPos = parent.DrawPos;
            drawPos.y = AltitudeLayer.BuildingOnTop.AltitudeFor();
            Graphics.DrawMesh(mesh, drawPos + Props.activeGraphic.drawOffset.RotatedBy(parent.Rotation), Quaternion.identity,
                Props.activeGraphic.Graphic.MatAt(parent.Rotation), 0);
        }
    }
}

public class CompProperties_MotionDetection : CompProperties
{
    public GraphicData activeGraphic;
    public ThingDef moteGlow;
    public ThingDef moteScan;
    public bool onlyHumanlike;
    public float radius;
    public SoundDef soundEmitting;
    public bool triggerOnPawnInRoom;
}

public class PlaceWorker_ShowMotionDetectionRadius : PlaceWorker
{
    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        base.DrawGhost(def, center, rot, ghostCol, thing);
        var compProperties = def.GetCompProperties<CompProperties_MotionDetection>();
        if (compProperties == null) return;
        GenDraw.DrawRadiusRing(center, compProperties.radius);
    }
}
