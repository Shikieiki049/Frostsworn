using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
namespace Frostsworn;

public static class EclipseUi
{
    private static readonly Dictionary<int,Texture2D> Icons=new();
    public static Texture2D Icon(int level)
    {
        level=Math.Clamp(level,0,8);
        if(Icons.TryGetValue(level,out var icon))return icon;
        var source=ResourceLoader.Load<Texture2D>($"res://Frostsworn/eclipse084/Eclipse{level}.png");
        // Trim transparent margins at draw time so the visible emblem stays centered.
        var used=source.GetImage().GetUsedRect();
        return Icons[level]=new AtlasTexture {Atlas=source,Region=new Rect2(used.Position,used.Size)};
    }
    public static TextureRect Badge(Control parent,Func<int> level,Vector2 size)
    {
        var icon=new TextureRect {Name="EclipseIcon",Size=size,ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter=Control.MouseFilterEnum.Stop,FocusMode=Control.FocusModeEnum.All,Texture=Icon(level())};parent.AddChild(icon);
        void Show()
        {
            NHoverTipSet.Remove(icon);
            var title=new LocString("static_hover_tips","FROSTSWORN_ECLIPSE.title");title.Add("Level",level());
            NHoverTipSet.CreateAndShow(icon,new HoverTip(title,Eclipse.Description(level()),null),HoverTip.GetHoverTipAlignment(icon))?.SetFollowOwner();
        }
        icon.MouseEntered+=Show;icon.FocusEntered+=Show;icon.MouseExited+=()=>NHoverTipSet.Remove(icon);icon.FocusExited+=()=>NHoverTipSet.Remove(icon);
        icon.TreeExiting+=()=>NHoverTipSet.Remove(icon);
        int previous=level();var timer=new Timer {WaitTime=0.15,Autostart=true};icon.AddChild(timer);
        timer.Timeout+=()=>{int current=level();if(current!=previous){previous=current;icon.Texture=Icon(current);NHoverTipSet.Remove(icon);}if(!icon.IsVisibleInTree())NHoverTipSet.Remove(icon);};
        return icon;
    }
    public static void Select(NCharacterSelectScreen screen)
    {
        if(screen.GetNodeOrNull<Control>("EclipseSelect")!=null)return;
        var panel=new Control {Name="EclipseSelect",Size=new Vector2(184,94),MouseFilter=Control.MouseFilterEnum.Ignore};screen.AddChild(panel);
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);panel.AnchorLeft=panel.AnchorRight=0.775f;panel.AnchorTop=panel.AnchorBottom=0.765f;
        panel.OffsetLeft=-92;panel.OffsetRight=92;panel.OffsetTop=-47;panel.OffsetBottom=47;
        int Level()=>screen.Lobby==null?0:Eclipse.LobbyLevel(screen.Lobby);
        var icon=Badge(panel,Level,new Vector2(72,72));icon.Position=new Vector2(56,0);
        var label=new Label {Name="LevelLabel",Position=new Vector2(0,72),Size=new Vector2(184,24),HorizontalAlignment=HorizontalAlignment.Center,MouseFilter=Control.MouseFilterEnum.Ignore};
        label.AddThemeFontSizeOverride("font_size",18);panel.AddChild(label);
        Button Arrow(string text,float x,int step)
        {
            var button=new Button {Text=text,Flat=true,Position=new Vector2(x,8),Size=new Vector2(48,56),FocusMode=Control.FocusModeEnum.All};
            button.AddThemeFontSizeOverride("font_size",40);button.AddThemeColorOverride("font_color",new Color("F0C75A"));panel.AddChild(button);
            button.Pressed+=()=>{if(screen.Lobby!=null)Eclipse.Choose(screen.Lobby,Level()+step);};return button;
        }
        var left=Arrow("◀",0,-1);var right=Arrow("▶",136,1);
        var timer=new Timer {WaitTime=0.1,Autostart=true};panel.AddChild(timer);
        void Update()
        {
            var lobby=screen.Lobby;if(lobby!=null)EclipseNetwork.Poll(lobby);panel.Visible=lobby!=null && lobby.Players.Count>0;
            if(!panel.Visible)return;
            bool host=Eclipse.CanChoose(lobby!);int value=Level();
            int limit=host?Eclipse.LobbyLimit(lobby!):8;
            if(host && value>limit){Eclipse.Choose(lobby!,limit);value=Level();}
            left.Disabled=!host || value<=0;right.Disabled=!host || value>=limit;
            label.Text=$"日食 {value}";icon.Texture=Icon(value);
        }
        timer.Timeout+=Update;Update();
    }
    public static void TopBar(NTopBar bar,IRunState run)
    {
        if(!Eclipse.Available(run) || bar.Portrait.GetNodeOrNull<Control>("EclipseIcon")!=null)return;
        var badge=Badge(bar.Portrait,()=>Eclipse.Level(run),new Vector2(30,30));
        badge.Position=new Vector2(-6,-5);badge.ZIndex=5;
    }
    public static bool HiddenMapPoint(IRunState? run,MapPoint? point)
    {
        if(run==null || point==null || Eclipse.Level(run)<2 || run.Map==null)return false;
        int boss=run.Map.BossMapPoint.coord.row;
        return point.coord.row>=boss-3 && point.coord.row<boss;
    }
    public static void HideMapType(NNormalMapPoint point,IRunState run)
    {
        if(!HiddenMapPoint(run,point.Point) || point.State==MapPointState.Traveled)return;
        point.GetNode<TextureRect>("%Icon").Texture=ResourceLoader.Load<Texture2D>("res://images/atlases/ui_atlas.sprites/map/icons/map_unknown.tres");
        point.GetNode<TextureRect>("%Outline").Texture=ResourceLoader.Load<Texture2D>("res://images/atlases/compressed.sprites/map/map_unknown_outline.tres");
        point.GetNode<Control>("%QuestIcon").Hide();
    }
}
[HarmonyPatch(typeof(NCharacterSelectScreen),nameof(NCharacterSelectScreen._Ready))]
public static class EclipseSelectUiPatch {public static void Postfix(NCharacterSelectScreen __instance)=>EclipseUi.Select(__instance);}
[HarmonyPatch(typeof(NTopBar),nameof(NTopBar.Initialize))]
public static class EclipseTopBarPatch {public static void Postfix(NTopBar __instance,IRunState runState)=>EclipseUi.TopBar(__instance,runState);}
[HarmonyPatch(typeof(NNormalMapPoint),"UpdateIcon")]
public static class EclipseFogPatch {public static void Postfix(NNormalMapPoint __instance,IRunState ____runState)=>EclipseUi.HideMapType(__instance,____runState);}
[HarmonyPatch(typeof(NNormalMapPoint),"OnHighlightPointType")]
public static class EclipseFogLegendPatch
{
    public static bool Prefix(NNormalMapPoint __instance,IRunState ____runState)=>!EclipseUi.HiddenMapPoint(____runState,__instance.Point) || __instance.State==MapPointState.Traveled;
}
