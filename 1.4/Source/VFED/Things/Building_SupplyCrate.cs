using RimWorld;
using UnityEngine;
using Verse;

namespace VFED;

public class Building_SupplyCrate : Building_Casket
{
    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (!respawningAfterLoad)
        {
            contentsKnown = false;
            GenerateContents();
        }
    }

    protected virtual void GenerateContents()
    {
        var (def, ext) = ContrabandManager.AllContraband.RandomElement();
        var thing = ThingMaker.MakeThing(def);
        thing.stackCount = ext.useCriticalIntel ? Mathf.CeilToInt(3f / ext.intelCost) : Mathf.CeilToInt(10f / ext.intelCost);
        innerContainer.TryAdd(thing);
        if (Rand.Chance(0.1f))
        {
            var intel = ThingMaker.MakeThing(VFED_DefOf.VFED_Intel);
            intel.stackCount = Rand.Range(1, 4);
            innerContainer.TryAdd(intel);
        }
    }

    public override void Open()
    {
        base.Open();
        QuestUtility.SendQuestTargetSignals(questTags, "Opened", this.Named("SUBJECT"));
        var pos = Position;
        var map = Map;
        Destroy();
        GenSpawn.Spawn(ThingDefOf.ChunkSlagSteel, pos, map);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        innerContainer.ClearAndDestroyContents(mode);
        base.Destroy(mode);
    }
}
