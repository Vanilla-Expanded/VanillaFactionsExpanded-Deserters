using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace VFED;

[StaticConstructorOnStartup]
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
    private Mesh[] beamMeshes;

    private CannonState state;
    private Sustainer sustainer;
    private int ticksLeftInState;

    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        var mesh = flip ? MeshPool.GridPlaneFlip(def.graphicData.drawSize) : MeshPool.GridPlane(def.graphicData.drawSize);
        var rot = Rotation.AsQuat;
        drawLoc.y = AltitudeLayer.Building.AltitudeFor();
        drawLoc.IncAlt(-0.25f);
        Graphics.DrawMesh(mesh, drawLoc, rot, BaseOutline, 0);
        drawLoc.IncAlt(0.25f);
        Graphics.DrawMesh(mesh, drawLoc, rot, Base, 0);
        drawLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor();
        drawLoc.IncAlt(1f);
        Graphics.DrawMesh(mesh, drawLoc, rot, GunSupport, 0);
        DrawGun(drawLoc, mesh);
    }

    private void DrawGun(Vector3 drawLoc, Mesh mesh)
    {
        drawLoc.z += 2.25f;
        var angle = Rotation.AsAngle + state switch
        {
            CannonState.Idle => -45,
            CannonState.Charging => -45,
            CannonState.Aiming => Mathf.Lerp(-45, 65, Mathf.InverseLerp(600, 0, ticksLeftInState)),
            CannonState.Firing => 65,
            CannonState.Returning => Mathf.Lerp(65, -45, Mathf.InverseLerp(240, 0, ticksLeftInState)),
            _ => throw new ArgumentOutOfRangeException()
        };
        drawLoc += Vector3.left.RotatedBy(angle);
        if (state == CannonState.Firing)
        {
            var offset = Mathf.InverseLerp(ticksLeftInState > 30 ? 60 : 0, 30, ticksLeftInState);
            drawLoc += (Vector3.right * offset * 0.5f).RotatedBy(angle);
        }

        var rot = Quaternion.AngleAxis(angle, Vector3.up);
        drawLoc.y = AltitudeLayer.Building.AltitudeFor();
        drawLoc.IncAlt(-0.5f);
        Graphics.DrawMesh(mesh, drawLoc, rot, GunOutline, 0);
        drawLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor();
        drawLoc.IncAlt(0.5f);
        Graphics.DrawMesh(mesh, drawLoc, rot, Gun, 0);
        var chargePct = state switch
        {
            CannonState.Idle => 0f,
            CannonState.Charging => Mathf.InverseLerp(900, 0, ticksLeftInState),
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

        if (state == CannonState.Firing)
        {
            drawLoc += (Vector3.left * 3.2f).RotatedBy(angle);
            DrawFiringEffects(drawLoc, angle);
        }
    }

    private void DrawFiringEffects(Vector3 drawLoc, float angle)
    {
        var firingPct = Mathf.InverseLerp(0, 60, ticksLeftInState);
        drawLoc.y = AltitudeLayer.Weather.AltitudeFor();
        for (var i = 0; i < 6; i++)
            Graphics.DrawMesh(beamMeshes[i], drawLoc + (Vector3.forward * (i % 3 * 0.3f - 0.5f)).RotatedBy(angle), Quaternion.AngleAxis(angle - 90, Vector3.up),
                FadedMaterialPool.FadedVersionOf(LightningMat, firingPct), 0);
        var s = 90 * (1 - firingPct);
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
        }

        if (sustainer == null || sustainer.Ended)
            if (state is CannonState.Aiming or CannonState.Returning)
                sustainer = VFED_DefOf.VFE_ZeusCannon_Aiming.TrySpawnSustainer(this);
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
        ticksLeftInState = (state switch
        {
            CannonState.Idle => -1f,
            CannonState.Charging => 15f,
            CannonState.Aiming => 10f,
            CannonState.Firing => 1f,
            CannonState.Returning => 4f,
            _ => throw new ArgumentOutOfRangeException()
        }).SecondsToTicks();

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
        else if (state == CannonState.Firing) VFED_DefOf.VFE_ZeusCannon_Shot.PlayOneShot(this);
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

public static class LightningBeamMeshMaker
{
    private static List<Vector2> verts2D;

    private static Vector2 lightningTop;

    public static Mesh NewBeamMesh()
    {
        lightningTop = new Vector2(0, 200f);
        MakeVerticesBase();
        PerturbVerticesRandomly();
        DoubleVertices();
        return MeshFromVerts();
    }

    private static void MakeVerticesBase()
    {
        var num = (int)Math.Ceiling((Vector2.zero - lightningTop).magnitude / 0.25f);
        var b = lightningTop / num;
        verts2D = new List<Vector2>();
        var vector = Vector2.zero;
        for (var i = 0; i < num; i++)
        {
            verts2D.Add(vector);
            vector += b;
        }
    }

    private static void PerturbVerticesRandomly()
    {
        var perlin = new Perlin(0.0070000002160668373, 2.0, 0.5, 6, Rand.Range(0, int.MaxValue), QualityMode.High);
        var list = verts2D.ListFullCopy();
        verts2D.Clear();
        for (var i = 0; i < list.Count; i++)
        {
            var d = (float)perlin.GetValue(i, 0.0, 0.0);
            var item = list[i] + d * Vector2.right;
            verts2D.Add(item);
        }
    }

    private static void DoubleVertices()
    {
        var list = verts2D.ListFullCopy();
        var a = default(Vector2);
        verts2D.Clear();
        for (var i = 0; i < list.Count; i++)
        {
            if (i <= list.Count - 2)
            {
                var vector = Quaternion.AngleAxis(90f, Vector3.up) * (list[i] - list[i + 1]);
                a = new Vector2(vector.y, vector.z);
                a.Normalize();
            }

            var item = list[i] - 1f * a;
            var item2 = list[i] + 1f * a;
            verts2D.Add(item);
            verts2D.Add(item2);
        }
    }

    private static Mesh MeshFromVerts()
    {
        var array = new Vector3[verts2D.Count];
        for (var i = 0; i < array.Length; i++) array[i] = new Vector3(verts2D[i].x, 0f, verts2D[i].y);

        var num = 0f;
        var array2 = new Vector2[verts2D.Count];
        for (var j = 0; j < verts2D.Count; j += 2)
        {
            array2[j] = new Vector2(0f, num);
            array2[j + 1] = new Vector2(1f, num);
            num += 0.04f;
        }

        var array3 = new int[verts2D.Count * 3];
        for (var k = 0; k < verts2D.Count - 2; k += 2)
        {
            var num2 = k * 3;
            array3[num2] = k;
            array3[num2 + 1] = k + 1;
            array3[num2 + 2] = k + 2;
            array3[num2 + 3] = k + 2;
            array3[num2 + 4] = k + 1;
            array3[num2 + 5] = k + 3;
        }

        return new Mesh
        {
            vertices = array,
            uv = array2,
            triangles = array3,
            name = "MeshFromVerts()"
        };
    }
}

public static class LightningBeamMeshPool
{
    private static readonly List<Mesh> boltMeshes = new();

    public static Mesh RandomBeamMesh
    {
        get
        {
            if (boltMeshes.Count < 20)
            {
                var mesh = LightningBeamMeshMaker.NewBeamMesh();
                boltMeshes.Add(mesh);
                return mesh;
            }

            return boltMeshes.RandomElement();
        }
    }
}
