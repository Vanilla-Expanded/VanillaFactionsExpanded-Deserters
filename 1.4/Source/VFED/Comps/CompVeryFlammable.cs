using System.Linq;
using RimWorld;
using Verse;

namespace VFED;

public class CompVeryFlammable : ThingComp
{
    public bool OnFire => parent.OccupiedRect().Cells.SelectMany(c => c.GetThingList(parent.Map)).OfType<Fire>().Any();


    public override void CompTick()
    {
        base.CompTick();
        if (OnFire)
        {
            if (parent.IsHashIntervalTick(60))
            {
                var rect = parent.OccupiedRect();
                var dest = rect.ExpandedBy(4).Cells.Except(rect.Cells).RandomElement();
                (GenSpawn.Spawn(VFED_DefOf.VFED_Spark, rect.Cells.RandomElement(), parent.Map) as Projectile)?.Launch(parent, dest, dest,
                    ProjectileHitFlags.All);
            }

            if (parent.IsHashIntervalTick(40))
            {
                FleckMaker.ThrowSmoke(parent.DrawPos, parent.Map, 10f);
                parent.Map.gasGrid.AddGas(parent.OccupiedRect().Cells.RandomElement(), GasType.BlindSmoke, 100);
            }
        }
    }
}

public class Projectile_Spark : Projectile
{
    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        if (hitThing != null) hitThing.TryAttachFire(0.5f);
        else FireUtility.TryStartFireIn(DestinationCell, Map, 0.5f);
        base.Impact(hitThing, blockedByShield);
    }
}
