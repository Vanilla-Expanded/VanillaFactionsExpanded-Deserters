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
        if (typeof(MoteAttached).IsAssignableFrom(thingDef.thingClass))
            return MoteMaker.MakeAttachedOverlay(parent, thingDef, Vector3.zero);
        var drawPos = parent.DrawPos;
        return drawPos.InBounds(parent.Map) ? MoteMaker.MakeStaticMote(drawPos, parent.Map, thingDef) : null;
    }
}

public class CompProperties_MotionDetection : CompProperties
{
    public ThingDef moteGlow;
    public ThingDef moteScan;
    public bool onlyHumanlike;
    public float radius;
    public SoundDef soundEmitting;
    public bool triggerOnPawnInRoom;
    public float warmupPulseFadeInTime;
    public float warmupPulseFadeOutTime;
    public float warmupPulseSolidTime;
}
