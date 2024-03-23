using RimWorld;
using Verse;

namespace VFED;

public class GameCondition_MissileRain : GameCondition
{
    public override void GameConditionTick()
    {
        base.GameConditionTick();
        if (Find.TickManager.TicksGame % 300 == 0)
        {
            if (SingleMap != null && SingleMap.mapPawns.FreeColonists.TryRandomElement(out var pawn))
                pawn.PositionHeld.DoAerodroneStrike(SingleMap);
            else
                End();
        }
    }
}
