using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Nodes.Events;
namespace Frostsworn.Tests;
public static partial class Suite
{
    public static async Task Check088()
    {
        var tree=(SceneTree)Engine.GetMainLoop();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);var run=RunState.CreateForTest([player]);player.Creature.SetCurrentHpInternal(30);
        Eclipse.Data.Set(run,new EclipseRunData {Level=6});
        typeof(Node).Assembly.GetType("Godot.Bridge.ScriptManagerBridge")!.GetMethod("LookupScriptsInAssembly",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(null,new object[]{typeof(NEventOptionButton).Assembly});
        var bg=new ColorRect {Color=new Color("202A38"),Size=new Vector2(1000,640)};tree.Root.AddChild(bg);
        run.PushRoom(new EventRoom(ModelDb.Event<SpiritGrafter>()));
        foreach(var (input,expected) in new[]{
            ("失去[color=red]6[/color]点生命。", "失去[color=red]9[/color]点生命。"),
            ("失去15点生命，然后随机升级2张牌。", "失去23点生命，然后随机升级2张牌。"),
            ("获得106金币。失去7点生命值。", "获得106金币。失去11点生命值。"),
            ("恢复20点生命。失去3点最大生命。", "恢复13点生命。失去3点最大生命。"),
            ("失去20点最大生命。最大生命值提升20。", "失去20点最大生命。最大生命值提升20。"),
            ("失去20点生命值上限。失去3点生命上限。", "失去20点生命值上限。失去3点生命上限。"),
            ("回复1点生命。", "回复1点生命。"),
            ("Take [color=red]7[/color] damage. Lose 20 Max HP. Heal 20 HP.", "Take [color=red]11[/color] damage. Lose 20 Max HP. Heal 13 HP."),
            ("失去17点生命，恢复26点生命，获得135金币。", "失去26点生命，恢复17点生命，获得135金币。")
        })Assert(EclipseEventText.Adjust(input,player)==expected,"generic health sentence including BBCode and unrelated values: "+expected);
        var table=LocManager.Instance.GetTable("events");int count=0,changed=0;int visible=0;
        foreach(var canonical in ModelDb.AllEvents.Where(e=>e is not AncientEventModel))
        {
            var model=(EventModel)canonical.ToMutable();typeof(EventModel).GetProperty("Owner")!.SetValue(model,player);
            foreach(string key in table.Keys.Where(k=>k.StartsWith(model.Id.Entry+".") && k.Contains(".options.") && k.EndsWith(".description")))
            {
                var loc=new LocString("events",key);model.DynamicVars.AddTo(loc);player.Character.AddDetailsTo(loc);loc.Add("IsMultiplayer",false);
                string original;
                try {original=loc.GetFormattedText();}catch(Exception error) when(error.GetType().Name=="FormattingException"){continue;}
                string adjusted=EclipseEventText.Adjust(original,player);
                count++;if(adjusted!=original)changed++;
                // Test real native button construction for every page of reported events,
                // including literal damage and untyped DynamicVar values.
                if(model is not (SlipperyBridge or StoneOfAllTime or HungryForMushrooms or ColossalFlower or SunkenStatue or TabletOfTruth or SpiritGrafter or DrowningBeacon))continue;
                if(model is SlipperyBridge)model.DynamicVars["HpLoss"].BaseValue=6;
                var option=new EventOption(model,null,key[..^12]);
                var button=NEventOptionButton.Create(model,option,0);bg.AddChild(button);
                var actual=button.GetNode<RichTextLabel>("%Text").Text;
                model.DynamicVars.AddTo(option.Description);
                string raw=option.Description.GetFormattedText();
                Assert(actual.EndsWith(EclipseEventText.Adjust(raw,player)),"native event button health preview: "+key+" => "+actual);
                if(key.EndsWith(".SMASH.description") && model is TabletOfTruth)Assert(actual.Contains("13"),"Tablet smash heals thirteen");
                if(model is DrowningBeacon)Assert(EclipseEventText.Adjust(raw,player)==raw,"Drowning beacon maximum HP cost remains unchanged");
                if(model is ColossalFlower && key.Contains("POLLINOUS_CORE"))Assert(actual.Contains("11"),"Flower core costs eleven");
                if(key.Contains("HOLD_ON") && model is SlipperyBridge)Assert(actual.Contains("9"),"Bridge later pages cost nine at base six");
                var second=NEventOptionButton.Create(model,option,0);bg.AddChild(second);Assert(second.GetNode<RichTextLabel>("%Text").Text==actual,"rebuilding button does not compound the adjustment");second.QueueFree();
                if(visible<5 && adjusted!=original){button.Position=new Vector2(10,10+visible*115);button.Scale=new Vector2(0.65f,0.65f);visible++;}else button.QueueFree();
            }
        }
        var mushrooms=(HungryForMushrooms)ModelDb.Event<HungryForMushrooms>().ToMutable();typeof(EventModel).GetProperty("Owner")!.SetValue(mushrooms,player);
        var mushroomOptions=(System.Collections.Generic.IReadOnlyList<EventOption>)AccessTools.Method(typeof(HungryForMushrooms),"GenerateInitialOptions").Invoke(mushrooms,null)!;
        foreach(var option in mushroomOptions){var button=NEventOptionButton.Create(mushrooms,option,0);bg.AddChild(button);string actual=button.GetNode<RichTextLabel>("%Text").Text;GD.Print("MUSHROOM088="+actual);Assert(actual.Contains(option==mushroomOptions[1]?"23":"20"),"native mushroom relic description preserves maximum HP and adjusts fifteen to twenty-three");button.QueueFree();}
        Assert(count>150 && changed>20,"all ordinary native event option pages scanned: "+count+", health previews adjusted: "+changed);
        Eclipse.Data.Modify(run,d=>d.Level=0);Assert(EclipseEventText.Adjust("失去7点生命。恢复20点生命。",player)=="失去7点生命。恢复20点生命。","normal difficulty retains native text");
        if(DisplayServer.GetName()!="headless"){await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../work/ui088.png"));}
        GD.Print("REVISION088_COMPLETE");
    }
}


