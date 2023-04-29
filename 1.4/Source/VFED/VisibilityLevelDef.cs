using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace VFED;

public class VisibilityLevelDef : Def
{
    public float contrabandIntelCostModifier = 1;
    public float contrabandSiteTimeActiveModifier = 1;
    public float contrabandTimeToReceiveModifier = 1;
    public Texture2D Icon;
    public string iconPath;
    public float imperialResponseTime;
    public string imperialResponseType;
    public List<string> specialEffects;
    public IntRange visibilityRange;

    public override void PostLoad()
    {
        base.PostLoad();
        LongEventHandler.ExecuteWhenFinished(delegate { Icon = ContentFinder<Texture2D>.Get(iconPath); });
    }
}
