using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using rjw;
using rjw.Modules.Interactions;

namespace Stripper
{
    public class Dialog_ProstitutionNegotiation : Window
    {
        private string text;
        private string title;
        private Pawn dancer;
        private Pawn customer;
        private Action<SexInteractionResolved> onAccept;
        private Action onReject;

        // 行為選択リスト
        private List<SexInteractionResolved> availableInteractions;
        private SexInteractionResolved selectedInteraction;
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(640f, 520f);

        public Dialog_ProstitutionNegotiation(
            string title,
            string text,
            Pawn dancer,
            Pawn customer,
            Action<SexInteractionResolved> onAccept,
            Action onReject)
        {
            this.title = title;
            this.text = text;
            this.dancer = dancer;
            this.customer = customer;
            this.onAccept = onAccept;
            this.onReject = onReject;

            this.forcePause = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.absorbInputAroundWindow = true;

            // ダンサー(initiator)と客(recipient)の間で実行可能な行為一覧を取得
            availableInteractions = BuildInteractionList(dancer, customer);

            // デフォルトで最初の行為を選択しておく
            selectedInteraction = availableInteractions.FirstOrDefault();
        }

        private static List<SexInteractionResolved> BuildInteractionList(Pawn initiator, Pawn recipient)
        {
            try
            {
                var props = new SexProps(initiator, recipient) { isWhoring = true };

                var interactions = SexInteractionFinder.FindInteractions(props)
                    .GroupBy(i => i.Interaction.Def)
                    .Select(g => g.First())
                    .Where(i => i.Interaction.Sextype != xxx.rjwSextype.None
                             && i.Interaction.Sextype != xxx.rjwSextype.Masturbation)
                    .OrderBy(i => i.Interaction.Extension.RMBLabel)
                    .ToList();

                return interactions;
            }
            catch (Exception ex)
            {
                Log.Warning($"[StripperPole] Failed to build interaction list: {ex.Message}");
                return new List<SexInteractionResolved>();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            // タイトル
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 32f), title);
            Text.Font = GameFont.Small;

            float y = 38f;

            const float iconSize = 72f;
            const float iconAreaWidth = 88f;

            // 左 ダンサー
            Rect dancerIconRect = new Rect(10f, y, iconSize, iconSize);
            Widgets.ThingIcon(dancerIconRect, dancer);
            Widgets.Label(new Rect(10f, y + iconSize + 2f, iconAreaWidth, 20f), dancer.LabelShort);

            // 右 客
            Rect customerIconRect = new Rect(inRect.width - iconAreaWidth + 8f, y, iconSize, iconSize);
            Widgets.ThingIcon(customerIconRect, customer);
            Widgets.Label(new Rect(inRect.width - iconAreaWidth + 8f, y + iconSize + 2f, iconAreaWidth, 20f), customer.LabelShort);

            // 中央 テキスト
            Rect textRect = new Rect(iconAreaWidth + 12f, y, inRect.width - (iconAreaWidth + 12f) * 2f, iconSize + 22f);
            Widgets.Label(textRect, text);

            y += iconSize + 28f;

            // 
            Text.Font = GameFont.Small;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(10f, y, inRect.width - 20f, 22f), "SP_SelectAct".Translate());
            GUI.color = Color.white;
            y += 24f;

            Widgets.DrawLineHorizontal(10f, y, inRect.width - 20f);
            y += 4f;

            const float buttonHeight = 34f;
            const float buttonSpacing = 4f;

            float buttonAreaBottom = inRect.height - 50f;
            float scrollAreaHeight = buttonAreaBottom - y - 8f;

            Rect scrollOuterRect = new Rect(10f, y, inRect.width - 20f, scrollAreaHeight);

            float totalContentHeight = availableInteractions.Count * (buttonHeight + buttonSpacing);
            Rect scrollInnerRect = new Rect(0f, 0f, scrollOuterRect.width - 20f, Mathf.Max(totalContentHeight, scrollAreaHeight));

            Widgets.BeginScrollView(scrollOuterRect, ref scrollPosition, scrollInnerRect);

            if (availableInteractions.Count == 0)
            {
                Text.Font = GameFont.Small;
                GUI.color = Color.yellow;
                Widgets.Label(new Rect(4f, 4f, scrollInnerRect.width, 30f), "SP_NoActsAvailable".Translate());
                GUI.color = Color.white;
            }
            else
            {
                float btnY = 0f;
                foreach (var resolved in availableInteractions)
                {
                    bool isSelected = resolved == selectedInteraction;

                    Rect btnRect = new Rect(4f, btnY, scrollInnerRect.width - 8f, buttonHeight);

                    if (isSelected)
                    {
                        GUI.color = new Color(0.5f, 0.7f, 1f, 1f);
                    }

                    string label = resolved.Interaction.Extension.RMBLabel.CapitalizeFirst();
                    if (label.NullOrEmpty())
                        label = resolved.Interaction.Def.defName;

                    string subLabel = BuildPartLabel(resolved);
                    string fullLabel = subLabel.NullOrEmpty() ? label : $"{label}  [{subLabel}]";

                    if (Widgets.ButtonText(btnRect, fullLabel))
                    {
                        selectedInteraction = resolved;
                    }

                    GUI.color = Color.white;

                    btnY += buttonHeight + buttonSpacing;
                }
            }

            Widgets.EndScrollView();

            Rect buttonRow = new Rect(0f, inRect.height - 42f, inRect.width, 38f);

            bool canAccept = selectedInteraction != null;

            GUI.enabled = canAccept;
            if (Widgets.ButtonText(new Rect(buttonRow.x + 50f, buttonRow.y, 200f, buttonRow.height), "SP_Prostitute_Accept".Translate()))
            {
                onAccept?.Invoke(selectedInteraction);
                Close();
            }
            GUI.enabled = true;

            if (Widgets.ButtonText(new Rect(buttonRow.width - 250f, buttonRow.y, 200f, buttonRow.height), "SP_Prostitute_Reject".Translate()))
            {
                onReject?.Invoke();
                Close();
            }
        }

        private static string BuildPartLabel(SexInteractionResolved resolved)
        {
            try
            {
                var parts = new List<string>();

                foreach (var p in resolved.InitiatorParts)
                    parts.Add(p.Label);
                foreach (var p in resolved.RecipientParts)
                    parts.Add(p.Label);

                parts.Add("SP_PartsMult".Translate(StripperPoleHelper.GetProstitutePartsMult(resolved.Interaction.Sextype).ToString()));
                return string.Join(", ", parts.Distinct().Take(3));
            }
            catch
            {
                return "";
            }
        }
    }
}