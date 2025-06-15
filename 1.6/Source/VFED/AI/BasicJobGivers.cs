using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFED;

public class JobGiver_StandGuard : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        var job = JobMaker.MakeJob(JobDefOf.Wait_Combat);
        job.expiryInterval = 600;
        var map = pawn.Map;
        foreach (var c in GenAdjFast.AdjacentCellsCardinal(pawn.Position))
        {
            if (c.Standable(map)) continue;
            var rot = Rot4.FromIntVec3(c - pawn.Position).Rotated(RotationDirection.Clockwise);
            if ((c + rot.FacingCell).GetFirstThing<Building_Door>(map) != null)
            {
                job.overrideFacing = rot;
                break;
            }

            if ((c + rot.Opposite.FacingCell).GetFirstThing<Building_Door>(map) != null)
            {
                job.overrideFacing = rot.Opposite;
                break;
            }
        }

        return job;
    }
}

public class JobGiver_SitOnThrone : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        var throne = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Throne), PathEndMode.InteractionCell,
            TraverseParms.For(pawn, Danger.None));
        if (throne == null) return null;
        if (throne.Position == pawn.Position) return JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
        return JobMaker.MakeJob(JobDefOf.Goto, throne.Position);
    }
}

public class JobGiver_AIPatrol : JobGiver_AIFightEnemy
{
    protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
    {
        var enemyTarget = pawn.mindState.enemyTarget;
        var verb = verbToUse ?? pawn.TryGetAttackVerb(enemyTarget, !pawn.IsColonist);
        if (verb == null)
        {
            dest = IntVec3.Invalid;
            return false;
        }

        return CastPositionFinder.TryFindCastPosition(new()
        {
            caster = pawn,
            target = enemyTarget,
            verb = verb,
            maxRangeFromTarget = 9999f,
            locus = pawn.Position,
            maxRangeFromLocus = 5,
            wantCoverFromTarget = verb.verbProps.range > 7f
        }, out dest);
    }
}

public class JobGiver_WanderBetweenDutyLocations : JobGiver_Wander
{
    protected override IntVec3 GetExactWanderDest(Pawn pawn)
    {
        var target1 = pawn.mindState.duty.focus;
        var target2 = pawn.mindState.duty.focusSecond;
        if (pawn.Position.InHorDistOf(target1.Cell, 5)) return target2.Cell;

        if (pawn.Position.InHorDistOf(target2.Cell, 5)) return target1.Cell;

        var dist1 = target1.Cell.DistanceToSquared(pawn.Position);
        var dist2 = target2.Cell.DistanceToSquared(pawn.Position);

        if (dist1 > dist2) return target2.Cell;
        if (dist1 < dist2) return target1.Cell;
        return Rand.Bool ? target1.Cell : target2.Cell;
    }

    protected override IntVec3 GetWanderRoot(Pawn pawn) => throw new NotImplementedException();
}

public class JobGiver_FleeEnemies : ThinkNode_JobGiver
{
    private readonly List<Thing> tmpThreats = new();

    protected override Job TryGiveJob(Pawn pawn)
    {
        tmpThreats.Clear();
        var potentialTargetsFor = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
        for (var i = 0; i < potentialTargetsFor.Count; i++)
        {
            var thing = potentialTargetsFor[i].Thing;
            if (FleeUtility.ShouldFleeFrom(thing, pawn, false, true)) tmpThreats.Add(thing);
        }

        var list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.AlwaysFlee);
        for (var j = 0; j < list.Count; j++)
        {
            var thing2 = list[j];
            if (FleeUtility.ShouldFleeFrom(thing2, pawn, false, true)) tmpThreats.Add(thing2);
        }

        if (!tmpThreats.Any()) return null;
        var c = CellFinderLoose.GetFleeDest(pawn, tmpThreats);
        tmpThreats.Clear();
        return JobMaker.MakeJob(VFED_DefOf.VFED_FleeEnemies, c);
    }
}
