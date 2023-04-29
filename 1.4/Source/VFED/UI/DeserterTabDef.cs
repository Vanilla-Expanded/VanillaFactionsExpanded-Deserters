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
    public abstract void DoLeftPart(Rect inRect);
    public abstract void DoMainPart(Rect inRect);
}
