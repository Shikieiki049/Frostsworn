using System;
using System.Linq;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
namespace Frostsworn.Tests;
public static partial class Suite
{
    private static void CheckArt080()
    {
        var cards=ModelDb.AllCards.OfType<FrostCard>().ToArray();
        Assert(cards.Length==93,"93 cards have supplied artwork including previous strike and defend");
        Assert(DirAccess.GetFilesAt("res://Frostsworn/card_art").Count(f=>f.EndsWith(".png"))==91,"91 portraits from two named archives packed");
        var scene=ResourceLoader.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<Control>();
        var regular=scene.GetNode<TextureRect>("%Portrait");var ancient=scene.GetNode<TextureRect>("%AncientPortrait");
        var ratio=ancient.Size.X/ancient.Size.Y;
        Assert(Math.Abs(ratio-598f/842)<0.001f,"ancient crop ratio matches actual full-height native card portrait");
        foreach(var card in cards)
        {
            Assert(card.Portrait!=null && card.Portrait.GetWidth()>0,card.GetType().Name+" native portrait loads");
            if(card is FrostStrike or FrostDefend)continue;
            var atlas=(AtlasTexture)card.Portrait;
            var expected=card.Rarity==CardRarity.Ancient?ratio:250f/190;
            Assert(Math.Abs(atlas.Region.Size.X/atlas.Region.Size.Y-expected)<0.001f && atlas.Region.Position.X>=0 && atlas.Region.Position.Y>=0 && atlas.Region.End.X<=atlas.Atlas.GetWidth()+0.01f && atlas.Region.End.Y<=atlas.Atlas.GetHeight()+0.01f,card.GetType().Name+" crop fills matching native frame without stretching or overshooting source");
            var target=card.Rarity==CardRarity.Ancient?ancient:regular;
            FrostArtLayout.FitCardPortrait(target,card);
            Assert(target.StretchMode==TextureRect.StretchModeEnum.KeepAspectCovered,card.GetType().Name+" correct portrait control uses fill mode");
        }
        FrostArtLayout.FitCardPortrait(ancient,null);
        Assert(ancient.StretchMode==TextureRect.StretchModeEnum.Scale,"ancient portrait fit resets for pooled native cards");
        scene.Free();GD.Print("ART080_COMPLETE");
    }
}
