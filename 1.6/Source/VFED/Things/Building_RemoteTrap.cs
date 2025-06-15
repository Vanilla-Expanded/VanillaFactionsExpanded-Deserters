using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFED;

[StaticConstructorOnStartup]
public abstract class Building_RemoteTrap : Building_Trap
{
    protected override float SpringChance(Pawn p) => 0f;

    public override IEnumerable<Gizmo> GetGizmos() =>
        base.GetGizmos()
           .Append(new Command_Action
            {
                defaultLabel = "VFED.Detonate".Translate(),
                icon = TexDeserters.DetonateTex,
                action = delegate
                {
                    Spring(this.GetRegion().ListerThings.ThingsInGroup(ThingRequestGroup.Pawn).OfType<Pawn>().RandomElementWithFallback()
                        ?? Map.mapPawns.AllPawnsSpawned.RandomElementWithFallback());
                }
            });
}

public class Building_RemoteTrapExplosive : Building_RemoteTrap
{
    protected override void SpringSub(Pawn p)
    {
        GetComp<CompExplosive>().StartWick();
    }
}
