using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;

namespace Frostsworn.Tests;
public static partial class Suite
{
    private static async Task CheckVisual0814()
    {
        await CheckIdle0813();
        var tree = (SceneTree)Engine.GetMainLoop();
        var canvas = new Node2D(); tree.Root.AddChild(canvas);
        int index = 0;
        foreach (var relic in ModelDb.AllRelics.Where(FrostRelicFlashLayout.IsOurs))
        {
            Assert(relic.Icon.GetSize() == new Vector2(64,64), relic.Id + " inventory and GPU flash texture is physically 64px");
            Assert(relic.BigIcon.GetWidth() > 500, relic.Id + " detail view retains original high resolution");
            Assert(FrostRelicFlashLayout.IsOurs(relic.Icon), relic.Id + " normalized icon matches existing UI flash limit");
            var flash = ResourceLoader.Load<PackedScene>("res://scenes/vfx/relic_inventory_flash_vfx.tscn").Instantiate<Node2D>();
            var particles = flash.GetNode<GpuParticles2D>("Particles");
            particles.Texture = relic.Icon; // The same assignment made by native inventory DoFlash.
            flash.Position = new Vector2(65 + index++ * 120, 65);
            canvas.AddChild(flash);
            Assert(particles.Texture.GetWidth() == 64 && particles.Texture.GetHeight() == 64, "actual native GPU particle no longer receives thousand-pixel art");
        }
        Assert(index == 9, "all nine frost relics use native-sized inventory particle art");
        foreach (int x in new[] {300,850})
        {
            var idle = ResourceLoader.Load<PackedScene>("res://Frostsworn/idle/player.tscn").Instantiate<Node2D>();
            idle.Scale = Vector2.One * 0.4125f;
            idle.Position = new Vector2(x, 620);
            canvas.AddChild(idle);
        }
        if (DisplayServer.GetName() != "headless")
        {
            await tree.ToSignal(tree.CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../outputs/visual-0.8.14.png"));
        }
        canvas.Free();
        GD.Print("VISUAL0814_COMPLETE");
    }
}
