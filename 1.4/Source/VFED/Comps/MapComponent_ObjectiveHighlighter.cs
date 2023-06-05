using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VFED;

[StaticConstructorOnStartup]
public class MapComponent_ObjectiveHighlighter : MapComponent, ISignalReceiver
{
    private static readonly Material ArrowMatWhite = MaterialPool.MatFrom("UI/Overlays/Arrow", ShaderDatabase.CutoutFlying, Color.white);
    private string completeSignal;
    private List<Thing> objectives;
    private List<string> questTags;
    public MapComponent_ObjectiveHighlighter(Map map) : base(map) { }

    public void Notify_SignalReceived(Signal signal)
    {
        if (signal.tag == completeSignal)
        {
            var target = signal.args.GetArg<Thing>("SUBJECT");
            if (target == null) return;
            objectives.Remove(target);
            if (objectives.NullOrEmpty())
            {
                QuestUtility.SendQuestTargetSignals(questTags, "Complete");
                Deactivate();
            }
        }
    }

    public override void MapComponentUpdate()
    {
        base.MapComponentUpdate();
        if (objectives.NullOrEmpty() || WorldRendererUtility.WorldRenderedNow || Find.CurrentMap != map) return;

        for (var i = objectives.Count; i-- > 0;)
        {
            var objective = objectives[i];

            var progress = Math.Abs(objective.HashOffsetTicks()) % 1000;
            if (progress < 500) progress = 1000 - progress;

            var pos = objective.DrawPos + Vector3.forward * (1f + progress / 1000f);
            pos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
            var opacity = 1f - progress / 2000f;

            Log.Message($"Drawing arrow: {progress}, {pos}, {opacity}");

            var rotation = Quaternion.AngleAxis(180, Vector3.up);
            Graphics.DrawMesh(MeshPool.plane10, pos, rotation, FadedMaterialPool.FadedVersionOf(ArrowMatWhite, opacity), 0);
        }
    }

    public void Activate(string completeSignal, string questTag)
    {
        this.completeSignal = completeSignal;
        questTags = new List<string> { questTag };
        objectives = new List<Thing>();
        Find.SignalManager.RegisterReceiver(this);
    }

    public void Deactivate()
    {
        completeSignal = null;
        questTags = null;
        objectives = null;
        Find.SignalManager.DeregisterReceiver(this);
    }

    public void AddObjective(Thing thing)
    {
        objectives.Add(thing);
    }

    public override void MapRemoved()
    {
        base.MapRemoved();
        if (completeSignal != null) Deactivate();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref completeSignal, nameof(completeSignal));
        Scribe_Collections.Look(ref questTags, "questTags", LookMode.Value);
        Scribe_Collections.Look(ref objectives, nameof(objectives), LookMode.Reference);
    }
}
