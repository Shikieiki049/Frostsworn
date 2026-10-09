using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Characters.Visuals;

namespace Frostsworn.Tests;
public static partial class Suite
{
    private static string Plain(string text) => Regex.Replace(text, @"\[[^\]]*\]", "");
    private static async Task CheckTextAndCharacter(CombatState combat, Player player, Creature enemy, PlayerChoiceContext context)
    {
        var attack = combat.CreateCard<FrostNeedle>(player);
        await CardPileCmd.Add(attack, PileType.Hand);
        await PowerCmd.Apply<StrengthPower>(context, player.Creature, 3, player.Creature, null);
        attack.UpdateDynamicVarPreview(CardPreviewMode.Normal, enemy, attack.DynamicVars);
        string boosted = attack.GetDescriptionForPile(PileType.Hand, enemy);
        GD.Print("TEXT_STRENGTH=" + boosted);
        Assert(Plain(boosted).Contains("11点伤害"), "description follows Strength: 8 damage becomes 11");
        Assert(boosted != Plain(boosted), "modified damage includes native highlight markup");
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>()!);
        await PowerCmd.Apply<WeakPower>(context, player.Creature, 1, enemy, null);
        attack.UpdateDynamicVarPreview(CardPreviewMode.Normal, enemy, attack.DynamicVars);
        Assert(Plain(attack.GetDescriptionForPile(PileType.Hand, enemy)).Contains("6点伤害"), "description follows Weak: 8 damage becomes 6");
        await PowerCmd.Remove(player.Creature.GetPower<WeakPower>()!);
        var defend = combat.CreateCard<FrostDefend>(player);
        await CardPileCmd.Add(defend, PileType.Hand);
        await PowerCmd.Apply<DexterityPower>(context, player.Creature, 2, player.Creature, null);
        defend.UpdateDynamicVarPreview(CardPreviewMode.Normal, null, defend.DynamicVars);
        GD.Print("TEXT_DEXTERITY=" + defend.GetDescriptionForPile(PileType.Hand));
        Assert(Plain(defend.GetDescriptionForPile(PileType.Hand)).Contains("7点格挡"), "description follows Dexterity: 5 block becomes 7");
        await PowerCmd.Remove(player.Creature.GetPower<DexterityPower>()!);
        attack.UpgradeInternal();
        attack.FinalizeUpgradeInternal();
        attack.UpdateDynamicVarPreview(CardPreviewMode.Normal, enemy, attack.DynamicVars);
        Assert(Plain(attack.GetDescriptionForPile(PileType.Hand, enemy)).Contains("11点伤害"), "upgraded description uses upgraded damage");
        var preview = combat.CreateCard<FrostStrike>(player);
        preview.UpgradeInternal();
        preview.UpgradePreviewType = CardUpgradePreviewType.Deck;
        GD.Print("TEXT_UPGRADE=" + preview.GetDescriptionForUpgradePreview());
        Assert(Plain(preview.GetDescriptionForUpgradePreview()).Contains("9点伤害"), "upgrade preview displays future damage");
        await CardPileCmd.RemoveFromCombat(attack);
        await CardPileCmd.RemoveFromCombat(defend);

        var character = (FrostswornCharacter)player.Character;
        var texture = ResourceLoader.Load<Texture2D>(FrostswornCharacter.WizardTexture);
        Assert(texture.GetSize() == new Vector2(979,1606), "supplied character image packed at original resolution");
        Assert(character.CharacterSelectIcon is CompressedTexture2D, "native unlocked character-select getter accepts portrait type");
        Assert(character.CharacterSelectLockedIcon is CompressedTexture2D, "native locked character-select getter accepts portrait type");
        Assert(character.MapMarker is CompressedTexture2D, "native map-marker getter accepts portrait type");
        foreach (var path in new[] {character.CustomIconTexturePath, character.CustomIconOutlineTexturePath, character.CustomIconPath})
            Assert(MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetCompressedTexture2D(path) != null, "native compressed texture cache accepts " + path);
        var visuals = (NCreatureVisuals)typeof(FrostswornCharacter).GetMethod("TryCreateCreatureVisuals", BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(character, null)!;
        visuals._Ready();
        Assert(!visuals.HasSpineAnimation && visuals.Body is Sprite2D && visuals.TalkPosition != null, "native visual initialization accepts static sprite and anchors");
        Assert(visuals.GetNode<Control>("Bounds").Size.Y == 322, "static character has scaled combat bounds");
        foreach (var cue in new[] { "idle", "attack", "cast", "hit", "dead" })
        {
            Assert(ModCreatureVisualPlayback.TryPlayCue(visuals, character, cue), "static character handles " + cue);
            Assert(visuals.GetNode<Sprite2D>("%Visuals").Texture.ResourcePath == FrostswornCharacter.WizardTexture, "character keeps supplied sprite for " + cue);
        }
        visuals.Free();
        var selection = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetScene(character.CharacterSelectBg).Instantiate<Control>();
        var background = selection.GetNode<TextureRect>("Background");
        Assert(background.Texture is CompressedTexture2D && background.Texture.GetSize() == new Vector2(1672,941), "native select background scene loads supplied full artwork");
        Assert(background.AnchorRight == 1 && background.AnchorBottom == 1 && background.MouseFilter == Control.MouseFilterEnum.Ignore, "selection background covers screen without intercepting buttons");
        selection.Free();
        var merchant = ModWorldSceneVisualNodeFactory.TryInstantiateMerchantCharacter(character)!;
        Assert(merchant != null, "static merchant character factory");
        Assert(ModCreatureVisualPlayback.TryPlayOnVisualRoot(merchant!, character, "idle", cueSetOverride: character.WorldProceduralVisuals.Merchant!.CueSet), "merchant renders supplied static art");
        merchant!.Free();
        // The standalone harness cannot bind the game's scripted selection-reticle scene.
        // Exercise cue playback on the same visual-node layout without that unrelated UI dependency.
        var rest = new Control();
        var controlRoot = new Control { Name = "ControlRoot" };
        rest.AddChild(controlRoot);
        controlRoot.AddChild(new Sprite2D { Name = "Visuals" });
        Assert(ModCreatureVisualPlayback.TryPlayOnVisualRoot(rest, character, "rest", cueSetOverride: character.WorldProceduralVisuals.RestSite!.CueSet), "rest-site cue renders supplied art on world visual layout");
        rest.Free();
    }
}
