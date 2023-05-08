using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace VFED.Jobs;

public class JobDriver_ExtractIntel : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.WaitWith(TargetIndex.A, 3600, true, true, false, TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.DoAtomic(delegate { job.targetA.Thing.TryGetComp<CompIntelExtract>()?.Extract(pawn); });
    }
}
