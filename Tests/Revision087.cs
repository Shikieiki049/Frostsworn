using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Nodes.Events;
namespace Frostsworn.Tests;
public static partial class Suite
{
    public static async Task Check087()
    {
        var tree=(SceneTree)Engine.GetMainLoop();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);var run=RunState.CreateForTest([player]);player.Creature.SetCurrentHpInternal(30);
        Eclipse.Data.Set(run,new EclipseRunData {Level=6});
        var rest=new HealRestSiteOption(player);var restText=rest.Description.GetFormattedText();
        Assert(restText.Contains("14") && !restText.Contains("21") && !restText.Contains("30%"),"actual rest sentence shows fourteen without misleading percentage: "+restText);
        Eclipse.Data.Modify(run,d=>d.Level=0);Assert(rest.Description.GetFormattedText().Contains("21"),"normal rest keeps native twenty-one");Eclipse.Data.Modify(run,d=>d.Level=6);
        var evt=(SpiritGrafter)ModelDb.Event<SpiritGrafter>().ToMutable();typeof(EventModel).GetProperty("Owner")!.SetValue(evt,player);
        run.PushRoom(new EventRoom(ModelDb.Event<SpiritGrafter>()));
        var options=(IReadOnlyList<EventOption>)AccessTools.Method(typeof(SpiritGrafter),"GenerateInitialOptions").Invoke(evt,null)!;
        foreach(var option in options)evt.DynamicVars.AddTo(option.Description);
        string heal=options[0].Description.GetFormattedText(),loss=options[1].Description.GetFormattedText();
        Assert(EclipseEventText.Adjust(heal,player).Contains("16") && heal.Contains("25"),"real SpiritGrafter healing sentence shows sixteen: "+heal);
        Assert(EclipseEventText.Adjust(loss,player).Contains("15") && loss.Contains("10"),"real SpiritGrafter damage sentence shows fifteen: "+loss);
        Assert(options[0].Description.GetFormattedText()==heal && evt.DynamicVars["LetItInHealAmount"].BaseValue==25 && evt.DynamicVars["RejectionHpLoss"].BaseValue==10,"repeated UI formatting does not change actual event mechanics");
        typeof(Node).Assembly.GetType("Godot.Bridge.ScriptManagerBridge")!.GetMethod("LookupScriptsInAssembly",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(null,new object[]{typeof(NEventOptionButton).Assembly});
        var bg=new ColorRect {Color=new Color("202A38"),Size=new Vector2(1000,640)};tree.Root.AddChild(bg);
        var button=NEventOptionButton.Create(evt,options[1],1);bg.AddChild(button);button.Position=new Vector2(50,200);button.Scale=new Vector2(0.7f,0.7f);
        Assert(button.GetNode<RichTextLabel>("%Text").Text.Contains("15") && !button.GetNode<RichTextLabel>("%Text").Text.Contains("10"),"native event button renders fifteen in the actual UI label");
        var restLabel=new RichTextLabel {Text=rest.Description.GetFormattedText(),Position=new Vector2(50,100),Size=new Vector2(900,80),BbcodeEnabled=true};restLabel.AddThemeFontSizeOverride("normal_font_size",28);bg.AddChild(restLabel);
        run.PopCurrentRoom();
        var ownerField=AccessTools.Field(typeof(DynamicVar),"_owner");int events=0,formatted=0,vars=0;var seen=new HashSet<string>();
        foreach(var canonical in ModelDb.AllEvents)
        {
            var model=(EventModel)canonical.ToMutable();typeof(EventModel).GetProperty("Owner")!.SetValue(model,player);
            var affected=model.DynamicVars.Where(p=>p.Value is HealVar or HpLossVar or DamageVar).ToArray();
            if(affected.Length==0)continue;events++;vars+=affected.Length;
            run.PushRoom(new EventRoom(canonical));
            var table=LocManager.Instance.GetTable("events");
            foreach(string key in table.Keys.Where(k=>k.StartsWith(model.Id.Entry+".") && k.EndsWith(".description")))
            {
                var loc=new LocString("events",key);model.DynamicVars.AddTo(loc);player.Character.AddDetailsTo(loc);loc.Add("IsMultiplayer",false);
                string raw=loc.GetRawText();if(!affected.Any(p=>raw.Contains("{"+p.Key)))continue;
                var expected=new Dictionary<string,object>(loc.Variables);
                // Render the game's real template through the same final formatter the UI calls.
                string wanted=LocManager.Instance.SmartFormat(loc,expected),actual=LocManager.Instance.SmartFormat(loc,new Dictionary<string,object>(loc.Variables));
                Assert(actual==wanted,"native event final formatted description: "+key);formatted++;seen.Add(model.Id.Entry);
            }
            foreach(var pair in affected)Assert(ownerField.GetValue(pair.Value)==model,"original event variable retains its model owner: "+model.Id.Entry+"/"+pair.Key);
            run.PopCurrentRoom();
        }
        Assert(events>10 && formatted>20,"all native event health variable scan: "+events+" events, "+vars+" variables, "+formatted+" formatted descriptions");
        var flashScene=ResourceLoader.Load<PackedScene>("res://scenes/vfx/relic_flash_vfx.tscn");
        var uiScene=ResourceLoader.Load<PackedScene>("res://scenes/vfx/ui_flash_vfx.tscn");
        int flashCount=0;
        foreach(var relic in ModelDb.AllRelics.Where(r=>FrostRelicFlashLayout.IsOurs(r)))
        {
            var flash=flashScene.Instantiate<MegaCrit.Sts2.Core.Nodes.Vfx.NRelicFlashVfx>();AccessTools.Field(flash.GetType(),"_relic").SetValue(flash,relic);bg.AddChild(flash);flash.Position=new Vector2(60+flashCount*90,440);
            foreach(string name in new[]{"Image1","Image2","Image3"})
            {
                var image=flash.GetNode<TextureRect>(name);GD.Print("FLASH087_SIZE="+relic.Id+"/"+name+":"+image.Size+":"+image.ExpandMode);Assert(image.Size==new Vector2(64,64) && image.ExpandMode==TextureRect.ExpandModeEnum.IgnoreSize,"actual native relic activation flash is 64 pixels: "+relic.Id+"/"+name);
            }
            var ui=uiScene.Instantiate<MegaCrit.Sts2.Core.Nodes.Vfx.NUiFlashVfx>();AccessTools.Field(ui.GetType(),"_texture").SetValue(ui,relic.Icon);bg.AddChild(ui);ui.Position=new Vector2(60+flashCount*90,540);
            Assert(ui.GetNode<TextureRect>("TextureRect").Size==new Vector2(64,64),"actual relic bar flash is 64 pixels: "+relic.Id);flashCount++;
        }
        Assert(flashCount==9,"all nine character relics use native-sized activation flashes");
        GD.Print("EVENT087_COVERAGE="+events+","+vars+","+formatted);
        if(DisplayServer.GetName()!="headless") {await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../work/ui087.png"));}
        GD.Print("REVISION087_COMPLETE");
    }
}

