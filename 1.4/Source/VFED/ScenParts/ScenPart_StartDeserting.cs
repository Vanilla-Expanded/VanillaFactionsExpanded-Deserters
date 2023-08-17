using RimWorld;

namespace VFED;

public class ScenPart_StartDeserting : ScenPart
{
    public int startingVisibility;

    public override void PostWorldGenerate()
    {
        base.PostWorldGenerate();
        WorldComponent_Deserters.Instance.JoinDeserters(null);
        WorldComponent_Deserters.Instance.Visibility = startingVisibility;
        WorldComponent_Deserters.Instance.Notify_VisibilityChanged();
    }
}
