using HarmonyLib;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
namespace Frostsworn;

[HarmonyPatch(typeof(MendRestSiteOption),nameof(MendRestSiteOption.Description),MethodType.Getter)]
public static class EclipseMendPreviewPatch
{
    public static void Postfix(MendRestSiteOption __instance,HealVar ____healVar,ref LocString? ____description,ref LocString __result)
    {
        var player=(Player)AccessTools.Property(typeof(RestSiteOption),"Owner").GetValue(__instance)!;
        // Mend mutates its cached description while targeting. Keep that same
        // object so HasTarget/Name survive, and leave the gameplay HealVar intact.
        if(__result.LocEntryKey!="FROSTSWORN_ECLIPSE_MEND.description")
        {
            var description=new LocString("static_hover_tips","FROSTSWORN_ECLIPSE_MEND.description");
            description.AddVariablesFrom(__result);
            ____description=__result=description;
        }
        __result.Add("Percent",Eclipse.Level(player.RunState)>=5?20:30);
        __result.Add(new HealVar(____healVar.Name,Eclipse.Healing(player.Creature,____healVar.PreviewValue)));
    }
}

[HarmonyPatch(typeof(HealRestSiteOption),nameof(HealRestSiteOption.Description),MethodType.Getter)]
public static class EclipseRestPreviewPatch
{
    public static void Postfix(HealRestSiteOption __instance,ref LocString __result)
    {
        var player=(Player)AccessTools.Property(typeof(RestSiteOption),"Owner").GetValue(__instance)!;
        if(Eclipse.Level(player.RunState)<5 || Eclipse.Ancient(player.RunState))return;
        if(__result.Variables.TryGetValue("Heal",out var value) && value is DynamicVar original)
        {
            var description=new LocString("static_hover_tips","FROSTSWORN_ECLIPSE_REST.description");
            description.AddVariablesFrom(__result);
            description.Add(new HealVar(original.Name,Eclipse.Healing(player.Creature,original.PreviewValue)));
            __result=description;
        }
    }
}
public static class FrostTinyColors
{
    private static ShaderMaterial? _tint;
    public static void Apply(NTinyCard icon)
    {
        if(_tint is null)
        {
            _tint=new ShaderMaterial {Shader=new Shader {Code="""
                shader_type canvas_item;
                uniform vec4 tint : source_color;
                void fragment() {
                    vec4 tex = texture(TEXTURE, UV);
                    float brightness = max(tex.r, max(tex.g, tex.b));
                    // Keep the dark outline, replace the body rather than multiplying gray twice.
                    COLOR = vec4(mix(tex.rgb, tint.rgb, smoothstep(0.12, 0.30, brightness)), tex.a);
                }
                """}};
            _tint.SetShaderParameter("tint",ModelDb.CardPool<FrostCardPool>().DeckEntryCardColor);
        }
        var back=icon.GetNode<TextureRect>("%CardBack");
        back.Material=_tint;back.Modulate=Colors.White;
        var body=icon.GetNode<TextureRect>("%PortraitShadow");
        body.Material=_tint;body.Modulate=Colors.White;
        icon.GetNode<TextureRect>("%Portrait").Modulate=Colors.White;
    }
}
[HarmonyPatch(typeof(NTinyCard),nameof(NTinyCard.SetCard))]
public static class FrostTinyCardPatch
{
    public static void Postfix(NTinyCard __instance,CardModel card)
    {if(card is FrostCard)FrostTinyColors.Apply(__instance);}
}
[HarmonyPatch(typeof(NTinyCard),nameof(NTinyCard.Set))]
public static class FrostTinyPoolPatch
{
    public static void Postfix(NTinyCard __instance,CardPoolModel cardPool)
    {if(cardPool is FrostCardPool)FrostTinyColors.Apply(__instance);}
}
[HarmonyPatch(typeof(NSpeechBubbleVfx),"GetCreatureSpeechPosition")]
public static class FrostSpeechPositionPatch
{
    public static void Postfix(Creature speaker,ref Vector2 __result)
    {
        if(speaker.Player?.Character is not FrostswornCharacter || speaker.GetCreatureNode() is not {} node)return;
        // Use a world-space mouth marker rather than the portrait/UI scale or fallback hitbox.
        __result=node.GetGlobalTransform()*new Vector2(125,-280);
    }
}
