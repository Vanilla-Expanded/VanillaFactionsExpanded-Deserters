using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VFED;

public class Building_CannonControl : Building
{
    public const float RADIUS = 8.9f;

    public bool Controlled;
    private MapComponent_FlagshipFight manager;
    private Mote moteGlow;
    private Mote moteScan;
    private MapComponent_FlagshipFight Manager => manager ??= Map.GetComponent<MapComponent_FlagshipFight>();

    public ThingDef MoteGlow => Controlled ? VFED_DefOf.VFED_Mote_ActivatorProximityGlow_Green : VFED_DefOf.Mote_ActivatorProximityGlow;
    public ThingDef MoteScan => Controlled ? VFED_DefOf.VFED_Mote_ProximityScannerRadius_Green : VFED_DefOf.Mote_ProximityScannerRadius;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        CheckControlled();
    }

    public override void Tick()
    {
        base.Tick();

        if (moteGlow == null || moteGlow.Destroyed) moteGlow = ThrowMote(MoteGlow);
        if (moteGlow is { Destroyed: false }) moteGlow.Maintain();
        if (moteScan == null || moteScan.Destroyed) moteScan = ThrowMote(MoteScan);
        if (moteScan is { Destroyed: false }) moteScan.Maintain();

        if (this.IsHashIntervalTick(200)) CheckControlled();
    }

    private void CheckControlled()
    {
        if (!Spawned) return;

        var thing = GenClosest.ClosestThingReachable(Position, Map, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell,
            TraverseParms.For(TraverseMode.NoPassClosedDoors), RADIUS, CanControl);

        if (thing == null && Controlled)
        {
            Controlled = false;
            moteGlow?.Destroy();
            moteScan?.Destroy();
        }
        else if (thing != null && !Controlled)
        {
            Controlled = true;
            moteGlow?.Destroy();
            moteScan?.Destroy();
        }
    }

    private static bool CanControl(Thing t) => t is Pawn pawn && pawn.RaceProps.Humanlike && pawn.Faction.IsPlayerSafe();

    private Mote ThrowMote(ThingDef thingDef)
    {
        const float scale = RADIUS / 12.9f;
        if (typeof(MoteAttached).IsAssignableFrom(thingDef.thingClass))
            return MoteMaker.MakeAttachedOverlay(this, thingDef, Vector3.zero, scale);
        var drawPos = DrawPos;
        return drawPos.InBounds(Map) ? MoteMaker.MakeStaticMote(drawPos, Map, thingDef, scale) : null;
    }
}
