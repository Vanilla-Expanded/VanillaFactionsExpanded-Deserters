using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace VFED;

public class CompIntelExtract : ThingComp
{
    private bool intelExtracted;

    public bool CanExtract => !intelExtracted;

    private bool HasDesignation => parent.MapHeld.designationManager.DesignationOn(parent, VFED_DefOf.VFED_ExtractIntel) != null;

    public void Extract(Pawn pawn)
    {
        if (!CanExtract) return;
        intelExtracted = true;
        parent.MapHeld?.designationManager?.TryRemoveDesignationOn(parent, VFED_DefOf.VFED_ExtractIntel);
        var intel = ThingMaker.MakeThing(VFED_DefOf.VFED_Intel);
        intel.stackCount = DesertersMod.IntelFromExtraction;
        GenPlace.TryPlaceThing(intel, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
    }

    private void AddDesignation()
    {
        if (!HasDesignation) parent.MapHeld.designationManager.AddDesignation(new(parent, VFED_DefOf.VFED_ExtractIntel));
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra() =>
        CanExtract && parent.MapHeld != null && !HasDesignation
            ? base.CompGetGizmosExtra()
               .Append(new Command_Action
                {
                    defaultLabel = VFED_DefOf.VFED_ExtractIntel.LabelCap,
                    defaultDesc = VFED_DefOf.VFED_ExtractIntel.description,
                    icon = TexDeserters.ExtractIntelTex,
                    action = AddDesignation
                })
            : base.CompGetGizmosExtra();

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn) =>
        CanExtract
            ? base.CompFloatMenuOptions(selPawn)
               .Append(selPawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly)
                    ? new(VFED_DefOf.VFED_ExtractIntel.LabelCap,
                        delegate
                        {
                            AddDesignation();
                            selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VFED_DefOf.VFED_ExtractIntelJob, parent), JobTag.DraftedOrder);
                        })
                    : new("CannotUseNoPath".Translate(), null))
            : base.CompFloatMenuOptions(selPawn);

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref intelExtracted, nameof(intelExtracted));
    }
}
