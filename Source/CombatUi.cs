using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.CardPiles.Nodes;

namespace Frostsworn;

public static class FrostDisplay
{
    public static string Description(Creature creature)
    {
        var state = FrostActions.Freeze(creature);
        string key = creature.Monster?.NextMove.Id == MonsterModel.stunnedMoveId ? "FROZEN" :
            state.NeedsNormalMove ? "RECOVERY" : !FrostActions.CanFreeze(creature) ? "UNINTERRUPTIBLE" : "READY";
        var text = new LocString("static_hover_tips", "FROSTSWORN_FROST_STATUS.description");
        text.Add("Frost", creature.GetPowerAmount<FrostPower>());
        text.Add("Threshold", FrostActions.Threshold(creature));
        text.Add("Status", FrostText.Get($"FROSTSWORN_FROST_{key}.description"));
        text.Add("Count", state.Count);
        return text.GetFormattedText();
    }

    public static ProgressBar Create(Control parent, Creature creature)
    {
        var row = new Control { Name="FrostswornFrostRow", Position=new Vector2(0,-34),
            Size=new Vector2(Math.Max(140,parent.Size.X),32),
            MouseFilter=Control.MouseFilterEnum.Stop, FocusMode=Control.FocusModeEnum.All };
        parent.AddChild(row);
        var bar = FrostThermometer.Create(row);
        var numbers=new Label { Name="FrostNumbers", Text="0 / 0", Size=new Vector2(72,24),
            VerticalAlignment=VerticalAlignment.Center, MouseFilter=Control.MouseFilterEnum.Ignore };
        numbers.AddThemeFontSizeOverride("font_size",16);
        numbers.AddThemeColorOverride("font_color",new Color("c7f4ff"));
        numbers.AddThemeColorOverride("font_outline_color",Colors.Black);
        numbers.AddThemeConstantOverride("outline_size",3);
        row.AddChild(numbers);
        bool hovered=false;
        string previous="";
        void Show()
        {
            // None leaves the native tooltip at the origin. Explicitly anchor to this row.
            // Remove the previous set before recreating it when values change under the mouse.
            NHoverTipSet.Remove(row);
            var tip=NHoverTipSet.CreateAndShow(row,new HoverTip(new LocString("static_hover_tips","FROSTSWORN_FROST.title"),Description(creature),null),HoverTip.GetHoverTipAlignment(row));
            tip?.SetFollowOwner();
        }
        void Update()
        {
            row.Visible=!creature.IsDead && (creature.GetPowerAmount<FrostPower>()>0 || FrostActions.Freeze(creature).Count>0 ||
                creature.CombatState?.PlayerCreatures.Any(c=>c.Player?.Character is FrostswornCharacter)==true);
            row.Size=new Vector2(Math.Max(140,parent.Size.X),32);
            bar.Size=new Vector2(row.Size.X-94,12);
            numbers.Position=new Vector2(row.Size.X-70,4);
            bar.MaxValue=FrostActions.Threshold(creature);
            bar.Value=creature.GetPowerAmount<FrostPower>();
            FrostThermometer.Refresh(bar);
            numbers.Text=$"{creature.GetPowerAmount<FrostPower>()} / {FrostActions.Threshold(creature)}";
            string text=Description(creature);
            if(hovered && text!=previous && row.IsVisibleInTree()) Show();
            if(!row.IsVisibleInTree()) NHoverTipSet.Remove(row);
            previous=text;
        }
        row.MouseEntered+=()=> { hovered=true; Show(); };
        row.MouseExited+=()=> { hovered=false; NHoverTipSet.Remove(row); };
        row.FocusEntered+=()=> { hovered=true; Show(); };
        row.FocusExited+=()=> { hovered=false; NHoverTipSet.Remove(row); };
        row.TreeExiting+=()=>NHoverTipSet.Remove(row);
        var timer=new Timer { WaitTime=0.1, Autostart=true };
        timer.Timeout+=Update;
        row.AddChild(timer);
        Update();
        return bar;
    }
    public static string ColdDescription(Player player) => FrostText.Format("FROSTSWORN_COLD_STATUS.description", ("Count",ColdStorage.Pile(player).Cards.Count),("Capacity",ColdStorage.Capacity(player)),("Thaw",1+player.Creature.GetPowerAmount<SlowReleasePower>()));
}

[HarmonyPatch(typeof(NCreatureStateDisplay),nameof(NCreatureStateDisplay.SetCreature))]
internal static class FrostBarPatch
{
    public static void Postfix(NCreatureStateDisplay __instance,Creature creature)
    {
        if(creature.Monster==null) return;
        var health=__instance.GetNode<Control>("%HealthBar");
        if(health.GetNodeOrNull<Control>("FrostswornFrostRow")==null) FrostDisplay.Create(health,creature);
    }
}

[HarmonyPatch(typeof(NModCardPileButton),"ShowHoverTipAnchored")]
internal static class ColdCapacityTipPatch
{
    public static void Prefix(NModCardPileButton __instance,Player? ____player,ref HoverTip? ____hoverTip)
    {
        if(____player==null || __instance.Definition?.PileType!=Entry.ColdPileType) return;
        ____hoverTip=new HoverTip(new LocString("static_hover_tips","FROSTSWORN_CARDPILE_COLD_STORAGE.title"),FrostDisplay.ColdDescription(____player),null);
    }
}
