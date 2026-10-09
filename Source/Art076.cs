using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace Frostsworn;

// All adjustments belong to the native UI. Source artwork stays intact.
public static class FrostArtLayout
{
    public static void ApplyEnergy(NEnergyCounter counter)
    {
        var layers=counter.GetNode<Control>("%Layers");
        foreach(var child in layers.GetChildren())
            if(child is CanvasItem item)item.Hide();
        counter.GetNode<CanvasItem>("%EnergyVfxBack").Hide();
        counter.GetNode<CanvasItem>("%EnergyVfxFront").Hide();
        var art=layers.GetNodeOrNull<TextureRect>("FrostEnergy");
        if(art==null)
        {
            art=new TextureRect {Name="FrostEnergy",Texture=ResourceLoader.Load<Texture2D>("res://Frostsworn/art075/energy.png"),
                ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter=Control.MouseFilterEnum.Ignore};
            layers.AddChild(art);
            art.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }
        art.Show();
        // Enlarge only the artwork: the native number keeps its original size and center.
        art.OffsetLeft=art.OffsetTop=-41.6f;
        art.OffsetRight=art.OffsetBottom=41.6f;
        ApplyEnergyFont(counter);
        // Native control remains the hit target; its own _Ready wires energy hover tips.
        counter.MouseFilter=Control.MouseFilterEnum.Stop;
    }

    public static void ApplyEnergyFont(NEnergyCounter counter)
    {
        var label=counter.GetNode<Label>("Label");
        label.AddThemeFontOverride("font",ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres"));
        label.AddThemeConstantOverride("outline_size",16);
        label.AddThemeConstantOverride("shadow_outline_size",16);
        label.AddThemeConstantOverride("shadow_offset_x",3);
        label.AddThemeConstantOverride("shadow_offset_y",2);
        label.AddThemeColorOverride("font_shadow_color",new Color(0,0,0,0.188235f));
    }

    public static void FitCardEnergy(TextureRect icon,CardModel? card)
    {
        const string key="frost_energy_offsets";
        if(card is FrostCard)
        {
            if(!icon.HasMeta(key))icon.SetMeta(key,new Vector4(icon.OffsetLeft,icon.OffsetTop,icon.OffsetRight,icon.OffsetBottom));
            var original=icon.GetMeta(key).AsVector4();
            icon.OffsetLeft=original.X-27.2f;icon.OffsetTop=original.Y-27.2f;
            icon.OffsetRight=original.Z+27.2f;icon.OffsetBottom=original.W+27.2f;
        }
        else if(icon.HasMeta(key))
        {
            var original=icon.GetMeta(key).AsVector4();
            icon.OffsetLeft=original.X;icon.OffsetTop=original.Y;icon.OffsetRight=original.Z;icon.OffsetBottom=original.W;
            icon.RemoveMeta(key);
        }
        var slash=icon.GetNodeOrNull<Control>("UnplayableEnergyIcon");
        if(slash!=null)
        {
            const string slashKey="frost_slash_offsets";
            if(card is FrostCard)
            {
                if(!slash.HasMeta(slashKey))slash.SetMeta(slashKey,new Vector4(slash.OffsetLeft,slash.OffsetTop,slash.OffsetRight,slash.OffsetBottom));
                var original=slash.GetMeta(slashKey).AsVector4();
                slash.OffsetLeft=original.X+27.2f;slash.OffsetRight=original.Z+27.2f;
                slash.OffsetTop=original.Y+24.2f;slash.OffsetBottom=original.W+24.2f;
            }
            else if(slash.HasMeta(slashKey))
            {
                var original=slash.GetMeta(slashKey).AsVector4();
                slash.OffsetLeft=original.X;slash.OffsetTop=original.Y;slash.OffsetRight=original.Z;slash.OffsetBottom=original.W;
                slash.RemoveMeta(slashKey);
            }
        }
        var label=icon.GetNodeOrNull<Control>("EnergyLabel");
        if(label==null)return;
        const string labelKey="frost_energy_label_offsets";
        if(card is FrostCard)
        {
            if(!label.HasMeta(labelKey))label.SetMeta(labelKey,new Vector2(label.OffsetTop,label.OffsetBottom));
            var original=label.GetMeta(labelKey).AsVector2();
            // Stock label sits 2px below the box center; the crystal center is slightly above it.
            label.OffsetTop=original.X-3f;label.OffsetBottom=original.Y-3f;
        }
        else if(label.HasMeta(labelKey))
        {
            var original=label.GetMeta(labelKey).AsVector2();
            label.OffsetTop=original.X;label.OffsetBottom=original.Y;
            label.RemoveMeta(labelKey);
        }
    }

    private static AtlasTexture? _selection;
    public static Texture2D SelectionPortrait => _selection ??= new AtlasTexture {
        Atlas=ResourceLoader.Load<Texture2D>("res://Frostsworn/art075/selection.png"),
        Region=new Rect2(1175,225,300,398),FilterClip=true };

    public static void FitCardPortrait(TextureRect portrait,CardModel? card)
    {
        const string key="frost_original_stretch";
        if(card is FrostCard frost && (card is FreezeRay || frost.CustomPortraitPath.StartsWith("res://Frostsworn/card_art/",StringComparison.Ordinal)))
        {
            if(!portrait.HasMeta(key))portrait.SetMeta(key,(int)portrait.StretchMode);
            portrait.StretchMode=TextureRect.StretchModeEnum.KeepAspectCovered;
        }
        else if(portrait.HasMeta(key))
        {
            portrait.StretchMode=(TextureRect.StretchModeEnum)portrait.GetMeta(key).AsInt32();
            portrait.RemoveMeta(key);
        }
    }

    public static void ApplySelection(NCharacterSelectButton button)
    {
        if(button.Character is not FrostswornCharacter)return;
        foreach(var name in new[]{"%Icon","%IconAdd"})
        {
            var icon=button.GetNodeOrNull<TextureRect>(name);
            if(icon==null)continue;
            icon.Texture=SelectionPortrait;
            icon.ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize;
            icon.StretchMode=TextureRect.StretchModeEnum.KeepAspectCovered;
        }
    }
}

[HarmonyPatch(typeof(NEnergyCounter),nameof(NEnergyCounter.Create))]
public static class FrostNativeEnergyArtPatch
{
    public static void Postfix(Player player,NEnergyCounter? __result)
    {
        if(player.Character is FrostswornCharacter && __result!=null)FrostArtLayout.ApplyEnergy(__result);
    }
}

[HarmonyPatch(typeof(NEnergyCounter),nameof(NEnergyCounter._Ready))]
public static class FrostEnergyFontPatch
{
    public static void Postfix(NEnergyCounter __instance,Player ____player)
    {
        if(____player.Character is FrostswornCharacter)FrostArtLayout.ApplyEnergyFont(__instance);
    }
}

[HarmonyPatch(typeof(NCard),"Reload")]
public static class FrostCardEnergySizePatch
{
    public static void Postfix(NCard __instance)
    {
        if(__instance.GetNodeOrNull<TextureRect>("%EnergyIcon") is {} icon)
            FrostArtLayout.FitCardEnergy(icon,__instance.Model);
    }
}

[HarmonyPatch(typeof(NCharacterSelectButton),nameof(NCharacterSelectButton.Init))]
public static class FrostSelectionArtPatch
{
    public static void Postfix(NCharacterSelectButton __instance)=>FrostArtLayout.ApplySelection(__instance);
}

// The native unlock animation swaps the icon after awaiting. Reapply the region when it does.
[HarmonyPatch(typeof(NCharacterSelectButton),nameof(NCharacterSelectButton._Process))]
public static class FrostSelectionRefreshPatch
{
    public static void Postfix(NCharacterSelectButton __instance)
    {
        if(__instance.Character is FrostswornCharacter && __instance.GetNodeOrNull<TextureRect>("%Icon") is {} icon && icon.Texture!=FrostArtLayout.SelectionPortrait)
            FrostArtLayout.ApplySelection(__instance);
    }
}

[HarmonyPatch(typeof(NCard),"UpdatePortrait")]
public static class FrostCardPortraitFitPatch
{
    public static void Postfix(NCard __instance)
    {
        // Ancient cards use a separate full-height portrait, not the regular art window.
        var ancient=__instance.Model?.Rarity==CardRarity.Ancient;
        if(__instance.GetNodeOrNull<TextureRect>("%Portrait") is {} portrait)
            FrostArtLayout.FitCardPortrait(portrait,ancient ? null : __instance.Model);
        if(__instance.GetNodeOrNull<TextureRect>("%AncientPortrait") is {} ancientPortrait)
            FrostArtLayout.FitCardPortrait(ancientPortrait,ancient ? __instance.Model : null);
    }
}
