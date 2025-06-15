using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using VEF.Sounds;
using VFEEmpire;

namespace VFED;

public class GameCondition_DivineInferno : GameCondition
{
    private int bombardmentEndTick;
    private int bombardmentStartTick;
    private bool sentStartLetter;
    private bool BombardmentActive => Find.TickManager.TicksGame >= bombardmentStartTick;
    private bool BombardmentEnded => Find.TickManager.TicksGame >= bombardmentEndTick;

    private string Forecast =>
        "\n" + "VFED.GlassingDate".Translate().CapitalizeFirst() + ": " + GenDate.DateFullStringAt(GenDate.TickGameToAbs(bombardmentStartTick),
            Find.CurrentMap != null ? Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile) : default)
      + ("\n" + "TimeLeft".Translate().CapitalizeFirst() + ": " + (bombardmentStartTick - Find.TickManager.TicksGame).ToStringTicksToPeriod());

    public override string TooltipString =>
        def.LabelCap + "\n" + "\n" + Description + (BombardmentActive ? BombardmentEnded ? "" : BombardmentStatus : Forecast);

    private string BombardmentStatus =>
        "\n" + "VFED.ShotsFiredLeft".Translate(Mathf.FloorToInt((float)(Find.TickManager.TicksGame - bombardmentStartTick) / 5f.SecondsToTicks()), 30 - Mathf
           .FloorToInt((float)(Find.TickManager.TicksGame - bombardmentStartTick) / 5f.SecondsToTicks()));

    public override void Init()
    {
        base.Init();
        bombardmentStartTick = startTick + Duration;
        Duration += 5f.SecondsToTicks() * 30;
        bombardmentEndTick = bombardmentStartTick + 5f.SecondsToTicks() * 30;
        Duration += GenDate.TicksPerDay * 10;
    }

    public override float TemperatureOffset() =>
        BombardmentActive
            ? BombardmentEnded
                ? Mathf.Lerp(0, 200,
                    Mathf.InverseLerp(0, GenDate.TicksPerDay * 10, bombardmentEndTick - Find.TickManager.TicksGame))
                : 200f
            : 0f;

    public override void GameConditionTick()
    {
        base.GameConditionTick();
        if (!BombardmentEnded)
        {
            if (BombardmentActive)
            {
                if (!sentStartLetter)
                {
                    Find.LetterStack.ReceiveLetter("VFED.DivineInferno.Init".Translate(), "VFED.DivineInferno.Desc".Translate(), LetterDefOf.ThreatBig,
                        new LookTargets(AffectedMaps.Select(map => map.Parent)), Faction.OfEmpire);
                    ForcedMusicManager.ForceSong(VFED_DefOf.VFED_DivineInferno, 5);
                    sentStartLetter = true;
                }

                var titleHolders = WorldComponent_Hierarchy.Instance.TitleHolders;
                var emperor = titleHolders[titleHolders.Count - 1];

                if ((Find.TickManager.TicksGame - bombardmentStartTick) % 5f.SecondsToTicks() == 0)
                    foreach (var map in AffectedMaps)
                    {
                        SoundDefOf.OrbitalStrike_Ordered.PlayOneShotOnCamera();
                        var from = CellFinder.RandomCell(map);
                        var to = CellFinder.RandomCell(map);
                        OrbitalSlicer.DoSlice(from, to, map, emperor);
                    }
            }
            else if (WorldComponent_Deserters.Instance.Visibility <= 20) End();
        }
        else if (WorldComponent_Deserters.Instance.Visibility > 20)
        {
            WorldComponent_Deserters.Instance.Visibility = 10;
            WorldComponent_Deserters.Instance.Notify_VisibilityChanged();
            ForcedMusicManager.EndSong(VFED_DefOf.VFED_DivineInferno);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref bombardmentStartTick, nameof(bombardmentStartTick));
        Scribe_Values.Look(ref bombardmentEndTick, nameof(bombardmentEndTick));
        Scribe_Values.Look(ref sentStartLetter, nameof(sentStartLetter));
    }
}
