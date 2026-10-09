using System;
using System.Threading.Tasks;
using Frostsworn;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;

namespace Frostsworn.Tests;
public static partial class Suite
{
    static async Task CheckKeyword0816()
    {
        // Reproduce a warm Chinese cache, then switch language in the same process.
        foreach(string language in new[]{"zhs","eng","zhs","eng"})
        {
            LocManager.Instance.SetLanguage(language);
            foreach(var keyword in Enum.GetValues<CardKeyword>())
            {
                if(keyword.ToString()=="None")continue;
                var tip=(HoverTip)HoverTipFactory.FromKeyword(keyword);
                var key=keyword.ToString().ToUpperInvariant();
                Assert(tip.Title==new LocString("card_keywords",key+".title").GetFormattedText() && tip.Description==new LocString("card_keywords",key+".description").GetFormattedText(),language+" "+keyword+" hover tip matches current native language");
                var repeated=(HoverTip)HoverTipFactory.FromKeyword(keyword);
                Assert(repeated.Title==tip.Title && repeated.Description==tip.Description,"repeated keyword lookup stays in "+language);
            }
        }
        await CheckEnglish0815();
    }
}
