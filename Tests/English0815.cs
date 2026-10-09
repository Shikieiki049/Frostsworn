using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static async Task CheckEnglish0815()
    {
        LocManager.Instance.SetLanguage("eng");
        var tables=(Dictionary<string,LocTable>)typeof(LocManager).GetField("_tables",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(LocManager.Instance)!;
        foreach(var name in new[]{"cards","powers","relics","characters","epochs","static_hover_tips","enchantments","ancients","potions"})
        {
            var entries=JsonSerializer.Deserialize<Dictionary<string,string>>(Godot.FileAccess.GetFileAsString($"res://Frostsworn/localization/eng/{name}.json"))!;
            Assert(entries.Values.All(s=>!Regex.IsMatch(s,@"[\u3400-\u9fff]")),name+" contains only English text");
            tables[name]=new LocTable(name,entries,tables.GetValueOrDefault(name));
        }
        var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
        var run=RunState.CreateForTest([player]);player.ResetCombatState();
        var combat=new CombatState(runState:run);combat.AddPlayer(player);CombatManager.Instance.SetUpCombat(combat);
        int count=0;
        foreach(var model in ModelDb.AllCards.OfType<FrostCard>())
        {
            var card=combat.CreateCard(model,player);
            foreach(bool upgrade in new[]{false,true})
            {
                if(upgrade && card.MaxUpgradeLevel>0)card.UpgradeInternal();
                var text=card.GetDescriptionForPile(PileType.None);
                Assert(!text.Contains('{')&&!Regex.IsMatch(text,@"[\u3400-\u9fff]"),card.GetType().Name+" formats "+(upgrade?"upgraded":"base")+" English variables");
                _=card.HoverTips.ToArray();
            }
            count++;
        }
        Assert(count==93,"all 93 cards formatted through native CardModel");
        foreach(var relic in ModelDb.AllRelics.Where(FrostRelicFlashLayout.IsOurs))
            Assert(!relic.DynamicDescription.GetFormattedText().Contains('{'),relic.Id+" formats English relic description");
        Assert(!FrostDisplay.Description(player.Creature).Contains('{') && !Regex.IsMatch(FrostDisplay.ColdDescription(player),@"[\u3400-\u9fff]"),"live Frost and Cold Storage status are English");
        Assert(FrostText.Contains("Gain [gold]Strength[/gold].","力量") && FrostText.Contains("Put into Cold Storage.","冷藏") && !FrostText.Contains("Gain Self-Frost.","寒霜"),"English keyword discovery keeps native/custom tooltips and distinguishes Self-Frost");
        Assert(Eclipse.Description(8).Split('\n').Length==9 && !Eclipse.Description(8).Contains("Eclipse1") && Eclipse.Description(8).StartsWith('"'),"Eclipse cumulative rules and quoted line are localized");
        Assert(FrostText.EclipseTitle(5)=="Eclipse 5" && ModelDb.CardPool<FrostCardPool>().Title=="Frostsworn","runtime difficulty and card pool names are English");
        Assert(new LocString("characters",player.Character.Id.Entry+".banter.dead.endTurnPing").GetFormattedText()=="The rest is up to you.","multiplayer Ping uses English dialogue");
        var tree=(SceneTree)Engine.GetMainLoop();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        if(DisplayServer.GetName()!="headless")
        {
            var canvas=new ColorRect {Color=new Color("17202d"),Size=new Vector2(1152,648)};tree.Root.AddChild(canvas);
            var label=new RichTextLabel {Text="Frostsworn — English Localization\n\n"+Eclipse.Description(8)+"\n\n"+FrostDisplay.ColdDescription(player),Size=new Vector2(1050,600),Position=new Vector2(40,25),BbcodeEnabled=true};
            label.AddThemeFontSizeOverride("normal_font_size",22);canvas.AddChild(label);
            await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../outputs/english-0.8.15.png"));canvas.Free();
        }
    }
}
