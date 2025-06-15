using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFED;

public class WorkGiver_ExtractIntel : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) =>
        pawn.Map.designationManager.SpawnedDesignationsOfDef(VFED_DefOf.VFED_ExtractIntel).Select(designation => designation.target.Thing);

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !pawn.Map.designationManager.AnySpawnedDesignationOfDef(VFED_DefOf.VFED_ExtractIntel);

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) =>
        pawn.Map.designationManager.DesignationOn(t, VFED_DefOf.VFED_ExtractIntel) != null && pawn.CanReserve(t, 1, -1, null, forced)
                                                                                           && pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly);

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) => JobMaker.MakeJob(VFED_DefOf.VFED_ExtractIntelJob, t);
}
