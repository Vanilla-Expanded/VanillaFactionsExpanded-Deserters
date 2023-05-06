using System.Collections.Generic;
using RimWorld;
using Verse;
using Command_ActionWithCooldown = VFECore.Command_ActionWithCooldown;

namespace VFED;

public class CompInvisibilityEngulfer : ThingComp
{
    private const int CooldownTime = GenDate.TicksPerHour;
    private int lastUsedTick = -1;
    public Pawn Wearer => ReloadableUtility.WearerOf(this);

    public void Notify_InvisibilityEnded()
    {
        lastUsedTick = Find.TickManager.TicksGame;
    }

    public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
    {
        foreach (var gizmo in base.CompGetWornGizmosExtra()) yield return gizmo;

        Command_Action invisible;

        if (lastUsedTick > 0)
            invisible = new Command_ActionWithCooldown(lastUsedTick, CooldownTime)
            {
                defaultLabel = "VFED.EnableInvisibility".Translate(),
                defaultDesc = "VFED.EnableInvisibility.Desc".Translate(),
                icon = TexDeserters.EnableInvisibilityTex,
                action = Activate
            };
        else
            invisible = new Command_Action
            {
                defaultLabel = "VFED.EnableInvisibility".Translate(),
                defaultDesc = "VFED.EnableInvisibility.Desc".Translate(),
                icon = TexDeserters.EnableInvisibilityTex,
                action = Activate
            };

        if (lastUsedTick > 0 && Find.TickManager.TicksGame - lastUsedTick < CooldownTime) invisible.Disable("VFED.OnCooldown".Translate());

        if (Wearer.health.hediffSet.GetFirstHediffOfDef(VFED_DefOf.VFED_Invisibility) != null) invisible.Disable("VFED.AlreadyInvisibile".Translate());

        yield return invisible;
    }

    private void Activate()
    {
        var hediff = HediffMaker.MakeHediff(VFED_DefOf.VFED_Invisibility, Wearer);
        hediff.TryGetComp<HediffComp_InvisibilityEngulfer>()?.Notify_Created(parent);
        Wearer.health.AddHediff(hediff);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        Scribe_Values.Look(ref lastUsedTick, nameof(lastUsedTick));
    }
}

public class HediffComp_InvisibilityEngulfer : HediffComp
{
    private Thing source;
    public override bool CompShouldRemove => !Pawn.apparel.WornApparel.Any(t => t.TryGetComp<CompInvisibilityEngulfer>() != null);

    public void Notify_Created(Thing sourceApparel)
    {
        source = sourceApparel;
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        source?.TryGetComp<CompInvisibilityEngulfer>()?.Notify_InvisibilityEnded();
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref source, nameof(source));
    }
}
