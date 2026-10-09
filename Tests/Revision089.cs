using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
namespace Frostsworn.Tests;
public static partial class Suite
{
    public static async Task Check089()
    {
        var tree=(SceneTree)Engine.GetMainLoop();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        typeof(Node).Assembly.GetType("Godot.Bridge.ScriptManagerBridge")!.GetMethod("LookupScriptsInAssembly",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(null,new object[]{typeof(NTinyCard).Assembly});
        var bg=new ColorRect {Color=new Color("121618"),Size=new Vector2(1000,640)};tree.Root.AddChild(bg);
        var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);var color=new Color("7797F7");
        Assert(player.Character.MapDrawingColor==color && ModelDb.CardPool<FrostCardPool>().DeckEntryCardColor==color,"map drawing and tiny card share frame color #7797F7");
        var drawings=new NMapDrawings();var scene=new PackedScene();var template=new Line2D {Width=8};Assert(scene.Pack(template)==Error.Ok,"native line scene prepared");template.Free();AccessTools.Field(typeof(NMapDrawings),"_lineDrawScene").SetValue(drawings,scene);
        var line=(Line2D)AccessTools.Method(typeof(NMapDrawings),"CreateLineForPlayer").Invoke(drawings,new object[]{player,false})!;
        Assert(line.DefaultColor==color,"native map drawing line receives character frame color");bg.AddChild(line);line.AddPoint(new Vector2(80,360));line.AddPoint(new Vector2(200,410));line.AddPoint(new Vector2(320,360));line.AddPoint(new Vector2(440,410));
        int index=0;
        foreach(var card in new CardModel[]{ModelDb.Card<FreezeRay>(),ModelDb.Card<CrystalRite>(),ModelDb.Card<ExpandColdStore>()})
        {
            var tiny=ResourceLoader.Load<PackedScene>("res://scenes/cards/tiny_card.tscn").Instantiate<NTinyCard>();bg.AddChild(tiny);tiny.SetCard(card);tiny.Position=new Vector2(100+index*260,110);tiny.Scale=new Vector2(4,4);
            var back=tiny.GetNode<TextureRect>("%CardBack");Assert(back.Modulate==Colors.White && back.Material is ShaderMaterial material && material.GetShaderParameter("tint").AsColor()==color,"tiny icon directly colors body without double modulation: "+card.Id);
            var portrait=tiny.GetNode<TextureRect>("%Portrait");Assert(portrait.Modulate==Colors.White,"tiny portrait keeps native contrast");index++;
        }
        var poolTiny=ResourceLoader.Load<PackedScene>("res://scenes/cards/tiny_card.tscn").Instantiate<NTinyCard>();bg.AddChild(poolTiny);poolTiny.Set(ModelDb.CardPool<FrostCardPool>(),CardType.Attack,CardRarity.Basic);Assert(poolTiny.GetNode<TextureRect>("%CardBack").Material is ShaderMaterial,"pool-based miniature uses same tint");poolTiny.QueueFree();
        if(DisplayServer.GetName()!="headless"){await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../work/ui089.png"));}
        drawings.Free();GD.Print("REVISION089_COMPLETE");
    }
}


