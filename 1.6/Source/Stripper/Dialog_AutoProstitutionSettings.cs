using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using rjw;
using static rjw.xxx;
using UnityEngine;
using Verse;

namespace Stripper
{
    public class Dialog_AutoProstitutionSettings : Window
    {
        private readonly Building_StripperPole pole;
        private Gender currentGender = Gender.Male;

        private static List<rjwSextype> cachedWhoringSexTypes = null;

        public override Vector2 InitialSize => new Vector2(560f, 415f);

        public Dialog_AutoProstitutionSettings(Building_StripperPole pole)
        {
            this.pole = pole;
            this.forcePause = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.doCloseButton = false;
            this.doCloseX = true;

            // リストの安全確保（3枠保証）
            EnsureSlots(pole.autoMaleInteractions);
            EnsureSlots(pole.autoFemaleInteractions);
        }

        private static void EnsureSlots(List<string> list)
        {
            if (list == null) return;
            while (list.Count < 3) list.Add(null);
        }

        public static bool TryResolveSextype(string input, out rjwSextype sextype)
        {
            sextype = rjwSextype.None;
            if (string.IsNullOrEmpty(input)) return false;

            if (Enum.TryParse<rjwSextype>(input, true, out sextype))
            {
                return sextype != rjwSextype.None;
            }

            return false;
        }

        public static List<rjwSextype> GetAvailableWhoringSexTypes()
        {
            if (cachedWhoringSexTypes != null) return cachedWhoringSexTypes;

            try
            {
                cachedWhoringSexTypes = SexUtility.SexInteractions
                    .Where(def =>
                    {
                        var ext = def.GetModExtension<SexInteractionExtension>();
                        if (ext == null || !ext.tags.Contains(SexInteractionTag.Whoring)) return false;
                        var sextype = ParseHelper.FromString<xxx.rjwSextype>(ext.rjwSextype);
                        return sextype != xxx.rjwSextype.None && sextype != xxx.rjwSextype.Masturbation;
                    })
                    .Select(def => ParseHelper.FromString<xxx.rjwSextype>(def.GetModExtension<SexInteractionExtension>().rjwSextype))
                    .Distinct()
                    .OrderBy(st => GetSexTypeLabel(st))
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Warning($"[StripperPole] Failed to get whoring sex types: {ex.Message}");
                cachedWhoringSexTypes = new List<rjwSextype>();
            }

            return cachedWhoringSexTypes;
        }

        public static string GetSexTypeLabel(rjwSextype sextype)
        {
            string key = "SP_SexType_" + sextype.ToString();
            if (key.CanTranslate())
            {
                return key.Translate();
            }
            return sextype.ToString();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 32f), "SP_AutoProstitutionSettings_Title".Translate());
            Text.Font = GameFont.Small;

            float y = 36f;
            string poleName = pole.LabelCap;
            string dancerName = pole.owners.Count > 0 ? string.Join(", ", pole.owners.Select(p => p.LabelShort)) : "SP_SharedPole".Translate().ToString();
            Widgets.Label(new Rect(0f, y, inRect.width, 24f), "SP_AutoProstitutionSettings_PoleInfo".Translate(poleName, dancerName));
            y += 28f;

            // 客の性別選択タブ
            float tabWidth = 150f;
            float tabHeight = 32f;
            Rect maleTabRect = new Rect(0f, y, tabWidth, tabHeight);
            Rect femaleTabRect = new Rect(tabWidth + 10f, y, tabWidth, tabHeight);

            bool isMale = currentGender == Gender.Male;
            bool isFemale = currentGender == Gender.Female;

            if (isMale) GUI.color = new Color(0.6f, 0.8f, 1f, 1f);
            if (Widgets.ButtonText(maleTabRect, "SP_CustomerGender_Male".Translate()))
            {
                currentGender = Gender.Male;
            }
            GUI.color = Color.white;

            if (isFemale) GUI.color = new Color(1f, 0.7f, 0.85f, 1f);
            if (Widgets.ButtonText(femaleTabRect, "SP_CustomerGender_Female".Translate()))
            {
                currentGender = Gender.Female;
            }
            GUI.color = Color.white;

            y += tabHeight + 8f;
            Widgets.DrawLineHorizontal(0f, y, inRect.width);
            y += 8f;

            // 優先度スロット描画
            List<string> currentPrefs = (currentGender == Gender.Male)
                ? pole.autoMaleInteractions
                : pole.autoFemaleInteractions;

            EnsureSlots(currentPrefs);

            float rowHeight = 36f;
            float spacing = 8f;

            for (int i = 0; i < 3; i++)
            {
                int slotIndex = i;
                Rect rowRect = new Rect(0f, y, inRect.width, rowHeight);
                Widgets.DrawAltRect(rowRect);

                // スロット名（メインサービス / 第2希望 / 第3希望）
                Rect labelRect = new Rect(rowRect.x + 8f, rowRect.y + 7f, 115f, 24f);
                string slotKey = $"SP_PrioritySlot_{slotIndex + 1}";
                string slotLabel = slotKey.CanTranslate() ? slotKey.Translate().ToString() : "SP_PrioritySlot".Translate(slotIndex + 1).ToString();
                Widgets.Label(labelRect, slotLabel);

                // 現在の行為名ボタン（クリックで変更）
                string currentRawValue = currentPrefs[slotIndex];
                bool hasValue = TryResolveSextype(currentRawValue, out var resolvedSextype);

                string buttonLabel = hasValue
                    ? GetSexTypeLabel(resolvedSextype)
                    : "SP_ActNone".Translate().ToString();

                Rect selectBtnRect = new Rect(rowRect.x + 128f, rowRect.y + 2f, inRect.width - 218f, rowHeight - 4f);

                if (!hasValue)
                {
                    GUI.color = Color.gray;
                }
                if (Widgets.ButtonText(selectBtnRect, buttonLabel))
                {
                    OpenSexTypeSelectionMenu(slotIndex, currentPrefs);
                }
                GUI.color = Color.white;

                // クリアボタン
                Rect clearBtnRect = new Rect(inRect.width - 80f, rowRect.y + 2f, 75f, rowHeight - 4f);
                if (Widgets.ButtonText(clearBtnRect, "SP_ClearSlot".Translate()))
                {
                    currentPrefs[slotIndex] = null;
                }

                y += rowHeight + spacing;
            }

            // ランダム接客チェックボックス
            Rect randomRect = new Rect(0f, y, inRect.width, 24f);
            Widgets.CheckboxLabeled(randomRect, "SP_AutoProstitution_RandomOrder".Translate(), ref pole.autoProstitutionRandom);
            y += 28f;

            // 説明テキスト
            y += 4f;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(0f, y, inRect.width, 40f), "SP_AutoProstitutionSettings_Hint".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // 閉じるボタン
            Rect closeRect = new Rect(inRect.width / 2f - 70f, inRect.height - 36f, 140f, 34f);
            if (Widgets.ButtonText(closeRect, "CloseButton".Translate()))
            {
                Close();
            }
        }

        private void OpenSexTypeSelectionMenu(int slotIndex, List<string> targetList)
        {
            var options = new List<FloatMenuOption>();

            // 未設定にする選択肢
            options.Add(new FloatMenuOption("SP_ActNone".Translate(), () =>
            {
                targetList[slotIndex] = null;
            }));

            var allWhoringTypes = GetAvailableWhoringSexTypes();
            foreach (var sextype in allWhoringTypes)
            {
                rjwSextype st = sextype;
                string label = GetSexTypeLabel(st);
                options.Add(new FloatMenuOption(label, () =>
                {
                    targetList[slotIndex] = st.ToString();
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
