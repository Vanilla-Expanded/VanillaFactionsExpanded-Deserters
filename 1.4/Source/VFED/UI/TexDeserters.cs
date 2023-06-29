using UnityEngine;
using Verse;

namespace VFED;

[StaticConstructorOnStartup]
public static class TexDeserters
{
    public static readonly Texture2D DetonateTex = ContentFinder<Texture2D>.Get("UI/BombPack_Detonate");
    public static readonly Texture2D ExtractIntelTex = ContentFinder<Texture2D>.Get("Designators/RetrieveIntel");
    public static readonly Texture2D DeserterQuestTex = ContentFinder<Texture2D>.Get("QuestIcons/DeserterQuestIcon");
    public static readonly Texture2D EnableInvisibilityTex = ContentFinder<Texture2D>.Get("UI/EnableInvisibility");
    public static readonly Texture2D VisibilityIncreaseTex = ContentFinder<Texture2D>.Get("UI/IconVisibility_5");
    public static readonly Texture2D VisibilityDecreaseTex = ContentFinder<Texture2D>.Get("UI/IconVisibility_1");
    public static Texture2D RatingIcon = ContentFinder<Texture2D>.Get("UI/Icons/ChallengeRatingIcon");
    public static Texture2D PlotCompletedTex = ContentFinder<Texture2D>.Get("UI/Plot_Completed");
    public static Texture2D CombatLowIcon = ContentFinder<Texture2D>.Get("UI/IconCombat_1");
    public static Texture2D CombatMediumIcon = ContentFinder<Texture2D>.Get("UI/IconCombat_2");
    public static Texture2D CombatHighIcon = ContentFinder<Texture2D>.Get("UI/IconCombat_3");
}
