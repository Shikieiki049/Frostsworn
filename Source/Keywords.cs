using MegaCrit.Sts2.Core.HoverTips;

namespace Frostsworn;

internal static class FrostKeywords
{
    public static IHoverTip Crystal(bool upgraded=false,bool sharp=false)
    {
        // Use the same native card-hover factory as BladeDance/Accuracy.
        if(!sharp)return HoverTipFactory.FromCard<IceCrystal>(upgraded);
        var crystal=ModelDb.Card<IceCrystal>().ToMutable();
        CardCmd.Enchant<SharpEnchantment>(crystal,1);
        return HoverTipFactory.FromCard(crystal,upgraded);
    }
    public static HoverTip Shatter => new(new LocString("static_hover_tips", "FROSTSWORN_SHATTER.title"),
        new LocString("static_hover_tips", "FROSTSWORN_SHATTER.description"), null);
}
