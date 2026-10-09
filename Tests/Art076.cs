using System;
using System.Reflection;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.HoverTips;

namespace Frostsworn.Tests;
public static partial class Suite
{
    private static async System.Threading.Tasks.Task CheckArt076()
    {
        var tree=(SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
        var run=RunState.CreateForTest([player]);player.ResetCombatState();
        var combat=new MegaCrit.Sts2.Core.Combat.CombatState(runState:run);combat.AddPlayer(player);
        MegaCrit.Sts2.Core.Combat.CombatManager.Instance.SetUpCombat(combat);
        var counter=NEnergyCounter.Create(player)!;
        Assert(counter!=null && counter.Name=="IroncladEnergyCounter","native energy scene is reused through game Create entry point");
        Assert(counter!.MouseFilter==Control.MouseFilterEnum.Stop,"energy control receives mouse input");
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(counter);
        Assert(counter.IsConnected(Control.SignalName.MouseEntered,new Callable(counter,"OnHovered")) || counter.GetSignalConnectionList(Control.SignalName.MouseEntered).Count>0,"native ready connects mouse-enter hover handler");
        var hover=(HoverTip)typeof(NEnergyCounter).GetField("_hoverTip",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(counter)!;
        Assert(hover!=null,"native energy tooltip initialized");
        var label=counter.GetNode<Control>("Label");
        GD.Print($"LABEL_CENTER {label.Position+label.Size/2}; ROOT={counter.Size}");
        Assert((label.Position+label.Size/2-counter.Size/2).Length()<1,"native energy label remains centered after automatic text sizing");
        Assert(counter.GetNode<TextureRect>("%Layers/FrostEnergy").Texture.ResourcePath.EndsWith("art075/energy.png"),"native panel displays frost art");
        var energyArt=counter.GetNode<TextureRect>("%Layers/FrostEnergy");
        Assert(Math.Abs(energyArt.Size.X-211.2f)<0.1f && (energyArt.Position+energyArt.Size/2-counter.Size/2).Length()<1,"battle energy artwork enlarged 1.65x around unchanged number center");
        var number=counter.GetNode<Label>("Label");
        Assert(number.GetThemeFont("font").ResourcePath=="res://themes/kreon_bold_shared.tres" && number.GetThemeConstant("outline_size")==16,"battle numbers explicitly use native Kreon bold and 16px outline");
        Assert(player.Character.EnergyLabelOutlineColor.A==1 && number.GetThemeColor("font_outline_color").A==1,"energy outline remains opaque after native energy refresh");
        counter.Free();
        var pool=ModelDb.CardPool<FrostCardPool>();
        var small=ResourceLoader.Load<Texture2D>(pool.TextEnergyIconPath);
        var stock=ResourceLoader.Load<Texture2D>("res://images/packed/sprite_fonts/ironclad_energy_icon.png");
        GD.Print($"ENERGY_DIMENSIONS native={stock.GetSize()} frost={small.GetSize()}");
        Assert(small.GetSize()==stock.GetSize(),"inline icon uses the same native dimensions as the stock energy icon");
        var frame=(ShaderMaterial)pool.PoolFrameMaterial;
        Assert(Math.Abs(frame.GetShaderParameter("h").AsSingle()-0.619f)<0.001f && Math.Abs(frame.GetShaderParameter("s").AsSingle()-0.62f)<0.001f && Math.Abs(frame.GetShaderParameter("v").AsSingle()-1.64f)<0.001f,"revised frame HSV values");
        foreach(var potion in new PotionModel[]{ModelDb.Potion<FrostBottle>(),ModelDb.Potion<LiquidNitrogen>(),ModelDb.Potion<FractalSnowflake>()})
        {
            var atlas=(AtlasTexture)potion.Image;
            Assert(atlas.Region.Size.X<550 && atlas.Region.Size.Y<550,potion.GetType().Name+" cropped UI region removes excess sheet padding");
        }
        var portrait=(AtlasTexture)FrostArtLayout.SelectionPortrait;
        Assert(portrait.Atlas.ResourcePath.EndsWith("selection.png") && portrait.Region==new Rect2(1175,225,300,398),"selection portrait uses face and upper body from original background");
        var card=ResourceLoader.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<Control>();
        var cost=card.GetNode<TextureRect>("%EnergyIcon");
        var costLabel=card.GetNode<Label>("%EnergyLabel");var labelFontSize=costLabel.GetThemeFontSize("font_size");
        var offsets=new Vector4(cost.OffsetLeft,cost.OffsetTop,cost.OffsetRight,cost.OffsetBottom);
        FrostArtLayout.FitCardEnergy(cost,ModelDb.Card<FrostStrike>());
        FrostArtLayout.FitCardEnergy(cost,ModelDb.Card<FrostStrike>());
        Assert(Math.Abs(cost.Size.X-118.4f)<0.1f && costLabel.GetThemeFontSize("font_size")==labelFontSize,"card energy artwork enlarged 1.85x without enlarging number or accumulating scale");
        Assert(costLabel.OffsetTop==-29 && costLabel.OffsetBottom==27,"card number shifted upward three pixels to crystal visual center without cumulative drift");
        FrostArtLayout.FitCardEnergy(cost,null);
        Assert(new Vector4(cost.OffsetLeft,cost.OffsetTop,cost.OffsetRight,cost.OffsetBottom)==offsets,"pooled non-frost card recovers original energy icon dimensions");
        Assert(costLabel.OffsetTop==-26 && costLabel.OffsetBottom==30,"pooled non-frost card recovers stock number alignment");
        FrostArtLayout.FitCardPortrait(card.GetNode<TextureRect>("%Portrait"),ModelDb.Card<FreezeRay>());
        Assert(card.GetNode<TextureRect>("%Portrait").StretchMode==TextureRect.StretchModeEnum.KeepAspectCovered,"freeze ray fills native portrait box without bars");
        FrostArtLayout.FitCardPortrait(card.GetNode<TextureRect>("%Portrait"),ModelDb.Card<FrostStrike>());
        Assert(card.GetNode<TextureRect>("%Portrait").StretchMode==TextureRect.StretchModeEnum.KeepAspectCentered,"pooled card returns to stock portrait mode");
        card.Free();
        GD.Print("ART076_COMPLETE");
        GD.Print("ART077_COMPLETE");
        GD.Print("ART078_COMPLETE");
    }
}
