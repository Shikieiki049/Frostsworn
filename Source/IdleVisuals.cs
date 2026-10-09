using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Rooms;
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
    public static void EnsureMerchant(NMerchantCharacter root)
    {
        // Room/skin initialization may replace the factory's first sprite.
        // Attach to the final body; provide a shell if that path used Spine.
        var body=root.FindChild("Visuals",true,false) as Sprite2D;
        if(body==null)
        {
            body=root.GetNodeOrNull<Sprite2D>("FrostIdleBody");
            if(body==null)
            {
                body=new Sprite2D {Name="FrostIdleBody",Texture=ResourceLoader.Load<Texture2D>(EmptyTexture),Scale=Vector2.One*0.465f,Position=new Vector2(0,-244.125f)};
                root.AddChild(body);
            }
            if(root.GetChildCount()>0 && root.GetChild(0) is CanvasItem previous && previous!=body)previous.Hide();
            if(!body.HasNode("IdleLoop"))
            {
                var idle=ResourceLoader.Load<PackedScene>("res://Frostsworn/idle/player.tscn").Instantiate<Node2D>();
                idle.Position=new Vector2(0,525);idle.Scale=Vector2.One*(0.275f/0.31f);body.AddChild(idle);
            }
        }
        else
        {
            if(root.GetNodeOrNull<Sprite2D>("FrostIdleBody") is {} stale)
            {root.RemoveChild(stale);stale.QueueFree();}
            body.Texture=ResourceLoader.Load<Texture2D>(EmptyTexture);
            body.Scale=Vector2.One*0.465f;body.Position=new Vector2(0,-244.125f);
            Attach(root);
        }
        body.Show();
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
        if (character is FrostswornCharacter)
        {
            if(root is NMerchantCharacter merchant)FrostIdleVisuals.EnsureMerchant(merchant);
            FrostIdleVisuals.OnCue(root, animName);
        }
    }
}
[HarmonyPatch(typeof(NMerchantRoom),"AfterRoomIsLoaded")]
internal static class FrostIdleMerchantRoomPatch
{
    public static void Postfix(NMerchantRoom __instance,List<Player> ____players)
    {
        for(int i=0;i<____players.Count && i<__instance.PlayerVisuals.Count;i++)
            if(____players[i].Character is FrostswornCharacter)FrostIdleVisuals.EnsureMerchant(__instance.PlayerVisuals[i]);
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
