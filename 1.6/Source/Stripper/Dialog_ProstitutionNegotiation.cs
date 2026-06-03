using System;
using UnityEngine;
using Verse;

namespace Stripper
{
    public class Dialog_ProstitutionNegotiation : Window
    {
        private string text;
        private string title;
        private Pawn dancer;
        private Pawn customer;
        private Action onAccept;
        private Action onReject;

        // ダイアログの基本サイズを設定
        public override Vector2 InitialSize => new Vector2(600f, 300f);

        public Dialog_ProstitutionNegotiation(string title, string text, Pawn dancer, Pawn customer, Action onAccept, Action onReject)
        {
            this.title = title;
            this.text = text;
            this.dancer = dancer;
            this.customer = customer;
            this.onAccept = onAccept;
            this.onReject = onReject;

            this.forcePause = true; // ゲームを一時停止
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            // タイトルの描画
            Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), title);
            Text.Font = GameFont.Small;

            // 左 ダンサー
            Rect dancerRect = new Rect(10f, 45f, 80f, 80f);
            Widgets.ThingIcon(dancerRect, dancer);
            Widgets.Label(new Rect(10f, 130f, 80f, 20f), dancer.LabelShort);

            // 右側 顧客
            Rect customerRect = new Rect(inRect.width - 90f, 45f, 80f, 80f);
            Widgets.ThingIcon(customerRect, customer);
            Widgets.Label(new Rect(inRect.width - 90f, 130f, 80f, 20f), customer.LabelShort);

            // 中央 本文テキストの描画
            Rect textRect = new Rect(100f, 45f, inRect.width - 200f, 150f);
            Widgets.Label(textRect, text);

            // 画面下部のボタン配置用ベースRect
            Rect buttonRow = new Rect(0f, inRect.height - 45f, inRect.width, 35f);

            // はい
            if (Widgets.ButtonText(new Rect(buttonRow.x + 50f, buttonRow.y, 180f, buttonRow.height), "はい (Accept)"))
            {
                onAccept?.Invoke();
                Close();
            }

            // いいえ
            if (Widgets.ButtonText(new Rect(buttonRow.width - 230f, buttonRow.y, 180f, buttonRow.height), "いいえ (Reject)"))
            {
                onReject?.Invoke();
                Close();
            }
        }
    }
}