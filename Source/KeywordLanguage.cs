using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;

namespace Frostsworn;

// Native HoverTip stores formatted strings, while the factory caches them by
// keyword only. Keep native terminology, but never reuse another language's text.
[HarmonyPatch(typeof(HoverTipFactory), nameof(HoverTipFactory.FromKeyword))]
internal static class KeywordLanguagePatch
{
    private static string? cachedLanguage;
    private static void Prefix(Dictionary<CardKeyword, HoverTip> ____keywordHoverTips)
    {
        string language = LocManager.Instance.Language;
        if (cachedLanguage == language) return;
        ____keywordHoverTips.Clear();
        cachedLanguage = language;
    }
}
