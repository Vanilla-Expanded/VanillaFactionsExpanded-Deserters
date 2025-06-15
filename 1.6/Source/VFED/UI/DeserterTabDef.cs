using System;
using UnityEngine;
using Verse;

namespace VFED;

public class DeserterTabDef : Def
{
    public Type workerClass;

    private DeserterTabWorker worker;

    public DeserterTabDef() => description = "tab";

    public DeserterTabWorker Worker => worker ??= (DeserterTabWorker)Activator.CreateInstance(workerClass);
}

public abstract class DeserterTabWorker
{
    protected Dialog_DeserterNetwork Parent;
    public abstract void DoLeftPart(Rect inRect);
    public abstract void DoMainPart(Rect inRect);

    public virtual void Notify_Open(Dialog_DeserterNetwork parent)
    {
        Parent = parent;
    }
}
