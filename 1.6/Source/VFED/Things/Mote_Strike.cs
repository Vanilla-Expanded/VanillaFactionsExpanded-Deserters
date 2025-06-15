using RimWorld;
using Verse;
using VFEEmpire;

namespace VFED;

public class Mote_Strike : Mote
{
    private ThingDef strikeDef;
    private int ticksTillStrike;
    public override float Alpha => 0.5f;

    protected override void Tick()
    {
        base.Tick();
        if (ticksTillStrike > 0)
        {
            ticksTillStrike--;
            if (ticksTillStrike <= 0) Strike();
        }
    }

    protected virtual void Strike()
    {
        if (strikeDef != null)
        {
            if (strikeDef.skyfaller != null)
            {
                var strike = SkyfallerMaker.MakeSkyfaller(strikeDef);
                strike.TryGetComp<CompStrike>()?.Notify_Launched(this);
                GenSpawn.Spawn(strike, Position, Map);
            }
            else if (strikeDef.projectile != null)
            {
                var strike = ThingMaker.MakeThing(strikeDef);
                strike.TryGetComp<CompStrike>()?.Notify_Launched(this);
                var origin = new IntVec3(Rand.Bool ? 0 : Map.Size.x - 1, 0, Rand.Range(Map.Size.z - 17, Map.Size.z));
                GenSpawn.Spawn(strike, origin, Map);
                var titleHolders = WorldComponent_Hierarchy.Instance.TitleHolders;
                (strike as Projectile)?.Launch(titleHolders[titleHolders.Count - 1] /* Should be the emperor */, Position, Position,
                    ProjectileHitFlags.IntendedTarget);
            }
        }

        strikeDef = null;
        ticksTillStrike = -1;
    }

    public void LaunchStrike(ThingDef strike, float delaySeconds)
    {
        strikeDef = strike;
        ticksTillStrike = delaySeconds.SecondsToTicks();
    }

    protected override void TimeInterval(float deltaTime)
    {
        base.TimeInterval(deltaTime);
        exactRotation += 1.8f * deltaTime;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksTillStrike, nameof(ticksTillStrike));
        Scribe_Defs.Look(ref strikeDef, nameof(strikeDef));
    }
}

public class CompStrike : ThingComp
{
    private Mote_Strike strikeMote;

    public void Notify_Launched(Mote_Strike mote)
    {
        strikeMote = mote;
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        strikeMote.Destroy();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref strikeMote, nameof(strikeMote));
    }
}
