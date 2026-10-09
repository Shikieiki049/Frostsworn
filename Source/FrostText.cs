using System.Text.RegularExpressions;

namespace Frostsworn;

public static class FrostText
{
    public static string Get(string key) => new LocString("static_hover_tips", key).GetFormattedText();
    public static string Format(string key, params (string Key, object Value)[] values)
    {
        var text = new LocString("static_hover_tips", key);
        foreach (var (name, value) in values)
            if (value is int number) text.Add(name, number);
            else text.Add(name, (string)value);
        return text.GetFormattedText();
    }
    public static string EclipseTitle(int level)
    {
        var text = new LocString("static_hover_tips", "FROSTSWORN_ECLIPSE.title");
        text.Add("Level", level);
        return text.GetFormattedText();
    }
    // Tooltip discovery must work in both languages, without matching Frost
    // inside Self-Frost or names such as Frostbite.
    public static bool Contains(string text, string chinese)
    {
        if (text.Contains(chinese, StringComparison.Ordinal)) return true;
        string english = chinese switch
        {
            "力量" => "Strength", "敏捷" => "Dexterity", "易伤" => "Vulnerable", "虚弱" => "Weak",
            "消耗" => "Exhaust", "寒霜" => "Frost", "碎冰" => "Shatter", "冰甲" => "Ice Armor",
            "自霜" => "Self-Frost", "雪势" => "Snowfall", "冷藏" => "Cold Storage", "解冻" => "Thaw",
            "冰晶" => "Ice Crystal", "冰封" => "Freeze", _ => chinese
        };
        return Regex.IsMatch(text, @"(?<![A-Za-z-])" + Regex.Escape(english) + @"(?:s|ed)?(?![A-Za-z-])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
