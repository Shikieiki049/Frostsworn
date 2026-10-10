using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models.Powers;
namespace Frostsworn;
public static class RevisedEffects
{
    public static int BaseCost(CardModel c)=>c.EnergyCost.CostsX?0:Math.Max(0,c.EnergyCost.GetWithModifiers(CostModifiers.None));
    public static async Task Play(FrostCard card,PlayerChoiceContext context,CardPlay play)
    {
        var p=card.Owner;var own=p.Creature;var target=play.Target;
        int amount=(int)card.DynamicVars["Amount"].BaseValue,extra=(int)card.DynamicVars["Extra"].BaseValue;
        decimal damage=card.DynamicVars.Damage.BaseValue;
        Task Gain<T>(int n) where T:PowerModel=>FrostActions.Gain<T>(context,p,n,card);
        async Task<List<CardModel>> Store(int n,bool required=false)
        {
            if(!own.HasPower<ColdStoragePower>())await Gain<ColdStoragePower>(1);
            return await ColdStorage.Store(context,p,card,n,required);
        }
        async Task StorePile(CardPile from,int n,bool attacks=false,bool required=false)
        {
            if(!own.HasPower<ColdStoragePower>())await Gain<ColdStoragePower>(1);
            await ColdStorage.StoreFromPile(context,p,from,n,attacks,required);
        }
        async Task<int> Hit(decimal d,int hits=1,Creature? victim=null,bool all=false,bool random=false)
        {
            var command=DamageCmd.Attack(d).FromCard(card,play).WithHitCount(hits);
            if(random)command.TargetingRandomOpponents(own.CombatState!,true);
            else if(all)command.TargetingAllOpponents(own.CombatState!);
            else if((victim??target) is {} t)command.Targeting(t);else return 0;
            FrostCardVfx.ConfigureAttack(card,command);
            FrostCardAudio.ConfigureAttack(card,command);
            var result=await command.Execute(context);
            return result.Results.SelectMany(x=>x).Sum(r=>r.TotalDamage+r.OverkillDamage);
        }
        bool thinReady=target?.GetPowerAmount<FrostPower>()>0;
        int dealt=0;
        if(card is FrostShatter)
        {
            if(target==null || target.GetPowerAmount<FrostPower>()<3)return;
            await FrostActions.Shatter(context,target,3,card);await Gain<StrengthPower>(amount);
        }
        if(card is Snowline)await Gain<SelfFrostPower>(2);
        if(card is FrostBite && target!=null)
        {
            for(int i=0;i<amount;i++)await FrostActions.Frost(context,target,2,card);
            damage+=extra*FinalEvents.Ledger(p).FrostApplications;
        }
        if(card is ShieldCounter)damage+=own.GetPowerAmount<IceArmorPower>();
        if(card is ColdHammer)damage+=ColdStorage.Pile(p).Cards.Sum(BaseCost)*amount;
        if(card is SnowSweep)
        {
            foreach(var enemy in own.CombatState!.GetOpponentsOf(own).Where(c=>!c.IsDead).ToArray())
                await Hit(damage+await FrostActions.Shatter(context,enemy,3,card)*amount,victim:enemy);
        }
        else if(card.Spec.Damage>0)
            dealt=await Hit(damage,card is TwinBlades or Crystallize?2:card is Pierce?amount:1,all:card.Spec.Target==TargetType.AllEnemies,random:false);
        if(card.Spec.Block>0)await CreatureCmd.GainBlock(own,card.DynamicVars.Block,play);
        switch(card)
        {
            case FlowingPower: await Gain<FlowingPowerPower>(1);break;
            case HypothermiaStrike: if(target!=null)await FrostActions.Frost(context,target,dealt,card);break;
            case SnowUnsheathed: if(target!=null)await FrostActions.Frost(context,target,FinalEvents.Ledger(p).DebuffLayers*amount,card);break;
            case IceBreak:
                foreach(var foe in own.CombatState!.GetOpponentsOf(own).Where(c=>!c.IsDead).ToArray())
                {
                    await FrostActions.Frost(context,foe,amount,card);
                    int frost=foe.GetPowerAmount<FrostPower>();
                    if(frost>0){await PowerCmd.Remove(foe.GetPower<FrostPower>()!);await Hit(frost*extra,victim:foe);}
                }
                break;
            case WhiteNightRush:
                if(ColdStorage.Pile(p).Cards.Count<ColdStorage.Capacity(p))
                {
                    if(!own.HasPower<ColdStoragePower>())await Gain<ColdStoragePower>(1);
                    await CardPileCmd.Add(card,Entry.ColdPileType);await ExpansionEvents.Stored(context,p);
                }
                break;
            case Pierce:
                await CardPileCmd.AddGeneratedCardToCombat(own.CombatState!.CreateCard<Depleted>(p),PileType.Draw,p);
                await CardPileCmd.AddGeneratedCardToCombat(own.CombatState!.CreateCard<Depleted>(p),PileType.Discard,p);break;
            case SnowCharge:
                if(own.GetPower<SnowPower>() is {} snow)snow.SetAmount(snow.Amount*2);
                break;
            case IceCrystal: if(target!=null)await FrostActions.Frost(context,target,amount+own.GetPowerAmount<GlacialCorePower>(),card);break;
            case FreezeRay: if(target!=null)await FrostActions.Frost(context,target,amount,card);break;
            case AbsoluteBeam: if(target!=null)await FrostActions.Frost(context,target,card.IsUpgraded?FrostActions.Threshold(target):(int)Math.Ceiling(FrostActions.Threshold(target)/2m),card);break;
            case CrystalRite: await StorePile(p.PlayerCombatState!.DrawPile,1);break;
            case CrystalCluster or IceHarvest: await FrostActions.Crystals(p,amount);break;
            case Crystallize:
                if(ReferenceEquals(ColdStorage.State(card).ActivePlay,play))
                {if(target is {IsDead:false})await PowerCmd.Apply<VulnerablePower>(context,target,amount,own,card);await Gain<SelfFrostPower>(1);}break;
            case RimeCoat: await Gain<IceArmorPower>(amount);break;
            case SnowCache:
                foreach(var stored in await Store(1,true))await Gain<SnowPower>(BaseCost(stored)*amount);break;
            case MeltSeal: await ColdStorage.Thaw(context,p,1);break;
            case WakeIce: await ColdStorage.Thaw(context,p,2,random:!card.IsUpgraded);await Gain<SelfFrostPower>(2);break;
            case OverdrawWarmth: await PlayerCmd.GainEnergy(amount,p);await Gain<SelfFrostPower>(3);break;
            case RapidCalculation: await CardPileCmd.Draw(context,amount,p);await Gain<SelfFrostPower>(1);break;
            case Rewarm: await ExpansionEvents.Spend<SelfFrostPower>(context,p,amount,card);break;
            case SnowRoll: await Gain<SnowPower>(amount);break;
            case ExpandColdStore: await Gain<ColdExpansionPower>(amount);break;
            case SlowRelease: await Gain<SlowReleasePower>(amount);break;
            case ThinIce: if(thinReady)await Gain<IceArmorPower>(dealt);break;
            case TwinBlades:
                if(target!=null && !target.IsDead && await FrostActions.Shatter(context,target,1,card)>0)await Hit(damage,amount);break;
            case Avalanche: if(target!=null)await FrostActions.Frost(context,target,amount,card);await Gain<SelfFrostPower>(5);break;
            case MeltSlash: await ColdStorage.SelectThaw(context,p,amount);break;
            case WhiteMist:
                if(target is {IsDead:false}){await FrostActions.Frost(context,target,4,card);await PowerCmd.Apply<WeakPower>(context,target,amount,own,card);}break;
            case IceBind: if(target!=null)await FrostActions.Frost(context,target,amount,card);await Store(1);break;
            case AbsoluteZero:
                foreach(var enemy in own.CombatState!.GetOpponentsOf(own).Where(c=>!c.IsDead).ToArray())await FrostActions.Frost(context,enemy,amount,card);break;
            case Snowline: await Gain<SnowPower>(dealt/2);break;
            case ShellRecycle:
                int spent=await ExpansionEvents.Spend<IceArmorPower>(context,p,int.MaxValue,card);
                if(spent>0)await CreatureCmd.GainBlock(own,spent*2,ValueProp.Move,play);break;
            case SealSpell: await CardPileCmd.Draw(context,amount,p);await Store(1,true);break;
            case DeepCache: if((await Store(1)).Count>0)await CardPileCmd.Draw(context,amount,p);break;
            case BlizzardComing: await Gain<SnowPower>(amount);await Gain<SelfFrostGuardPower>(extra);break;
            case ThawFrost:
                await Gain<IceArmorPower>(amount);
                if(target!=null)await Gain<IceArmorPower>(2*await FrostActions.Shatter(context,target,extra,card));break;
            case ColdReturn: await Gain<SnowPower>(amount);await Gain<IceArmorPower>(own.GetPowerAmount<SnowPower>()*extra);break;
            case StepSnow: await CardPileCmd.Draw(context,Math.Min(amount,ColdStorage.Pile(p).Cards.Count),p);break;
            case SpringFlood: await ColdStorage.PlayStored(context,p);break;
            case GlacierBody: await Gain<GlacierBodyPower>(1);await Gain<IceArmorPower>(amount);break;
            case StormCore: await Gain<StormCorePower>(amount);break;
            case SnowEye: await Gain<SnowEyePower>(amount);break;
            case IceMirror: await Gain<IceMirrorPower>(amount);break;
            case PrismBlast:
                int consumed=await Consume(card,context,amount);
                for(int i=0;i<consumed;i++)await Gain<SnowPower>(extra);break;
            case IceChisel: if(target is {IsDead:false} && target.GetPowerAmount<FrostPower>()>=2)await PowerCmd.Apply<VulnerablePower>(context,target,amount,own,card);break;
            case Frostfire: await Gain<SelfFrostPower>(1);break;
            case CrystalVolley: await FrostActions.Crystals(p,Math.Max(0,CardPile.MaxCardsInHand-p.PlayerCombatState!.Hand.Cards.Count),card.IsUpgraded);break;
            case IceDust: await Gain<IceArmorPower>(amount);await Gain<SnowPower>(extra);await Consume(card,context,1,required:true);break;
            case PermafrostWard: await Gain<IceArmorPower>(amount);await Store(1);break;
            case WarmthRecovery: await FrostActions.Crystals(p,await Consume(card,context,amount));break;
            case ThawEnergy: await PlayerCmd.GainEnergy(await ColdStorage.SelectThaw(context,p,amount),p);await Gain<SelfFrostPower>(1);break;
            case CrystalFurnace:
                int burned=await Consume(card,context,2,crystalsOnly:true);
                if(burned>0){await PlayerCmd.GainEnergy(burned,p);await Gain<SelfFrostPower>(burned);}break;
            case CrystalEdge: await Gain<CrystalEdgePower>(amount);break;
            case GlacialCore: await Gain<GlacialCorePower>(amount);break;
            case CrystalAmulet: await Gain<CrystalAmuletPower>(amount);break;
            case IceCellar: await Gain<IceCellarPower>(amount);break;
            case DelayedChill: if(target!=null)await FrostActions.Frost(context,target,ReferenceEquals(ColdStorage.State(card).ActivePlay,play)?extra:amount,card);break;
            case PiercingCold: int storedCount=(await Store(amount)).Count;if(storedCount>0)await Hit(damage,storedCount);break;
            case SnowFinale: await CreatureCmd.Heal(own,FinalEvents.Ledger(p).DebtLifeLost);break;
            case SpreadFrost:
                var enemies=own.CombatState!.GetOpponentsOf(own).Where(c=>!c.IsDead).ToArray();
                foreach(var enemy in enemies)await FrostActions.Frost(context,enemy,5,card);
                await Gain<SnowPower>(enemies.Sum(e=>e.GetPowerAmount<FrostPower>()/5)*amount);break;
            case FrostCarving:
                foreach(var stored in await Store(1,true))ColdStorage.State(stored).CarvedCrystals=amount;
                await Gain<ThawNextPower>(1);own.GetPower<ThawNextPower>()!.DueTurn=p.PlayerCombatState!.TurnNumber+1;break;
            case FrozenMoment: await Store(amount);await StorePile(p.PlayerCombatState!.DiscardPile,amount);await StorePile(p.PlayerCombatState!.DrawPile,amount);break;
            case PhaseShift: if(target!=null)await Gain<IceArmorPower>(amount*await FrostActions.Shatter(context,target,10,card));break;
            case ColdSearch: await StorePile(p.PlayerCombatState!.DrawPile,amount,attacks:true);break;
            case BloodWinter: await Gain<SelfFrostPower>(3);await PlayerCmd.GainEnergy(1,p);await CardPileCmd.Draw(context,amount,p);await Store(extra);break;
            case DebtSettlement:
                int paid=await ExpansionEvents.Spend<SelfFrostPower>(context,p,int.MaxValue,card);if(paid>0)await CardPileCmd.Draw(context,paid,p);break;
            case QuietMeditation:
                await Gain<EnergyNextTurnPower>(amount);await Gain<ArmorNextPower>(extra);
                own.GetPower<ArmorNextPower>()!.DueTurn=p.PlayerCombatState!.TurnNumber+1;break;
            case CrystalResonance: await Gain<CrystalResonancePower>(amount);break;
            case WinterArchive: await Gain<WinterArchivePower>(amount);break;
            case EndlessStorm: await Gain<EndlessStormPower>(1);own.GetPower<EndlessStormPower>()!.Improve(amount);break;
            case SixfoldSnow: await Gain<SixfoldSnowPower>(amount);break;
            case HiddenBlade: await Gain<HiddenBladePower>(amount);break;
            case ColdBloodEcho: await Gain<ColdBloodEchoPower>(amount);break;
            case HeatExchange: await Gain<HeatExchangePower>(amount);break;
            case SnowBloom: await StorePile(p.PlayerCombatState!.ExhaustPile,3);break;
            case Dissolve: await ColdStorage.SelectThaw(context,p,int.MaxValue);break;
            case SilverFrost: await Gain<SnowPower>(amount);break;
            case FrozenWish:
                await StorePile(p.PlayerCombatState!.DrawPile,amount,required:true);
                await Gain<ThawNextPower>(amount);own.GetPower<ThawNextPower>()!.DueTurn=p.PlayerCombatState!.TurnNumber+1;break;
            case Glitter: await FrostActions.Crystals(p,2,card.IsUpgraded,sharp:true);break;
            case BlessingWind: await Gain<BlessingWindPower>(1);await Gain<DexterityPower>(1);break;
            case HeartInscription: await Gain<HeartInscriptionPower>(1);await Gain<StrengthPower>(1);break;
            case FatedStory: await Gain<FatedStoryPower>(1);break;
            case IceRelease: await Gain<IceReleasePower>(card.ResolveEnergyXValue()+(card.IsUpgraded?1:0));break;
            case GuardAdvance:
                int x=card.ResolveEnergyXValue();
                for(int i=0;i<x;i++)await Gain<IceArmorPower>(amount);
                for(int i=0;i<x;i++)await Gain<SnowPower>(extra);break;
        }
    }
    public static async Task<int> Consume(FrostCard source,PlayerChoiceContext context,int maximum,bool crystalsOnly=false,bool required=false)
    {
        var p=source.Owner;
        bool Eligible(CardModel c)=>c!=source && (!crystalsOnly || c is IceCrystal);
        int count=Math.Min(maximum,p.PlayerCombatState!.Hand.Cards.Count(Eligible));if(count<=0)return 0;
        var selected=(await CardSelectCmd.FromHand(context,p,new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt,required?count:0,count),Eligible,source)).ToArray();
        int done=0;
        foreach(var c in selected){if(c.Pile!=p.PlayerCombatState.Hand)continue;await CardCmd.Exhaust(context,c);if(c.Pile?.Type==PileType.Exhaust)done++;}
        return done;
    }
}
