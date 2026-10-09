using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Monsters.Mocks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Nodes.Combat;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static async Task Check083()
    {
        var tree=(SceneTree)Engine.GetMainLoop();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
        var run=RunState.CreateForTest([player]);player.ResetCombatState();
        var combat=new CombatState(runState:run);combat.AddPlayer(player);
        var monster=(MockAttackMonster)ModelDb.Monster<MockAttackMonster>().ToMutable();
        var enemy=combat.CreateCreature(monster,CombatSide.Enemy,"083");combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        var turn=typeof(CombatManager).GetField("_turnState",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(CombatManager.Instance)!;
        turn.GetType().GetProperty("IsInProgress")!.SetValue(turn,true);
        if(monster.MoveStateMachine==null)monster.SetUpForCombat();monster.RollMove(combat.PlayerCreatures);
        var context=new ThrowingPlayerChoiceContext();var selector=new TestCardSelector();using var selection=CardSelectCmd.UseSelector(selector);
        MegaCrit.Sts2.Core.Context.LocalContext.NetId=null;
        var own=player.Creature;var pcs=player.PlayerCombatState!;
        await CreatureCmd.SetMaxAndCurrentHp(enemy,1000);
        foreach(var r in player.Relics.ToArray())player.RemoveRelicInternal(r);
        T Relic<T>() where T:RelicModel {var r=(T)ModelDb.Relic<T>().ToMutable();player.AddRelicInternal(r);return r;}
        async Task<CardModel> Card<T>(PileType pile) where T:CardModel {var c=combat.CreateCard<T>(player);await CardPileCmd.Add(c,pile);return c;}
        async Task ClearCards(){selector.Cleanup();foreach(var c in pcs.AllPiles.Append(ColdStorage.Pile(player)).SelectMany(p=>p.Cards).Distinct().ToArray())await CardPileCmd.RemoveFromCombat(c);}
        var deep=(FrostCard)await Card<DeepCache>(PileType.Hand);
        Assert(deep.Spec.Cost==1 && deep.DynamicVars.Damage.BaseValue==4 && deep.DynamicVars["Amount"].BaseValue==2,"DeepCache costs one, hits four, draws two");
        CardCmd.Upgrade(deep);Assert(deep.EnergyCost.GetWithModifiers(CostModifiers.None)==0 && deep.DynamicVars.Damage.BaseValue==4 && deep.DynamicVars["Amount"].BaseValue==2,"DeepCache upgrade only reduces energy cost");
        await ClearCards();
        var bottle=Relic<WinterBottle>();await bottle.BeforeCombatStart();var stored=await Card<FrostStrike>(PileType.Draw);await Card<FrostDefend>(PileType.Draw);
        selector.PrepareToSelect(new[]{stored});await bottle.BeforeHandDraw(player,context,combat);await bottle.BeforeHandDraw(player,context,combat);
        Assert(ColdStorage.Pile(player).Cards.SequenceEqual(new[]{stored}) && pcs.Hand.Cards.Count==0,"bottle stores one card before first draw and only once");
        var bookmark=Relic<SnowBookmark>();await bookmark.BeforeSideTurnEnd(context,CombatSide.Player,new[]{own});
        Assert(own.GetPowerAmount<IceArmorPower>()==1,"bookmark counts stored cards at turn end");
        var soil=Relic<FrozenSoil>();await soil.BeforeCombatStart();await own.GetPower<IceArmorPower>()!.BeforeHandDrawLate(player,context,combat);
        Assert(own.GetPowerAmount<IceArmorPower>()==7,"soil restores seven after first melt removes last armor");
        await own.GetPower<IceArmorPower>()!.BeforeHandDrawLate(player,context,combat);Assert(own.GetPowerAmount<IceArmorPower>()==3,"soil does not restore twice");
        await ClearCards();var globe=Relic<PolarGlobe>();
        for(int i=0;i<2;i++){var c=await Card<FrostStrike>(PileType.Hand);selector.PrepareToSelect(new[]{c});await ColdStorage.Store(context,player,bottle,1);}
        Assert(globe.StoredCount==2,"globe counts each stored card");
        var third=await Card<FrostStrike>(PileType.Hand);selector.PrepareToSelect(new[]{third});selector.PrepareToSelect(new[]{third});await ColdStorage.Store(context,player,bottle,1);
        Assert(globe.StoredCount==0 && third.Pile==pcs.Hand && ColdStorage.State(third).Pending,"third cold storage grants optional thaw and normal thaw discount");
        var primer=Relic<SnowPrimer>();await primer.BeforeCombatStart();
        await PowerCmd.Apply<HeartInscriptionPower>(context,own,1,own,null);await PowerCmd.Apply<StrengthPower>(context,own,2,own,null);
        await PowerCmd.Apply<SnowPower>(context,own,3,own,null);Assert(own.GetPowerAmount<SnowPower>()==10,"primer doubles actual gain including strength exactly once");
        await PowerCmd.Apply<SnowPower>(context,own,3,own,null);Assert(own.GetPowerAmount<SnowPower>()==15,"second snow gain is not doubled");
        await primer.BeforeSideTurnStart(context,CombatSide.Player,new[]{own},combat);await PowerCmd.Apply<SnowPower>(context,own,3,own,null);Assert(own.GetPowerAmount<SnowPower>()==25,"primer refreshes next player turn");
        var diploma=Relic<FrostDiploma>();await CreatureCmd.GainBlock(enemy,20,ValueProp.Unpowered,null);
        await CreatureCmd.Damage(context,enemy,3,ValueProp.Unpowered,own);await CreatureCmd.Damage(context,enemy,3,ValueProp.Unpowered,own);
        Assert(enemy.GetPowerAmount<FrostPower>()==2,"diploma applies frost per hit including blocked damage");
        await PowerCmd.Apply<ArtifactPower>(context,enemy,1,enemy,null);await CreatureCmd.Damage(context,enemy,3,ValueProp.Unpowered,own);
        Assert(enemy.GetPowerAmount<FrostPower>()==2 && !enemy.HasPower<ArtifactPower>(),"diploma frost is stopped by artifact");
        enemy.LoseBlockInternal(enemy.Block);var key=Relic<IceKey>();await key.BeforeCombatStart();
        await PowerCmd.Apply<FrostPower>(context,enemy,FrostActions.Threshold(enemy),own,null);int before=enemy.CurrentHp;
        await enemy.GetPower<FrostPower>()!.ResolveFreeze(context,player);
        Assert(enemy.CurrentHp==before-12,"ice key deals twelve to the frozen enemy on actual freeze");
        await key.Frozen(context,enemy);Assert(enemy.CurrentHp==before-12,"ice key only triggers once this turn");
        await key.BeforeSideTurnStart(context,CombatSide.Player,new[]{own},combat);await key.Frozen(context,enemy);Assert(enemy.CurrentHp==before-24,"ice key refreshes each player turn");
        foreach(var r in player.Relics.OfType<FrostRelic>())
        {Assert(r.Icon!=null && !r.DynamicDescription.GetFormattedText().Contains('{'),r.GetType().Name+" has valid image and localization");}
        Assert(player.Relics.OfType<FrostRelic>().Count()==7,"seven new relics registered and usable");
        typeof(Node).Assembly.GetType("Godot.Bridge.ScriptManagerBridge")!.GetMethod("LookupScriptsInAssembly",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(null,new object[]{typeof(NHealthBar).Assembly});
        var bg=new ColorRect {Color=new Color("202A38"),Size=new Vector2(1000,640)};tree.Root.AddChild(bg);
        var health=ResourceLoader.Load<PackedScene>("res://scenes/combat/health_bar.tscn").Instantiate<NHealthBar>();tree.Root.AddChild(health);health.Position=new Vector2(350,140);health.Scale=new Vector2(2,2);health.SetCreature(own);health.RefreshValues();
        var badge=health.GetNode<Control>("FrostArmorBadge");
        Assert(badge.Visible && health.GetNode<Control>("%HpForeground").SelfModulate==IceArmorDisplay.ArmorColor,"armor icon visible and HP fill blue-white without block");
        await CreatureCmd.GainBlock(own,5,ValueProp.Unpowered,null);health.RefreshValues();
        Assert(badge.Visible && health.GetNode<Control>("%HpForeground").SelfModulate==new Color("3B6FA3"),"block overrides armor color while armor number remains visible");
        Assert(badge.GetNode<Label>("Amount").Text=="3","right-side badge displays current armor");
        own.LoseBlockInternal(own.Block);health.RefreshValues();
        var card=ResourceLoader.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<Control>();
        var cost=card.GetNode<TextureRect>("%EnergyIcon");var slash=cost.GetNode<Control>("UnplayableEnergyIcon");
        FrostArtLayout.FitCardEnergy(cost,ModelDb.Card<FrostStrike>());FrostArtLayout.FitCardEnergy(cost,ModelDb.Card<FrostStrike>());
        Assert(Math.Abs((slash.Position+slash.Size/2).X-cost.Size.X/2)<0.1f && Math.Abs((slash.Position+slash.Size/2).Y-(cost.Size.Y/2-3))<0.1f,"unplayable slash centered over resized cost with no cumulative drift");
        cost.GetParent().RemoveChild(cost);card.Free();tree.Root.AddChild(cost);cost.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);cost.Position=new Vector2(720,90);cost.Size=new Vector2(118.4f,118.4f);cost.Texture=ModelDb.Card<FrostStrike>().EnergyIcon;slash.Show();
        int index=0;foreach(var r in player.Relics.OfType<FrostRelic>())
        {
            var icon=new TextureRect {Texture=r.Icon,ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,Position=new Vector2(60+index*120,290),Size=new Vector2(64,64)};tree.Root.AddChild(icon);index++;
        }
        if(DisplayServer.GetName()!="headless")
        {await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../work/ui083.png"));}
        await PowerCmd.Remove(own.GetPower<IceArmorPower>()!);health.RefreshValues();Assert(!badge.Visible,"armor badge disappears at zero");
        FrostArtLayout.FitCardEnergy(cost,null);Assert(slash.OffsetLeft==8 && slash.OffsetTop==8,"pooled card restores native slash offsets");
        health.Free();GD.Print("REVISION083_COMPLETE");
    }
}
