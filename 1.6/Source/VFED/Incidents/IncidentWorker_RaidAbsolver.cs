using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFED;

public class IncidentWorker_RaidAbsolver : IncidentWorker
{
    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (parms.target is not Map map) return false;
        if (!PawnsArrivalModeDefOf.EdgeWalkIn.Worker.TryResolveRaidSpawnCenter(parms)) return false;

        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(VFED_DefOf.VFEE_Empire_Fighter_Absolver, Faction.OfEmpire,
            PawnGenerationContext.NonPlayer, map.Tile, mustBeCapableOfViolence: true, allowAddictions: false, biocodeWeaponChance: 1f,
            biocodeApparelChance: 1f, allowPregnant: false));
        PawnsArrivalModeDefOf.EdgeWalkIn.Worker.Arrive(new List<Pawn> { pawn }, parms);
        LordMaker.MakeNewLord(Faction.OfEmpire, new LordJob_AssassinateColonist(), map, Gen.YieldSingle(pawn));
        return true;
    }
}
