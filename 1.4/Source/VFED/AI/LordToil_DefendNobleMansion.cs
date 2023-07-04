using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFED;

public abstract class LordToil_DefendNobleMansion : LordToil
{
    public LordToilData_DefendNobleMansion Data
    {
        get => data as LordToilData_DefendNobleMansion;
        set => data = value;
    }
}

public class LordToilData_DefendNobleMansion : LordToilData
{
    public LordToilData_DefendNobleMansion() { }

    public LordToilData_DefendNobleMansion(Pawn noble, IEnumerable<Pawn> guards, IEnumerable<Pawn> onCall, IEnumerable<Pawn> patrollers, CellRect patrolArea,
        List<CellRect> bunkerAreas)
    {
        this.noble = noble;
        this.guards = guards.ToHashSet();
        this.onCall = onCall.ToHashSet();
        this.patrollers = patrollers.ToHashSet();
        this.patrolArea = patrolArea;
        this.bunkerAreas = bunkerAreas;
    }

    public LordToilData_DefendNobleMansion Clone() => new(noble, guards, onCall, patrollers, patrolArea, bunkerAreas.ListFullCopy());

    public override void ExposeData()
    {
        Scribe_References.Look(ref noble, nameof(noble));
        Scribe_Collections.Look(ref guards, nameof(guards), LookMode.Reference);
        Scribe_Collections.Look(ref onCall, nameof(onCall), LookMode.Reference);
        Scribe_Collections.Look(ref patrollers, nameof(patrollers), LookMode.Reference);
        Scribe_Values.Look(ref patrolArea, nameof(patrolArea));
        Scribe_Collections.Look(ref bunkerAreas, nameof(bunkerAreas), LookMode.Value);
    }

    // ReSharper disable InconsistentNaming
    public List<CellRect> bunkerAreas;
    public HashSet<Pawn> guards = new();
    public Pawn noble;
    public HashSet<Pawn> onCall = new();
    public CellRect patrolArea;

    public HashSet<Pawn> patrollers = new();
    // ReSharper restore InconsistentNaming
}

public class LordToil_DefendNobleMansion_Passive : LordToil_DefendNobleMansion
{
    public LordToil_DefendNobleMansion_Passive() { }

    public LordToil_DefendNobleMansion_Passive(LordToilData data) => this.data = data;

    public override void UpdateAllDuties()
    {
        foreach (var pawn in lord.ownedPawns)
            if (pawn == Data.noble) pawn.mindState.duty = new PawnDuty(VFED_DefOf.VFED_SitOnThrone);
            else if (Data.guards.Contains(pawn)) pawn.mindState.duty = new PawnDuty(VFED_DefOf.VFED_StandGuard, pawn.Position, 2.9f);
            else if (Data.onCall.Contains(pawn))
            {
                var bunker = Data.bunkerAreas.FirstOrDefault(rect => rect.Contains(pawn.Position));
                pawn.mindState.duty = new PawnDuty(DutyDefOf.Defend, bunker.CenterCell, bunker.Radius());
            }
            else if (Data.patrollers.Contains(pawn))
            {
                var cell = Data.patrolArea.ClosestCellTo(pawn.Position);
                var rot = Rot4.FromAngleFlat((cell - pawn.Position).AngleFlat);
                pawn.mindState.duty = new PawnDuty(VFED_DefOf.VFED_Patrol, Data.patrolArea.FarthestPoint(cell, rot.Rotated(RotationDirection.Clockwise)),
                    Data.patrolArea.FarthestPoint(cell, rot.Rotated(RotationDirection.Counterclockwise)));
            }
            else pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony) { pickupOpportunisticWeapon = true };
    }
}

public class LordToil_DefendNobleMansion_Active : LordToil_DefendNobleMansion
{
    public LordToil_DefendNobleMansion_Active() { }

    public LordToil_DefendNobleMansion_Active(LordToilData data) => this.data = data;

    public override void UpdateAllDuties()
    {
        foreach (var pawn in lord.ownedPawns)
            if (pawn == Data.noble) pawn.mindState.duty = new PawnDuty(VFED_DefOf.VFED_SitOnThrone);
            else if (Data.guards.Contains(pawn)) pawn.mindState.duty = new PawnDuty(VFED_DefOf.VFED_StandGuard);
            else if (Data.onCall.Contains(pawn)) pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony) { pickupOpportunisticWeapon = true };
            else if (Data.patrollers.Contains(pawn))
            {
                var cell = Data.patrolArea.ClosestCellTo(pawn.Position);
                var rot = Rot4.FromAngleFlat((cell - pawn.Position).AngleFlat);
                pawn.mindState.duty = new PawnDuty(VFED_DefOf.VFED_Patrol, Data.patrolArea.FarthestPoint(cell, rot.Rotated(RotationDirection.Clockwise)),
                    Data.patrolArea.FarthestPoint(cell, rot.Rotated(RotationDirection.Counterclockwise)));
            }
            else pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony) { pickupOpportunisticWeapon = true };
    }
}

public class LordToil_DefendNobleMansion_HighAlert : LordToil_DefendNobleMansion
{
    public LordToil_DefendNobleMansion_HighAlert() { }

    public LordToil_DefendNobleMansion_HighAlert(LordToilData data) => this.data = data;

    public override void UpdateAllDuties()
    {
        foreach (var pawn in lord.ownedPawns)
            if (pawn == Data.noble) pawn.mindState.duty = new PawnDuty(VFED_DefOf.VFED_SitOnThrone);
            else if (Data.guards.Contains(pawn)) pawn.mindState.duty = new PawnDuty(VFED_DefOf.VFED_StandGuard);
            else pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony) { pickupOpportunisticWeapon = true };
    }
}

public class LordToil_DefendNobleMansion_Emergency : LordToil_DefendNobleMansion
{
    public LordToil_DefendNobleMansion_Emergency() { }

    public LordToil_DefendNobleMansion_Emergency(LordToilData data) => this.data = data;

    public override void UpdateAllDuties()
    {
        foreach (var pawn in lord.ownedPawns)
            if (pawn == Data.noble) pawn.mindState.duty = new PawnDuty(VFED_DefOf.VFED_SitOnThrone);
            else if (Data.guards.Contains(pawn))
            {
                if (pawn.GetRoom()?.ContainsCell(Data.noble.Position) ?? false) pawn.mindState.duty = new PawnDuty(VFED_DefOf.VFED_StandGuard);
                else pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony) { pickupOpportunisticWeapon = true };
            }
            else pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony) { pickupOpportunisticWeapon = true };
    }
}

public class LordToil_DefendNobleMansion_Flee : LordToil_DefendNobleMansion
{
    public LordToil_DefendNobleMansion_Flee() { }

    public LordToil_DefendNobleMansion_Flee(LordToilData data) => this.data = data;

    public override void UpdateAllDuties()
    {
        foreach (var pawn in lord.ownedPawns)
            if (pawn == Data.noble) pawn.mindState.duty = new PawnDuty(DutyDefOf.ExitMapBestAndDefendSelf);
            else if (Data.guards.Contains(pawn))
            {
                if (pawn.Position.InHorDistOf(Data.noble.Position, 17.9f))
                    pawn.mindState.duty = Data.noble.Spawned
                        ? new PawnDuty(DutyDefOf.Escort, Data.noble, 8.9f)
                        : new PawnDuty(DutyDefOf.ExitMapBestAndDefendSelf);
                else pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony) { pickupOpportunisticWeapon = true };
            }
            else pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony) { pickupOpportunisticWeapon = true };
    }
}
