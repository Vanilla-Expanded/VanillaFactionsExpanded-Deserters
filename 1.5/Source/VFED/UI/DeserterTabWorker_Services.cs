using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using VFECore.UItils;

namespace VFED;

public class DeserterTabWorker_Services : DeserterTabWorker
{
    private static readonly List<GenUI.AnonymousStackElement> stackElements = new();
    private bool criticalOpen;
    private Vector2 descScrollPos;
    private Vector2 leftScrollPos;
    private bool normalOpen;

    private float rewardsRectLastHeight = 55;
    private Quest selected;

    public override void Notify_Open(Dialog_DeserterNetwork parent)
    {
        base.Notify_Open(parent);
        WorldComponent_Deserters.Instance.EnsureQuestListFilled();
        selected ??= WorldComponent_Deserters.Instance.ServiceQuests[0];
    }

    public override void DoLeftPart(Rect inRect)
    {
        var height = 50f;
        WorldComponent_Deserters.Instance.ServiceQuests.Split(out var normal, out var critical, quest => quest.challengeRating <= 3);

        if (normalOpen) height += normal.Count * 30;
        if (criticalOpen) height += critical.Count * 30;

        var viewRect = new Rect(0, 0, inRect.width - 20, height);
        Widgets.BeginScrollView(inRect, ref leftScrollPos, viewRect);
        var normalRect = viewRect.TakeTopPart(25);
        if (Widgets.ButtonImage(normalRect.TakeLeftPart(25), normalOpen ? TexButton.Collapse : TexButton.Reveal))
        {
            (normalOpen ? SoundDefOf.TabClose : SoundDefOf.TabOpen).PlayOneShotOnCamera();
            normalOpen = !normalOpen;
        }

        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(normalRect, "VFED.AvailableQuests".Translate());

        if (normalOpen) DoQuestList(ref viewRect, normal);

        var criticalRect = viewRect.TakeTopPart(25);

        if (Widgets.ButtonImage(criticalRect.TakeLeftPart(25), criticalOpen ? TexButton.Collapse : TexButton.Reveal))
        {
            (criticalOpen ? SoundDefOf.TabClose : SoundDefOf.TabOpen).PlayOneShotOnCamera();
            criticalOpen = !criticalOpen;
        }

        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(criticalRect, "VFED.CriticalQuests".Translate());

        if (criticalOpen) DoQuestList(ref viewRect, critical);

        Widgets.EndScrollView();
    }

    private void DoQuestList(ref Rect inRect, List<Quest> quests)
    {
        foreach (var quest in quests)
        {
            var rect = inRect.TakeTopPart(30);
            rect.TakeLeftPart(3);
            if (Widgets.ButtonInvisible(rect)) selected = quest;
            if (quest == selected) Widgets.DrawHighlightSelected(rect);
            rect = rect.ContractedBy(3f);
            GUI.DrawTexture(rect.TakeLeftPart(35).ContractedBy(2.5f, 0), TexDeserters.DeserterQuestTex);
            var starRect = rect.TakeRightPart(75);
            var num = Mathf.Max(quest.challengeRating, 1);
            for (var i = 0; i < num; i++)
                GUI.DrawTexture(new(starRect.xMax - 15 * (i + 1), starRect.y + starRect.height / 2f - 7f, 15f, 15f), TexDeserters.RatingIcon);

            if (Mouse.IsOver(starRect))
            {
                TooltipHandler.TipRegion(starRect, "QuestChallengeRatingTip".Translate());
                Widgets.DrawHighlight(starRect);
            }

            Widgets.Label(rect, quest.name);
        }
    }

    public override void DoMainPart(Rect inRect)
    {
        using (new TextBlock(GameFont.Medium))
            Widgets.Label(inRect.TakeTopPart(30).ContractedBy(3, 0), "VFED.QuestDescription".Translate());

        var descRect = inRect.TakeTopPart(174);
        Widgets.DrawMenuSection(descRect);
        descRect = descRect.ContractedBy(7);
        descRect.xMax += 5;
        var desc = selected.description.Resolve();
        var viewRect = new Rect(0, 0, descRect.width - 20, Text.CalcHeight(desc, descRect.width - 20));
        Widgets.BeginScrollView(descRect, ref descScrollPos, viewRect);
        Widgets.Label(viewRect, desc);
        Widgets.EndScrollView();

        QuestPart_Choice choice = null;
        var parts = selected.PartsListForReading;
        for (var i = 0; i < parts.Count; i++)
        {
            choice = parts[i] as QuestPart_Choice;
            if (choice != null) break;
        }

        if (choice != null)
        {
            stackElements.Clear();
            inRect.yMin += 2;
            var rewardsRect = inRect.TakeTopPart(15 + Text.LineHeight + rewardsRectLastHeight);
            Widgets.DrawMenuSection(rewardsRect);
            rewardsRect = rewardsRect.ContractedBy(5f);
            Widgets.Label(rewardsRect.TakeTopPart(Text.LineHeight), "VFED.Rewards".Translate());
            Widgets.DrawLineHorizontal(rewardsRect.x + 2.5f, rewardsRect.y, rewardsRect.width - 5);
            rewardsRect.yMin += 5;

            var rewards = choice.choices[0].rewards;
            for (var i = 0; i < rewards.Count; i++) stackElements.AddRange(rewards[i].StackElements);

            rewardsRectLastHeight = GenUI.DrawElementStack(rewardsRect, 24f, stackElements,
                    delegate(Rect r, GenUI.AnonymousStackElement obj) { obj.drawer(r); },
                    obj => obj.width, 4f, 5f, false)
               .height;
            stackElements.Clear();
            inRect.yMin += 3;
        }

        var basketRect = inRect.TakeTopPart(150);
        selected.GetIntelCost(out var intelCost, out var criticalIntelCost);
        if (DesertersUIUtility.DoPurchaseButton(basketRect.TakeRightPart(150).ContractedBy(30).TopPartPixels(60), "VFED.Activate".Translate(), intelCost,
                criticalIntelCost,
                Parent))
        {
            selected.hidden = false;
            selected.hiddenInUI = false;
            WorldComponent_Deserters.Instance.ServiceQuests.Remove(selected);
            WorldComponent_Deserters.Instance.EnsureQuestListFilled();
            selected.Accept(null);
            Parent.Close();
            MainButtonDefOf.Quests.Worker.Activate();
            ((MainTabWindow_Quests)MainButtonDefOf.Quests.TabWindow).Select(selected);
        }

        DesertersUIUtility.DrawIntelCost(ref basketRect, "VFED.TotalQuestCost".Translate(), intelCost, criticalIntelCost);

        using (new TextBlock(GameFont.Tiny)) Widgets.Label(basketRect, "VFED.QuestPurchaseDesc".Translate().Colorize(ColoredText.SubtleGrayColor));

        using (new TextBlock(GameFont.Medium)) Widgets.Label(inRect.TakeTopPart(30), "VFED.OtherServices".Translate());

        var count = DefDatabase<DeserterServiceDef>.DefCount;
        var rows = Mathf.CeilToInt(count / 6f);
        var height = rows * (Gizmo.Height + 25 + GizmoGridDrawer.GizmoSpacing.y);
        var width = Mathf.Min(count, 6) * (Gizmo.Height + GizmoGridDrawer.GizmoSpacing.x);
        var fullRect = new Rect(0, inRect.y + 5f, width, height).CenteredOnXIn(inRect);
        var rect = fullRect.TakeTopPart(Gizmo.Height + 25);
        count = 0;
        foreach (var service in DefDatabase<DeserterServiceDef>.AllDefs)
        {
            DrawService(rect.TakeLeftPart(Gizmo.Height), service);
            rect.xMin += GizmoGridDrawer.GizmoSpacing.x;
            count++;
            if (count >= 6)
            {
                fullRect.yMin += GizmoGridDrawer.GizmoSpacing.y;
                rect = fullRect.TakeTopPart(Gizmo.Height + 25);
                count = 0;
            }
        }
    }

    private void DrawService(Rect inRect, DeserterServiceDef service)
    {
        var cost = Mathf.FloorToInt(service.intelCost * WorldComponent_Deserters.Instance.VisibilityLevel.intelCostModifier);
        var intelRect = inRect.TakeTopPart(25);

        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(intelRect.RightHalf(), cost.ToString());

        Widgets.DefIcon(intelRect.LeftHalf().RightPartPixels(25).ContractedBy(1.5f),
            service.useCriticalIntel ? VFED_DefOf.VFED_CriticalIntel : VFED_DefOf.VFED_Intel);

        var color = Color.white;
        if (Mouse.IsOver(inRect)) color = GenUI.MouseoverColor;

        MouseoverSounds.DoRegion(inRect, SoundDefOf.Mouseover_Command);
        GUI.color = color;
        GenUI.DrawTextureWithMaterial(inRect, Command.BGTex, null);
        Widgets.DrawTextureFitted(inRect, service.Icon, 0.85f);
        GUI.color = Color.white;
        if (Widgets.ButtonInvisible(inRect))
        {
            if (service.useCriticalIntel)
            {
                if (cost > Parent.TotalCriticalIntel)
                    Messages.Message("VFED.NotEnough".Translate(VFED_DefOf.VFED_CriticalIntel.LabelCap, cost, Parent.TotalCriticalIntel),
                        MessageTypeDefOf.RejectInput, false);
                else
                {
                    Parent.TrySpendIntel(0, cost);
                    service.worker.Call();
                }
            }
            else
            {
                if (cost > Parent.TotalIntel)
                    Messages.Message("VFED.NotEnough".Translate(VFED_DefOf.VFED_Intel.LabelCap, cost, Parent.TotalIntel),
                        MessageTypeDefOf.RejectInput, false);
                else
                {
                    Parent.TrySpendIntel(cost, 0);
                    service.worker.Call();
                }
            }
        }

        var labelCap = service.LabelCap;
        if (!labelCap.NullOrEmpty())
            using (new TextBlock(GameFont.Tiny, TextAnchor.UpperCenter))
            {
                var num = Text.CalcHeight(labelCap, inRect.width);
                var rect3 = new Rect(inRect.x, inRect.yMax - num + 12f, inRect.width, num);
                GUI.DrawTexture(rect3, TexUI.GrayTextBG);
                Widgets.Label(rect3, labelCap);
            }


        TooltipHandler.TipRegion(inRect, service.description);
    }
}
