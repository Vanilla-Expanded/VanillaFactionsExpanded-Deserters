using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VFED;

public class JobDriver_UseItemOn : JobDriver
{
    private CompUsable compUsable;
    private Mote warmupMote;
    public CompUsable CompUsable => compUsable ??= TargetThingA.TryGetComp<CompUsable>();

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed) && pawn.Reserve(job.targetB, job, errorOnFailed: errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
        this.FailOn(() => !CompUsable.CanBeUsedBy(pawn, out _));
        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        this.FailOnDespawnedOrNull(TargetIndex.B);
        yield return Toils_Goto.GotoThing(TargetIndex.A, TargetThingA.def.hasInteractionCell ? PathEndMode.InteractionCell : PathEndMode.Touch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.B, TargetThingB.def.hasInteractionCell ? PathEndMode.InteractionCell : PathEndMode.Touch);
        var prepare = Toils_General.Wait(CompUsable.Props.useDuration, TargetIndex.B);
        prepare.WithProgressBarToilDelay(TargetIndex.B);
        prepare.FailOnCannotTouch(TargetIndex.B, TargetThingA.def.hasInteractionCell ? PathEndMode.InteractionCell : PathEndMode.Touch);
        prepare.handlingFacing = true;
        prepare.tickAction = delegate
        {
            TargetThingA.TryGetComp<CompUseEffect>()?.PrepareTick();

            if (warmupMote == null && CompUsable.Props.warmupMote != null)
                warmupMote = MoteMaker.MakeAttachedOverlay(TargetThingB, CompUsable.Props.warmupMote, Vector3.zero);

            warmupMote?.Maintain();
            pawn.rotationTracker.FaceTarget(TargetB);
        };
        if (TargetThingA.TryGetComp<CompTargetable>().Props.nonDownedPawnOnly)
        {
            prepare.FailOnDestroyedOrNull(TargetIndex.B);
            prepare.FailOnDowned(TargetIndex.B);
        }

        yield return prepare;
        var use = ToilMaker.MakeToil();
        use.initAction = delegate
        {
            CompUsable.UsedBy(pawn);
            if (CompUsable.Props.finalizeMote != null)
                MoteMaker.MakeAttachedOverlay(TargetThingB, CompUsable.Props.finalizeMote, Vector3.zero);
        };
        use.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return use;
    }
}
