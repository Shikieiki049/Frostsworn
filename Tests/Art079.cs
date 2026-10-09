using System;
using System.Linq;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;

namespace Frostsworn.Tests;
public static partial class Suite
{
    private static void CheckArt079()
    {
        var files=DirAccess.GetFilesAt("res://Frostsworn/card_art").Where(p=>p.EndsWith(".png")).ToArray();
        Assert(files.Length==25,"25 supplied portraits included in packed mod");
        var scene=ResourceLoader.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<Control>();
        var portrait=scene.GetNode<TextureRect>("%Portrait");
        foreach(var file in files)
        {
            var card=ModelDb.AllCards.OfType<FrostCard>().Single(c=>c.GetType().Name==file[..^4]);
            Assert(card.CustomPortraitPath=="res://Frostsworn/card_art/"+file.Replace(".png",".tres") && card.Portrait.GetWidth()>1000,card.GetType().Name+" resolves supplied art through native portrait getter");
            Assert(card.Portrait is AtlasTexture atlas && Math.Abs(atlas.Region.Size.X/atlas.Region.Size.Y-250f/190f)<0.001f,card.GetType().Name+" centered crop matches native portrait aspect ratio");
            FrostArtLayout.FitCardPortrait(portrait,card);
            Assert(portrait.StretchMode==TextureRect.StretchModeEnum.KeepAspectCovered,card.GetType().Name+" fills native card portrait box");
        }
        FrostArtLayout.FitCardPortrait(portrait,ModelDb.Card<FrostStrike>());
        Assert(portrait.StretchMode==TextureRect.StretchModeEnum.KeepAspectCentered,"reused card restores original fit for existing portraits");
        Assert(ModelDb.Card<FrostStrike>().CustomPortraitPath.EndsWith("art075/FrostStrike.png") && ModelDb.Card<FrostDefend>().CustomPortraitPath.EndsWith("art075/FrostDefend.png"),"existing strike and defend art retained");
        scene.Free();GD.Print("ART079_COMPLETE");
    }
}
