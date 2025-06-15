using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using VFEEmpire;

namespace VFED;

public class QuestNode_FlagshipFight : QuestNode
{
    public SlateRef<string> inSignal;
    public SlateRef<MapParent> mapParent;

    protected override void RunInt()
    {
        var shipDamaged = QuestGenUtility.HardcodedSignalWithQuestID("ship.Damaged");
        var shipDestroyed = QuestGenUtility.HardcodedSignalWithQuestID("ship.Destroyed");
        var slate = QuestGen.slate;

        QuestGen.quest.AddPart(new QuestPart_FlagshipFight
        {
            inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal"),
            mapParent = mapParent.GetValue(slate),
            shipDamaged = shipDamaged,
            shipDestroyed = shipDestroyed
        });
    }

    protected override bool TestRunInt(Slate slate) => true;
}

public class QuestPart_FlagshipFight : QuestPart
{
    public string inSignal;
    public MapParent mapParent;
    public string shipDamaged;
    public string shipDestroyed;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == inSignal && mapParent.HasMap) mapParent.Map.GetComponent<MapComponent_FlagshipFight>().Initiate(shipDamaged, shipDestroyed);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, nameof(inSignal));
        Scribe_References.Look(ref mapParent, nameof(mapParent));
        Scribe_Values.Look(ref shipDamaged, nameof(shipDamaged));
        Scribe_Values.Look(ref shipDestroyed, nameof(shipDestroyed));
    }
}

public class QuestNode_GetEmperor : QuestNode
{
    public SlateRef<string> storeAs;

    private bool DoIt(Slate slate)
    {
        var emperor = WorldComponent_Hierarchy.Instance.TitleHolders.Last();
        if (emperor?.royalty == null || emperor.royalty.GetCurrentTitle(Faction.OfEmpire) != VFEE_DefOf.Emperor
                                     || storeAs.GetValue(slate).NullOrEmpty()) return false;
        slate.Set(storeAs.GetValue(slate), emperor);
        return true;
    }

    protected override void RunInt() => DoIt(QuestGen.slate);

    protected override bool TestRunInt(Slate slate) => DoIt(slate);
}

public class QuestNode_GetSiteTileForComplex : QuestNode
{
    private static readonly List<PlanetTile> tmpTiles = new();

    public SlateRef<List<BiomeDef>> preferedBiomes;
    [NoTranslate] public SlateRef<string> storeAs;

    protected override void RunInt()
    {
        TryFindTile(QuestGen.slate);
    }

    protected override bool TestRunInt(Slate slate) => TryFindTile(slate);

    private bool TryFindTile(Slate slate)
    {
        tmpTiles.Clear();
        PlanetTile result;
        var root = (slate.Get<Map>("map") ?? Find.RandomPlayerHomeMap)?.Tile ?? Find.WorldObjects.Caravans.FirstOrDefault()?.Tile ?? -1;
        if (root == -1) return false;
        if (preferedBiomes.TryGetValue(slate, out var biomesList) && !biomesList.NullOrEmpty())
        {
            tmpTiles.Clear();
            var list = Find.World.tilesInRandomOrder.Tiles;
            var biomes = biomesList.ToHashSet();
            var grid = Find.WorldGrid;
            for (var i = list.Count; i-- > 0;)
            {
                if (biomes.Contains(grid[list[i]].PrimaryBiome)) tmpTiles.Add(list[i]);
                if (tmpTiles.Count > 50) break;
            }

            if (tmpTiles.TryRandomElementByWeight(tile => 1 / Find.WorldGrid.ApproxDistanceInTiles(tile, root), out result))
            {
                slate.Set(storeAs.GetValue(slate), result);
                return true;
            }
        }

        if (TileFinder.TryFindPassableTileWithTraversalDistance(root, 7, 27, out result, static tile => Find.WorldGrid[tile].hilliness == Hilliness.Flat &&
                                                                                                        !Find.WorldObjects.AnyWorldObjectAt(tile)
                                                                                                     && TileFinder.IsValidTileForNewSettlement(tile)))
        {
            slate.Set(storeAs.GetValue(slate), result);
            return true;
        }

        if (TileFinder.TryFindPassableTileWithTraversalDistance(root, 7, 27, out result, static tile => Find.WorldGrid[tile].hilliness == Hilliness.Flat &&
                                                                                                        !Find.WorldObjects.AnyWorldObjectAt(tile)
                                                                                                     && TileFinder.IsValidTileForNewSettlement(tile)
                                                                                                     && (!Find.World.Impassable(tile)
                                                                                                      || Find.WorldGrid[tile].WaterCovered),
                canTraverseImpassable: true))
        {
            slate.Set(storeAs.GetValue(slate), result);
            return true;
        }

        return false;
    }
}
