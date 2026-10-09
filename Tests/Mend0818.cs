using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static Task CheckMend0818()
    {
        var owner=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
        var target=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,2);
        var run=RunState.CreateForTest([owner,target]);
        foreach(string language in new[]{"zhs","eng"})
        {
            LocManager.Instance.SetLanguage(language);
            var tables=(Dictionary<string,LocTable>)typeof(LocManager).GetField("_tables",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(LocManager.Instance)!;
            var entries=JsonSerializer.Deserialize<Dictionary<string,string>>(Godot.FileAccess.GetFileAsString($"res://Frostsworn/localization/{language}/static_hover_tips.json"))!;
            tables["static_hover_tips"]=new LocTable("static_hover_tips",entries,tables.GetValueOrDefault("static_hover_tips"));
            foreach(int level in new[]{0,4,5,8})
            {
                Eclipse.Data.Set(run,new EclipseRunData {Level=level,GoldApplied=true});
                var mend=new MendRestSiteOption(owner);
                string text=mend.Description.GetFormattedText();
                Assert(text.Contains(level>=5?"20%":"30%"),language+" Mend percentage at Eclipse "+level);
                var cached=mend.Description;
                cached.Add("HasTarget",true);cached.Add("Name","Teammate");
                var heal=(HealVar)typeof(MendRestSiteOption).GetField("_healVar",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(mend)!;
                heal.PreviewValue=MendRestSiteOption.GetHealAmount(target);
                decimal original=heal.PreviewValue;
                text=mend.Description.GetFormattedText();
                Assert(ReferenceEquals(cached,mend.Description) && text.Contains("Teammate"),"cached target state survives preview refresh");
                Assert(text.Contains(((int)Eclipse.Healing(target.Creature,original)).ToString()),"Mend target preview matches actual healing formula: "+text);
                Assert(heal.PreviewValue==original,"Mend preview does not mutate gameplay healing variable");
                cached.Add("HasTarget",false);
                Assert(!mend.Description.GetFormattedText().Contains("Teammate"),"Mend target clears normally");
                if(level>=5)Assert(Eclipse.Healing(target.Creature,1)==1,"Mend minimum positive heal revives teammate");
            }
        }
        return Task.CompletedTask;
    }
}
