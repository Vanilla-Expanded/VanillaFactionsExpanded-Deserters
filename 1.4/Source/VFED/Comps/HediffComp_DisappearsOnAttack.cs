using Verse;

namespace VFED;

public class HediffComp_DissapearsOnAttack : HediffComp
{
    public override bool CompShouldRemove =>
        Pawn?.stances?.curStance is Stance_Warmup { ticksLeft: <= 1, verb: { verbProps: { violent: true } } }
         or Stance_Cooldown { verb: { verbProps: { violent: true } } };
}
