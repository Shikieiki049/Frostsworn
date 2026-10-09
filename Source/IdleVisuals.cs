using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using STS2RitsuLib.Scaffolding.Characters.Visuals;

namespace Frostsworn;

public static class FrostIdleVisuals
{
    public const string EmptyTexture = "res://Frostsworn/idle/empty.svg";
    public static void Attach(Node root)
    {
        var body = root.FindChild("Visuals", true, false) as Sprite2D;
        if (body == null || body.HasNode("IdleLoop")) return;
        var idle = ResourceLoader.Load<PackedScene>("res://Frostsworn/idle/player.tscn").Instantiate<Node2D>();
        // The old sprite was centered; the rig is anchored at the planted feet.
        idle.Position = new Vector2(0, 525);
        idle.Scale = Vector2.One * (0.275f / 0.31f);
        body.AddChild(idle);
    }
    public static void OnCue(Node root, string cue)
    {
        var idle = root.FindChild("IdleLoop", true, false);
        if (idle == null) return;
        if (cue.ToLowerInvariant() is "dead" or "death" or "die") idle.SetProcess(false);
        else if (cue.ToLowerInvariant() is "idle" or "relaxed" or "relaxed_loop" or "revive") idle.SetProcess(true);
    }
}

[HarmonyPatch(typeof(ModCreatureVisualPlayback), nameof(ModCreatureVisualPlayback.TryPlayCue))]
internal static class FrostIdleCombatCuePatch
{
    private static void Postfix(MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals visuals, CharacterModel? character, string primaryCue)
    {
        if (character is FrostswornCharacter) FrostIdleVisuals.OnCue(visuals, primaryCue);
    }
}
[HarmonyPatch(typeof(ModCreatureVisualPlayback), nameof(ModCreatureVisualPlayback.TryPlayOnVisualRoot))]
internal static class FrostIdleWorldCuePatch
{
    private static void Postfix(Node root, CharacterModel? character, string animName)
    {
        if (character is FrostswornCharacter) FrostIdleVisuals.OnCue(root, animName);
    }
}

// Cover ordinary shops, the event shop and every player's rest-site model.
[HarmonyPatch(typeof(ModWorldSceneVisualNodeFactory), nameof(ModWorldSceneVisualNodeFactory.TryInstantiateMerchantCharacter))]
internal static class FrostIdleMerchantPatch
{
    private static void Postfix(CharacterModel character, NMerchantCharacter? __result)
    {
        if (character is FrostswornCharacter && __result != null) FrostIdleVisuals.Attach(__result);
    }
}
[HarmonyPatch(typeof(ModWorldSceneVisualNodeFactory), nameof(ModWorldSceneVisualNodeFactory.TryCreateRestSiteCharacter))]
internal static class FrostIdleRestPatch
{
    private static void Postfix(Player player, NRestSiteCharacter? __result)
    {
        if (player.Character is FrostswornCharacter && __result != null) FrostIdleVisuals.Attach(__result);
    }
}
