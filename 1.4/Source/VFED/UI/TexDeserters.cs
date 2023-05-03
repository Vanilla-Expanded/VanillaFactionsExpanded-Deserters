using UnityEngine;
using Verse;

namespace VFED;

[StaticConstructorOnStartup]
public static class TexDeserters
{
    public static readonly Texture2D DetonateTex = ContentFinder<Texture2D>.Get("UI/BombPack_Detonate");
    public static readonly Texture2D ExtractIntelTex = ContentFinder<Texture2D>.Get("Designators/RetrieveIntel");
    public static readonly Texture2D DeserterQuestTex = ContentFinder<Texture2D>.Get("QuestIcons/DeserterQuestIcon");
}
