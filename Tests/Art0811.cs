using System;
using System.Linq;
using System.Text.Json;
using Godot;
using Frostsworn;

namespace Frostsworn.Tests;
public static partial class Suite
{
    private static async System.Threading.Tasks.Task CheckArt0811()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        var background = new ColorRect { Color = new Color("202830"), Size = new Vector2(1000,640) };
        tree.Root.AddChild(background);
        int slot = 0;
        var rows = JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://Frostsworn/buffs/mapping.json")).RootElement;
        Assert(rows.GetArrayLength() == 32, "32 named buff icons mapped in supplied row-major order");
        foreach (var row in rows.EnumerateArray())
        {
            string name = row.GetProperty("power").GetString()!;
            var type = typeof(FrostPowerBase).Assembly.GetType("Frostsworn." + name)!;
            var getter = typeof(MegaCrit.Sts2.Core.Models.ModelDb).GetMethods().Single(m => m.Name == "Power" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            var power = (FrostPowerBase)getter.MakeGenericMethod(type).Invoke(null, null)!;
            Assert(power.CustomIconPath == FrostPowerArt.PathFor(name) && power.CustomBigIconPath == power.CustomIconPath, name + " uses new art for both icon sizes");
            var texture = ResourceLoader.Load<AtlasTexture>(power.CustomIconPath);
            Assert(texture != null && texture.Atlas != null && texture.Region.End.X <= texture.Atlas.GetWidth() && texture.Region.End.Y <= texture.Atlas.GetHeight(), name + " atlas region within bounds");
            var image = texture!.GetImage();
            Assert(image.DetectAlpha() != Image.AlphaMode.None && image.GetPixel(0,0).A < 0.01f, name + " transparent background imported");
            var position = new Vector2(12 + slot % 8 * 124, 12 + slot / 8 * 156);
            background.AddChild(new TextureRect { Texture = texture, Position = position, Size = new Vector2(90,90), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered });
            background.AddChild(new TextureRect { Texture = texture, Position = position + new Vector2(88,66), Size = new Vector2(32,32), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered });
            var label = new Label { Text = row.GetProperty("name").GetString(), Position = position + new Vector2(0,102) };
            label.AddThemeFontSizeOverride("font_size", 13); background.AddChild(label);
            slot++;
        }
        Assert(FrostPowerArt.PathFor("EclipseStrengthPower").EndsWith(".svg"), "unsupplied helper status keeps its existing icon");
        if (DisplayServer.GetName() != "headless")
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../work/ui0811.png"));
        }
        GD.Print("REVISION0811_COMPLETE");
    }
}
