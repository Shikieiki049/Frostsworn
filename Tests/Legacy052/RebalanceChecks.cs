using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Models.Capabilities;

namespace Frostsworn.Tests;
public static partial class Suite
{
    private static async Task RebalanceChecks(ThrowingPlayerChoiceContext context,Player player,Creature enemy,CombatState combat,TestCardSelector selector)
    {
        var own=player.Creature; var pcs=player.PlayerCombatState!;
        foreach(var power in own.Powers.Concat(enemy.Powers).ToArray()) await PowerCmd.Remove(power);
        foreach(var card in pcs.Hand.Cards.Concat(ColdStorage.Pile(player).Cards).ToArray()) await CardPileCmd.RemoveFromCombat(card);
        await CreatureCmd.SetMaxAndCurrentHp(enemy,9999);
        FrostActions.Freeze(enemy).NeedsNormalMove=false;
        selector.Cleanup();
        var source=combat.CreateCard<FrostNeedle>(player);
        async Task<T> Effect<T>(bool upgrade=false) where T:CardModel
        {
            var card=combat.CreateCard<T>(player);
            if(upgrade){card.UpgradeInternal();card.FinalizeUpgradeInternal();}
            var play=Play(card,enemy);
            var type=card is FinalCard ? typeof(FinalCard) : card is MoreCard ? typeof(MoreCard) : card is ExpansionCard ? typeof(ExpansionCard) : typeof(FrostCard);
            await (Task)type.GetMethod("OnPlay",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(card,[context,play])!;
            return card;
        }
        await PowerCmd.Apply<ArtifactPower>(context,enemy,2,enemy,null);
        await FrostActions.Frost(context,enemy,6,source);
        Assert(enemy.GetPowerAmount<ArtifactPower>()==1 && !enemy.HasPower<FrostPower>(),"Artifact blocks hidden frost and spends exactly one charge");
        await FrostActions.Frost(context,enemy,99,source);
        Assert(!enemy.HasPower<ArtifactPower>() && !enemy.HasPower<FrostPower>(),"Artifact blocks entire threshold-crossing frost application");
        await FrostActions.Frost(context,enemy,5,source);
        await PowerCmd.Apply<ArtifactPower>(context,enemy,1,enemy,null);
        await FrostActions.Shatter(context,enemy,2,source);
        Assert(enemy.GetPowerAmount<ArtifactPower>()==1 && enemy.GetPowerAmount<FrostPower>()==3,"shattering existing frost does not spend Artifact");
        await FrostActions.Frost(context,enemy,6,source);
        Assert(!enemy.HasPower<ArtifactPower>() && enemy.GetPowerAmount<FrostPower>()==3,"Artifact blocks additional stacks of existing hidden frost");
        await PowerCmd.Remove(enemy.GetPower<FrostPower>()!);
        await PowerCmd.Apply<ArtifactPower>(context,enemy,2,enemy,null);
        int hp=enemy.CurrentHp; await Effect<Crystallize>();
        Assert(enemy.CurrentHp==hp-16 && !enemy.HasPower<ArtifactPower>() && !enemy.HasPower<FrostPower>(),"prism cleave deals two hits and each frost application meets Artifact");
        hp=enemy.CurrentHp; await Effect<FreezeRay>();
        Assert(enemy.CurrentHp==hp-5 && enemy.GetPowerAmount<FrostPower>()==8,"freeze ray is now an attack with high frost application");
        hp=enemy.CurrentHp; await Effect<WhiteMist>();
        Assert(enemy.CurrentHp==hp-7 && enemy.GetPowerAmount<FrostPower>()==11 && enemy.GetPowerAmount<WeakPower>()==1,"mist blade attacks then applies frost and Weak");
        await FrostActions.Gain<SnowPower>(context,player,5,source);
        hp=enemy.CurrentHp; await Effect<Snowline>();
        Assert(enemy.CurrentHp==hp-6 && enemy.GetPowerAmount<FrostPower>()==14 && own.GetPowerAmount<IceArmorPower>()==5,"snowline combines area attack with its snow threshold reward");
        await PowerCmd.Remove(own.GetPower<IceArmorPower>()!);
        var forgeFuel=combat.CreateCard<FrostDefend>(player); await CardPileCmd.Add(forgeFuel,PileType.Hand);
        selector.PrepareToSelect(new[]{forgeFuel}); hp=enemy.CurrentHp; await Effect<PrismBlast>();
        Assert(enemy.CurrentHp==hp-18 && forgeFuel.Pile?.Type==PileType.Exhaust && pcs.Hand.Cards.OfType<IceCrystal>().Count()==2,"prism forging exhausts a selected card to generate two crystals");
        selector.Cleanup();
        foreach(var card in pcs.Hand.Cards.ToArray()) await CardPileCmd.RemoveFromCombat(card);

        foreach(var type in typeof(FrostCard).Assembly.GetTypes().Where(t=>!t.IsAbstract && typeof(FrostCard).IsAssignableFrom(t)))
        {
            var card=ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();
            var before=card.DynamicVars.ToDictionary(v=>v.Key,v=>v.Value.BaseValue);
            card.UpgradeInternal();
            Assert(card.DynamicVars.All(v=>v.Value.WasJustUpgraded == (v.Value.BaseValue != before[v.Key])),type.Name+" only changed dynamic values marked upgraded");
        }
        var unchanged=combat.CreateCard<FrostNeedle>(player); unchanged.UpgradeInternal();
        string desc=unchanged.GetDescriptionForUpgradePreview();
        Assert(desc.Contains("[green]11[/green]") && !desc.Contains("[green]4[/green]"),"upgrade preview highlights damage but not unchanged frost");
        var costOnly=combat.CreateCard<IceCellar>(player); costOnly.UpgradeInternal();
        Assert(!costOnly.GetDescriptionForUpgradePreview().Contains("[green]"),"cost-only upgrade leaves description numbers uncolored");
        foreach(var type in typeof(FrostPowerBase).Assembly.GetTypes().Where(t=>!t.IsAbstract && typeof(FrostPowerBase).IsAssignableFrom(t)))
        {
            var power=(FrostPowerBase)ModelDb.GetById<PowerModel>(ModelDb.GetId(type));
            Assert(ResourceLoader.Load<Texture2D>(power.CustomIconPath) is CompressedTexture2D,type.Name+" distinct vector icon imports as native texture");
        }

        await Effect<CrystalEdge>();
        await CreatureCmd.GainBlock(enemy,20,ValueProp.Unpowered,null);
        hp=enemy.CurrentHp; await Effect<IceCrystal>();
        GD.Print($"CRYSTAL_PIERCE hp loss={hp-enemy.CurrentHp} block={enemy.Block} edge={own.GetPowerAmount<CrystalEdgePower>()}");
        Assert(enemy.CurrentHp==hp-5 && enemy.Block==20,"Crystal Edge ice crystal ignores block with revised plus-three damage");
        hp=enemy.CurrentHp; await Effect<FrostStrike>();
        Assert(enemy.CurrentHp==hp && enemy.Block==14,"Crystal Edge does not let other attacks bypass block");
        await Effect<CrystalEdge>(true);
        hp=enemy.CurrentHp; await Effect<IceCrystal>();
        Assert(enemy.CurrentHp==hp-10 && enemy.Block==14,"stacked normal and upgraded Crystal Edge add three plus five damage");
        await Effect<GlacialCore>();
        int frost=enemy.GetPowerAmount<FrostPower>(); await Effect<IceCrystal>();
        Assert(enemy.GetPowerAmount<FrostPower>()==frost+2,"Glacial Core adds one frost to an ice crystal");
        await Effect<GlacialCore>(true);
        frost=enemy.GetPowerAmount<FrostPower>(); await Effect<IceCrystal>();
        Assert(enemy.GetPowerAmount<FrostPower>()==frost+4,"normal and upgraded Glacial Core stack to four total frost per crystal");
        await PowerCmd.Apply<ArtifactPower>(context,enemy,1,enemy,null);
        frost=enemy.GetPowerAmount<FrostPower>(); await Effect<IceCrystal>();
        Assert(enemy.GetPowerAmount<FrostPower>()==frost && !enemy.HasPower<ArtifactPower>(),"Artifact blocks merged crystal frost including all core bonuses with one charge");
        var previewCrystal=combat.CreateCard<IceCrystal>(player); await CardPileCmd.Add(previewCrystal,PileType.Hand);
        previewCrystal.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemy,previewCrystal.DynamicVars);
        Assert(previewCrystal.GetDescriptionForPile(PileType.Hand,enemy).Contains("[green]4[/green]层寒霜"),"ice crystal tooltip previews increased frost without changing base value");
        Assert(previewCrystal.DynamicVars["Amount"].BaseValue==1,"Glacial Core preview cannot double-apply the gameplay bonus");
        await CardPileCmd.RemoveFromCombat(previewCrystal);
        await CreatureCmd.LoseBlock(context,enemy,enemy.Block,own);
        await FrostActions.Gain<IceArmorPower>(context,player,10,source);
        hp=enemy.CurrentHp; await Effect<IceArmor>();
        Assert(enemy.CurrentHp==hp-22 && own.GetPowerAmount<IceArmorPower>()==10,"ice armor thrust uses armor as damage without spending it");
        await FrostActions.Gain<SelfFrostPower>(context,player,2,source);
        hp=enemy.CurrentHp; await Effect<TwinBlades>();
        Assert(enemy.CurrentHp==hp-12 && own.GetPowerAmount<SelfFrostPower>()==1,"twin blades removes one self-frost for a third hit");
        hp=enemy.CurrentHp; int armor=own.GetPowerAmount<IceArmorPower>(); await Effect<ThinIce>();
        Assert(enemy.CurrentHp==hp-4 && own.GetPowerAmount<IceArmorPower>()==armor+4,"thin ice rewards striking a frosted target");

        await FrostActions.Gain<ColdStoragePower>(context,player,1,source);
        var cargo1=combat.CreateCard<ColdHammer>(player); var cargo2=combat.CreateCard<FrostStrike>(player);
        await CardPileCmd.Add(cargo1,Entry.ColdPileType); await CardPileCmd.Add(cargo2,Entry.ColdPileType);
        hp=enemy.CurrentHp; await Effect<ColdHammer>();
        Assert(enemy.CurrentHp==hp-23,"cold hammer scales with stored base costs rather than count");
        foreach(var card in pcs.Hand.Cards.ToArray()) await CardPileCmd.RemoveFromCombat(card);
        int hand=pcs.Hand.Cards.Count; await Effect<StepSnow>();
        Assert(pcs.Hand.Cards.Count==hand+2,"step snow draws two with two stored cards");
        foreach(var card in pcs.Hand.Cards.ToArray()) await CardPileCmd.RemoveFromCombat(card);
        var cargo3=combat.CreateCard<FrostDefend>(player); await CardPileCmd.Add(cargo3,PileType.Hand);
        selector.PrepareToSelect(new[]{cargo3}); await Effect<DeepCache>();
        Assert(cargo3.Pile==ColdStorage.Pile(player) && pcs.Hand.Cards.Count==1,"hidden blade attack stores one actual card before drawing");
        selector.Cleanup();
        foreach(var card in pcs.Hand.Cards.ToArray()) await CardPileCmd.RemoveFromCombat(card);
        await Effect<IceCellar>();
        hand=pcs.Hand.Cards.Count;
        await ColdStorage.Thaw(context,player,2,random:true);
        Assert(pcs.Hand.Cards.Count==hand+4,"Ice Cellar draws for each of two actual thaws in one turn");
        foreach(var card in pcs.Hand.Cards.ToArray()) await CardPileCmd.RemoveFromCombat(card);
        while(ColdStorage.Pile(player).Cards.Count < ColdStorage.Capacity(player))
            await CardPileCmd.Add(combat.CreateCard<FrostDefend>(player),Entry.ColdPileType);
        await Effect<DeepCache>();
        Assert(pcs.Hand.Cards.Count==0,"full cold storage cannot turn hidden blade into a free draw");
        await Effect<CrystalResonance>(); await Effect<CrystalResonance>(true);
        foreach(var card in pcs.Hand.Cards.ToArray()) await CardPileCmd.RemoveFromCombat(card);
        await FrostActions.Crystals(player,1);
        Assert(pcs.Hand.Cards.OfType<IceCrystal>().Count()==4,"stacked resonance adds amounts once per generation batch");
        await FrostActions.Crystals(player,1);
        Assert(pcs.Hand.Cards.OfType<IceCrystal>().Count()==8,"uncapped resonance handles repeated batches without recursion");
        await BugfixChecks(context,player,enemy,combat);
    }
}
