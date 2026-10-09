using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Vfx;
namespace Frostsworn;
public static class FrostRelicFlashLayout
{
    public static bool IsOurs(RelicModel? relic)=>relic is FrostRelic or WinterCore or WinterCrown;
    public static bool IsOurs(Texture2D? texture)
    {
        string path=texture?.ResourcePath??"";
        return path.StartsWith("res://Frostsworn/relics083/",StringComparison.Ordinal)
            || path is "res://Frostsworn/art075/WinterCore.tres" or "res://Frostsworn/art075/WinterCrown.tres";
    }
    public static void Fit(TextureRect image)
    {
        var center=image.Position+image.Size/2;
        image.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        image.CustomMinimumSize=Vector2.Zero;
        image.ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize;
        image.StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered;
        image.Size=new Vector2(64,64);
        image.PivotOffset=new Vector2(32,32);
        image.Position=center-new Vector2(32,32);
    }
}
[HarmonyPatch(typeof(NRelicFlashVfx),nameof(NRelicFlashVfx._Ready))]
public static class FrostRelicFlashSizePatch
{
    public static void Prefix(NRelicFlashVfx __instance,RelicModel? ____relic)
    {
        if(!FrostRelicFlashLayout.IsOurs(____relic))return;
        foreach(var name in new[]{"Image1","Image2","Image3"})FrostRelicFlashLayout.Fit(__instance.GetNode<TextureRect>(name));
    }
}
[HarmonyPatch(typeof(NUiFlashVfx),nameof(NUiFlashVfx._Ready))]
public static class FrostRelicUiFlashSizePatch
{
    public static void Prefix(NUiFlashVfx __instance,Texture2D? ____texture)
    {if(FrostRelicFlashLayout.IsOurs(____texture))FrostRelicFlashLayout.Fit(__instance.GetNode<TextureRect>("TextureRect"));}
}
