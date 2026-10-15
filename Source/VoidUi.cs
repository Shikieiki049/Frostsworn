using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
namespace Frostsworn;

[HarmonyPatch(typeof(NInspectRelicScreen),"SetRarityVisuals")]
public static class VoidInspectRarityPatch
{
    static bool Prefix(NInspectRelicScreen __instance,RelicRarity rarity)
    {
        if(rarity!=VoidRelics.Rarity)return true;
        var label=AccessTools.Field(typeof(NInspectRelicScreen),"_rarityLabel").GetValue(__instance) as MegaLabel;
        if(label!=null){label.Modulate=Colors.White;label.AddThemeColorOverride("font_color",VoidRelics.Color);label.SetTextAutoSize(new LocString("static_hover_tips","FROSTSWORN_VOID_RARITY.title").GetFormattedText());}
        var name=AccessTools.Field(typeof(NInspectRelicScreen),"_nameLabel").GetValue(__instance) as MegaLabel;
        if(name!=null){name.Modulate=Colors.White;name.RemoveThemeColorOverride("font_color");}
        if(AccessTools.Field(typeof(NInspectRelicScreen),"_frameHsv").GetValue(__instance) is ShaderMaterial frame)
        {frame.SetShaderParameter("h",.85f);frame.SetShaderParameter("s",.45f);frame.SetShaderParameter("v",.95f);}
        return false;
    }
    static void Postfix(NInspectRelicScreen __instance,RelicRarity rarity)
    {
        if(rarity==VoidRelics.Rarity)return;
        if(AccessTools.Field(typeof(NInspectRelicScreen),"_nameLabel").GetValue(__instance) is MegaLabel label){label.Modulate=Colors.White;label.RemoveThemeColorOverride("font_color");}
        if(AccessTools.Field(typeof(NInspectRelicScreen),"_rarityLabel").GetValue(__instance) is MegaLabel rarityLabel)rarityLabel.RemoveThemeColorOverride("font_color");
    }
}

[HarmonyPatch(typeof(NInspectRelicScreen),"UpdateRelicDisplay")]
public static class VoidInspectDisplayPatch
{
    static bool Prefix(NInspectRelicScreen __instance)
    {
        var type=typeof(NInspectRelicScreen);
        object? Get(string name)=>AccessTools.Field(type,name).GetValue(__instance);
        var relics=Get("_relics") as IReadOnlyList<RelicModel>;
        if(relics==null || relics[(int)Get("_index")!] is not VoidRelic relic)return true;
        bool unlocked=(Get("_allUnlockedRelics") as HashSet<RelicModel>)!.Contains(relic.CanonicalInstance);
        bool seen=SaveManager.Instance.IsRelicSeen(relic);
        var name=(MegaLabel)Get("_nameLabel")!;var rarity=(MegaLabel)Get("_rarityLabel")!;
        var description=(MegaRichTextLabel)Get("_description")!;var flavor=(MegaRichTextLabel)Get("_flavor")!;
        var image=(TextureRect)Get("_relicImage")!;
        AccessTools.Method(type,"SetRarityVisuals").Invoke(__instance,new object[]{VoidRelics.Rarity});
        name.SetTextAutoSize(unlocked && seen?relic.Title.GetFormattedText():new LocString("inspect_relic_screen",unlocked?"UNDISCOVERED_TITLE":"LOCKED_TITLE").GetFormattedText());
        rarity.SetTextAutoSize(unlocked?new LocString("static_hover_tips","FROSTSWORN_VOID_RARITY.title").GetFormattedText():"");
        description.SetTextAutoSize(unlocked && seen?relic.DynamicDescription.GetFormattedText():new LocString("inspect_relic_screen",unlocked?"UNDISCOVERED_DESCRIPTION":"LOCKED_DESCRIPTION").GetFormattedText());
        flavor.Text=unlocked && seen?relic.Flavor.GetFormattedText():"";
        image.Texture=relic.BigIcon;image.SelfModulate=unlocked && seen?Colors.White:new Color(.1f,.1f,.1f);
        NHoverTipSet.Clear();
        if(MegaCrit.Sts2.Core.Nodes.NGame.Instance!=null && seen && unlocked && Get("_hoverTipRect") is Control rect)
            NHoverTipSet.CreateAndShow(__instance,relic.HoverTipsExcludingRelic)?.SetAlignment(rect,MegaCrit.Sts2.Core.HoverTips.HoverTip.GetHoverTipAlignment(__instance));
        return false;
    }
}

// Keep the native collection entry/inspection flow, but add a separate custom-rarity section.
[HarmonyPatch(typeof(NRelicCollectionCategory),nameof(NRelicCollectionCategory.LoadRelics))]
public static class VoidCollectionPatch
{
    static void Postfix(NRelicCollectionCategory __instance,RelicRarity relicRarity,NRelicCollection collection,HashSet<RelicModel> seenRelics,UnlockState unlockState,HashSet<RelicModel> allUnlockedRelics)
    {
        if(relicRarity!=RelicRarity.Event)return;
        if(__instance.GetNodeOrNull<Node>("FrostswornVoidRelics") is {} previous){__instance.RemoveChild(previous);previous.QueueFree();}
        var section=ResourceLoader.Load<PackedScene>(NRelicCollectionCategory.scenePath).Instantiate<NRelicCollectionCategory>();
        section.Name="FrostswornVoidRelics";
        __instance.AddChild(section);
        section.LoadRelics(VoidRelics.Rarity,collection,new LocString("static_hover_tips","FROSTSWORN_VOID_COLLECTION.title"),seenRelics,unlockState,allUnlockedRelics);
        section.GetNode<MegaRichTextLabel>("%Header").Modulate=Colors.White;
        section.GetNode<MegaRichTextLabel>("%Header").RemoveThemeColorOverride("default_color");

    }
}

[HarmonyPatch(typeof(NRelicCollectionCategory),nameof(NRelicCollectionCategory.GetGridItems))]
public static class VoidCollectionNavigationPatch
{
    static void Postfix(NRelicCollectionCategory __instance,ref List<IReadOnlyList<Control>> __result)
    {if(__instance.GetNodeOrNull<NRelicCollectionCategory>("FrostswornVoidRelics") is {} section)__result.AddRange(section.GetGridItems());}
}

