using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using VFECore;

namespace VFED;

[StaticConstructorOnStartup]
[HotSwappable]
public class Building_ZeusCannon : Building
{
    public enum CannonState
    {
        Idle,
        Charging,
        Aiming,
        Firing,
        Returning
    }

    public static Material BaseOutline = MaterialPool.MatFrom("Endgame/ZeusCannon/ZeusCannon_Base_Outline");
    public static Material Base = MaterialPool.MatFrom("Endgame/ZeusCannon/ZeusCannon_Base");
    public static Material GunOutline = MaterialPool.MatFrom("Endgame/ZeusCannon/ZeusCannon_Gun_Outline");
    public static Material Gun = MaterialPool.MatFrom("Endgame/ZeusCannon/ZeusCannon_Gun");
    public static Material GunSupport = MaterialPool.MatFrom("Endgame/ZeusCannon/ZeusCannon_GunSupport");
    public static Material Charge = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f));
    public static Material LightningMat = MatLoader.LoadMat("Weather/LightningBolt");
    public static Mesh GunInnerMesh = MeshPool.GridPlane(new Vector2(1.5f, 0.5f));

    private Mesh[] beamMeshes;
    private CannonState state;
    private Sustainer sustainer;
    private int ticksLeftInState;

    public CannonState State => state;
    public float StateFactor => Mathf.InverseLerp(TicksToCompleteState, 0, ticksLeftInState);

    private int TicksToCompleteState =>
        (state switch
        {
            CannonState.Idle => -1f,
            CannonState.Charging => 15f,
            CannonState.Aiming => 10f,
            CannonState.Firing => 1f,
            CannonState.Returning => 4f,
            _ => throw new ArgumentOutOfRangeException()
        }).SecondsToTicks();

    private float GunAngle =>
        Rotation.AsAngle + state switch
        {
            CannonState.Idle => -45,
            CannonState.Charging => -45,
            CannonState.Aiming => Mathf.Lerp(-45, 65, StateFactor),
            CannonState.Firing => 65,
            CannonState.Returning => Mathf.Lerp(65, -45, StateFactor),
            _ => throw new ArgumentOutOfRangeException()
        };

    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        var mesh = flip ? MeshPool.GridPlaneFlip(def.graphicData.drawSize) : MeshPool.GridPlane(def.graphicData.drawSize);
        var rot = Rotation.AsQuat;
        drawLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor();
        drawLoc.IncAlt(0.25f);
        Graphics.DrawMesh(mesh, drawLoc, rot, BaseOutline, 0);
        drawLoc.IncAlt(0.25f);
        Graphics.DrawMesh(mesh, drawLoc, rot, Base, 0);
        drawLoc.IncAlt(1f);
        Graphics.DrawMesh(mesh, drawLoc, rot, GunSupport, 0);
        DrawGun(drawLoc, mesh);
    }

    private void DrawGun(Vector3 drawLoc, Mesh mesh)
    {
        drawLoc.z += 2.25f;
        var angle = GunAngle;
        drawLoc += Vector3.left.RotatedBy(angle);
        if (state == CannonState.Firing)
        {
            var offset = Mathf.InverseLerp(ticksLeftInState > 45 ? 60 : 30, 45, ticksLeftInState);
            drawLoc += (Vector3.right * offset * 0.5f).RotatedBy(angle);
        }

        var rot = Quaternion.AngleAxis(angle, Vector3.up);
        drawLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor();
        Graphics.DrawMesh(mesh, drawLoc, rot, GunOutline, 0);
        drawLoc.IncAlt(0.75f);
        Graphics.DrawMesh(mesh, drawLoc, rot, Gun, 0);
        var chargePct = state switch
        {
            CannonState.Idle => 0f,
            CannonState.Charging => StateFactor,
            CannonState.Aiming => 1f,
            CannonState.Firing => 0f,
            CannonState.Returning => 0f,
            _ => throw new ArgumentOutOfRangeException()
        };
        if (chargePct > 0.001f)
        {
            var s = new Vector3(0.6f * chargePct, 1f, 0.18f);
            var pos = drawLoc + (Vector3.right * (2.2f - (chargePct - 1) * (0.6f * 0.5f))).RotatedBy(angle)
                              + (Vector3.forward * 0.04f).RotatedBy(angle) + Vector3.up * 0.01f;
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, rot, s), Charge, 0);
        }

        if (state == CannonState.Firing) DrawFiringEffects(drawLoc, angle);
    }

    private void DrawFiringEffects(Vector3 drawLoc, float angle)
    {
        var firingPct = Mathf.InverseLerp(30, 45, ticksLeftInState);
        if (firingPct <= 0) return;
        drawLoc += (Vector3.left * 2.7f).RotatedBy(angle);
        Graphics.DrawMesh(GunInnerMesh, drawLoc, Quaternion.AngleAxis(angle, Vector3.up), FadedMaterialPool.FadedVersionOf(LightningMat, firingPct), 0);
        drawLoc += (Vector3.left * 0.5f).RotatedBy(angle);
        drawLoc.y = AltitudeLayer.Weather.AltitudeFor();
        for (var i = 0; i < 6; i++)
            Graphics.DrawMesh(beamMeshes[i], drawLoc + (Vector3.forward * (i % 3 * 0.3f - 0.5f)).RotatedBy(angle), Quaternion.AngleAxis(angle - 90, Vector3.up),
                FadedMaterialPool.FadedVersionOf(LightningMat, firingPct), 0);
        var s = 90 * Mathf.InverseLerp(60, 45, ticksLeftInState);
        Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(drawLoc, Quaternion.identity, new Vector3(s, 1, s)), FadedMaterialPool.FadedVersionOf(
            ThingDefOf.Mote_PowerBeam.graphic.MatSingle, firingPct), 0);
    }

    public override void Tick()
    {
        if (ticksLeftInState > 0)
        {
            ticksLeftInState--;
            if (ticksLeftInState <= 0)
                GotoState(state switch
                {
                    CannonState.Idle => CannonState.Charging,
                    CannonState.Charging => CannonState.Aiming,
                    CannonState.Aiming => CannonState.Firing,
                    CannonState.Firing => CannonState.Returning,
                    CannonState.Returning => CannonState.Idle,
                    _ => throw new ArgumentOutOfRangeException()
                });
            else if (ticksLeftInState == 200 && state == CannonState.Returning)
                VFED_DefOf.VFE_ZeusCannon_DistantExplosion.PlayOneShot(this);
            else if (ticksLeftInState == 5 && state == CannonState.Aiming)
                VFED_DefOf.VFE_ZeusCannon_Shot.PlayOneShot(this);
        }

        if ((sustainer == null || sustainer.Ended) && state is CannonState.Aiming or CannonState.Returning)
            sustainer = VFED_DefOf.VFE_ZeusCannon_Aiming.TrySpawnSustainer(this);

        if (state == CannonState.Firing)
        {
            var radius = 3 + 12 * StateFactor;
            for (var i = Mathf.CeilToInt(radius); i-- > 0;)
            {
                var randomAngle = Rand.RangeInclusive(0, 360);
                FleckMaker.ThrowDustPuff(DrawPos + (Vector3.left * radius).RotatedBy(randomAngle), Map, 1);
            }

            var gunPos = DrawPos;
            var angle = GunAngle;
            gunPos.z += 2.5f;
            gunPos += Vector3.left.RotatedBy(angle);
            if (state == CannonState.Firing)
            {
                var offset = Mathf.InverseLerp(ticksLeftInState > 45 ? 60 : 30, 45, ticksLeftInState);
                gunPos += (Vector3.right * offset * 0.5f).RotatedBy(angle);
            }

            gunPos += (Vector3.left * 3.7f).RotatedBy(angle);
            if (ticksLeftInState == 60)
                FleckMaker.Static(gunPos, Map, FleckDefOf.ShotFlash, 50);
        }
    }

    public override IEnumerable<Gizmo> GetGizmos() =>
        DebugSettings.ShowDevGizmos && state == CannonState.Idle
            ? base.GetGizmos()
               .Append(new Command_Action
                {
                    defaultLabel = "DEV: Begin Firing",
                    action = () => GotoState(CannonState.Charging)
                })
            : base.GetGizmos();

    private void GotoState(CannonState newState)
    {
        state = newState;
        ticksLeftInState = TicksToCompleteState;

        sustainer?.End();
        sustainer = null;

        if (state == CannonState.Firing)
        {
            beamMeshes = new Mesh[6];
            beamMeshes[0] = LightningBeamMeshPool.RandomBeamMesh;
            beamMeshes[1] = LightningBeamMeshPool.RandomBeamMesh;
            beamMeshes[2] = LightningBeamMeshPool.RandomBeamMesh;
            beamMeshes[3] = LightningBeamMeshPool.RandomBeamMesh;
            beamMeshes[4] = LightningBeamMeshPool.RandomBeamMesh;
            beamMeshes[5] = LightningBeamMeshPool.RandomBeamMesh;
        }
        else beamMeshes = null;

        if (state == CannonState.Charging) VFED_DefOf.VFE_ZeusCannon_Recharging.PlayOneShot(this);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksLeftInState, nameof(ticksLeftInState));
        Scribe_Values.Look(ref state, nameof(state));
    }

    public override string GetInspectString() =>
        Prefs.DevMode ? base.GetInspectString() + $"State: {state}, Ticks Left: {ticksLeftInState}" : base.GetInspectString();
}

public class CompAffectsSky_ZeusCannon : CompAffectsSky
{
    private Building_ZeusCannon Cannon => parent as Building_ZeusCannon;

    public override float LerpFactor => Cannon.State == Building_ZeusCannon.CannonState.Firing ? 1 - Cannon.StateFactor : 0;

    public override SkyTarget SkyTarget =>
        new(1f, new SkyColorSet(new Color(0.9f, 0.95f, 1f),
            new Color(0.784313738f, 0.8235294f, 0.847058833f), new Color(0.9f, 0.95f, 1f), 1.15f), 1f, 1f);
}

public class CompProperties_AffectsSky_ZeusCannon : CompProperties_AffectsSky
{
    public CompProperties_AffectsSky_ZeusCannon() => compClass = typeof(CompAffectsSky_ZeusCannon);
}
