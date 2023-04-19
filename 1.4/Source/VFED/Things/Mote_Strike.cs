using RimWorld;
using Verse;

namespace VFED;

public class Mote_Strike : Mote
{
    private ThingDef strikeDef;
    private int ticksTillStrike;
    public override float Alpha => 0.5f;

    public override void Tick()
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
                (strike as Projectile)?.Launch(this, Position, Position, ProjectileHitFlags.IntendedTarget);
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
        exactRotation += 1.2f * deltaTime;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksTillStrike, nameof(ticksTillStrike));
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
