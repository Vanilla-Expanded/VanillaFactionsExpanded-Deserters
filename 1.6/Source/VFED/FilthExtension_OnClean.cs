using RimWorld;
using Verse;

namespace VFED;

public class FilthExtension_OnClean : DefModExtension
{
    public FactionDef factionIdeologyConvertTo;

    public ThoughtDef thoughtCleaned;

    public static void Notify_FilthCleaned(Filth filth, Pawn cleaner)
    {
        if (filth.def.GetModExtension<FilthExtension_OnClean>() is { } ext)
        {
            if (ext.thoughtCleaned != null) cleaner.needs?.mood?.thoughts?.memories?.TryGainMemory(ext.thoughtCleaned);

            if (ModsConfig.IdeologyActive && ext.factionIdeologyConvertTo != null)
            {
                var targetIdeo = Find.FactionManager.FirstFactionOfDef(ext.factionIdeologyConvertTo).ideos.PrimaryIdeo;
                if (cleaner.Ideo != targetIdeo) cleaner.ideo.IdeoConversionAttempt(0.01f, targetIdeo);
            }
        }
    }
}
