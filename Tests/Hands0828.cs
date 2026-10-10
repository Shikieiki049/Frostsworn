using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static async Task CheckHands0828()
    {
        var tree=(SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        var character=ModelDb.Character<FrostswornCharacter>();
        var native=ModelDb.Character<MegaCrit.Sts2.Core.Models.Characters.Ironclad>();
        var hands=new[]{character.ArmPointingTexture,character.ArmRockTexture,character.ArmPaperTexture,character.ArmScissorsTexture};
        var originals=new[]{native.ArmPointingTexture,native.ArmRockTexture,native.ArmPaperTexture,native.ArmScissorsTexture};
        var stage=new Node2D();tree.Root.AddChild(stage);
        for(int i=0;i<hands.Length;i++)
        {
            Assert(hands[i].ResourcePath.StartsWith("res://Frostsworn/multiplayer/"),"native hand getter resolves custom pose "+i);
            Assert(hands[i].GetSize()==originals[i].GetSize() && hands[i].GetSize()==new Vector2(422,1200),"pose "+i+" matches original texture canvas");
            Assert(!hands[i].GetImage().IsInvisible(),"pose "+i+" contains visible artwork");
            stage.AddChild(new Sprite2D {Texture=originals[i],Centered=false,Position=new Vector2(35+i*270,30),Scale=Vector2.One*0.26f});
            stage.AddChild(new Sprite2D {Texture=hands[i],Centered=false,Position=new Vector2(155+i*270,30),Scale=Vector2.One*0.26f});
        }
        if(DisplayServer.GetName()!="headless")
        {
            await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../outputs/hands-0828.png"));
        }
        stage.QueueFree();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        var visuals=(NCreatureVisuals)typeof(FrostswornCharacter).GetMethod("TryCreateCreatureVisuals",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(character,null)!;
        tree.Root.AddChild(visuals);visuals.Position=new Vector2(350,510);
        var body=visuals.GetNode<Sprite2D>("%Visuals");var idle=body.GetNode<Node2D>("IdleLoop");
        foreach(var cue in new[]{"dead","die","death"})
        {
            Assert(ModCreatureVisualPlayback.TryPlayCue(visuals,character,cue),"death cue supported: "+cue);
            var portrait=body.GetNode<Sprite2D>("DeathPortrait");
            Assert(portrait.Visible && !idle.Visible && !idle.IsProcessing(),"death replaces breathing rig with static portrait: "+cue);
            Assert(Math.Abs(body.Position.Y+body.Scale.Y*(portrait.Position.Y+FrostIdleVisuals.DeathGroundY*portrait.Scale.Y))<0.001f,"death portrait remains on floor");
        }
        Assert(body.GetChildCount()==2,"repeated death cues do not duplicate portraits");
        if(DisplayServer.GetName()!="headless")
        {
            await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../outputs/death-0828.png"));
        }
        ModCreatureVisualPlayback.TryPlayCue(visuals,character,"idle");
        Assert(idle.Visible && idle.IsProcessing() && !body.GetNode<Sprite2D>("DeathPortrait").Visible,"revival restores approved idle and hides corpse");
        var shop=ModWorldSceneVisualNodeFactory.TryInstantiateMerchantCharacter(character)!;
        Assert(ModCreatureVisualPlayback.TryPlayOnVisualRoot(shop,character,"dead",true,character.WorldProceduralVisuals.Merchant!.CueSet),"world death uses existing native playback");
        var shopBody=shop.GetNode<Sprite2D>("Visuals");
        Assert(shopBody.GetNode<Sprite2D>("DeathPortrait").Visible && !shopBody.GetNode<Node2D>("IdleLoop").Visible,"shop uses static death portrait");
        ModCreatureVisualPlayback.TryPlayOnVisualRoot(shop,character,"idle",true,character.WorldProceduralVisuals.Merchant!.CueSet);
        Assert(shopBody.GetNode<Node2D>("IdleLoop").Visible && !shopBody.GetNode<Sprite2D>("DeathPortrait").Visible,"world idle restores live appearance");
        visuals.Free();shop.Free();
    }
}
