using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFED;

[StaticConstructorOnStartup]
public static class ContrabandManager
{
    public static List<(ThingDef, ContrabandExtension)> AllContraband = new();
    public static Dictionary<ContrabandCategoryDef, List<(ThingDef, ContrabandExtension)>> ContrabandByCategory = new();
    public static Dictionary<ContrabandCategoryDef, bool> OpenCategories = new();
    public static Dictionary<(ThingDef, ContrabandExtension), int> ShoppingCart = new();
    public static int TotalCostIntel;
    public static int TotalCostCriticalIntel;
    public static int TotalAmount;

    private static readonly List<string> projects = new()
    {
        "ArtificialMetabolism",
        "NeuralComputation",
        "SkinHardening",
        "HealingFactors",
        "FleshShaping",
        "MolecularAnalysis",
        "CircadianInfluence"
    };

    static ContrabandManager()
    {
        foreach (var thing in from project in projects
                 from thing in DefDatabase<ResearchProjectDef>.GetNamed(project).UnlockedDefs.OfType<ThingDef>()
                 where thing.techHediffsTags.Any(tag => tag.StartsWith("ImplantEmpire"))
                 select thing)
            GiveExtension(thing);

        foreach (var def in DefDatabase<ThingDef>.AllDefs)
            if (def.GetModExtension<ContrabandExtension>() is { } extension || TryGiveExtension(def, out extension))
            {
                SetCostIfMissing(def, extension);
                Register(def, extension);
            }

        foreach (var category in DefDatabase<ContrabandCategoryDef>.AllDefs) OpenCategories.Add(category, false);
    }

    public static void AddToCart(ThingDef item, ContrabandExtension ext)
    {
        if (ext.useCriticalIntel) TotalCostCriticalIntel += ext.TotalIntelCost();
        else TotalCostIntel += ext.TotalIntelCost();

        TotalAmount++;

        if (ShoppingCart.ContainsKey((item, ext))) ShoppingCart[(item, ext)]++;
        else ShoppingCart.Add((item, ext), 1);
    }

    public static void RemoveFromCart(ThingDef item, ContrabandExtension ext, bool all = false)
    {
        if (all)
        {
            var amount = ShoppingCart[(item, ext)];
            TotalAmount -= amount;
            var totalCost = ext.TotalIntelCost() * amount;
            if (ext.useCriticalIntel) TotalCostCriticalIntel -= totalCost;
            else TotalCostIntel -= totalCost;
            ShoppingCart.Remove((item, ext));
        }
        else
        {
            if (ShoppingCart[(item, ext)] == 1) ShoppingCart.Remove((item, ext));
            else ShoppingCart[(item, ext)]--;
            TotalAmount--;
            if (ext.useCriticalIntel) TotalCostCriticalIntel -= ext.TotalIntelCost();
            else TotalCostIntel -= ext.TotalIntelCost();
        }
    }

    public static void ClearCart()
    {
        ShoppingCart.Clear();
        TotalCostIntel = TotalCostCriticalIntel = 0;
    }

    public static bool TryGiveExtension(ThingDef item, out ContrabandExtension ext)
    {
        if (item.GetCompProperties<CompProperties_Techprint>() != null)
        {
            ext = GiveExtension(item);
            return true;
        }

        ext = null;
        return false;
    }

    public static ContrabandExtension GiveExtension(ThingDef item, ContrabandCategoryDef category = null)
    {
        var ext = new ContrabandExtension { category = category ?? VFED_DefOf.VFED_Imperial };
        item.modExtensions ??= new List<DefModExtension>();
        item.modExtensions.Add(ext);
        return ext;
    }

    public static void SetCostIfMissing(ThingDef item, ContrabandExtension ext)
    {
        if (ext.intelCost == -1) ext.intelCost = Math.Max(1, Mathf.FloorToInt(item.BaseMarketValue / (ext.useCriticalIntel ? 300 : 100)));
    }

    public static void Register(ThingDef item, ContrabandExtension ext)
    {
        AllContraband.Add((item, ext));
        if (!ContrabandByCategory.TryGetValue(ext.category, out var list))
        {
            list = new List<(ThingDef, ContrabandExtension)>();
            ContrabandByCategory.Add(ext.category, list);
        }

        list.Add((item, ext));
    }
}
