using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using VFEEmpire;

namespace VFED;

public class Building_BombPackDeployed : Building_Bomb
{
    protected override void Tick()
    {
        base.Tick();
        ticksLeft++;
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
    {
        yield break;
    }

    public override IEnumerable<Gizmo> GetGizmos() =>
        base.GetGizmos()
           .Append(new Command_Action
            {
                defaultLabel = "VFED.Detonate.BombPack".Translate(),
                defaultDesc = "VFED.Detonate.BombPack.Desc".Translate(),
                icon = TexDeserters.DetonateTex,
                action = delegate
                {
                    GenExplosion.DoExplosion(Position, Map, 10f, DamageDefOf.Bomb, this, 60, 5f, ignoredThings: new List<Thing> { this });
                    Destroy();
                }
            });
}
