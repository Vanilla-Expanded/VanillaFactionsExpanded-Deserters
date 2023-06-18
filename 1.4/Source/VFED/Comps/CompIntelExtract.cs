using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace VFED;

public class CompIntelExtract : ThingComp
{
    private bool intelExtracted;

    public bool CanExtract => !intelExtracted;

    public void Extract(Pawn pawn)
    {
        if (!CanExtract) return;
        intelExtracted = true;
        parent.Map.designationManager.TryRemoveDesignationOn(parent, VFED_DefOf.VFED_ExtractIntel);
        var intel = ThingMaker.MakeThing(VFED_DefOf.VFED_Intel);
        intel.stackCount = DesertersMod.IntelFromExtraction;
        GenPlace.TryPlaceThing(intel, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra() =>
        CanExtract
            ? base.CompGetGizmosExtra()
               .Append(new Command_Action
                {
                    defaultLabel = VFED_DefOf.VFED_ExtractIntel.LabelCap,
                    defaultDesc = VFED_DefOf.VFED_ExtractIntel.description,
                    icon = TexDeserters.ExtractIntelTex,
                    action = delegate { parent.Map.designationManager.AddDesignation(new Designation(parent, VFED_DefOf.VFED_ExtractIntel)); }
                })
            : base.CompGetGizmosExtra();

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn) =>
        CanExtract && parent.Map.designationManager.DesignationOn(parent, VFED_DefOf.VFED_ExtractIntel) != null
            ? base.CompFloatMenuOptions(selPawn)
               .Append(new FloatMenuOption(VFED_DefOf.VFED_ExtractIntel.LabelCap,
                    delegate { selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VFED_DefOf.VFED_ExtractIntelJob, parent), JobTag.DraftedOrder); }))
            : base.CompFloatMenuOptions(selPawn);

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref intelExtracted, nameof(intelExtracted));
    }
}
