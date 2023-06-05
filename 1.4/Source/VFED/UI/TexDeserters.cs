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
}
