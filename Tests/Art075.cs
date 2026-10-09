using System;
using System.Reflection;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Godot;

namespace Frostsworn.Tests;
public static partial class Suite
{
    private static void CheckArt075()
    {
        const string art = "res://Frostsworn/art075/";
        foreach (var name in new[]{"battle","map","portrait","energy","sharp","potions","relics"})
        {
            var texture=ResourceLoader.Load<CompressedTexture2D>(art+name+".png");
            Assert(texture != null && texture.GetWidth()>0 && texture.GetImage().DetectAlpha()!=Image.AlphaMode.None, name+" imported with transparent alpha");
        }
        foreach (var name in new[]{"WinterCore","WinterCrown","FrostBottle","LiquidNitrogen","FractalSnowflake"})
        {
            var texture=ResourceLoader.Load<AtlasTexture>(art+name+".tres");
            Assert(texture.Atlas!=null && texture.Region.End.X<=texture.Atlas.GetWidth() && texture.Region.End.Y<=texture.Atlas.GetHeight(), name+" separate atlas region loads within bounds");
        }
        foreach (var card in new FrostCard[]{ModelDb.Card<FrostStrike>(),ModelDb.Card<FrostDefend>(),ModelDb.Card<FreezeRay>()})
            Assert(card.CustomPortraitPath.StartsWith(art) && ResourceLoader.Load<Texture2D>(card.CustomPortraitPath).GetWidth()>1000,card.GetType().Name+" supplied portrait loads");
        var character=ModelDb.Character<FrostswornCharacter>();
        Assert(character.CharacterSelectIcon!=null && character.CharacterSelectLockedIcon!=null && character.MapMarker!=null,"native character-select and map getters load without texture type errors");
        foreach (var relic in new RelicModel[]{ModelDb.Relic<WinterCore>(),ModelDb.Relic<WinterCrown>()})
            Assert(relic.Icon is AtlasTexture && relic.BigIcon is AtlasTexture && relic.IconOutline is AtlasTexture,relic.GetType().Name+" native relic getters use supplied atlas");
        foreach (var potion in new PotionModel[]{ModelDb.Potion<FrostBottle>(),ModelDb.Potion<LiquidNitrogen>(),ModelDb.Potion<FractalSnowflake>()})
            Assert(potion.Image is AtlasTexture && potion.Outline is AtlasTexture,potion.GetType().Name+" native potion getters use supplied atlas");
        Assert(ResourceLoader.Load<CompressedTexture2D>(character.CustomCharacterSelectIconPath)!=null,"select icon uses imported PNG");
        var selection=ResourceLoader.Load<PackedScene>(character.CustomCharacterSelectBgPath).Instantiate<Control>();
        Assert(selection.GetNode<TextureRect>("Background").Texture.ResourcePath==art+"selection.png","select scene loads supplied background");
        selection.Free();
        var visuals=(NCreatureVisuals)typeof(FrostswornCharacter).GetMethod("TryCreateCreatureVisuals",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(character,null)!;
        Assert(visuals.GetNode<Sprite2D>("%Visuals").Texture.ResourcePath==FrostIdleVisuals.EmptyTexture && visuals.HasNode("Visuals/IdleLoop"),"combat visuals instantiate first idle animation");
        Assert(Math.Abs(visuals.GetNode<Sprite2D>("%Visuals").Scale.X-0.31f)<0.001f,"combat art scale aligns new full-height pose");
        visuals.Free();
        var counter=RitsuGodotNodeFactories.CreateFromScenePath<NEnergyCounter>(character.CustomEnergyCounterPath);
        Assert(counter.GetNode<Control>("%Layers").GetNode<TextureRect>("Art").Texture.ResourcePath==art+"energy.png","native energy counter factory accepts custom scene and texture");
        Assert(counter.HasNode("Label") && counter.HasNode("%StarAnchor") && counter.HasNode("%EnergyVfxBack"),"energy counter required nodes survive conversion");
        counter.Free();
        var pool=ModelDb.CardPool<FrostCardPool>();var frame=(ShaderMaterial)pool.PoolFrameMaterial;
        Assert(Math.Abs(frame.GetShaderParameter("h").AsSingle()-0.62f)<0.001f && Math.Abs(frame.GetShaderParameter("s").AsSingle()-2.37f)<0.001f && Math.Abs(frame.GetShaderParameter("v").AsSingle()-2.62f)<0.001f,"card frame uses supplied HSV parameters");
        Assert(pool.EnergyColorName=="frostsworn" && pool.BigEnergyIconPath==art+"energy.png" && pool.TextEnergyIconPath==pool.BigEnergyIconPath,"energy icons have independent character color key");
        GD.Print("ART075_COMPLETE");
    }
}
