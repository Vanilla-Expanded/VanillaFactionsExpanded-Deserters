using Verse;
using Verse.AI;
using Verse.AI.Group;
using VFEEmpire;

namespace VFED;

public class LordToil_AttackClosest : LordToil
{
    public LordToil_AttackClosest() => data = new LordToilData_PawnTarget();

    private LordToilData_PawnTarget Data => data as LordToilData_PawnTarget;

    public override void UpdateAllDuties()
    {
        Data.Target ??= AttackTargetFinder
           .BestAttackTarget(lord.ownedPawns[0], TargetScanFlags.NeedActiveThreat | TargetScanFlags.NeedReachable, thing => thing is Pawn)
           .Thing as Pawn;
        if (Data.Target != null)
            foreach (var pawn in lord.ownedPawns)
                pawn.mindState.duty = new PawnDuty(VFEE_DefOf.VFEE_AttackEnemySpecifc, Data.Target);
    }

    public override void LordToilTick()
    {
        base.LordToilTick();
        if (Data.Target != null && (Data.Target.Dead || Data.Target.Downed)) lord.ReceiveMemo("TargetDead");
    }
}
