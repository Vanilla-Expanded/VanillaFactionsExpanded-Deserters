using HarmonyLib;
using UnityEngine;
using Verse;

namespace VFED;

public class DesertersMod : Mod
{
    public static Harmony Harm;
    public static DesertersMod Instance;
    public DesertersSettings Settings;

    public DesertersMod(ModContentPack content) : base(content)
    {
        Harm = new Harmony("vanillaexpanded.factions.deserters");
        Harm.PatchAll();
        Settings = GetSettings<DesertersSettings>();
    }

    public static int VisibilityChangePerDay => Instance.Settings.VisibilityChangePerDay;
    public static int IntelFromExtraction => Instance.Settings.IntelFromExtraction;
    public static int VisibilityFromPillar => Instance.Settings.VisibilityFromPillar;
    public static float ResponseTimeMultiplier => Instance.Settings.ResponseTimeMultiplier;

    public override string SettingsCategory() => "VFEDeserters".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        var listing = new Listing_Standard();
        listing.Begin(inRect);

        listing.Label("VFED.VisibilityChangePerDay".Translate(Settings.VisibilityChangePerDay));
        Settings.VisibilityChangePerDay = (int)listing.Slider(Settings.VisibilityChangePerDay, -5, 5);

        listing.Label("VFED.IntelFromExtractingIntel".Translate(Settings.IntelFromExtraction));
        Settings.IntelFromExtraction = (int)listing.Slider(Settings.IntelFromExtraction, 1, 5);

        listing.Label("VFED.VisibilityFromPillar".Translate(Settings.VisibilityFromPillar));
        Settings.VisibilityFromPillar = (int)listing.Slider(Settings.VisibilityFromPillar, 1, 25);

        listing.Label("VFED.ResponseTimeMultiplier".Translate(Settings.ResponseTimeMultiplier));
        Settings.ResponseTimeMultiplier = listing.Slider(Settings.ResponseTimeMultiplier, 0.01f, 5);

        listing.End();
    }
}

public class DesertersSettings : ModSettings
{
    public int IntelFromExtraction = 1;
    public float ResponseTimeMultiplier = 1;
    public int VisibilityChangePerDay = 1;
    public int VisibilityFromPillar = 10;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref VisibilityChangePerDay, nameof(VisibilityChangePerDay), 1);
        Scribe_Values.Look(ref IntelFromExtraction, nameof(IntelFromExtraction), 1);
        Scribe_Values.Look(ref VisibilityFromPillar, nameof(VisibilityFromPillar), 10);
        Scribe_Values.Look(ref ResponseTimeMultiplier, nameof(ResponseTimeMultiplier), 1);
    }
}
