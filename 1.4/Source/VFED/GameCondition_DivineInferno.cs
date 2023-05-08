using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using VFEEmpire;

namespace VFED;

public class GameCondition_DivineInferno : GameCondition
{
    private int bombardmentEndTick;
    private int bombardmentStartTick;
    private bool BombardmentActive => Find.TickManager.TicksGame >= bombardmentStartTick;
    private bool BombardmentEnded => Find.TickManager.TicksGame >= bombardmentEndTick;

    private string Forecast =>
        "\n" + "VFED.GlassingDate".Translate().CapitalizeFirst() + ": " + GenDate.DateFullStringAt(GenDate.TickGameToAbs(bombardmentStartTick),
            Find.CurrentMap != null ? Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile) : default)
      + ("\n" + "TimeLeft".Translate().CapitalizeFirst() + ": " + (bombardmentStartTick - Find.TickManager.TicksGame).ToStringTicksToPeriod());

    public override string TooltipString =>
        def.LabelCap + "\n" + "\n" + Description + (BombardmentActive ? BombardmentEnded ? "" : BombardmentStatus : Forecast);

    private string BombardmentStatus =>
        "VFED.ShotsFiredLeft".Translate(Mathf.FloorToInt((float)(Find.TickManager.TicksGame - bombardmentStartTick) / 5f.SecondsToTicks()), 30 - Mathf
           .FloorToInt((float)(Find.TickManager.TicksGame - bombardmentStartTick) / 5f.SecondsToTicks()));

    public override void Init()
    {
        base.Init();
        bombardmentStartTick = startTick + Duration;
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
                if (Find.TickManager.TicksGame == bombardmentStartTick)
                    Find.LetterStack.ReceiveLetter("VFED.DivineInferno.Init".Translate(), "VFED.DivineInferno.Desc".Translate(), LetterDefOf.ThreatBig,
                        new LookTargets(AffectedMaps.Select(map => map.Parent)), Faction.OfEmpire);

                if ((Find.TickManager.TicksGame - bombardmentStartTick) % 5f.SecondsToTicks() == 0)
                    foreach (var map in AffectedMaps)
                    {
                        SoundDefOf.OrbitalStrike_Ordered.PlayOneShotOnCamera();
                        var from = CellFinder.RandomCell(map);
                        var to = CellFinder.RandomCell(map);
                        OrbitalSlicer.DoSlice(from, to, map);
                    }
            }
            else if (WorldComponent_Deserters.Instance.Visibility <= 20) End();
        }
        else if (Find.TickManager.TicksGame == bombardmentEndTick)
        {
            WorldComponent_Deserters.Instance.Visibility = 10;
            WorldComponent_Deserters.Instance.Notify_VisibilityChanged();
        }
    }
}
