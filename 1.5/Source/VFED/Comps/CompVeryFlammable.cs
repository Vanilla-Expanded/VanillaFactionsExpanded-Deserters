using System.Linq;
using RimWorld;
using Verse;

namespace VFED;

public class CompVeryFlammable : ThingComp
{
    private int ticksTillSpark;
    public bool OnFire => parent.Map != null ? parent.OccupiedRect().Cells.SelectMany(c => c.GetThingList(parent.Map)).OfType<Fire>().Any() : false;

    public override void CompTick()
    {
        if (OnFire)
        {
            ticksTillSpark--;
            if (ticksTillSpark <= 0)
            {
                var rect = parent.OccupiedRect();
                var dest = rect.ExpandedBy(4).Cells.Except(rect.Cells).RandomElement();
                (GenSpawn.Spawn(VFED_DefOf.VFED_Spark, rect.Cells.RandomElement(), parent.Map) as Projectile)?.Launch(parent, dest, dest,
                    ProjectileHitFlags.All);
                ticksTillSpark = Rand.Range(25, 35);
            }

            if (parent.IsHashIntervalTick(40))
            {
                FleckMaker.ThrowSmoke(parent.DrawPos, parent.Map, 10f);
                parent.Map.gasGrid.AddGas(parent.OccupiedRect().Cells.RandomElement(), GasType.BlindSmoke, 100);
            }
        }
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref ticksTillSpark, nameof(ticksTillSpark));
    }
}

public class Projectile_Spark : Projectile
{
    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        if (hitThing != null) hitThing.TryAttachFire(0.5f, this);
        else FireUtility.TryStartFireIn(DestinationCell, Map, 0.5f, this);
        base.Impact(hitThing, blockedByShield);
    }
}
