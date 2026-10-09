using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
namespace Frostsworn;

public static class IceArmorDisplay
{
    public static readonly Color ArmorColor=new("B9E7F2");
    public static void Attach(NHealthBar health,Creature creature)
    {
        if(health.GetNodeOrNull<Control>("FrostArmorBadge")!=null)return;
        var badge=new Control {Name="FrostArmorBadge",Size=new Vector2(52,52),MouseFilter=Control.MouseFilterEnum.Stop,FocusMode=Control.FocusModeEnum.All};
        health.AddChild(badge);
        var icon=new TextureRect {Name="Shield",ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture=ResourceLoader.Load<Texture2D>("res://Frostsworn/relics083/IceArmor.tres"),MouseFilter=Control.MouseFilterEnum.Ignore};
        badge.AddChild(icon);icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var label=new Label {Name="Amount",HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,MouseFilter=Control.MouseFilterEnum.Ignore};
        var original=health.GetNode<Label>("%BlockLabel");
        label.AddThemeFontOverride("font",original.GetThemeFont("font"));
        label.AddThemeFontSizeOverride("font_size",24);
        label.AddThemeColorOverride("font_color",new Color("FFF6E2"));
        label.AddThemeColorOverride("font_outline_color",new Color("17445F"));
        label.AddThemeConstantOverride("outline_size",10);
        badge.AddChild(label);label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        bool hovered=false;
        void Show()
        {
            NHoverTipSet.Remove(badge);
            if(!badge.Visible)return;
            var desc=new LocString("static_hover_tips","FROSTSWORN_ARMOR.description").GetFormattedText();
            NHoverTipSet.CreateAndShow(badge,new HoverTip(new LocString("static_hover_tips","FROSTSWORN_ARMOR.title"),FrostText.Format("FROSTSWORN_ARMOR_STATUS.description",("Amount",creature.GetPowerAmount<IceArmorPower>()),("Description",desc)),null),HoverTip.GetHoverTipAlignment(badge))?.SetFollowOwner();
        }
        badge.MouseEntered+=()=>{hovered=true;Show();};badge.MouseExited+=()=>{hovered=false;NHoverTipSet.Remove(badge);};
        badge.FocusEntered+=()=>{hovered=true;Show();};badge.FocusExited+=()=>{hovered=false;NHoverTipSet.Remove(badge);};
        badge.TreeExiting+=()=>NHoverTipSet.Remove(badge);
        int previous=-1;
        var timer=new Timer {WaitTime=0.1,Autostart=true};health.AddChild(timer);
        timer.Timeout+=()=>
        {
            int amount=creature.GetPowerAmount<IceArmorPower>();
            if(amount!=previous){previous=amount;health.RefreshValues();if(hovered)Show();}
            Update(health,creature);
        };
        Update(health,creature);
    }
    public static void Update(NHealthBar health,Creature creature)
    {
        var badge=health.GetNodeOrNull<Control>("FrostArmorBadge");if(badge==null)return;
        int amount=creature.GetPowerAmount<IceArmorPower>();
        badge.Visible=amount>0 && !creature.IsDead;
        var stock=health.GetNode<Control>("%BlockContainer");
        badge.Size=new Vector2(stock.Size.X*0.75f,stock.Size.Y*0.75f);
        badge.Position=health.HpBarContainer.Position+new Vector2(health.HpBarContainer.Size.X-badge.Size.X*0.5f,(health.HpBarContainer.Size.Y-badge.Size.Y)/2);
        badge.GetNode<Label>("Amount").Text=amount.ToString();
        // Use the native HP fill itself. Native block and infinite HP styling take precedence.
        if(badge.Visible && creature.Block<=0 && !health.GetNode<Control>("%BlockOutline").Visible && !creature.HpDisplay.IsInfinite())
        {
            health.GetNode<Control>("%HpForeground").SelfModulate=ArmorColor;
            var hpLabel=health.GetNode<Label>("%HpLabel");
            if(hpLabel.GetThemeColor("font_outline_color")==new Color("900000"))
                hpLabel.AddThemeColorOverride("font_outline_color",new Color("17445F"));
        }
        if(!badge.Visible)NHoverTipSet.Remove(badge);
    }
}
[HarmonyPatch(typeof(NHealthBar),nameof(NHealthBar.SetCreature))]
public static class IceArmorHealthAttach
{
    public static void Postfix(NHealthBar __instance,Creature creature)=>IceArmorDisplay.Attach(__instance,creature);
}
[HarmonyPatch(typeof(NHealthBar),nameof(NHealthBar.RefreshValues))]
public static class IceArmorHealthRefresh
{
    public static void Postfix(NHealthBar __instance,Creature ____creature)
    {if(____creature!=null)IceArmorDisplay.Update(__instance,____creature);}
}

