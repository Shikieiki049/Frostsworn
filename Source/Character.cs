using MegaCrit.Sts2.Core.Entities.Characters;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Timeline.Scaffolding;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Characters.Visuals.Definition;

namespace Frostsworn;

[RegisterCharacter]
public sealed class FrostswornCharacter : ModCharacterTemplate<FrostCardPool, FrostRelicPool, FrostPotionPool>
{
    public override CharacterGender Gender => CharacterGender.Masculine;
    public override Color NameColor => FrostTheme.FrameColor;
    public override Color MapDrawingColor => ModelDb.CardPool<FrostCardPool>().DeckEntryCardColor;
    public override Color EnergyLabelOutlineColor => new("17283FFF");
    public override int StartingHp => 70;
    public override int StartingGold => 99;
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;
    public override string PlaceholderCharacterId => "ironclad";
    public const string WizardTexture = "res://Frostsworn/art075/battle.png";
    // Native character-select and map getters require CompressedTexture2D, not AtlasTexture.
    private const string PortraitTexture = "res://Frostsworn/art075/portrait.png";
    public override string CustomIconTexturePath => PortraitTexture;
    public override string CustomIconOutlineTexturePath => PortraitTexture;
    public override string CustomIconPath => PortraitTexture;
    public override string CustomCharacterSelectIconPath => "res://Frostsworn/art075/selection.png";
    public override string CustomCharacterSelectLockedIconPath => CustomCharacterSelectIconPath;
    public override string CustomMapMarkerPath => "res://Frostsworn/art075/map.png";
    public override string CustomCharacterSelectBgPath => "res://Frostsworn/character/selection.tscn";
    public override string CustomEnergyCounterPath => "res://scenes/combat/energy_counters/ironclad_energy_counter.tscn";

    // Keep the supplied PNG intact; display size and anchors belong to the scene.
    private static VisualCueSet StaticCues(float scale, float y)
    {
        var builder = VisualCueSetBuilder.Create();
        var style = new VisualNodeStyle { Scale = new Vector2(scale, scale), Position = new Vector2(0, y) };
        foreach (var cue in new[] { "idle", "attack", "cast", "hit", "dead", "die", "relaxed", "rest", "sleep", "wake" })
            builder.Single(cue, FrostIdleVisuals.EmptyTexture, style);
        return builder.Build();
    }
    public override VisualCueSet VisualCues => StaticCues(0.31f, -162.75f);
    public override CharacterWorldProceduralVisualSet WorldProceduralVisuals =>
        ModCharacterWorldSceneVisuals.Procedural()
            .Merchant(StaticCues(0.465f, -244.125f))
            .RestSite(StaticCues(0.465f, -244.125f)).Build();

    protected override NCreatureVisuals TryCreateCreatureVisuals()
    {
        var visuals = RitsuGodotNodeFactories.CreateFromResource<NCreatureVisuals>(ResourceLoader.Load<Texture2D>(FrostIdleVisuals.EmptyTexture));
        var sprite = visuals.GetNode<Sprite2D>("%Visuals");
        sprite.Scale = new Vector2(0.31f, 0.31f);
        sprite.Position = new Vector2(0, -162.75f);
        var bounds = visuals.GetNode<Control>("Bounds");
        bounds.Position = new Vector2(-150, -330);
        bounds.Size = new Vector2(300, 330);
        visuals.GetNode<Marker2D>("%CenterPos").Position = new Vector2(0, -150);
        visuals.GetNode<Marker2D>("IntentPos").Position = new Vector2(0, -375);
        foreach (var (name, position) in new[] { ("OrbPos", new Vector2(110, -170)), ("TalkPos", new Vector2(125, -280)) })
        {
            var marker = new Marker2D { Name = name, Position = position };
            visuals.AddChild(marker);
            marker.Owner = visuals;
            marker.UniqueNameInOwner = true;
        }
        FrostIdleVisuals.Attach(visuals);
        return visuals;
    }
    public override bool RequiresEpochAndTimeline => true;
    public override List<string> GetArchitectAttackVfx() => ["vfx/vfx_attack_slash"];
}

public sealed class FrostCardPool : TypeListCardPoolModel
{
    public override string Title => "霜誓者";
    public override string EnergyColorName => "frostsworn";
    public override string BigEnergyIconPath => "res://Frostsworn/art075/energy.png";
    public override string TextEnergyIconPath => "res://Frostsworn/art076/energy_text.png";
    public override Color DeckEntryCardColor => FrostTheme.DeckAndMapColor;
    public override bool IsColorless => false;
    private static ShaderMaterial? _frame;
    public override Material PoolFrameMaterial
    {
        get
        {
            if (_frame is null)
            {
                _frame = (ShaderMaterial)ResourceLoader.Load<ShaderMaterial>("res://materials/cards/frames/card_frame_colorless_mat.tres").Duplicate();
                _frame.SetShaderParameter("h", FrostTheme.FrameHue);
                _frame.SetShaderParameter("s", FrostTheme.FrameSaturation);
                _frame.SetShaderParameter("v", FrostTheme.FrameValue);
            }
            return _frame;
        }
    }
}
public sealed class FrostRelicPool : TypeListRelicPoolModel
{
    public override string EnergyColorName => "frostsworn";
    public override string BigEnergyIconPath => "res://Frostsworn/art075/energy.png";
    public override string TextEnergyIconPath => "res://Frostsworn/art076/energy_text.png";
}
public sealed class FrostPotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "frostsworn";
    public override string BigEnergyIconPath => "res://Frostsworn/art075/energy.png";
    public override string TextEnergyIconPath => "res://Frostsworn/art076/energy_text.png";
}

[RegisterStory]
public sealed class FrostStory : ModStoryTemplate
{
    protected override string StoryKey => "frostsworn_character_frostsworn_character";
}
[RegisterEpoch, RegisterStoryEpoch(typeof(FrostStory)), AutoTimelineSlotAfterColumn(EpochEra.Invitation0)]
public sealed class FrostEpoch : ModEpochTemplate
{
    public override string Id => "FROSTSWORN_CHARACTER_FROSTSWORN_CHARACTER_EPOCH";
}

[RegisterRelic(typeof(FrostRelicPool)), RegisterCharacterStarterRelic(typeof(FrostswornCharacter))]
public sealed class WinterCore : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override string CustomIconPath => "res://Frostsworn/art075/WinterCore_small.tres";
    public override string CustomBigIconPath => "res://Frostsworn/art075/WinterCore.tres";
    public override string CustomIconOutlinePath => CustomIconPath;
    private bool _given;
    public override Task BeforeCombatStart()
    {
        _given = false;
        return Task.CompletedTask;
    }
    protected override IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip> AdditionalHoverTips =>
        [new MegaCrit.Sts2.Core.HoverTips.HoverTip(new LocString("static_hover_tips", "FROSTSWORN_FROST.title"),
            new LocString("static_hover_tips", "FROSTSWORN_FROST.description"), null)];
    public override async Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner.Creature) || _given || Owner.Creature.IsDead) return;
        _given = true;
        Flash();
        await PowerCmd.Apply<FrostPower>(context, combatState.HittableEnemies, 3, Owner.Creature, null);
    }
}


