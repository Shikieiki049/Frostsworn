using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Frostsworn;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Models.Capabilities;
using Godot;

namespace Frostsworn.Tests;
public static partial class Suite
{
    private static async Task FinalChecks(ThrowingPlayerChoiceContext context,Player player,Creature enemy,CombatState combat,TestCardSelector selector)
    {
        var own=player.Creature;
        var pcs=player.PlayerCombatState!;
        foreach(var power in own.Powers.ToArray()) await PowerCmd.Remove(power);
        foreach(var power in enemy.Powers.ToArray()) await PowerCmd.Remove(power);
        await CreatureCmd.SetMaxAndCurrentHp(own,70);
        foreach(var card in ColdStorage.Pile(player).Cards.ToArray()) await CardPileCmd.Add(card,PileType.Discard);
        async Task ClearHand() { foreach(var card in pcs.Hand.Cards.ToArray()) await CardPileCmd.Add(card,PileType.Discard); selector.Cleanup(); }
        await ClearHand();
        var source=combat.CreateCard<FrostNeedle>(player);
        async Task<T> Effect<T>(bool upgraded=false) where T:CardModel
        {
            var card=combat.CreateCard<T>(player);
            if(upgraded) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
            await (Task)typeof(FinalCard).GetMethod("OnPlay",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(card,[context,Play(card,enemy)])!;
            return card;
        }
        async Task Attack(CardModel card)
        {
            var play=Play(card,enemy);
            await ModelCapabilities.Get(card).GetOrCreate<ThawDiscount>().BeforeCardPlayed(play);
            var type=card is FinalCard ? typeof(FinalCard) : card is MoreCard ? typeof(MoreCard) : card is ExpansionCard ? typeof(ExpansionCard) : typeof(FrostCard);
            await (Task)type.GetMethod("OnPlay",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(card,[context,play])!;
        }
        await FrostActions.Gain<IceArmorPower>(context,player,12,source);
        selector.PrepareToSelect(new[]{0});
        await Effect<PhaseShift>();
        Assert(own.GetPowerAmount<IceArmorPower>()==2 && enemy.GetPowerAmount<FrostPower>()==10,"phase shift armor-to-frost spends ten actual armor");
        selector.Cleanup(); selector.PrepareToSelect(new[]{1});
        await Effect<PhaseShift>();
        Assert(own.GetPowerAmount<IceArmorPower>()==12 && !enemy.HasPower<FrostPower>(),"phase shift frost-to-armor selects the opposite conversion");
        foreach(var type in new[]{typeof(PhaseToFrostOption),typeof(PhaseToArmorOption)})
        {
            var option=ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();
            Assert(option.Rarity==CardRarity.Token && ResourceLoader.Load<Texture2D>(option.PortraitPath)!=null,"phase choice is a localized non-reward option");
        }
        await ClearHand();
        var carved=combat.CreateCard<IceArmor>(player);
        await CardPileCmd.Add(carved,PileType.Hand);
        await CardPileCmd.Add(combat.CreateCard<FrostDefend>(player),PileType.Hand);
        selector.PrepareToSelect(new[]{carved});
        await Effect<FrostCarving>(true);
        Assert(ColdStorage.State(carved).CarvedCrystals==2 && carved.Pile==ColdStorage.Pile(player),"upgraded carving attaches reward only to successfully stored card");
        selector.Cleanup();
        await ColdStorage.Thaw(context,player,1,random:true);
        Assert(pcs.Hand.Cards.OfType<IceCrystal>().Count()==2 && ColdStorage.State(carved).CarvedCrystals==0,"carving generates two crystals and clears its one-use reward");
        selector.PrepareToSelect(new[]{carved}); await ColdStorage.Store(context,player,source,1);
        pcs.IncrementTurnNumber(); await ColdStorage.Thaw(context,player,1,random:true);
        Assert(pcs.Hand.Cards.OfType<IceCrystal>().Count()==2,"recooling the same card does not repeat carving reward");

        await ClearHand();
        var moment=await Effect<FrozenMoment>(true);
        Assert(moment.EnergyCost.GetWithModifiers(CostModifiers.None)==1,"frozen moment upgrades its energy cost from two to one");
        var draw=own.GetPower<DrawNextPower>()!;
        Assert(draw.ModifyHandDraw(player,5)==5,"delayed draw cannot activate during its creation turn");
        pcs.IncrementTurnNumber();
        Assert(draw.ModifyHandDraw(player,5)==7,"delayed draw increases next normal hand draw");
        await draw.AfterModifyingHandDraw();
        Assert(!own.HasPower<DrawNextPower>(),"delayed draw is removed after actual draw-count calculation");
        int energy=pcs.Energy;
        await Effect<QuietMeditation>(true);
        var meditation=own.GetPower<MeditationPower>()!;
        await meditation.BeforeHandDrawLate(player,context,combat);
        Assert(pcs.Energy==energy,"meditation does not pay energy early");
        pcs.IncrementTurnNumber(); await meditation.BeforeHandDrawLate(player,context,combat);
        Assert(pcs.Energy==energy+2 && own.GetPowerAmount<SnowPower>()==2 && !own.HasPower<MeditationPower>(),"meditation pays upgraded snow and energy once next turn");

        await Effect<HiddenBlade>();
        var twin=combat.CreateCard<TwinBlades>(player); ColdStorage.State(twin).Pending=true;
        int hp=enemy.CurrentHp; await Attack(twin);
        Assert(enemy.CurrentHp==hp-16,"hidden blade enhances both hits of the whole thawed attack");
        var twin2=combat.CreateCard<TwinBlades>(player); ColdStorage.State(twin2).Pending=true;
        hp=enemy.CurrentHp; await Attack(twin2);
        Assert(enemy.CurrentHp==hp-16,"hidden blade also enhances the second thawed attack this turn");
        pcs.IncrementTurnNumber(); hp=enemy.CurrentHp; await Attack(twin);
        Assert(enemy.CurrentHp==hp-8,"replaying the same card without a thaw mark gains no bonus");
        await PowerCmd.Remove(own.GetPower<HiddenBladePower>()!);
        await Effect<HiddenBlade>(true);
        var volley=combat.CreateCard<ShatterVolley>(player); ColdStorage.State(volley).Pending=true;
        await FrostActions.Frost(context,enemy,6,source); hp=enemy.CurrentHp; await Attack(volley);
        Assert(enemy.CurrentHp==hp-54,"upgraded hidden blade enhances all six shatter-volley hits");
        await Effect<SixfoldSnow>(); await Effect<CrystalResonance>();
        await ClearHand();
        var chill=combat.CreateCard<DelayedChill>(player); ColdStorage.State(chill).Pending=true;
        int frost=enemy.GetPowerAmount<FrostPower>(); await Attack(chill);
        Assert(enemy.GetPowerAmount<FrostPower>()==frost+10,"delayed chill detects consumed thaw marker during the same play");
        Assert(pcs.Hand.Cards.OfType<IceCrystal>().Count()==2,"sixfold snow combines with first-generation resonance once");
        await FrostActions.Crystals(player,1);
        Assert(pcs.Hand.Cards.OfType<IceCrystal>().Count()==4,"resonance rewards each generation batch without recursion");
        await PowerCmd.Remove(own.GetPower<CrystalResonancePower>()!);
        await Effect<HeatExchange>(true);
        await ClearHand();
        var heatCard=combat.CreateCard<IceArmor>(player);
        await CardPileCmd.Add(heatCard,PileType.Hand);
        selector.PrepareToSelect(new[]{heatCard}); await ColdStorage.Store(context,player,source,1);
        energy=pcs.Energy; await ColdStorage.Thaw(context,player,1,random:true);
        Assert(pcs.Energy==energy+1,"heat exchange checks undiscounted cost and rewards a two-cost card");

        await Effect<ShardBurst>();
        await ClearHand();
        int armor=own.GetPowerAmount<IceArmorPower>();
        if(armor>0) await PowerCmd.Remove(own.GetPower<IceArmorPower>()!);
        await FrostActions.Gain<IceArmorPower>(context,player,32,source);
        // The preview must not emit crystals or alter the burst counter.
        own.GetPower<IceArmorPower>()!.ModifyHpLostBeforeOsty(own,100,ValueProp.Move,enemy,null);
        Assert(pcs.Hand.Cards.Count==0,"armor loss previews do not generate shard-burst crystals");
        await ExpansionEvents.Spend<IceArmorPower>(context,player,24,source);
        Assert(pcs.Hand.Cards.OfType<IceCrystal>().Count()==2,"shard burst obeys two-trigger limit for large armor spend");
        await Effect<ShardBurst>(true);
        await ExpansionEvents.Spend<IceArmorPower>(context,player,8,source);
        Assert(pcs.Hand.Cards.OfType<IceCrystal>().Count()==4,"stacked burst raises shared cap to three and gives two crystals per trigger");
        pcs.IncrementTurnNumber(); await ClearHand();
        await FrostActions.Gain<IceArmorPower>(context,player,16,source);
        await own.GetPower<IceArmorPower>()!.BeforeHandDrawLate(player,context,combat);
        Assert(pcs.Hand.Cards.OfType<IceCrystal>().Count()==2,"natural melting counts as armor loss on the new turn");
        await PowerCmd.Remove(own.GetPower<ShardBurstPower>()!);

        foreach(var power in new PowerModel?[]{own.GetPower<IceArmorPower>(),own.GetPower<SelfFrostPower>()}) if(power!=null) await PowerCmd.Remove(power);
        await Effect<ColdBloodEcho>();
        int debtLoss=FinalEvents.Ledger(player).DebtLifeLost;
        await FrostActions.Gain<SelfFrostPower>(context,player,3,source);
        await own.GetPower<SelfFrostPower>()!.BeforeHandDraw(player,context,combat);
        Assert(FinalEvents.Ledger(player).DebtLifeLost==debtLoss+3 && own.GetPowerAmount<IceArmorPower>()==6,"self-frost records actual loss and cold-blood echo grants matching armor");
        await own.GetPower<IceArmorPower>()!.BeforeHandDrawLate(player,context,combat);
        Assert(own.GetPowerAmount<IceArmorPower>()==3,"echo armor participates in the following natural melt");
        hp=enemy.CurrentHp; await Effect<SnowFinale>();
        Assert(enemy.CurrentHp==hp-10-FinalEvents.Ledger(player).DebtLifeLost*2,"snow finale scales with recorded self-frost loss only");
        await FrostActions.Gain<IceArmorPower>(context,player,9,source);
        await FrostActions.Gain<SelfFrostPower>(context,player,2,source);
        await ClearHand(); await Effect<DebtSettlement>(true);
        Assert(!own.HasPower<SelfFrostPower>() && pcs.Hand.Cards.Count==2,"debt settlement draws only for actual self-frost removed");

        var snowPower=own.GetPower<SnowPower>(); if(snowPower!=null) await PowerCmd.Remove(snowPower);
        await FrostActions.Gain<SnowPower>(context,player,8,source);
        await Effect<EndlessStorm>(); await Effect<EndlessStorm>(true); await Effect<EndlessStorm>();
        await own.GetPower<SnowPower>()!.BeforeSideTurnEnd(context,CombatSide.Player,[own]);
        Assert(own.GetPowerAmount<SnowPower>()==7,"endless storm keeps strongest decay rule across upgraded and normal copies");
        await ExpansionEvents.Spend<SnowPower>(context,player,4,source);
        Assert(own.GetPowerAmount<SnowPower>()==3,"endless storm does not reduce active snow consumption");
        await Effect<WinterArchive>(); await Effect<PolarCycle>(true);
        await ClearHand(); pcs.IncrementTurnNumber();
        var archiveCard=combat.CreateCard<FrostDefend>(player);
        await CardPileCmd.Add(archiveCard,PileType.Hand);
        selector.PrepareToSelect(new[]{archiveCard});
        await own.GetPower<WinterArchivePower>()!.BeforeSideTurnEndVeryEarly(context,CombatSide.Player,[own]);
        Assert(archiveCard.Pile==ColdStorage.Pile(player),"winter archive stores before ordinary end-turn discard");
        await FrostActions.Frost(context,enemy,6,source); await FrostActions.Shatter(context,enemy,6,source);
        var exhausted=combat.CreateCard<IceCrystal>(player); await CardPileCmd.Add(exhausted,PileType.Hand); await CardCmd.Exhaust(context,exhausted);
        armor=own.GetPowerAmount<IceArmorPower>(); int snow=own.GetPowerAmount<SnowPower>();
        await own.GetPower<PolarCyclePower>()!.BeforeSideTurnEndEarly(context,CombatSide.Player,[own]);
        Assert(own.GetPowerAmount<IceArmorPower>()==armor+6 && own.GetPowerAmount<SnowPower>()==snow+3 && own.GetPowerAmount<DrawNextPower>()==1,"polar cycle recognizes real shatter/store/exhaust and pays all three branches");
        await own.GetPower<PolarCyclePower>()!.BeforeSideTurnEndEarly(context,CombatSide.Player,[own]);
        Assert(own.GetPowerAmount<DrawNextPower>()==1,"polar cycle cannot pay the same turn twice");
        await ClearHand();
        foreach(var card in ColdStorage.Pile(player).Cards.ToArray()) await CardPileCmd.Add(card,PileType.Discard);
        var searched=combat.CreateCard<FrostStrike>(player); await CardPileCmd.Add(searched,PileType.Draw);
        selector.PrepareToSelect(new[]{searched});
        await Effect<ColdSearch>();
        Assert(searched.Pile==ColdStorage.Pile(player),"cold search moves selected attack directly from draw pile to cold storage");
        selector.Cleanup();
        int selfLoss=FinalEvents.Ledger(player).DebtLifeLost;
        await CreatureCmd.SetMaxAndCurrentHp(own,70);
        int playerHp=own.CurrentHp;
        selector.PrepareToSelect(Array.Empty<CardModel>());
        await Effect<BloodWinter>();
        Assert(own.CurrentHp==playerHp-4 && FinalEvents.Ledger(player).DebtLifeLost==selfLoss,"blood winter pays direct life without inflating self-frost damage history");
        selector.Cleanup();
        foreach(var power in enemy.Powers.ToArray()) await PowerCmd.Remove(power);
        await CreatureCmd.GainBlock(enemy,4,ValueProp.Unpowered,null);
        hp=enemy.CurrentHp;
        await Effect<PiercingCold>();
        Assert(enemy.CurrentHp==hp-14 && enemy.GetPowerAmount<FrostPower>()==7,"piercing cold derives frost from unblocked attack damage");
        pcs.IncrementTurnNumber();
        var area=combat.CreateCard<SnowSweep>(player); ColdStorage.State(area).Pending=true;
        hp=enemy.CurrentHp; await Attack(area);
        Assert(enemy.CurrentHp==hp-19,"hidden blade combines with shatter bonus on an area attack");
        int spreadSnow=own.GetPowerAmount<SnowPower>();
        await Effect<SpreadFrost>();
        Assert(own.GetPowerAmount<SnowPower>()==spreadSnow+3,"spread frost rewards the existing frozen enemy");
        Assert(typeof(FrostCard).Assembly.GetTypes().Count(t=>!t.IsAbstract && typeof(FrostCard).IsAssignableFrom(t))==90,"complete playable card catalog contains exactly 90 designs");
        await RebalanceChecks(context, player, enemy, combat, selector);
    }
}
