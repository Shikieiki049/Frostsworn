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
    private static async Task CheckIdle0813()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        var character = ModelDb.Character<FrostswornCharacter>();
        var visuals = (NCreatureVisuals)typeof(FrostswornCharacter).GetMethod("TryCreateCreatureVisuals", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(character, null)!;
        tree.Root.AddChild(visuals);
        visuals.Position = new Vector2(260, 410);
        var body = visuals.GetNode<Sprite2D>("%Visuals");
        var idle = body.GetNode<Node2D>("IdleLoop");
        Assert(idle.GetChildCount() == 2, "combat initializes both native Godot animation meshes");
        idle.SetProcess(false);
        var mesh = idle.GetChild<Polygon2D>(0);
        var eyes = idle.GetChild<Polygon2D>(1);
        idle.Call("set_time", 0.0);
        var start = mesh.Polygon;
        idle.Call("set_time", 3.0);
        var middle = mesh.Polygon;
        float movement = 0, feet = 0;
        for (int i = 0; i < start.Length; i++)
        {
            movement = Math.Max(movement, start[i].DistanceTo(middle[i]));
            if (start[i].Y > -100) feet = Math.Max(feet, start[i].DistanceTo(middle[i]));
        }
        Assert(movement > 1 && feet < 0.001f, "approved first idle breathes while feet stay planted");
        idle.Call("set_time", 6.0);
        var end = mesh.Polygon;
        Assert(start[100].DistanceTo(end[100]) < 0.001f, "six-second idle wraps without a seam");
        idle.Call("set_time", 2.45);
        Assert(eyes.Visible, "blink overlay appears only during closed-eye interval");
        idle.Call("set_time", 2.6);
        Assert(!eyes.Visible, "original open eyes return after blink");
        foreach (var cue in new[] {"idle", "attack", "cast", "hit", "dead", "relaxed"})
        {
            Assert(ModCreatureVisualPlayback.TryPlayCue(visuals, character, cue), cue + " cue preserves compatible character playback");
            Assert(body.Texture.ResourcePath == FrostIdleVisuals.EmptyTexture && body.HasNode("IdleLoop"), "cue does not restore the old static body or import attack prototypes");
        }
        Assert(Math.Abs(body.Scale.X * idle.Scale.X - 0.275f) < 0.0001f && Math.Abs(body.Position.Y + idle.Position.Y * body.Scale.Y) < 0.001f, "character size and foot anchor match game scene");
        var merchant = ModWorldSceneVisualNodeFactory.TryInstantiateMerchantCharacter(character)!;
        Assert(merchant.GetNode<Sprite2D>("Visuals").HasNode("IdleLoop"), "ordinary and event shops receive idle at actual factory creation");
        ModCreatureVisualPlayback.TryPlayOnVisualRoot(merchant,character,"idle",true,character.WorldProceduralVisuals.Merchant.CueSet);
        var merchantBody = merchant.GetNode<Sprite2D>("Visuals");
        Assert(Math.Abs(merchantBody.Scale.X - 0.465f) < 0.0001f && Math.Abs(merchantBody.Position.Y + 525 * merchantBody.Scale.Y) < 0.001f, "shop character is 1.5 times combat size with unchanged floor anchor");
        // Inspect the exact rest-site factory output without entering its UI lifecycle.
        var player = MegaCrit.Sts2.Core.Entities.Players.Player.CreateForNewRun<FrostswornCharacter>(MegaCrit.Sts2.Core.Unlocks.UnlockState.all, 1);
        // This standalone harness cannot bind the game's scripted selection-reticle scene.
        var rest = new MegaCrit.Sts2.Core.Nodes.RestSite.NRestSiteCharacter();
        var controlRoot = new Control {Name="ControlRoot"}; rest.AddChild(controlRoot);
        var restBody = new Sprite2D {Name="Visuals"}; controlRoot.AddChild(restBody);
        typeof(FrostIdleVisuals).Assembly.GetType("Frostsworn.FrostIdleRestPatch")!.GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,new object[]{player,rest});
        Assert(restBody.HasNode("IdleLoop"), "rest-site factory postfix attaches idle to native rest-site hierarchy");
        Assert(ModCreatureVisualPlayback.TryPlayOnVisualRoot(rest,character,"relaxed",true,character.WorldProceduralVisuals.RestSite.CueSet), "rest-site relaxed cue preserves idle body");
        Assert(Math.Abs(restBody.Scale.X - 0.465f) < 0.0001f && Math.Abs(restBody.Position.Y + 525 * restBody.Scale.Y) < 0.001f, "rest-site character is 1.5 times combat size with unchanged floor anchor");
        ModCreatureVisualPlayback.TryPlayCue(visuals,character,"dead");
        Assert(!idle.IsProcessing(), "dead character stops breathing");
        ModCreatureVisualPlayback.TryPlayCue(visuals,character,"idle");
        Assert(idle.IsProcessing(), "idle resumes after revival or reset");
        merchant.Free(); rest.Free();
        if (DisplayServer.GetName() != "headless")
        {
            idle.Call("set_time", 3.0);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../outputs/idle-in-game.png"));
        }
        visuals.Free();
        GD.Print("IDLE0813_COMPLETE");
    }
}
