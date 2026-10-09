using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;
namespace Frostsworn;

public static class EclipseEventText
{
    // Match the meaning of the final sentence, independent of variable names/types,
    // page, event identity, or whether the author used a literal number.
    private static readonly Regex Health = new(
        @"(?<loss>失去|损失|扣除|受到|承受|Lose|Take|Suffer)\s*(?<amount>\d+(?:\.\d+)?)\s*(?:点)?\s*(?:生命值|生命|伤害|HP|[Hh]ealth|[Dd]amage)(?!值?上限)|(?<heal>恢复|回复|回復|回覆|Heal|Restore|Recover)\s*(?<amount>\d+(?:\.\d+)?)\s*(?:点)?\s*(?:生命值|生命|HP|[Hh]ealth)(?!值?上限)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string Adjust(string text, Player player)
    {
        if(Eclipse.Level(player.RunState)==0)return text;
        // BBCode can split a sentence around the number. Keep a map back to its
        // original characters so only digits change; colors and links stay intact.
        var plain=new StringBuilder();var positions=new List<int>();
        for(int i=0;i<text.Length;i++)
        {
            if(text[i]=='[' && text.IndexOf(']',i) is int end && end>=i) {i=end;continue;}
            plain.Append(text[i]);positions.Add(i);
        }
        StringBuilder? result=null;
        foreach(Match match in Health.Matches(plain.ToString()).Cast<Match>().Reverse())
        {
            var digits=match.Groups["amount"];
            if(!decimal.TryParse(digits.Value,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out decimal amount))continue;
            decimal adjusted=match.Groups["heal"].Success?Eclipse.Healing(player.Creature,amount):Eclipse.EventDamage(player.Creature,amount);
            if(adjusted==amount)continue;
            result ??= new StringBuilder(text);
            int start=positions[digits.Index];
            // Normal formatting has contiguous digits, but retain markup even if
            // a third-party event unusually wraps each digit separately.
            for(int i=digits.Length-1;i>=0;i--)result.Remove(positions[digits.Index+i],1);
            result.Insert(start,adjusted.ToString(CultureInfo.InvariantCulture));
        }
        return result?.ToString() ?? text;
    }
}

[HarmonyPatch(typeof(NEventOptionButton),nameof(NEventOptionButton._Ready))]
public static class EclipseEventLabelPatch
{
    public static void Postfix(NEventOptionButton __instance)
    {
        if(__instance.Event?.Owner is not {} player)return;
        var label=__instance.GetNode<RichTextLabel>("%Text");
        label.Text=EclipseEventText.Adjust(label.Text,player);
    }
}
