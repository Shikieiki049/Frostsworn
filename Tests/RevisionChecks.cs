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
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Models.Capabilities;
namespace Frostsworn.Tests;
public static partial class Suite
{
    private static async Task CheckRevision()
    {
        var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
        var run=RunState.CreateForTest([player]);player.ResetCombatState();
        var combat=new CombatState(runState:run);combat.AddPlayer(player);
        var monster=(MockAttackMonster)ModelDb.Monster<MockAttackMonster>().ToMutable();
        var enemy=combat.CreateCreature(monster,CombatSide.Enemy,"revision");combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        var turn=typeof(CombatManager).GetField("_turnState",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(CombatManager.Instance)!;
        turn.GetType().GetProperty("IsInProgress")!.SetValue(turn,true);
        if(monster.MoveStateMachine==null)monster.SetUpForCombat();monster.RollMove(combat.PlayerCreatures);
        var context=new ThrowingPlayerChoiceContext();var selector=new TestCardSelector();using var selection=CardSelectCmd.UseSelector(selector);
        var own=player.Creature;var pcs=player.PlayerCombatState!;
        Assert(player.Deck.Cards.Count==10 && player.Deck.Cards.OfType<FrostStrike>().Count()==4 && player.Deck.Cards.OfType<FrostDefend>().Count()==4 && player.Deck.Cards.OfType<FreezeRay>().Count()==1 && player.Deck.Cards.OfType<CrystalRite>().Count()==1,"starter deck has four strikes, four defends, freeze ray and crystal rite");
        var types=typeof(FrostCard).Assembly.GetTypes().Where(t=>!t.IsAbstract && typeof(FrostCard).IsAssignableFrom(t)).ToArray();
        Assert(types.Length==93,"93 current card definitions and no removed cards or choice tokens");
        async Task Reset()
        {
            selector.Cleanup(); MegaCrit.Sts2.Core.Context.LocalContext.NetId=null;
            foreach(var power in own.Powers.Concat(enemy.Powers).ToArray())await PowerCmd.Remove(power);
            foreach(var c in pcs.AllPiles.Concat(new[]{ColdStorage.Pile(player)}).SelectMany(p=>p.Cards).Distinct().ToArray())await CardPileCmd.RemoveFromCombat(c);
            own.LoseBlockInternal(own.Block);enemy.LoseBlockInternal(enemy.Block);
            await CreatureCmd.SetMaxAndCurrentHp(own,1000);await CreatureCmd.SetMaxAndCurrentHp(enemy,10000);
            pcs.Energy=3;pcs.IncrementTurnNumber();FinalEvents.Ledger(player).DebtLifeLost=0;FinalEvents.Ledger(player).FrostApplications=0;
            FrostActions.Freeze(enemy).NeedsNormalMove=false;
            if(monster.NextMove.Id==MonsterModel.stunnedMoveId){await monster.PerformMove();enemy.PrepareForNextTurn(combat.PlayerCreatures);}
        }
        FrostCard New(Type t,bool upgrade=false)
        {
            var c=(FrostCard)combat.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(t)),player);
            if(upgrade&&c.MaxUpgradeLevel>0){c.UpgradeInternal();c.FinalizeUpgradeInternal();}return c;
        }
        async Task<FrostCard> Effect<T>(bool upgrade=false) where T:FrostCard
        {var c=New(typeof(T),upgrade);await RevisedEffects.Play(c,context,Play(c,enemy));return c;}
        async Task<CardModel> Cargo<T>(PileType pile)where T:CardModel
        {var c=combat.CreateCard<T>(player);await CardPileCmd.Add(c,pile);return c;}
        async Task Power<T>(int amount,Creature? receiver=null)where T:PowerModel
            =>await PowerCmd.Apply<T>(context,receiver??own,amount,enemy,null);
        if(System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST")=="074")
        {
            await Reset();var marked=await Cargo<FrostStrike>(Entry.ColdPileType);await ColdStorage.ThawCard(context,player,marked);
            Assert(!marked.GetDescriptionForPile(PileType.Hand).Contains("【物语】"),"074 ordinary thaw has no fated marker");
            await Power<FatedStoryPower>(1);
            string markedText=marked.GetDescriptionForPile(PileType.Hand);
            Assert(markedText.Contains("[gold]【物语】下次打出重放1。[/gold]")&&!markedText.Contains('{'),"074 eligible thawed card displays distinct localized next-play marker");
            MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;await CardCmd.AutoPlay(context,marked,enemy);
            Assert(enemy.CurrentHp==9988&&!marked.GetDescriptionForPile(PileType.Discard).Contains("【物语】"),"074 one next play replays once then clears marker");
            await CardPileCmd.Add(marked,PileType.Hand);await CardCmd.AutoPlay(context,marked,enemy);
            Assert(enemy.CurrentHp==9982,"074 later ordinary play is not replayed");
            await CardPileCmd.Add(marked,Entry.ColdPileType);await ColdStorage.ThawCard(context,player,marked);
            Assert(marked.GetDescriptionForPile(PileType.Hand).Contains("下次打出重放1"),"074 thawing again restores marker");
            await Power<FatedStoryPower>(1);
            Assert(marked.GetDescriptionForPile(PileType.Hand).Contains("下次打出重放2"),"074 stacked story displays actual repeat count");
            var ordinary=await Cargo<FrostDefend>(PileType.Hand);
            Assert(!ordinary.GetDescriptionForPile(PileType.Hand).Contains("【物语】"),"074 non-thawed cards have no marker");
            await PowerCmd.Remove(own.GetPower<FatedStoryPower>()!);
            Assert(!marked.GetDescriptionForPile(PileType.Hand).Contains("【物语】"),"074 removing story removes inactive marker");
            return;
        }
        if(System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST")=="073")
        {
            await Reset();await Power<BlessingWindPower>(1);await Power<HeartInscriptionPower>(1);await Power<DexterityPower>(3);await Power<StrengthPower>(4);
            foreach(var type in types)
            foreach(bool upgrade in new[]{false,true})
            {
                var c=New(type,upgrade);if(c.Type!=CardType.Power)continue;
                await CardPileCmd.Add(c,PileType.Hand);c.DynamicVars.ClearPreview();c.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,c.DynamicVars);
                Assert(c.DynamicVars["Amount"].PreviewValue==c.DynamicVars["Amount"].BaseValue&&c.DynamicVars["Extra"].PreviewValue==c.DynamicVars["Extra"].BaseValue,"073 power preview preserves base or upgraded gains: "+type.Name+" upgrade="+upgrade);
                Assert(!c.GetDescriptionForPile(PileType.Hand).Contains('{'),"073 power description renders: "+type.Name);
                await CardPileCmd.RemoveFromCombat(c);
            }
            var dust=await Cargo<IceDust>(PileType.Hand);dust.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,dust.DynamicVars);
            Assert(dust.DynamicVars["Amount"].PreviewValue==9&&dust.DynamicVars["Extra"].PreviewValue==6,"073 skill gain previews still include stats");
            await Effect<CrystalAmulet>();await Effect<SnowEye>();
            var crystal=await Cargo<IceCrystal>(PileType.Hand);MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;await CardCmd.AutoPlay(context,crystal,enemy);
            Assert(own.GetPowerAmount<IceArmorPower>()==own.GetPower<CrystalAmuletPower>()!.Amount+3,"073 crystal amulet actual armor still includes dexterity");
            Assert(own.GetPowerAmount<SnowPower>()==own.GetPower<SnowEyePower>()!.Amount+4,"073 snow eye actual snow still includes strength");
            return;
        }
        if(System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST")=="072")
        {
            foreach(bool upgrade in new[]{false,true})
            {
                await Reset();var pierce=New(typeof(Pierce),upgrade);await CardPileCmd.Add(pierce,PileType.Hand);
                MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;await CardCmd.AutoPlay(context,pierce,null);
                Assert(pierce.Pile==pcs.ExhaustPile&&pcs.DrawPile.Cards.Single() is Depleted&&pcs.DiscardPile.Cards.Single() is Depleted,"072 pierce exhausts and generates both statuses, upgrade="+upgrade);
                Assert(enemy.CurrentHp==10000-7*(upgrade?5:4),"072 pierce damage preserved");
                var release=New(typeof(IceRelease),upgrade);
                string text=release.GetDescriptionForPile(PileType.None);
                Assert(text.Contains("非X费")&&(upgrade?text.Contains("X+1"):text.Contains("下X张")),"072 ice release normal and upgraded text");
            }
            await Reset();await Power<IceReleasePower>(2);var x=await Cargo<GuardAdvance>(Entry.ColdPileType);await ColdStorage.ThawCard(context,player,x);
            Assert(x.EnergyCost.GetAmountToSpend()==3,"072 thawed X card retains full X energy payment");
            MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;await x.SpendResources();await x.OnPlayWrapper(context,null,false,default,true);
            Assert(pcs.Energy==0&&own.GetPowerAmount<IceArmorPower>()==18&&own.GetPowerAmount<SnowPower>()==18&&own.GetPowerAmount<IceReleasePower>()==2,"072 X card resolves X=3 without consuming release charges");
            var fixedCard=await Cargo<ColdHammer>(Entry.ColdPileType);await ColdStorage.ThawCard(context,player,fixedCard);pcs.Energy=3;
            Assert(fixedCard.EnergyCost.GetAmountToSpend()==0,"072 non-X thawed card still costs zero");
            await fixedCard.SpendResources();await fixedCard.OnPlayWrapper(context,enemy,false,default,true);
            Assert(pcs.Energy==3&&own.GetPowerAmount<IceReleasePower>()==1,"072 non-X play consumes exactly one release charge");
            return;
        }
        foreach(var type in types)
        {
            await Reset();var c=New(type);var desc=c.GetDescriptionForPile(PileType.None);
            Assert(!desc.Contains('{') && !string.IsNullOrWhiteSpace(c.Title),type.Name+" normal text formats");
            Assert(ResourceLoader.Load<Texture2D>(c.CustomPortraitPath)!=null,type.Name+" portrait loads");
            Assert(c.HoverTips.OfType<HoverTip>().All(t=>!t.Description.Contains('{')),type.Name+" keyword tips resolve");
            if(c.MaxUpgradeLevel>0){c.UpgradeInternal();c.FinalizeUpgradeInternal();}desc=c.GetDescriptionForPile(PileType.None);
            Assert(!desc.Contains('{'),type.Name+" upgraded text formats");
            // Exercise NCard's refresh path, not just successfully formatting a string.
            c.DynamicVars.ClearPreview();c.UpdateDynamicVarPreview(CardPreviewMode.Upgrade,null,c.DynamicVars);
            string refreshed=System.Text.RegularExpressions.Regex.Replace(c.GetDescriptionForUpgradePreview(),@"\[[^\]]*\]","");
            string expected=System.Text.RegularExpressions.Regex.Replace(c.Description.GetRawText(),@"\{(Damage|Block|Amount|Extra|Third|CalculatedDamage):diff\(\)\}",m=>c.DynamicVars[m.Groups[1].Value].PreviewValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            expected=System.Text.RegularExpressions.Regex.Replace(expected,@"\[[^\]]*\]","");
            if(!expected.Contains('{'))Assert(refreshed.Contains(expected),type.Name+" upgraded UI refresh displays every upgraded numeric variable");
            foreach(var entry in new[]{("Damage",c.Spec.Damage+c.Spec.DamageUpgrade),("Block",c.Spec.Block+c.Spec.BlockUpgrade),("Amount",c.Spec.Amount+c.Spec.AmountUpgrade),("Extra",c.Spec.Extra+c.Spec.ExtraUpgrade)})
                Assert(c.DynamicVars[entry.Item1].BaseValue==entry.Item2,type.Name+" upgraded "+entry.Item1+" matches card data");
            Assert(c.Keywords.Contains(CardKeyword.Exhaust)==(c.Spec.Exhaust&&!c.Spec.RemoveExhaustUpgrade) && c.Keywords.Contains(CardKeyword.Retain)==(c.Spec.Retain||c.Spec.RetainUpgrade) && c.Keywords.Contains(CardKeyword.Innate)==(c.Spec.Innate||c.Spec.InnateUpgrade),type.Name+" upgraded gameplay keywords match spec");
            Assert(c.EnergyCost.GetWithModifiers(CostModifiers.None)==c.Spec.Cost+c.Spec.CostUpgrade,type.Name+" upgraded cost matches spec");
            if(c.Spec.RemoveExhaustUpgrade)Assert(!desc.Contains("消耗。"),type.Name+" upgraded text removes Exhaust");
            if(c.Spec.InnateUpgrade)Assert(desc.Contains("固有"),type.Name+" upgraded text includes Innate");
            if(c.Spec.RetainUpgrade)Assert(desc.Contains("保留"),type.Name+" upgraded text includes Retain");
            if(c.EnergyCost.CostsX)await CardPileCmd.Add(c,PileType.Play);
            await RevisedEffects.Play(c,context,Play(c,enemy));
            Assert(true,type.Name+" upgraded effect executes on empty-resource boundary");
        }
        foreach(var type in typeof(FrostPowerBase).Assembly.GetTypes().Where(t=>!t.IsAbstract&&typeof(FrostPowerBase).IsAssignableFrom(t)))
        {
            await Reset();var canonical=ModelDb.GetById<PowerModel>(ModelDb.GetId(type));
            var power=canonical.ToMutable();await PowerCmd.Apply(context,power,own,2,enemy,null);
            Assert(!canonical.DumbHoverTip.Description.Contains('{') && power.HoverTips.OfType<HoverTip>().All(t=>!t.Description.Contains('{')),type.Name+" generic and live tips resolve");
            Assert(ResourceLoader.Load<Texture2D>(((FrostPowerBase)power).CustomIconPath)!=null,type.Name+" status icon loads");
        }
        await Reset();await Effect<IceCrystal>(true);
        Assert(enemy.CurrentHp==9997&&enemy.GetPowerAmount<FrostPower>()==2,"upgraded crystal deals 3 and applies 2 frost");
        await Reset();await Effect<FreezeRay>();
        Assert(enemy.CurrentHp==9997&&enemy.GetPowerAmount<FrostPower>()==10,"starter freeze ray deals three damage and ten frost");
        await Reset();await Effect<FreezeRay>(true);
        Assert(enemy.CurrentHp==9996&&enemy.GetPowerAmount<FrostPower>()==11,"upgraded starter freeze ray deals four damage and eleven frost");
        Assert(New(typeof(FreezeRay)).Rarity==CardRarity.Basic,"freeze ray uses starter rarity");
        await Reset();await Power<FrostPower>(2,enemy);await Effect<ThinIce>(true);
        Assert(enemy.CurrentHp==9993&&own.GetPowerAmount<IceArmorPower>()==7,"thin ice grants matching armor on frosted target");
        await Reset();await Power<IceArmorPower>(8);await Effect<ShieldCounter>(true);
        Assert(enemy.CurrentHp==9987&&own.GetPowerAmount<IceArmorPower>()==8,"shield counter uses armor without consuming it");
        await Reset();await Power<FrostPower>(1,enemy);await Effect<TwinBlades>(true);
        Assert(enemy.CurrentHp==9984&&!enemy.HasPower<FrostPower>(),"upgraded twin blades attacks four times with one shatter");
        await Reset();await Power<FrostPower>(2,enemy);await Effect<IceChisel>(true);
        Assert(enemy.CurrentHp==9992&&enemy.GetPowerAmount<VulnerablePower>()==2,"ice chisel applies upgraded vulnerability");
        await Reset();await Power<FrostPower>(3,enemy);await Effect<FrostShatter>(true);
        Assert(enemy.CurrentHp==9988&&own.GetPowerAmount<StrengthPower>()==2&&!enemy.HasPower<FrostPower>(),"frost shatter gains strength before attack");
        await Reset();await Power<FrostPower>(2,enemy);await Effect<FrostShatter>();
        Assert(enemy.CurrentHp==10000&&!own.HasPower<StrengthPower>(),"frost shatter condition gates the written sequence");
        await Reset();await Power<FrostPower>(3,enemy);await Effect<SnowSweep>(true);
        Assert(enemy.CurrentHp==9984&&!enemy.HasPower<FrostPower>(),"snow sweep multiplies actual shatter layers");
        await Reset();await Effect<Crystallize>(true);
        Assert(enemy.CurrentHp==9980&&!own.HasPower<SelfFrostPower>(),"crystallize deals two hits with empty storage");
        await Cargo<FrostStrike>(Entry.ColdPileType);await Cargo<FrostDefend>(Entry.ColdPileType);await Effect<Crystallize>();
        Assert(pcs.Hand.Cards.Count==0&&!own.HasPower<SelfFrostPower>(),"crystallize no longer generates crystals from storage");
        await Reset();await Cargo<GlacierBody>(Entry.ColdPileType);await Cargo<CrystalRite>(Entry.ColdPileType);await Effect<ColdHammer>(true);
        Assert(enemy.CurrentHp==9965,"cold hammer adds five per stored base energy");
        await Reset();var saved=await Cargo<FrostStrike>(PileType.Hand);for(int i=0;i<3;i++)await Cargo<FrostDefend>(PileType.Draw);
        selector.PrepareToSelect(new[]{saved});await Effect<DeepCache>(true);
        Assert(saved.Pile==ColdStorage.Pile(player)&&pcs.Hand.Cards.Count==2&&enemy.CurrentHp==9996,"hidden blade deals four, stores optionally and draws two");
        await Reset();await Effect<CrystalVolley>(true);
        Assert(pcs.Hand.Cards.Count==CardPile.MaxCardsInHand&&pcs.Hand.Cards.All(c=>c is IceCrystal&&c.IsUpgraded)&&enemy.CurrentHp==9981,"crystal volley fills hand with upgraded crystals");
        await Reset();var a=await Cargo<FrostStrike>(PileType.Hand);var b=await Cargo<FrostDefend>(PileType.Hand);
        selector.PrepareToSelect(new[]{a,b});await Effect<WarmthRecovery>(true);
        Assert(a.Pile==pcs.ExhaustPile&&b.Pile==pcs.ExhaustPile&&pcs.Hand.Cards.OfType<IceCrystal>().Count()==2,"warmth recovery exhausts selected cards into crystals");
        await Reset();await Effect<BlessingWind>();await Power<IceArmorPower>(4);
        Assert(own.GetPowerAmount<DexterityPower>()==1&&own.GetPowerAmount<IceArmorPower>()==5,"blessing wind applies dexterity to armor gain");
        await Effect<BlessingWind>();await Power<IceArmorPower>(4);
        Assert(own.GetPowerAmount<IceArmorPower>()==11,"additional blessing grants dexterity but does not double its application");
        await ExpansionEvents.Spend<IceArmorPower>(context,player,3,New(typeof(FrostDefend)));
        Assert(own.GetPowerAmount<IceArmorPower>()==8,"dexterity does not alter armor spending");
        await Reset();await Effect<HeartInscription>();await Power<SnowPower>(4);
        Assert(own.GetPowerAmount<StrengthPower>()==1&&own.GetPowerAmount<SnowPower>()==5,"heart inscription applies strength to snow gain");
        await Reset();await Power<DexterityPower>(3);await Power<StrengthPower>(4);
        var previewDust=(FrostCard)await Cargo<IceDust>(PileType.Hand);
        previewDust.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,previewDust.DynamicVars);
        Assert(previewDust.DynamicVars["Amount"].PreviewValue==6&&previewDust.DynamicVars["Extra"].PreviewValue==2,"stats alone do not modify ice armor or snow preview");
        await Power<BlessingWindPower>(1);await Power<HeartInscriptionPower>(1);
        previewDust.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,previewDust.DynamicVars);
        Assert(previewDust.DynamicVars["Amount"].PreviewValue==9&&previewDust.DynamicVars["Extra"].PreviewValue==6,"both conversion powers update the same card independently");
        Assert(previewDust.DynamicVars["Amount"].BaseValue==6&&previewDust.DynamicVars["Extra"].BaseValue==2,"gain preview never changes effect base values");
        var dustText=System.Text.RegularExpressions.Regex.Replace(previewDust.GetDescriptionForPile(PileType.Hand),@"\[[^\]]*\]","");
        Assert(dustText.Contains("9层冰甲和6层雪势"),"rendered ice dust text includes dexterity and strength gains");
        await Effect<RimeCoat>();await Effect<SilverFrost>();
        Assert(own.GetPowerAmount<IceArmorPower>()==7&&own.GetPowerAmount<SnowPower>()==17,"preview does not double apply bonuses in real effects");
        previewDust.UpgradeInternal();previewDust.FinalizeUpgradeInternal();
        previewDust.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,previewDust.DynamicVars);
        Assert(previewDust.DynamicVars["Amount"].PreviewValue==10&&previewDust.DynamicVars["Extra"].PreviewValue==7,"upgraded gains combine upgrade and stats");
        await PowerCmd.Remove(own.GetPower<BlessingWindPower>()!);await PowerCmd.Remove(own.GetPower<HeartInscriptionPower>()!);
        previewDust.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,previewDust.DynamicVars);
        Assert(previewDust.DynamicVars["Amount"].PreviewValue==7&&previewDust.DynamicVars["Extra"].PreviewValue==3,"removing conversion powers clears preview bonuses");
        await Reset();await Power<BlessingWindPower>(1);await Power<DexterityPower>(-8);
        var coatPreview=await Cargo<RimeCoat>(PileType.Hand);coatPreview.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,coatPreview.DynamicVars);
        Assert(coatPreview.DynamicVars["Amount"].PreviewValue==0,"negative dexterity clamps armor preview at zero");
        foreach(var t in new[]{typeof(HeatExchange),typeof(Snowline)})
            Assert(New(t).EnergyCost.GetWithModifiers(CostModifiers.None)==2&&New(t,true).EnergyCost.GetWithModifiers(CostModifiers.None)==2,t.Name+" costs two before and after upgrade");
        await Reset();await Effect<Snowline>();
        Assert(enemy.CurrentHp==9993&&own.GetPowerAmount<SnowPower>()==3,"snow line rounds half total damage down");
        await Reset();await CreatureCmd.GainBlock(enemy,3,ValueProp.Unpowered,null);await Effect<Snowline>(true);
        Assert(enemy.CurrentHp==9993&&own.GetPowerAmount<SnowPower>()==5,"upgraded snow line includes blocked damage");
        await Reset();await Effect<ColdBloodEcho>(true);await FinalEvents.DebtPaid(context,player,2);await FinalEvents.DebtPaid(context,player,3);
        Assert(own.GetPowerAmount<IceArmorPower>()==15,"cold blood echo rewards multiple self frost losses in same turn");
        await CreatureCmd.SetCurrentHp(own,900);await Effect<SnowFinale>(true);await Effect<SnowFinale>(true);
        Assert(own.CurrentHp==910,"upgraded snow finale heals accumulated debt on repeated plays");
        await Reset();await Effect<QuietMeditation>(true);
        Assert(own.GetPowerAmount<EnergyNextTurnPower>()==3&&own.GetPowerAmount<ArmorNextPower>()==6,"meditation uses native next energy and separate next armor");
        pcs.IncrementTurnNumber();await own.GetPower<EnergyNextTurnPower>()!.AfterEnergyReset(player);await own.GetPower<ArmorNextPower>()!.AfterPlayerTurnStart(context,player);
        Assert(pcs.Energy==6&&own.GetPowerAmount<IceArmorPower>()==6&&!own.HasPower<ArmorNextPower>()&&!own.HasPower<EnergyNextTurnPower>(),"meditation delayed effects pay once next turn");
        await Reset();await Effect<Glitter>(true);
        var sharp=(IceCrystal)pcs.Hand.Cards.First();
        Assert(pcs.Hand.Cards.Count==2&&pcs.Hand.Cards.All(c=>c.IsUpgraded&&c.Enchantment is SharpEnchantment),"glitter generates two upgraded sharp crystals");
        Assert(!sharp.GetDescriptionForPile(PileType.Hand).Contains('{')&&sharp.Enchantment!.Title.GetFormattedText()=="锐利","sharp enchantment localization formats");
        MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;
        await CardCmd.AutoPlay(context,sharp,enemy);
        Assert(enemy.GetPowerAmount<VulnerablePower>()==1&&enemy.GetPowerAmount<FrostPower>()==2&&sharp.Pile==pcs.ExhaustPile,"sharp crystal uses native autoplay enchantment and exhaust hooks");
        await Reset();await Cargo<StormCore>(Entry.ColdPileType);await Cargo<FrostDefend>(Entry.ColdPileType);
        await Cargo<FrostStrike>(Entry.ColdPileType);var storedCrystal=await Cargo<IceCrystal>(Entry.ColdPileType);
        int energy=pcs.Energy;MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;await Effect<SpringFlood>();
        Assert(ColdStorage.Pile(player).Cards.Count==0&&enemy.CurrentHp==9992&&storedCrystal.Pile==pcs.ExhaustPile&&pcs.Energy==energy,"spring flood natively autoplays storage without energy spending");
        Assert(own.GetPowerAmount<StormCorePower>()==3&&own.Block==5,"spring flood also resolves stored powers and skills");
        await Reset();await Power<HeatExchangePower>(2);await Power<SixfoldSnowPower>(4);await Power<IceCellarPower>(2);
        var cold=await Cargo<FrostDefend>(Entry.ColdPileType);var cold2=await Cargo<FrostDefend>(Entry.ColdPileType);
        for(int i=0;i<4;i++)await Cargo<FrostStrike>(PileType.Draw);
        selector.PrepareToSelect(new[]{cold,cold2});await Effect<Dissolve>();
        Assert(pcs.Energy==5&&pcs.Hand.Cards.Count==6&&own.GetPowerAmount<IceArmorPower>()==4,"thaw triggers heat and armor once, cellar for each card");
        Assert(cold.EnergyCost.GetWithModifiers(CostModifiers.All)==0,"thaw discount still applies to next play");
        await CardPileCmd.Add(cold,Entry.ColdPileType);
        Assert(await ColdStorage.ThawCard(context,player,cold)&&pcs.Energy==5,"same card thaws twice but heat exchange remains once per turn");
        await Reset();var first=await Cargo<FrostStrike>(PileType.Exhaust);var second=await Cargo<FrostDefend>(PileType.Exhaust);
        selector.PrepareToSelect(new[]{first,second});await Effect<SnowBloom>(true);
        Assert(ColdStorage.Pile(player).Cards.Count==2&&pcs.ExhaustPile.Cards.Count==0,"snow bloom moves selected exhausted cards to storage");
        await Reset();var atk=await Cargo<FrostStrike>(PileType.Draw);var skill=await Cargo<FrostDefend>(PileType.Draw);
        selector.PrepareToSelect(new[]{atk,skill});await Effect<FrozenWish>();
        Assert(ColdStorage.Pile(player).Cards.Count==2&&own.GetPowerAmount<ThawNextPower>()==2,"frozen wish stores any drawn card types and queues extra thaw");
        pcs.IncrementTurnNumber();selector.Cleanup(); MegaCrit.Sts2.Core.Context.LocalContext.NetId=null;selector.PrepareToSelect(new[]{atk});selector.PrepareToSelect(new[]{skill});
        await own.GetPower<ColdStoragePower>()!.AfterPlayerTurnStartLate(context,player);
        Assert(ColdStorage.Pile(player).Cards.Count==0&&!own.HasPower<ThawNextPower>(),"queued thaw consumed once at next natural thaw");
        await Reset();await Power<ArtifactPower>(1,enemy);var frostCard=New(typeof(FreezeRay));await FrostActions.Frost(context,enemy,8,frostCard);
        Assert(!enemy.HasPower<ArtifactPower>()&&!enemy.HasPower<FrostPower>(),"hidden frost still respects Artifact");
        await Power<ImbalancedPower>(1,enemy);await Power<IceArmorPower>(5);await CreatureCmd.Damage(context,own,5,ValueProp.Move,enemy);
        Assert(own.CurrentHp==1000&&monster.NextMove.Id==MonsterModel.stunnedMoveId,"fully absorbed ice armor still triggers Imbalanced");
        await Reset();await FrostActions.Frost(context,enemy,60,frostCard);await enemy.GetPower<FrostPower>()!.AfterCardPlayedLate(context,Play(frostCard,enemy));
        Assert(monster.NextMove.Id==MonsterModel.stunnedMoveId,"frost threshold still stuns native monster");
        await Reset();await Effect<GlacierBody>();
        Assert(own.GetPowerAmount<IceArmorPower>()==7,"glacier form starts with seven armor");
        await own.GetPower<IceArmorPower>()!.BeforeHandDrawLate(player,context,combat);
        await own.GetPower<GlacierBodyPower>()!.BeforeHandDrawLate(player,context,combat);
        Assert(own.GetPowerAmount<IceArmorPower>()==8,"glacier form stops melting and gains one per turn");
        await Reset();await Power<IceArmorPower>(11);await Effect<ShellRecycle>(true);
        Assert(!own.HasPower<IceArmorPower>()&&own.Block==22,"ice shell consumes all armor for double block");
        await Reset();await Power<SelfFrostPower>(4);for(int i=0;i<6;i++)await Cargo<FrostStrike>(PileType.Draw);
        await Effect<DebtSettlement>();
        Assert(!own.HasPower<SelfFrostPower>()&&pcs.Hand.Cards.Count==4&&FinalEvents.Ledger(player).DebtLifeLost==0,"debt settlement draws for removed debt without counting life loss");
        await Reset();await Power<FrostPower>(4,enemy);await Effect<ThawFrost>(true);
        Assert(own.GetPowerAmount<IceArmorPower>()==12&&enemy.GetPowerAmount<FrostPower>()==1,"thaw frost adds base armor and twice actual shatter");
        await Reset();await Power<FrostPower>(7,enemy);await Effect<PhaseShift>();
        Assert(own.GetPowerAmount<IceArmorPower>()==14&&!enemy.HasPower<FrostPower>(),"new phase shift consumes up to ten without choice tokens");
        await Reset();await Power<SnowPower>(2);await Effect<ColdReturn>(true);
        Assert(own.GetPowerAmount<SnowPower>()==7&&own.GetPowerAmount<IceArmorPower>()==21,"cold return gains snow before multiplying current snow");
        await Reset();var armorCargo=await Cargo<GlacierBody>(PileType.Hand);selector.PrepareToSelect(new[]{armorCargo});await Effect<SnowCache>(true);
        Assert(ColdStorage.Pile(player).Cards.Contains(armorCargo)&&own.GetPowerAmount<SnowPower>()==12,"snow cache converts actual stored base cost to snow");
        await Reset();var exhaustFuel=await Cargo<FrostStrike>(PileType.Hand);selector.PrepareToSelect(new[]{exhaustFuel});await Effect<IceDust>(true);
        Assert(own.GetPowerAmount<IceArmorPower>()==7&&own.GetPowerAmount<SnowPower>()==3&&exhaustFuel.Pile==pcs.ExhaustPile,"ice dust upgraded resources and exhaust");
        await Reset();var atk1=await Cargo<FrostStrike>(PileType.Draw);var atk2=await Cargo<IceCrystal>(PileType.Draw);await Cargo<FrostDefend>(PileType.Draw);
        selector.PrepareToSelect(new[]{atk1,atk2});await Effect<ColdSearch>(true);
        Assert(ColdStorage.Pile(player).Cards.Count==2&&pcs.DrawPile.Cards.Single() is FrostDefend&&own.Block==7,"upgraded search stores only selected attacks");
        await Reset();for(int i=0;i<5;i++)await Cargo<FrostDefend>(PileType.Draw);selector.PrepareToSelect(new[]{0});await Effect<SealSpell>(true);
        Assert(pcs.Hand.Cards.Count==3&&ColdStorage.Pile(player).Cards.Count==1,"seal spell draws four before storing one");
        await Reset();var thaw1=await Cargo<FrostStrike>(Entry.ColdPileType);var thaw2=await Cargo<FrostDefend>(Entry.ColdPileType);
        selector.PrepareToSelect(new[]{thaw1,thaw2});await Effect<ThawEnergy>(true);
        Assert(pcs.Energy==5&&own.GetPowerAmount<SelfFrostPower>()==1&&pcs.Hand.Cards.Count==2,"frozen energy counts actual selected thaws");
        await Reset();var fuel1=await Cargo<IceCrystal>(PileType.Hand);var fuel2=await Cargo<IceCrystal>(PileType.Hand);
        selector.PrepareToSelect(new[]{fuel1,fuel2});await Effect<CrystalFurnace>(true);
        Assert(pcs.Energy==5&&own.GetPowerAmount<SelfFrostPower>()==2&&pcs.ExhaustPile.Cards.Count==2,"crystal recycling exchanges two crystals for energy and debt");
        await Reset();var extraMonster=(MockAttackMonster)ModelDb.Monster<MockAttackMonster>().ToMutable();
        var extraEnemy=combat.CreateCreature(extraMonster,CombatSide.Enemy,"area-test");combat.AddCreature(extraEnemy);await CreatureCmd.SetMaxAndCurrentHp(extraEnemy,10000);
        await Power<FrostPower>(3,enemy);await Power<FrostPower>(1,extraEnemy);await Effect<SnowSweep>(true);
        Assert(enemy.CurrentHp==9984&&extraEnemy.CurrentHp==9990,"sweep calculates each target independently");
        await Effect<Snowline>();
        Assert(own.GetPowerAmount<SelfFrostPower>()==2&&own.GetPowerAmount<SnowPower>()==7,"snow line applies self frost and half total area damage converted to snow");
        await Effect<SpreadFrost>(true);
        Assert(enemy.GetPowerAmount<FrostPower>()==5&&extraEnemy.GetPowerAmount<FrostPower>()==5&&own.GetPowerAmount<SnowPower>()==15,"spread frost counts each enemy's five-layer groups");
        combat.RemoveCreature(extraEnemy);
        await Reset();await Power<HiddenBladePower>(6);var multihit=(FrostCard)await Cargo<Crystallize>(Entry.ColdPileType);
        await ColdStorage.ThawCard(context,player,multihit);MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;
        await CardCmd.AutoPlay(context,multihit,enemy);
        Assert(enemy.CurrentHp==9972&&!ColdStorage.State(multihit).Pending&&enemy.GetPowerAmount<VulnerablePower>()==2&&own.GetPowerAmount<SelfFrostPower>()==1,"hidden blade bonus applies to both hits and consumes thaw mark");
        await Reset();await Power<CrystalEdgePower>(3);await Power<GlacialCorePower>(2);await CreatureCmd.GainBlock(enemy,20,ValueProp.Unpowered,null);await Effect<IceCrystal>();
        Assert(enemy.CurrentHp==9995&&enemy.Block==20&&enemy.GetPowerAmount<FrostPower>()==3,"crystal edge and glacial core preserve penetration and merged frost");
        Assert(ResourceLoader.Load<PackedScene>("res://Frostsworn/character/selection.tscn")!=null,"character selection background remains loadable");
        await Reset();await Power<IceArmorPower>(9);await Power<ConstrictPower>(5);
        await own.GetPower<ConstrictPower>()!.AfterSideTurnEnd(context,CombatSide.Player,[own]);
        Assert(own.CurrentHp==1000&&own.GetPowerAmount<IceArmorPower>()==4,"native Constrict damage is fully absorbed by ice armor");
        await own.GetPower<ConstrictPower>()!.AfterSideTurnEnd(context,CombatSide.Player,[own]);
        Assert(own.CurrentHp==999&&!own.HasPower<IceArmorPower>(),"Constrict consumes armor and only remaining damage reaches HP");
        await Reset();await Power<IceArmorPower>(4);await Power<ConstrictPower>(8);await CreatureCmd.GainBlock(own,3,ValueProp.Unpowered,null);
        await own.GetPower<ConstrictPower>()!.AfterSideTurnEnd(context,CombatSide.Player,[own]);
        Assert(own.CurrentHp==999&&own.Block==0&&!own.HasPower<IceArmorPower>(),"Constrict resolves block then armor then HP");
        await Reset();await Power<IceArmorPower>(10);await Power<SelfFrostPower>(3);
        await own.GetPower<SelfFrostPower>()!.BeforeHandDraw(player,context,combat);
        Assert(own.CurrentHp==997&&own.GetPowerAmount<IceArmorPower>()==10,"self frost HP cost continues to bypass armor");
        await CreatureCmd.Damage(context,own,2,ValueProp.Unpowered|ValueProp.Unblockable,own);
        Assert(own.CurrentHp==995&&own.GetPowerAmount<IceArmorPower>()==10,"unblockable non attack damage still bypasses armor");
        foreach(var type in types)
        {
            foreach(bool upgrade in new[]{false,true})
            {
                var c=New(type,upgrade);
                if(c is IceCrystal || !c.Description.GetRawText().Contains("冰晶"))continue;
                var preview=c.HoverTips.OfType<CardHoverTip>().Single().Card;
                Assert(preview is IceCrystal&&preview.IsUpgraded==(upgrade&&c is CrystalVolley or Glitter),type.Name+(upgrade?"+":"")+" has native crystal preview matching generated upgrade");
                Assert((preview.Enchantment is SharpEnchantment)==(c is Glitter),type.Name+(upgrade?"+":"")+" preview preserves Sharp only for Glitter");
                Assert(!c.HoverTips.OfType<HoverTip>().Any(t=>t.Title=="冰晶"),type.Name+" omits obsolete static crystal tooltip");
                Assert(!preview.GetDescriptionForPile(PileType.None).Contains('{'),type.Name+" preview card text resolves");
            }
        }
        foreach(var type in typeof(FrostPowerBase).Assembly.GetTypes().Where(t=>!t.IsAbstract&&typeof(FrostPowerBase).IsAssignableFrom(t)))
        {
            var p=ModelDb.GetById<PowerModel>(ModelDb.GetId(type));
            if(p.Description.GetRawText().Contains("冰晶"))
                Assert(p.HoverTips.OfType<CardHoverTip>().Single().Card is IceCrystal,type.Name+" includes native crystal preview");
        }
        Assert(!ModelDb.Card<IceCrystal>().IsUpgraded&&ModelDb.Card<IceCrystal>().Enchantment==null,"hover preview never mutates canonical crystal");
        Assert(ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.BladeDance>().HoverTips.OfType<CardHoverTip>().Any(),"crystal uses the same hover tip type as native Blade Dance");
        GD.Print("NATIVE_SHIV_TEXT="+ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.BladeDance>().Description.GetRawText());
        foreach(var type in types)
        foreach(bool upgraded in new[]{false,true})
        {
            var c=New(type,upgraded);
            string raw=c.Description.GetRawText();
            string rendered=System.Text.RegularExpressions.Regex.Replace(c.GetDescriptionForPile(PileType.None),@"\[[^\]]*\]","");
            foreach(var (word,keyword) in new[]{("消耗",CardKeyword.Exhaust),("保留",CardKeyword.Retain),("固有",CardKeyword.Innate)})
            {
                Assert(!raw.Split('\n').Any(line=>line.Trim()==word+"。"),type.Name+(upgraded?"+":"")+" raw text has no duplicate "+word);
                int appearances=rendered.Split('\n').Count(line=>line.Trim()==word+"。");
                Assert(appearances==(c.Keywords.Contains(keyword)?1:0),type.Name+(upgraded?"+":"")+" renders native "+word+" exactly when present");
            }
        }
        await Reset();await Effect<FlowingPower>();await FrostActions.Crystals(player,2);
        Assert(pcs.Hand.Cards.Count==2&&pcs.Hand.Cards.All(c=>c.IsUpgraded),"newly generated crystals are upgraded through native generation hook");
        await Reset();await Effect<BlizzardComing>(true);
        Assert(own.GetPowerAmount<SnowPower>()==12&&own.GetPowerAmount<SelfFrostGuardPower>()==2,"blizzard grants snow and two prevention charges");
        await Power<SelfFrostPower>(9);await Power<SelfFrostPower>(1);
        Assert(!own.HasPower<SelfFrostPower>()&&!own.HasPower<SelfFrostGuardPower>(),"self frost prevention consumes one charge per gain not per layer");
        await Power<SelfFrostPower>(5);await Effect<RapidCalculation>(true);
        Assert(own.GetPowerAmount<SelfFrostPower>()==6,"rapid calculation gains one self frost");
        await Reset();await Effect<FrostBite>();
        Assert(enemy.CurrentHp==9993&&enemy.GetPowerAmount<FrostPower>()==2&&FinalEvents.Ledger(player).FrostApplications==1,"frost bite counts its own successful frost application before damage");
        await Effect<FrostBite>(true);
        Assert(enemy.CurrentHp==9982&&FinalEvents.Ledger(player).FrostApplications==2,"upgraded frost erosion uses three damage per application");
        await Reset();await Power<ArtifactPower>(1,enemy);await Effect<FrostBite>();
        Assert(enemy.CurrentHp==9995&&FinalEvents.Ledger(player).FrostApplications==0,"artifact blocked frost does not grow frost bite");
        await Reset();await Effect<FreezeRay>();await Effect<SnowUnsheathed>(true);
        Assert(enemy.GetPowerAmount<FrostPower>()==30&&FinalEvents.Ledger(player).DebuffLayers==30,"unsheathed uses successful debuff layers earlier in current turn");
        pcs.IncrementTurnNumber();await Effect<SnowUnsheathed>();
        Assert(enemy.GetPowerAmount<FrostPower>()==30&&FinalEvents.Ledger(player).DebuffLayers==0,"debuff count resets next turn");
        await Reset();await CreatureCmd.GainBlock(enemy,3,ValueProp.Unpowered,null);await Effect<HypothermiaStrike>(true);
        Assert(enemy.CurrentHp==9994&&enemy.GetPowerAmount<FrostPower>()==9,"hypothermia frost includes blocked damage");
        await Reset();await Power<FrostPower>(4,enemy);await Effect<IceBreak>(true);
        Assert(enemy.CurrentHp==9942&&!enemy.HasPower<FrostPower>(),"ice break consumes existing and newly applied frost for extra damage");
        await Reset();await Power<SnowPower>(9);await Power<HeartInscriptionPower>(1);await Power<StrengthPower>(2);await Effect<SnowCharge>();
        Assert(own.GetPowerAmount<SnowPower>()==18,"snow charge doubles existing snow without extra gain modifier");
        await Reset();var rush=await Cargo<WhiteNightRush>(PileType.Hand);MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;
        await CardCmd.AutoPlay(context,rush,null);
        Assert(rush.Pile==ColdStorage.Pile(player)&&enemy.CurrentHp==9982&&rush.EnergyCost.GetWithModifiers(CostModifiers.All)==4,"native play leaves white night rush in cold storage");
        await ColdStorage.ThawCard(context,player,rush);await CardCmd.AutoPlay(context,rush,null);
        Assert(rush.Pile==ColdStorage.Pile(player)&&await ColdStorage.ThawCard(context,player,rush),"white night can store and thaw repeatedly in one turn");
        await Reset();await Effect<Pierce>(true);
        Assert(enemy.CurrentHp==9965&&pcs.DrawPile.Cards.Single() is Depleted&&pcs.DiscardPile.Cards.Single() is Depleted,"pierce hits five times and generates depleted in draw pile");
        var depleted=pcs.DrawPile.Cards.Single();
        await Cargo<FrostStrike>(PileType.Draw);await CardPileCmd.Add(depleted,PileType.Draw,CardPilePosition.Top);
        await CardPileCmd.Draw(context,2,player);
        Assert(pcs.Hand.Cards.Count==1&&pcs.Hand.Cards.Single()==depleted&&own.HasPower<NoDrawPower>(),"drawing depleted interrupts current multi-card draw");
        await own.GetPower<NoDrawPower>()!.AfterSideTurnEnd(context,CombatSide.Player,[own]);await CardPileCmd.Draw(context,1,player);
        Assert(pcs.Hand.Cards.Count==2,"depleted draw lock expires after the turn");
        Assert(depleted.Keywords.Contains(CardKeyword.Unplayable)&&depleted.Keywords.Contains(CardKeyword.Ethereal),"depleted is unplayable and ethereal");
        Assert(depleted.MaxUpgradeLevel==0,"depleted cannot be upgraded");
        await Reset();var naturalDepleted=await Cargo<Depleted>(PileType.Draw);await Cargo<FrostStrike>(PileType.Draw);
        await CardPileCmd.Draw(context,2,player,fromHandDraw:true);
        Assert(pcs.Hand.Cards.Count==1&&pcs.Hand.Cards.Single()==naturalDepleted,"depleted also interrupts natural turn-start draw");
        await Reset();await Effect<SlowRelease>(true);Assert(own.GetPowerAmount<SlowReleasePower>()==2&&!New(typeof(SlowRelease),true).Keywords.Contains(CardKeyword.Innate),"upgraded release grants two extra thaws without innate");
        Assert(!New(typeof(WinterArchive),true).Keywords.Contains(CardKeyword.Innate)&&New(typeof(WinterArchive),true).EnergyCost.GetWithModifiers(CostModifiers.None)==1,"upgraded archive costs one without innate");
        await Reset();await Power<FlowingPowerPower>(1);await FrostActions.Crystals(player,1,sharp:true);
        Assert(pcs.Hand.Cards.Single().IsUpgraded&&pcs.Hand.Cards.Single().Enchantment is SharpEnchantment,"crystal auto upgrade preserves sharp enchantment");
        Assert(!types.Any(t=>new[]{"BlizzardWard","ShardBurst","RimeErosion","WinterEdict","Ricochet"}.Contains(t.Name)),"removed cards are absent from registered card definitions");
        await Reset();await Effect<Avalanche>(true);
        Assert(enemy.CurrentHp==9960&&enemy.GetPowerAmount<FrostPower>()==25&&own.GetPowerAmount<SelfFrostPower>()==5,"upgraded avalanche deals forty and applies twenty-five frost");
        await Reset();await Power<FrostPower>(10,enemy);await Effect<PhaseShift>(true);
        Assert(own.GetPowerAmount<IceArmorPower>()==30&&!enemy.HasPower<FrostPower>(),"upgraded phase shift gives three armor per frost");
        await Reset();for(int i=0;i<4;i++)await Cargo<FrostStrike>(PileType.Draw);selector.PrepareToSelect(new[]{0,1,2});await Effect<BloodWinter>(true);
        Assert(pcs.Energy==4&&own.GetPowerAmount<SelfFrostPower>()==3&&ColdStorage.Pile(player).Cards.Count==3&&pcs.Hand.Cards.Count==1,"upgraded blood winter draws four and stores three");
        Assert(New(typeof(DebtSettlement),true).EnergyCost.GetWithModifiers(CostModifiers.None)==1,"upgraded debt settlement costs one");
        await Reset();await Power<FrostPower>(1,enemy);await CreatureCmd.GainBlock(enemy,20,ValueProp.Unpowered,null);await Effect<ThinIce>(true);
        Assert(enemy.CurrentHp==10000&&own.GetPowerAmount<IceArmorPower>()==7,"thin ice rewards damage fully absorbed by block");
        await Reset();await Power<IceArmorPower>(8);var counter=await Cargo<ShieldCounter>(PileType.Hand);
        counter.DynamicVars.ClearPreview();counter.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemy,counter.DynamicVars);
        Assert(counter.DynamicVars.CalculatedDamage.PreviewValue==13,"shield counter displays current total damage");
        await Reset();await Effect<FreezeRay>();var erosion=await Cargo<FrostBite>(PileType.Hand);
        erosion.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemy,erosion.DynamicVars);
        Assert(erosion.DynamicVars.CalculatedDamage.PreviewValue==7,"frost erosion displays accumulated damage using native calculated variable");
        erosion.UpgradeInternal();erosion.FinalizeUpgradeInternal();erosion.DynamicVars.ClearPreview();erosion.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemy,erosion.DynamicVars);
        Assert(erosion.DynamicVars.CalculatedDamage.PreviewValue==8,"upgraded frost erosion preview uses three per application");
        await Reset();await Power<IceArmorPower>(6);var liveShell=await Cargo<ShellRecycle>(PileType.Hand);liveShell.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,liveShell.DynamicVars);
        Assert(liveShell.DynamicVars["LiveAmount"].PreviewValue==12&&liveShell.GetDescriptionForPile(PileType.Hand).Contains("当前获得"),"ice shell shows current block in combat text");
        var livePhase=await Cargo<PhaseShift>(PileType.Hand);livePhase.UpgradeInternal();livePhase.FinalizeUpgradeInternal();await Power<FrostPower>(8,enemy);livePhase.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemy,livePhase.DynamicVars);
        Assert(livePhase.DynamicVars["LiveAmount"].PreviewValue==24&&!livePhase.GetDescriptionForPile(PileType.Hand,enemy).Contains('{'),"upgraded phase preview follows targeted frost");
        var liveReturn=await Cargo<ColdReturn>(PileType.Hand);await Power<SnowPower>(6);liveReturn.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,liveReturn.DynamicVars);
        Assert(liveReturn.DynamicVars["LiveAmount"].PreviewValue==20,"cold return preview includes snow gained before armor conversion");
        await Reset();var core=ModelDb.Relic<WinterCore>().ToMutable();core.Owner=player;await core.BeforeCombatStart();await core.BeforeSideTurnStart(context,CombatSide.Player,[own],combat);
        Assert(enemy.GetPowerAmount<FrostPower>()==3,"starter relic now applies three frost");
        foreach(var ancient in ModelDb.AllAncients)
        {
            var set=ancient.DialogueSet;
            Assert(set.CharacterDialogues.ContainsKey(player.Character.Id.Entry),ancient.Id.Entry+" has Frostsworn dialogue");
            foreach(var dialogue in set.CharacterDialogues[player.Character.Id.Entry])
            {
                using var layout=new MegaCrit.Sts2.Core.Nodes.Events.NAncientEventLayout();
                using var nextLabel=new MegaCrit.Sts2.addons.mega_text.MegaLabel();
                var layoutType=layout.GetType();const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
                var lines=(System.Collections.Generic.List<MegaCrit.Sts2.Core.Entities.Ancients.AncientDialogueLine>)layoutType.GetField("_dialogue",hidden)!.GetValue(layout)!;
                lines.AddRange(dialogue.Lines);
                layoutType.GetField("_fakeNextButtonLabel",hidden)!.SetValue(layout,nextLabel);
                for(int line=0;line<dialogue.Lines.Count;line++)
                {
                    var next=dialogue.Lines[line].NextButtonText;
                    if(line<dialogue.Lines.Count-1)Assert(next!=null&&next.Exists()&&!string.IsNullOrWhiteSpace(next.GetFormattedText()),ancient.Id.Entry+" dialogue next button resolves at line "+line);
                    else Assert(next==null,ancient.Id.Entry+" last dialogue line has no next button and yields to options");
                    layoutType.GetField("_currentDialogueLine",hidden)!.SetValue(layout,line);
                    layoutType.GetMethod("UpdateFakeNextButton",hidden)!.Invoke(layout,null);
                    bool last=(bool)layoutType.GetProperty("IsDialogueOnLastLine",hidden)!.GetValue(layout)!;
                    Assert(last==(line==dialogue.Lines.Count-1),ancient.Id.Entry+" native layout reaches option transition only on final line "+line);
                    Assert(last?!nextLabel.Visible:nextLabel.Text=="继续",ancient.Id.Entry+" native next-button renderer succeeds on line "+line);
                }
            }
            foreach(int visits in new[]{0,1,4,20})
            {
                var choices=set.GetValidDialogues(player.Character.Id,visits,visits+1,false).ToArray();
                Assert(choices.Length>0&&choices.All(d=>d.Lines.Count>0&&d.Lines.All(l=>l.LineText!=null&&l.LineText.Exists()&&!l.LineText.GetFormattedText().Contains('{'))),ancient.Id.Entry+" dialogue resolves for visit "+visits);
            }
        }
        var architect=ModelDb.Event<MegaCrit.Sts2.Core.Models.Events.TheArchitect>().DialogueSet;
        Assert(architect.CharacterDialogues[player.Character.Id.Entry].All(d=>d.Lines.Take(d.Lines.Count-1).All(l=>l.NextButtonText!=null&&l.NextButtonText.Exists())),"architect continuation buttons are localized");
        foreach(int visits in new[]{0,1,10})Assert(architect.GetValidDialogues(player.Character.Id,visits,visits,false).Any(d=>d.Lines.Count>0&&d.Lines.All(l=>l.LineText!=null&&l.LineText.Exists())),"architect ending dialogue resolves for visit "+visits);
        Assert(New(typeof(FrostBite)).Title=="霜噬","frost bite display name restored");
        foreach(bool upgraded in new[]{false,true})
        {
            await Reset();var blood=New(typeof(BloodWinter),upgraded);await CardPileCmd.Add(blood,PileType.Hand);
            MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;await CardCmd.AutoPlay(context,blood,null);
            Assert(blood.Pile==pcs.ExhaustPile,"blood winter exhausts in native play, upgraded="+upgraded);
            await Reset();var carved=await Cargo<FrostStrike>(PileType.Hand);var other=await Cargo<FrostDefend>(Entry.ColdPileType);
            selector.PrepareToSelect(new[]{carved});await Effect<FrostCarving>(upgraded);
            Assert(ColdStorage.State(carved).CarvedCrystals==(upgraded?2:1)&&own.GetPowerAmount<ThawNextPower>()==1,"frost carving marks crystals and queues one extra thaw");
            pcs.IncrementTurnNumber();selector.PrepareToSelect(new[]{carved});selector.PrepareToSelect(new[]{other});
            await own.GetPower<ColdStoragePower>()!.AfterPlayerTurnStartLate(context,player);
            Assert(ColdStorage.Pile(player).Cards.Count==0&&!own.HasPower<ThawNextPower>()&&pcs.Hand.Cards.OfType<IceCrystal>().Count()==(upgraded?2:1),"next turn thaws two cards and grants carved crystals once");
        }
        // 0.7.0: actual trigger paths, native resources, ancient mappings and potions.
        await Reset();await Effect<BlessingWind>();await Effect<CrystalAmulet>();
        var amulet=own.GetPower<CrystalAmuletPower>()!;
        Assert(amulet.Amount==2,"amulet stores base amount independently of current dexterity");
        var crystalFuel=await Cargo<IceCrystal>(PileType.Hand);await CardCmd.Exhaust(context,crystalFuel);
        Assert(own.GetPowerAmount<IceArmorPower>()==3,"native crystal exhaust grants three armor with one dexterity and blessing");
        amulet.Applier=enemy; // Headless fixture has no platform player-name service.
        Assert(amulet.HoverTips.OfType<HoverTip>().First().Description.Contains("3"),"amulet live tooltip displays modified armor yield");
        await Power<DexterityPower>(2);await CardCmd.Exhaust(context,await Cargo<IceCrystal>(PileType.Hand));
        Assert(own.GetPowerAmount<IceArmorPower>()==8,"amulet responds to subsequent dexterity changes without double counting");
        await Reset();await Effect<HeartInscription>();await Effect<SnowEye>();
        var eye=own.GetPower<SnowEyePower>()!;int eyeBase=eye.Amount;
        var crystalPlay=await Cargo<IceCrystal>(PileType.Hand);MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;
        await CardCmd.AutoPlay(context,crystalPlay,enemy);
        Assert(own.GetPowerAmount<SnowPower>()==eyeBase+1,"native crystal play applies strength to snow-eye trigger");
        eye.Applier=enemy;
        Assert(eye.HoverTips.OfType<HoverTip>().First().Description.Contains((eyeBase+1).ToString()),"snow-eye live tooltip displays modified snow yield");
        await PowerCmd.Remove(own.GetPower<HeartInscriptionPower>()!);
        await CardCmd.AutoPlay(context,await Cargo<IceCrystal>(PileType.Hand),enemy);
        Assert(own.GetPowerAmount<SnowPower>()==eyeBase*2+1,"snow-eye returns to base yield when inscription is removed");
        await Reset();var deepCargo=await Cargo<FrostDefend>(PileType.Draw);selector.PrepareToSelect(new[]{deepCargo});await Effect<CrystalRite>(true);
        Assert(own.Block==8&&deepCargo.Pile==ColdStorage.Pile(player),"deep burial stores a non-attack from draw pile and grants eight block");
        await Reset();await Power<SelfFrostPower>(5);await Effect<Rewarm>(true);
        Assert(own.Block==11&&own.GetPowerAmount<SelfFrostPower>()==2,"rewarm upgraded block and three self-frost removal");
        await Reset();for(int i=0;i<4;i++)await Cargo<FrostStrike>(PileType.Draw);await Effect<RapidCalculation>(true);
        Assert(pcs.Hand.Cards.Count==3&&own.GetPowerAmount<SelfFrostPower>()==1,"rapid calculation upgraded draw and self frost");
        await Reset();await Effect<WhiteMist>(true);
        Assert(enemy.CurrentHp==9989&&enemy.GetPowerAmount<FrostPower>()==4&&enemy.GetPowerAmount<WeakPower>()==2,"ice mist blade upgraded damage and debuffs");
        await Reset();await Power<HeartInscriptionPower>(1);await Power<StrengthPower>(1);
        var f1=await Cargo<FrostStrike>(PileType.Hand);var f2=await Cargo<FrostDefend>(PileType.Hand);selector.PrepareToSelect(new[]{f1,f2});await Effect<PrismBlast>(true);
        Assert(own.GetPowerAmount<SnowPower>()==10&&pcs.ExhaustPile.Cards.Count==2,"prism blast grants four snow plus strength separately for each exhaust");
        await Reset();await Power<ColdExpansionPower>(6);
        var hh=new[]{await Cargo<FrostStrike>(PileType.Hand),await Cargo<FrostDefend>(PileType.Hand),await Cargo<IceCrystal>(PileType.Hand)};
        var dd=new[]{await Cargo<FrostStrike>(PileType.Discard),await Cargo<FrostDefend>(PileType.Discard),await Cargo<IceCrystal>(PileType.Discard)};
        var dr=new[]{await Cargo<FrostStrike>(PileType.Draw),await Cargo<FrostDefend>(PileType.Draw),await Cargo<IceCrystal>(PileType.Draw)};
        selector.PrepareToSelect(hh);selector.PrepareToSelect(dd);selector.PrepareToSelect(dr);await Effect<FrozenMoment>(true);
        Assert(ColdStorage.Pile(player).Cards.Count==9,"frozen moment separately selects three cards from each of three piles");
        await Reset();var entryRush=await Cargo<WhiteNightRush>(PileType.Draw);
        for(int i=1;i<=5;i++)
        {await CardPileCmd.Add(entryRush,Entry.ColdPileType);Assert(entryRush.EnergyCost.GetWithModifiers(CostModifiers.All)==Math.Max(0,5-i),"white night permanent reduction for storage entry "+i);await CardPileCmd.Add(entryRush,PileType.Discard);}
        Assert(New(typeof(WhiteNightRush)).EnergyCost.GetWithModifiers(CostModifiers.All)==5,"fresh combat copy does not inherit another physical card's discount");
        await Reset();await Cargo<Depleted>(PileType.Hand);await Cargo<FrostStrike>(PileType.Draw);await CardPileCmd.Draw(context,1,player);
        Assert(pcs.Hand.Cards.Count==1&&own.HasPower<NoDrawPower>(),"direct hand entry of depleted prevents draw");
        await Reset();var thawDepleted=await Cargo<Depleted>(Entry.ColdPileType);await ColdStorage.ThawCard(context,player,thawDepleted);
        Assert(own.HasPower<NoDrawPower>(),"thawing depleted also prevents draw");
        await Reset();var generatedDepleted=combat.CreateCard<Depleted>(player);await CardPileCmd.AddGeneratedCardToCombat(generatedDepleted,PileType.Hand,player);
        Assert(own.HasPower<NoDrawPower>(),"generated depleted immediately prevents draw");
        await Reset();await Effect<FatedStory>();await Power<HiddenBladePower>(4);
        var replayBlade=await Cargo<Crystallize>(Entry.ColdPileType);await ColdStorage.ThawCard(context,player,replayBlade);
        MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;await CardCmd.AutoPlay(context,replayBlade,enemy);
        Assert(enemy.CurrentHp==9940&&enemy.GetPowerAmount<VulnerablePower>()==4&&own.GetPowerAmount<SelfFrostPower>()==2,"fated replay preserves thaw condition and blade bonus across both resolutions");
        await PowerCmd.Remove(enemy.GetPower<VulnerablePower>()!);await CardPileCmd.Add(replayBlade,PileType.Hand);await CardCmd.AutoPlay(context,replayBlade,enemy);
        Assert(enemy.CurrentHp==9924,"fated story does not replay later unthawed play of the same card");
        await Reset();var free1=await Cargo<ColdHammer>(Entry.ColdPileType);await ColdStorage.ThawCard(context,player,free1);
        var iceRelease=(FrostCard)await Cargo<IceRelease>(PileType.Hand);iceRelease.UpgradeInternal();iceRelease.FinalizeUpgradeInternal();
        MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;await iceRelease.SpendResources();await iceRelease.OnPlayWrapper(context,null,false,default,true);
        Assert(pcs.Energy==0&&own.GetPowerAmount<IceReleasePower>()==4,"ice release pays X=3 and upgraded grants four charges");
        pcs.Energy=3;Assert(free1.EnergyCost.GetAmountToSpend()==0,"ice release includes cards thawed before the power was acquired");
        await free1.SpendResources();await free1.OnPlayWrapper(context,enemy,false,default,true);
        Assert(pcs.Energy==3&&own.GetPowerAmount<IceReleasePower>()==3,"released card spends no energy and consumes one charge");
        await CardPileCmd.Add(free1,PileType.Hand);Assert(free1.EnergyCost.GetAmountToSpend()==2,"release no longer applies after thaw mark is spent");
        await Reset();await Effect<BlessingWind>();await Effect<HeartInscription>();
        var guard=(FrostCard)await Cargo<GuardAdvance>(PileType.Hand);guard.UpgradeInternal();guard.FinalizeUpgradeInternal();
        MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;await guard.SpendResources();await guard.OnPlayWrapper(context,null,false,default,true);
        Assert(pcs.Energy==0&&own.GetPowerAmount<IceArmorPower>()==27&&own.GetPowerAmount<SnowPower>()==27,"X guard pays three energy and applies dexterity and strength to each of three gains");
        await Reset();await Power<IceReleasePower>(1);var zeroX=await Cargo<GuardAdvance>(Entry.ColdPileType);await ColdStorage.ThawCard(context,player,zeroX);
        Assert(zeroX.EnergyCost.GetAmountToSpend()==3,"ice release excludes X energy payment");
        MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;await zeroX.SpendResources();await zeroX.OnPlayWrapper(context,null,false,default,true);
        Assert(pcs.Energy==0&&own.GetPowerAmount<IceReleasePower>()==1&&own.GetPowerAmount<IceArmorPower>()==18,"X play captures X=3 and preserves release charge");
        await Reset();var beam=await Cargo<AbsoluteBeam>(PileType.Hand);
        beam.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,beam.DynamicVars);
        Assert(beam.GetDescriptionForPile(PileType.Hand).Contains("一半"),"untargeted beam describes the formula rather than showing a misleading zero");
        beam.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemy,beam.DynamicVars);int beamHalf=(int)Math.Ceiling(FrostActions.Threshold(enemy)/2m);
        Assert(beam.DynamicVars["BeamFrost"].PreviewValue==beamHalf&&!beam.GetDescriptionForPile(PileType.Hand).Contains("一半"),"absolute beam displays target threshold half numerically");
        await RevisedEffects.Play((FrostCard)beam,context,Play(beam,enemy));Assert(enemy.GetPowerAmount<FrostPower>()==beamHalf,"absolute beam applies half current threshold");
        await Reset();var fullBeam=(FrostCard)await Cargo<AbsoluteBeam>(PileType.Hand);fullBeam.UpgradeInternal();fullBeam.FinalizeUpgradeInternal();
        fullBeam.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemy,fullBeam.DynamicVars);
        Assert(fullBeam.DynamicVars["BeamFrost"].PreviewValue==FrostActions.Threshold(enemy),"upgraded beam previews full current threshold");
        MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;await CardCmd.AutoPlay(context,fullBeam,enemy);
        Assert(monster.NextMove.Id==MonsterModel.stunnedMoveId,"upgraded absolute beam freezes through native card resolution");
        await Reset();var tome=(MegaCrit.Sts2.Core.Models.Relics.DustyTome)ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.DustyTome>().ToMutable();tome.SetupForPlayer(player);
        Assert(tome.AncientCard==ModelDb.Card<FatedStory>().Id,"native Dusty Tome setup selects fated story");
        var tooth=(MegaCrit.Sts2.Core.Models.Relics.ArchaicTooth)ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.ArchaicTooth>().ToMutable();
        var toothStarter=(CardModel)tooth.GetType().GetMethod("GetTranscendenceStarterCard",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(tooth,[player])!;
        var toothBeam=(CardModel)tooth.GetType().GetMethod("GetTranscendenceTransformedCard",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(tooth,[toothStarter])!;
        Assert(toothStarter is FreezeRay&&toothBeam is AbsoluteBeam,"native Archaic Tooth selectors map freeze ray to absolute beam");
        CardCmd.Upgrade(toothStarter);CardCmd.Enchant<SharpEnchantment>(toothStarter,1);
        toothBeam=(CardModel)tooth.GetType().GetMethod("GetTranscendenceTransformedCard",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(tooth,[toothStarter])!;
        Assert(toothBeam.IsUpgraded&&toothBeam.Enchantment is SharpEnchantment,"native tooth transform preserves upgrade and enchantment");
        var touch=(MegaCrit.Sts2.Core.Models.Relics.TouchOfOrobas)ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.TouchOfOrobas>().ToMutable();
        Assert(touch.GetUpgradedStarterRelic(ModelDb.Relic<WinterCore>()) is WinterCrown,"native Orobas Touch mapping returns winter crown");
        var crown=(WinterCrown)ModelDb.Relic<WinterCrown>().ToMutable();crown.Owner=player;await crown.BeforeCombatStart();await crown.BeforeSideTurnStart(context,CombatSide.Player,[own],combat);
        Assert(enemy.GetPowerAmount<FrostPower>()==7,"winter crown opening five plus first turn two");
        pcs.IncrementTurnNumber();await crown.BeforeSideTurnStart(context,CombatSide.Player,[own],combat);
        Assert(enemy.GetPowerAmount<FrostPower>()==9,"winter crown subsequent turn adds two only");
        Assert(player.Character.PotionPool.AllPotions.Count()==3&&player.Character.PotionPool.AllPotions.All(p=>p is FrostPotion),"character potion pool contains exactly three custom potions");
        Assert(!ModelDb.Character<MegaCrit.Sts2.Core.Models.Characters.Silent>().PotionPool.AllPotions.Any(p=>p is FrostPotion),"silent's character pool excludes frost potions");
        foreach(var p in player.Character.PotionPool.AllPotions)
        {Assert(!p.DynamicDescription.GetFormattedText().Contains('{')&&p.Title.GetFormattedText().Length>0,"potion localization resolves: "+p.Id.Entry);Assert(p.Image!=null&&p.Outline!=null,"potion placeholder images load: "+p.Id.Entry);}
        async Task UsePotion<T>(Creature? target=null)where T:PotionModel
        {var potion=ModelDb.Potion<T>().ToMutable();await PotionCmd.TryToProcure(potion,player);MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId+1;await potion.OnUseWrapper(context,target??own);}
        await Reset();await UsePotion<FrostBottle>(enemy);Assert(enemy.GetPowerAmount<FrostPower>()==16,"frost potion applies sixteen frost through native use wrapper");
        await Reset();await Power<ArtifactPower>(1,enemy);await UsePotion<FrostBottle>(enemy);Assert(!enemy.HasPower<FrostPower>()&&!enemy.HasPower<ArtifactPower>(),"artifact blocks frost potion");
        await Reset();FrostActions.Freeze(enemy).Count=0;await CreatureCmd.SetMaxAndCurrentHp(enemy,100);await UsePotion<FrostBottle>(enemy);
        Assert(monster.NextMove.Id==MonsterModel.stunnedMoveId,"frost potion freezes immediately without requiring another card");
        await Reset();var nc=new[]{await Cargo<FrostStrike>(PileType.Hand),await Cargo<FrostDefend>(PileType.Hand),await Cargo<IceCrystal>(PileType.Hand)};selector.PrepareToSelect(nc);await UsePotion<LiquidNitrogen>();
        Assert(ColdStorage.Pile(player).Cards.Count==3&&own.HasPower<ColdStoragePower>(),"liquid nitrogen stores three selected cards and enables thaw");
        await Reset();await UsePotion<FractalSnowflake>();await Power<FlowingPowerPower>(1);await FrostActions.Crystals(player,2);
        Assert(pcs.Hand.Cards.All(c=>c is IceCrystal&&c.IsUpgraded&&c.Enchantment is SharpEnchantment),"fractal potion and flowing power upgrade and enchant every generated crystal");
        await FrostActions.Crystals(player,1,sharp:true);Assert(pcs.Hand.Cards.Last().Enchantment!.Amount==1,"already sharp generation does not duplicate the enchantment");

        string Plain(CardModel c)=>System.Text.RegularExpressions.Regex.Replace(c.Description.GetRawText(),@"\[[^\]]*\]","");
        Assert(Plain(New(typeof(ShellRecycle))).Contains("消耗所有冰甲"),"removing standalone Exhaust preserves ice-shell armor consumption effect");
        Assert(Plain(New(typeof(IceDust))).Contains("消耗1张牌"),"removing standalone Exhaust preserves card-exhausting effects");
        foreach(var type in types)
        foreach(bool upgraded in new[]{false,true})
        {
            var c=New(type,upgraded);var raw=c.Description.GetRawText();
            foreach(var word in new[]{"格挡","力量","敏捷","伤害","易伤","虚弱"})
                if(raw.Contains(word))Assert(raw.Contains("[gold]"+word+"[/gold]"),type.Name+" highlights "+word);
            if(raw.Contains("energyIcons"))
            {
                var rendered=c.GetDescriptionForPile(PileType.None);
                var energyIcon=ModelDb.CardPool<FrostCardPool>().TextEnergyIconPath;
                Assert(rendered.Contains(energyIcon+"[/img]")&&!rendered.Contains('{'),type.Name+" renders character energy icons");
                if(c is OverdrawWarmth or QuietMeditation or HeatExchange)
                    Assert(System.Text.RegularExpressions.Regex.Matches(rendered,System.Text.RegularExpressions.Regex.Escape(energyIcon)).Count==(int)c.DynamicVars["Amount"].BaseValue,type.Name+" energy icon count matches upgrade");
            }
        }
    }
}








