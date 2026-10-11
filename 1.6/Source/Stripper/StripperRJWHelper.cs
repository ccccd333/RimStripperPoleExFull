using RimWorld;
using rjw;
using System;
using System.Collections.Generic;
using Verse;

namespace Stripper
{
    public static class StripperRJWHelper
    {
        public enum BreastCategory
        {
            Small,     // 並未満（平坦・小胸）
            Average,   // 並み以上
            Large      // 巨乳以上
        }

        public enum PenisCategory
        {
            None,      // なし
            Average,   // 標準（並み）
            Large      // 巨根以上
        }

        /// <summary>
        /// ポーンのペニスサイズカテゴリを判定（RJWおよびSizedApparel標準準拠）
        /// </summary>
        public static PenisCategory GetPenisCategory(Pawn pawn)
        {
            if (pawn == null) return PenisCategory.None;

            try
            {
                // 1. RJW公式拡張メソッドで性器リストを取得
                List<Hediff> genitals = null;
                try
                {
                    genitals = pawn.GetGenitalsList();
                }
                catch { }

                // 2. キャッシュが空の場合、BPRから直接取得
                if (genitals == null || genitals.Count == 0)
                {
                    try
                    {
                        var partBPR = Genital_Helper.get_genitalsBPR(pawn);
                        if (partBPR != null)
                        {
                            genitals = Genital_Helper.get_PartsHediffList(pawn, partBPR);
                        }
                    }
                    catch { }
                }

                // 3. hediffSetからの走査フォールバック
                if (genitals == null || genitals.Count == 0)
                {
                    genitals = pawn.health?.hediffSet?.hediffs;
                }

                if (genitals == null || genitals.Count == 0)
                {
                    return pawn.gender == Gender.Male ? PenisCategory.Average : PenisCategory.None;
                }

                float maxSev = 0f;
                bool hasPenis = false;

                foreach (var h in genitals)
                {
                    if (h == null) continue;

                    // RJW公式の判定（Genital_Helper.is_penis）またはSizedApparel準拠のdefName判定
                    if (Genital_Helper.is_penis(h) || IsPenisDef(h.def))
                    {
                        hasPenis = true;
                        float sev = h.Severity;
                        if (h.TryGetComp<HediffComp_SexPart>(out var comp))
                        {
                            try
                            {
                                float compSev = comp.GetSeverity();
                                if (compSev > sev) sev = compSev;
                            }
                            catch { }
                        }
                        if (sev > maxSev) maxSev = sev;
                    }
                }

                if (!hasPenis)
                {
                    return PenisCategory.None;
                }

                // RJW基準: 0.60以上でLarge（巨根）、0.30以上でAverage
                PenisCategory cat = (maxSev >= 0.60f) ? PenisCategory.Large : PenisCategory.Average;

                if (StripperMod.settings.debugLog)
                {
                    Log.Message($"[StripperPole] GetPenisCategory for {pawn}: maxSev={maxSev}, cat={cat}");
                }
                return cat;
            }
            catch (Exception ex)
            {
                if (StripperMod.settings?.debugLog == true)
                {
                    Log.Warning($"[StripperPole] Error getting penis category: {ex.Message}");
                }
                return pawn.gender == Gender.Male ? PenisCategory.Average : PenisCategory.None;
            }
        }

        /// <summary>
        /// ポーンの胸サイズカテゴリを判定（RJWおよびSizedApparel標準準拠）
        /// </summary>
        public static BreastCategory GetBreastCategory(Pawn pawn)
        {
            if (pawn == null) return BreastCategory.Small;

            try
            {
                // 1. RJW公式拡張メソッドで胸リストを取得
                List<Hediff> breasts = null;
                try
                {
                    breasts = pawn.GetBreastList();
                }
                catch { }

                // 2. キャッシュが空の場合、BPRから直接取得
                if (breasts == null || breasts.Count == 0)
                {
                    try
                    {
                        var partBPR = Genital_Helper.get_breastsBPR(pawn);
                        if (partBPR != null)
                        {
                            breasts = Genital_Helper.get_PartsHediffList(pawn, partBPR);
                        }
                    }
                    catch { }
                }

                // 3. hediffSetからの走査フォールバック
                if (breasts == null || breasts.Count == 0)
                {
                    breasts = pawn.health?.hediffSet?.hediffs;
                }

                if (breasts == null || breasts.Count == 0)
                {
                    return pawn.gender == Gender.Female ? BreastCategory.Average : BreastCategory.Small;
                }

                float maxSev = 0f;
                bool foundBreasts = false;

                foreach (var b in breasts)
                {
                    if (b == null) continue;

                    // RJW公式FamilyまたはSizedApparel準拠のdefName判定
                    if (IsBreastHediff(b))
                    {
                        foundBreasts = true;
                        float sev = b.Severity;
                        if (b.TryGetComp<HediffComp_SexPart>(out var comp))
                        {
                            try
                            {
                                float compSev = comp.GetSeverity();
                                if (compSev > sev) sev = compSev;
                            }
                            catch { }
                        }
                        if (sev > maxSev) maxSev = sev;
                    }
                }

                if (!foundBreasts)
                {
                    return pawn.gender == Gender.Female ? BreastCategory.Average : BreastCategory.Small;
                }

                // RJW基準: Averageは0.40以上, Largeは0.60以上 (超巨大などは1.0超え)
                BreastCategory cat;
                if (maxSev >= 0.60f) cat = BreastCategory.Large;
                else if (maxSev >= 0.40f) cat = BreastCategory.Average;
                else cat = BreastCategory.Small;

                if (StripperMod.settings.debugLog)
                {
                    Log.Message($"[StripperPole] GetBreastCategory for {pawn}: maxSev={maxSev}, cat={cat}");
                }
                return cat;
            }
            catch (Exception ex)
            {
                if (StripperMod.settings?.debugLog == true)
                {
                    Log.Warning($"[StripperPole] Error getting breast category: {ex.Message}");
                }
                return pawn.gender == Gender.Female ? BreastCategory.Average : BreastCategory.Small;
            }
        }

        /// <summary>
        /// SizedApparel/RJWに準拠したペニス判定（内部Def名による安全な判定）
        /// </summary>
        private static bool IsPenisDef(HediffDef def)
        {
            if (def == null) return false;
            if (def is HediffDef_SexPart sp && (sp.genitalFamily == GenitalFamily.Penis || sp.genitalFamily == GenitalFamily.MaleOvipositor))
            {
                return true;
            }
            string defLower = def.defName.ToLowerInvariant();
            return defLower.Contains("penis") || defLower.Contains("dick") || defLower.Contains("tentacle") || defLower.Contains("ovipositorm");
        }

        /// <summary>
        /// SizedApparel/RJWに準拠した胸判定（内部Def名による安全な判定）
        /// </summary>
        private static bool IsBreastHediff(Hediff hediff)
        {
            if (hediff?.def == null) return false;
            if (hediff.def is HediffDef_SexPart sp && (sp.genitalFamily == GenitalFamily.Breasts || sp.genitalFamily == GenitalFamily.Udders))
            {
                return true;
            }
            return hediff.def.defName.IndexOf("breast", StringComparison.OrdinalIgnoreCase) >= 0;
        }


        /// <summary>
        /// 露出癖または好色（ヌーディスト、Nymphomaniacなど）を持つか判定
        /// </summary>
        public static bool HasExhibitionistTrait(Pawn pawn)
        {
            if (pawn?.story?.traits == null) return false;

            // バニラ ヌーディスト
            if (pawn.story.traits.HasTrait(TraitDefOf.Nudist)) return true;

            // RJW 好色（Nymphomaniac）
            var nymph = DefDatabase<TraitDef>.GetNamedSilentFail("Nymphomaniac");
            if (nymph != null && pawn.story.traits.HasTrait(nymph)) return true;

            return false;
        }
    }
}
