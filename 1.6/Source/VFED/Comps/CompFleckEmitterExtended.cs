using RimWorld;
using Verse;
using Verse.Sound;

namespace VFED;

public class CompFleckEmitterExtended : CompFleckEmitter
{
    private CompProperties_FleckEmitterExtended Props => (CompProperties_FleckEmitterExtended)props;

    public override void CompTick()
    {
        if (Props.emissionInterval >= 0)
        {
            if (ticksSinceLastEmitted >= Props.emissionInterval)
            {
                Emit();
                ticksSinceLastEmitted = 0;
                return;
            }

            ticksSinceLastEmitted++;
        }
        else Emit();
    }

    protected new void Emit()
    {
        parent.MapHeld.flecks.CreateFleck(new FleckCreationData
        {
            def = Props.fleck,
            spawnPosition = parent.ActualDrawPos() + Props.offset,
            scale = Props.scale.RandomInRange,
            rotationRate = Props.rotationRate.RandomInRange,
            velocityAngle = Props.velocityAngle.RandomInRange,
            velocitySpeed = Props.velocitySpeed.RandomInRange,
            ageTicksOverride = -1
        });
        if (!Props.soundOnEmission.NullOrUndefined()) Props.soundOnEmission.PlayOneShot(SoundInfo.InMap(parent));
    }
}

public class CompProperties_FleckEmitterExtended : CompProperties_FleckEmitter
{
    public FloatRange rotationRate;

    public FloatRange scale;
    public FloatRange velocityAngle;
    public FloatRange velocitySpeed;

    public CompProperties_FleckEmitterExtended() => compClass = typeof(CompFleckEmitterExtended);
}
