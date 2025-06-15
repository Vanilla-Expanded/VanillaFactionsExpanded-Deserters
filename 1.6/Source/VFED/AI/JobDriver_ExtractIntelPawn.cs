﻿using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace VFED;

public class JobDriver_ExtractIntelPawn : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        var gotoToil = new Toil
        {
            initAction = pawn.pather.StopDead,
            tickAction = delegate
            {
                var targetPawn = job.targetA.Pawn;
                pawn.rotationTracker.FaceTarget(targetPawn);
                var map = pawn.Map;
                if (GenSight.LineOfSight(pawn.Position, targetPawn.Position, map, true)
                 && pawn.Position.DistanceTo(targetPawn.Position) <= 1.9f
                 && (!pawn.pather.Moving || pawn.pather.nextCell.GetDoor(map) == null))
                {
                    pawn.pather.StopDead();
                    pawn.rotationTracker.FaceTarget(targetPawn);
                    ReadyForNextToil();
                }
                else if (!pawn.pather.Moving) pawn.pather.StartPath(TargetA, PathEndMode.Touch);
            },
            handlingFacing = true,
            socialMode = RandomSocialMode.Off,
            defaultCompleteMode = ToilCompleteMode.Never
        };
        yield return gotoToil;

        var waitToil = Toils_General.WaitWith(TargetIndex.A, 30, true, true, false, TargetIndex.A);
        waitToil.AddPreTickAction(delegate
        {
            var targetPawn = job.targetA.Pawn;
            var map = pawn.Map;
            if (!GenSight.LineOfSight(pawn.Position, targetPawn.Position, map, true)
             || pawn.Position.DistanceTo(targetPawn.Position) > 1.9f)
                JumpToToil(gotoToil);
        });
        yield return waitToil;

        yield return Toils_General.DoAtomic(delegate
        {
            var targetPawn = job.targetA.Pawn;

            VFED_DefOf.DispensePaste.PlayOneShot(targetPawn);
            var drawPos = targetPawn.DrawPos;
            for (var i = 0; i < 3; i++)
            {
                var vector = Rand.InsideUnitCircle * Rand.Range(0.25f, 0.75f) * Rand.Sign;
                var loc = new Vector3(drawPos.x + vector.x, drawPos.y, drawPos.z + vector.y);
                FleckMaker.Static(loc, targetPawn.Map, VFED_DefOf.VFED_BloodMist);
            }

            var title = targetPawn.royalty.GetCurrentTitle(Faction.OfEmpire);
            var intel = ThingMaker.MakeThing(IntelTypeForTitle(title));
            intel.stackCount = IntelCountForTitle(title);
            GenPlace.TryPlaceThing(intel, job.targetA.Cell, targetPawn.Map, ThingPlaceMode.Near);

            var extractor = pawn.apparel.WornApparel.FirstOrDefault(t => t.TryGetComp<CompIntelExtractor>() != null);
            targetPawn.TakeDamage(new DamageInfo(DamageDefOf.ExecutionCut, 9999, 100, pawn.DrawPos.AngleToFlat(targetPawn.DrawPos), pawn,
                targetPawn.health.hediffSet.GetBrain(), extractor?.def));

            if (targetPawn.Faction != null && targetPawn.Faction != pawn.Faction && !targetPawn.Faction.HostileTo(pawn.Faction))
                targetPawn.Faction.TryAffectGoodwillWith(pawn.Faction, -25, reason: HistoryEventDefOf.UsedHarmfulAbility);
            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(VFED_DefOf.VFED_UsedDeclassifier);
            extractor?.Destroy();
        });
    }

    private static int IntelCountForTitle(RoyalTitleDef title) =>
        title.seniority switch
        {
            0 => 1, // Freeholder
            100 => 2, // Yeoman
            200 => 4, // Acolyte
            300 => 8, // Knight
            400 => 16, // Praetor
            500 => 24, // Baron
            600 => 3, // Count
            601 => 4, // Archcount
            602 => 5, // Marquess
            700 => 6, // Duke
            701 => 7, // Archduke
            800 => 8, // Consul
            801 => 9, // Magister
            802 => 10, // Despot
            900 => 12, // Stellarch
            901 => 18 // High Stellarch
        };

    private static ThingDef IntelTypeForTitle(RoyalTitleDef title)
    {
        return title.seniority switch
        {
            <= 500 => VFED_DefOf.VFED_Intel,
            <= 901 => VFED_DefOf.VFED_CriticalIntel
        };
    }
}
