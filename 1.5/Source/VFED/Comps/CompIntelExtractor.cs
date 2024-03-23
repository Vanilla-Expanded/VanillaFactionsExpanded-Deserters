using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFED;

public class CompIntelExtractor : ThingComp
{
    private Pawn Wearer => (parent.holdingOwner.Owner as Pawn_ApparelTracker).pawn;

    public override IEnumerable<Gizmo> CompGetWornGizmosExtra() =>
        base.CompGetWornGizmosExtra()
           .Append(new Command_Action
            {
                defaultLabel = "VFED.ExtractIntel".Translate(),
                defaultDesc = "VFED.ExtractIntel.Desc".Translate(),
                icon = TexDeserters.ExtractIntelTex,
                action = delegate
                {
                    Find.Targeter.BeginTargeting(new()
                        {
                            canTargetPawns = true,
                            canTargetAnimals = false,
                            canTargetHumans = true,
                            canTargetItems = true,
                            mapObjectTargetsMustBeAutoAttackable = false,
                            validator = x =>
                                x.Thing is Pawn { royalty: not null } pawn && pawn.royalty.GetCurrentTitle(Faction.OfEmpire) != null
                                                                           && !pawn.health.hediffSet.PartIsMissing(pawn.health.hediffSet.GetBrain())
                        },
                        delegate(LocalTargetInfo target)
                        {
                            Wearer.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VFED_DefOf.VFED_ExtractIntelPawn, target), JobTag.DraftedOrder);
                        }, Wearer, null, TexDeserters.ExtractIntelTex);
                }
            });
}
