using RimWorld;
using UnityEngine;

namespace VFED;

public abstract class StorytellerComp_ByVisibility : StorytellerComp
{
    public float VisibilityFactor => WorldComponent_Deserters.Instance.Active ? Mathf.InverseLerp(0, 100, WorldComponent_Deserters.Instance.Visibility) : 0;

    public bool AllowGoodEvents => WorldComponent_Deserters.Instance.Active && WorldComponent_Deserters.Instance.Visibility > 0;
}
