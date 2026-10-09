using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib;
using System.Reflection;
using System.Collections.Generic;
using System.Text.Json;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters.Mocks;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Models.Capabilities;

namespace Frostsworn.Tests;
public static partial class Suite
{
    public static async Task Run()
    {
        TestMode.IsOn = true;
        typeof(ModManager).GetProperty("State")!.SetValue(null,ModManagerState.Initialized);
        GD.Print("TEST_BOOT: Godot + game + RitsuLib assemblies loaded");
        GD.Print("TEST_USER_DIR=" + OS.GetUserDataDir());
        SaveManager.Instance.InitSettingsDataForTest();
        ProjectSettings.LoadResourcePack("C:/fz/steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.pck", false);
        ProjectSettings.LoadResourcePack(ProjectSettings.GlobalizePath("res://../../dist/Frostsworn/Frostsworn.pck"), true);
        RitsuLibFramework.Initialize();
        Assert(RitsuLibFramework.IsActive,"RitsuLib framework initialization");
        Entry.Initialize();
        typeof(RitsuLibFramework).Assembly.GetType("STS2RitsuLib.Interop.Patches.ModTypeDiscoveryPatch")!
            .GetMethod("Prefix",BindingFlags.Public|BindingFlags.Static)!.Invoke(null,null);
        ModelDb.Init(ModelDb.AllAbstractModelSubtypes.Concat(typeof(FrostCard).Assembly.GetTypes().Where(t=>!t.IsAbstract && typeof(AbstractModel).IsAssignableFrom(t))).Distinct().ToArray());
        LocManager.Initialize();
        var tables=(Dictionary<string,LocTable>)typeof(LocManager).GetField("_tables",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(LocManager.Instance)!;
        foreach(var table in new[]{"cards","powers","relics","characters","epochs","static_hover_tips"})
        {
            var text=Godot.FileAccess.GetFileAsString($"res://Frostsworn/localization/zhs/{table}.json");
            tables[table]=new LocTable(table,JsonSerializer.Deserialize<Dictionary<string,string>>(text)!,tables.GetValueOrDefault(table));
        }
        Assert(Rules.FreezeThreshold(72,0)==15,"72 HP freeze threshold");
        Assert(Rules.FreezeThreshold(300,1)==72,"increasing boss threshold");
        Assert(Rules.Capacity(20)==10,"capacity cap");
        using(var manifestStream=System.IO.File.OpenRead(ProjectSettings.GlobalizePath("res://../../dist/Frostsworn/mod_manifest.json")))
        {
            var manifest=ModManifest.ReadFromStream(manifestStream,out var errors);
            Assert(manifest!=null && (errors?.Count ?? 0)==0 && manifest.id=="Frostsworn" && manifest.dependencies!.Single().id=="STS2-RitsuLib","native game manifest reader accepts package and dependency");
        }
        foreach (var type in typeof(FrostCard).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(FrostCard).IsAssignableFrom(t)))
        {
            var card=ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();
            Assert(!string.IsNullOrWhiteSpace(card.Title),type.Name+" localization");
            Assert(ResourceLoader.Load<Texture2D>(card.PortraitPath)!=null,type.Name+" packed portrait");
            Assert(card.HoverTips.All(t=>t!=null),type.Name+" hover tips");
            var description = card.GetDescriptionForPile(PileType.None);
            Assert(!description.Contains('{') && !description.Contains("diff()"), type.Name+" formatted dynamic description");
            card.UpgradeInternal();
            card.FinalizeUpgradeInternal();
            Assert(card.IsUpgraded,type.Name+" upgrade");
        }
        GD.Print("CHARACTER_ID=" + ModContentRegistry.GetFixedPublicEntry(Entry.ModId,typeof(FrostswornCharacter)));
        var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
        Assert(player.Deck.Cards.Count==10,"starter deck has ten cards");
        Assert(player.Relics.Count==1,"starter relic exists");
        var run=RunState.CreateForTest([player]);
        player.ResetCombatState();
        var combat=new CombatState(runState:run);
        combat.AddPlayer(player);
        var monster=(MockAttackMonster)ModelDb.Monster<MockAttackMonster>().ToMutable();
        var enemy=combat.CreateCreature(monster,CombatSide.Enemy,"test");
        combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        var turn=typeof(CombatManager).GetField("_turnState",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(CombatManager.Instance)!;
        turn.GetType().GetProperty("IsInProgress")!.SetValue(turn,true);
        var context=new ThrowingPlayerChoiceContext();
        await CheckTextAndCharacter(combat, player, enemy, context);
        var core=(WinterCore)player.Relics.Single();
        await core.BeforeCombatStart();
        await core.BeforeSideTurnStart(context,CombatSide.Player,[player.Creature],combat);
        await core.BeforeSideTurnStart(context,CombatSide.Player,[player.Creature],combat);
        Assert(enemy.GetPowerAmount<FrostPower>()==2 && player.PlayerCombatState!.Hand.Cards.OfType<IceCrystal>().Count()==0,"starter relic applies two frost once without generating crystals");
        await PowerCmd.Remove(enemy.GetPower<FrostPower>()!);
        var cardSource=combat.CreateCard<FrostNeedle>(player);
        await CardPileCmd.Add(cardSource,PileType.Hand);
        Assert(player.PlayerCombatState!.Hand.Cards.Contains(cardSource),"actual game card movement");
        await FrostActions.Gain<IceArmorPower>(context,player,10,cardSource);
        Assert(player.Creature.GetPowerAmount<IceArmorPower>()==10,"ice armor applied through game power command");
        int hp=player.Creature.CurrentHp;
        var armor=player.Creature.GetPower<IceArmorPower>()!;
        armor.ModifyHpLostBeforeOsty(player.Creature,100,ValueProp.Move,enemy,null);
        Assert(armor.Amount==10,"damage preview does not consume armor");
        await CreatureCmd.GainBlock(player.Creature,4,ValueProp.Move,null);
        await CreatureCmd.Damage(context,player.Creature,12,ValueProp.Move,enemy);
        Assert(player.Creature.CurrentHp==hp && armor.Amount==2,"block then ice armor then HP");
        await CreatureCmd.Damage(context,player.Creature,3,ValueProp.Move,enemy);
        Assert(player.Creature.CurrentHp==hp-1 && !player.Creature.HasPower<IceArmorPower>(),"armor spent exactly once across hits");
        await FrostActions.Gain<IceArmorPower>(context,player,9,cardSource);
        await CreatureCmd.Damage(context,player.Creature,2,ValueProp.Unpowered,enemy);
        Assert(player.Creature.GetPowerAmount<IceArmorPower>()==9 && player.Creature.CurrentHp==hp-3,"non-attack damage bypasses armor");
        await player.Creature.GetPower<IceArmorPower>()!.BeforeHandDrawLate(player,context,combat);
        Assert(player.Creature.GetPowerAmount<IceArmorPower>()==4,"odd armor melts upward");
        await PowerCmd.Apply<ArtifactPower>(context,player.Creature,1,player.Creature,null);
        await FrostActions.Gain<SelfFrostPower>(context,player,3,cardSource);
        Assert(player.Creature.GetPowerAmount<ArtifactPower>()==1 && player.Creature.GetPowerAmount<SelfFrostPower>()==3,"Artifact cannot waive self-frost debt");
        hp=player.Creature.CurrentHp;
        await player.Creature.GetPower<SelfFrostPower>()!.BeforeHandDraw(player,context,combat);
        Assert(player.Creature.CurrentHp==hp-3 && player.Creature.GetPowerAmount<IceArmorPower>()==4,"self-frost bypasses armor and clears");
        await FrostActions.Gain<SnowPower>(context,player,3,cardSource);
        int enemyHp=enemy.CurrentHp;
        await player.Creature.GetPower<SnowPower>()!.BeforeSideTurnEnd(context,CombatSide.Player,[player.Creature]);
        Assert(enemy.CurrentHp==enemyHp-3 && player.Creature.GetPowerAmount<SnowPower>()==1,"snow damages enemy and halves downward");
        var warmth=combat.CreateCard<OverdrawWarmth>(player);
        int energy=player.PlayerCombatState.Energy;
        await (Task)typeof(FrostCard).GetMethod("OnPlay",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(warmth,[context,Play(warmth)])!;
        Assert(player.PlayerCombatState.Energy==energy+2 && player.Creature.GetPowerAmount<SelfFrostPower>()==3,"energy card applies benefit and unavoidable self-frost debt");

        var selector=new TestCardSelector();
        using var selection=CardSelectCmd.UseSelector(selector);
        var cache=combat.CreateCard<SnowCache>(player);
        for(int i=0;i<4;i++)
        {
            var stored=combat.CreateCard<IceArmor>(player);
            await CardPileCmd.Add(stored,PileType.Hand);
            selector.PrepareToSelect(new[]{stored});
            await ColdStorage.Store(context,player,cache,1);
        }
        Assert(ColdStorage.Pile(player).Cards.Count==4,"cold pile holds four separate card entities");
        await ColdStorage.Store(context,player,cache,1);
        Assert(ColdStorage.Pile(player).Cards.Count==4,"full cold pile refuses a fifth card");
        var thawed=ColdStorage.Pile(player).Cards[0];
        selector.PrepareToSelect(new[]{thawed});
        Assert(await ColdStorage.Thaw(context,player,1)==1,"selected thaw moves one card");
        Assert(thawed.Pile==player.PlayerCombatState.Hand && thawed.EnergyCost.GetWithModifiers(CostModifiers.All)==1,"thaw reduces a 2-energy card to 1");
        selector.PrepareToSelect(new[]{thawed});
        await ColdStorage.Store(context,player,cache,1);
        // Other entities remain eligible; this card specifically must not be eligible.
        Assert(ColdStorage.State(thawed).LastTurn==player.PlayerCombatState.TurnNumber,"same entity thaw lock recorded");
        player.PlayerCombatState.IncrementTurnNumber();
        selector.PrepareToSelect(new[]{thawed});
        await ColdStorage.Thaw(context,player,1);
        Assert(thawed.EnergyCost.GetWithModifiers(CostModifiers.All)==1,"repeat thaw cannot stack discounts");
        var clone=combat.CloneCard(thawed);
        Assert(clone.EnergyCost.GetWithModifiers(CostModifiers.All)==2,"card clone does not copy thaw discount");
        await ModelCapabilities.Get(thawed).Get<ThawDiscount>()!.BeforeCardPlayed(Play(thawed));
        Assert(thawed.EnergyCost.GetWithModifiers(CostModifiers.All)==2,"discount consumed on the next play only");
        while(player.PlayerCombatState.Hand.Cards.Count<CardPile.MaxCardsInHand)
            await CardPileCmd.Add(combat.CreateCard<FrostDefend>(player),PileType.Hand);
        int coldCount=ColdStorage.Pile(player).Cards.Count;
        Assert(await ColdStorage.Thaw(context,player,1)==0 && ColdStorage.Pile(player).Cards.Count==coldCount,"full hand leaves thaw target in cold storage");
        await FrostActions.Gain<ColdExpansionPower>(context,player,99,cardSource);
        Assert(ColdStorage.Capacity(player)==10,"expansion respects hard cap");

        await CreatureCmd.SetMaxAndCurrentHp(enemy,72);
        if(monster.MoveStateMachine==null) monster.SetUpForCombat();
        monster.RollMove(combat.PlayerCreatures);
        string intent=monster.NextMove.Id;
        await FrostActions.Frost(context,enemy,15,cardSource);
        await enemy.GetPower<FrostPower>()!.AfterCardPlayedLate(context,Play(cardSource));
        Assert(monster.NextMove.Id==MonsterModel.stunnedMoveId,"frost threshold stuns native monster");
        Assert(monster.NextMove.FollowUpStateId==intent,"freeze preserves next intended move");
        await FrostActions.Frost(context,enemy,99,cardSource);
        Assert(enemy.GetPowerAmount<FrostPower>()==26,"frozen target capped below new threshold");
        await monster.PerformMove();
        enemy.PrepareForNextTurn(combat.PlayerCreatures);
        Assert(FrostActions.Freeze(enemy).NeedsNormalMove,"stunned action does not clear recovery lock");
        await monster.PerformMove();
        enemy.PrepareForNextTurn(combat.PlayerCreatures);
        Assert(!FrostActions.Freeze(enemy).NeedsNormalMove,"normal completed move clears recovery lock");
        await FrostActions.Frost(context,enemy,1,cardSource);
        await enemy.GetPower<FrostPower>()!.AfterCardPlayedLate(context,Play(cardSource));
        Assert(FrostActions.Freeze(enemy).Count==2,"second freeze uses increased threshold");
        Assert(FrostDisplay.Description(enemy).Contains("当前冰封门槛：39"),"frost hover reports current increasing threshold");
        await FrostActions.Frost(context,enemy,7,cardSource);
        Assert(!enemy.GetPower<FrostPower>()!.IsVisible,"frost is hidden from the status icon row");
        using(var host=new Control { Size=new Vector2(220,24) })
        {
            var bar=FrostDisplay.Create(host,enemy);
            Assert(bar.Value==7 && bar.MaxValue==39 && bar.Size.X==144,"frost progress bar reserves room for current/threshold numbers");
            var row=host.GetNode<Control>("FrostswornFrostRow");
            Assert(row.GetNode<Label>("FrostNumbers").Text=="7 / 39","current frost and threshold are always shown beside the bar");
            Assert(row.Size.X==220 && row.Size.Y==24 && bar.MouseFilter==Control.MouseFilterEnum.Ignore,"entire frost row is one enlarged hover target");
            Assert(FrostDisplay.Description(enemy).Contains("当前寒霜：7") && FrostDisplay.Description(enemy).Contains("已冰封"),"frost tooltip includes amount and frozen state");
            host.Free();
        }
        await ExpansionChecks(context,player,enemy,combat,selector);
    }
    private static async Task ExpansionChecks(ThrowingPlayerChoiceContext context,Player player,MegaCrit.Sts2.Core.Entities.Creatures.Creature enemy,CombatState combat,TestCardSelector selector)
    {
        foreach(var card in player.PlayerCombatState!.Hand.Cards.ToArray()) await CardPileCmd.Add(card,PileType.Discard);
        foreach(var card in ColdStorage.Pile(player).Cards.ToArray()) await CardPileCmd.Add(card,PileType.Discard);
        var expansion=player.Creature.GetPower<ColdExpansionPower>();
        if(expansion!=null) await PowerCmd.Remove(expansion);
        Assert(ColdStorage.Capacity(player)==4 && FrostDisplay.ColdDescription(player).Contains("当前容量上限：4"),"base capacity remains four without removed relics");
        var source=combat.CreateCard<SnowCache>(player);
        await FrostActions.Gain<ColdStoragePower>(context,player,1,source);
        Assert(!player.Creature.GetPower<ColdStoragePower>()!.IsVisible,"cold storage tracker is hidden from status icons");
        await FrostActions.Gain<IceMirrorPower>(context,player,3,source);
        var saved=new CardModel[]{combat.CreateCard<IceArmor>(player),combat.CreateCard<IceArmor>(player),combat.CreateCard<IceArmor>(player)};
        foreach(var card in saved) await CardPileCmd.Add(card,PileType.Hand);
        selector.PrepareToSelect(saved);
        int block=player.Creature.Block;
        await ColdStorage.Store(context,player,source,3);
        Assert(player.Creature.Block==block && ColdStorage.Pile(player).Cards.Count==3,"storing cards no longer grants removed relic block");
        Assert(FrostDisplay.ColdDescription(player).Contains("3/4"),"cold tooltip shows live count and capacity");
        int armor=player.Creature.GetPowerAmount<IceArmorPower>();
        block=player.Creature.Block;
        await ColdStorage.Thaw(context,player,3,random:true);
        Assert(player.Creature.GetPowerAmount<IceArmorPower>()==armor+9,"ice mirror rewards each of first three thaws");
        Assert(player.Creature.Block==block,"thawing cards no longer grants removed relic block");
        await ExpansionEvents.Thawed(context,player);
        Assert(player.Creature.GetPowerAmount<IceArmorPower>()==armor+12,"ice mirror also rewards the fourth thaw");
        player.PlayerCombatState.IncrementTurnNumber();

        await CreatureCmd.SetMaxAndCurrentHp(enemy,9999);
        await FrostActions.Gain<RimeErosionPower>(context,player,3,source);
        await FrostActions.Frost(context,enemy,40,source);
        int snow=player.Creature.GetPowerAmount<SnowPower>();
        int crystals=player.PlayerCombatState.Hand.Cards.OfType<IceCrystal>().Count();
        await FrostActions.Shatter(context,enemy,6,source);
        await FrostActions.Shatter(context,enemy,6,source);
        await FrostActions.Shatter(context,enemy,6,source);
        Assert(player.Creature.GetPowerAmount<SnowPower>()==snow+9,"erosion rewards all three successful shatters");
        Assert(player.PlayerCombatState.Hand.Cards.OfType<IceCrystal>().Count()==crystals,"shattering no longer generates removed relic crystals");
        await FrostActions.Gain<GlacierBodyPower>(context,player,1,source);
        armor=player.Creature.GetPowerAmount<IceArmorPower>();
        await player.Creature.GetPower<IceArmorPower>()!.BeforeHandDrawLate(player,context,combat);
        Assert(player.Creature.GetPowerAmount<IceArmorPower>()==armor,"glacier body stops natural armor melting");

        async Task Effect<T>() where T:CardModel
        {
            var card=combat.CreateCard<T>(player);
            var play=Play(card,enemy);
            await (Task)typeof(ExpansionCard).GetMethod("OnPlay",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(card,[context,play])!;
        }
        int hp=enemy.CurrentHp;
        int twinHits=2+Math.Min(1,player.Creature.GetPowerAmount<SelfFrostPower>());
        await Effect<TwinBlades>();
        Assert(enemy.CurrentHp==hp-4*twinHits,"twin blades adds a real attack hit when self-frost is available");
        armor=player.Creature.GetPowerAmount<IceArmorPower>(); hp=enemy.CurrentHp;
        await Effect<ArmorThrow>();
        Assert(enemy.CurrentHp==hp-8-Math.Min(8,armor) && player.Creature.GetPowerAmount<IceArmorPower>()==Math.Max(0,armor-8),"armor throw converts only armor actually spent into damage");
        snow=player.Creature.GetPowerAmount<SnowPower>(); hp=enemy.CurrentHp;
        await Effect<Avalanche>();
        Assert(enemy.CurrentHp==hp-10-snow && player.Creature.GetPowerAmount<SnowPower>()==snow/2,"avalanche consumes snow after enhanced area attack");
        await Effect<StormCore>();
        snow=player.Creature.GetPowerAmount<SnowPower>();
        await player.Creature.GetPower<StormCorePower>()!.BeforeHandDrawLate(player,context,combat);
        Assert(player.Creature.GetPowerAmount<SnowPower>()==snow+3,"storm core adds snow before drawing");
        await Effect<SnowEye>();
        snow=player.Creature.GetPowerAmount<SnowPower>();
        for(int i=0;i<4;i++) await player.Creature.GetPower<SnowEyePower>()!.AfterCardPlayed(context,Play(combat.CreateCard<IceCrystal>(player)));
        Assert(player.Creature.GetPowerAmount<SnowPower>()==snow+4,"snow eye rewards all four played crystals");
        await MoreChecks(context,player,enemy,combat,selector);
    }
    private static async Task MoreChecks(ThrowingPlayerChoiceContext context,Player player,MegaCrit.Sts2.Core.Entities.Creatures.Creature enemy,CombatState combat,TestCardSelector selector)
    {
        async Task ClearHand()
        {
            foreach(var card in player.PlayerCombatState!.Hand.Cards.ToArray()) await CardPileCmd.Add(card,PileType.Discard);
        }
        async Task Effect<T>() where T:CardModel
        {
            var card=combat.CreateCard<T>(player);
            await (Task)typeof(MoreCard).GetMethod("OnPlay",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(card,[context,Play(card,enemy)])!;
        }
        await ClearHand();
        await Effect<CrystalEdge>();
        await Effect<CrystalAmulet>();
        var source=combat.CreateCard<FrostNeedle>(player);
        int armor=player.Creature.GetPowerAmount<IceArmorPower>();
        var crystal=combat.CreateCard<IceCrystal>(player);
        await CardPileCmd.Add(crystal,PileType.Hand);
        int hp=enemy.CurrentHp;
        await (Task)typeof(FrostCard).GetMethod("OnPlay",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(crystal,[context,Play(crystal,enemy)])!;
        Assert(enemy.CurrentHp==hp-5,"crystal edge adds three damage to an actual crystal attack");
        await CardCmd.Exhaust(context,crystal);
        Assert(player.Creature.GetPowerAmount<IceArmorPower>()==armor+2,"crystal amulet responds to native exhaust command");
        await ClearHand();
        var fuel=new CardModel[]{combat.CreateCard<IceCrystal>(player),combat.CreateCard<IceCrystal>(player)};
        foreach(var card in fuel) await CardPileCmd.Add(card,PileType.Hand);
        selector.PrepareToSelect(fuel);
        int energy=player.PlayerCombatState!.Energy;
        int debt=player.Creature.GetPowerAmount<SelfFrostPower>();
        armor=player.Creature.GetPowerAmount<IceArmorPower>();
        await Effect<CrystalFurnace>();
        Assert(fuel.All(c=>c.Pile?.Type==PileType.Exhaust) && player.PlayerCombatState.Energy==energy+2 && player.Creature.GetPowerAmount<SelfFrostPower>()==debt+2,"crystal furnace pays real crystals for energy and self-frost");
        Assert(player.Creature.GetPowerAmount<IceArmorPower>()==armor+4,"actively consumed crystals trigger amulet independently");
        energy=player.PlayerCombatState.Energy; debt=player.Creature.GetPowerAmount<SelfFrostPower>();
        await Effect<CrystalFurnace>();
        Assert(player.PlayerCombatState.Energy==energy && player.Creature.GetPowerAmount<SelfFrostPower>()==debt,"empty furnace creates neither free energy nor extra debt");
        var volleyFuel=new CardModel[]{combat.CreateCard<IceCrystal>(player),combat.CreateCard<IceCrystal>(player)};
        foreach(var card in volleyFuel) await CardPileCmd.Add(card,PileType.Hand);
        hp=enemy.CurrentHp;
        await Effect<CrystalVolley>();
        Assert(enemy.CurrentHp==hp-24 && volleyFuel.All(c=>c.Pile?.Type==PileType.Exhaust),"crystal volley attacks three times for two consumed crystals");
        await FrostActions.Frost(context,enemy,6,source);
        hp=enemy.CurrentHp;
        await Effect<ShatterVolley>();
        Assert(enemy.CurrentHp==hp-18,"shatter volley converts six frost into three extra hits");

        await ClearHand();
        foreach(var card in ColdStorage.Pile(player).Cards.ToArray()) await CardPileCmd.Add(card,PileType.Discard);
        energy=player.PlayerCombatState.Energy; debt=player.Creature.GetPowerAmount<SelfFrostPower>();
        await Effect<ThawEnergy>();
        Assert(player.PlayerCombatState.Energy==energy && player.Creature.GetPowerAmount<SelfFrostPower>()==debt+2,"failed thaw supplies no energy but retains its stated debt");
        await Effect<IceCellar>();
        var frozen=combat.CreateCard<IceArmor>(player);
        await CardPileCmd.Add(frozen,PileType.Hand);
        selector.PrepareToSelect(new[]{frozen});
        await ColdStorage.Store(context,player,source,1);
        selector.PrepareToSelect(new[]{frozen});
        int hand=player.PlayerCombatState.Hand.Cards.Count;
        energy=player.PlayerCombatState.Energy;
        await Effect<ThawEnergy>();
        Assert(player.PlayerCombatState.Energy==energy+1 && player.PlayerCombatState.Hand.Cards.Count==hand+2,"successful thaw gains energy and triggers cellar draw once");
        await Effect<WinterEdict>();
        armor=player.Creature.GetPowerAmount<IceArmorPower>();
        await ExpansionEvents.Frozen(context,player);
        await ExpansionEvents.Frozen(context,player);
        Assert(player.Creature.GetPowerAmount<IceArmorPower>()==armor+12,"winter edict rewards both freezes in a turn");
        await Effect<WinterStay>();
        selector.Cleanup(); // Single-option selections can be auto-resolved by the game.
        var saved=new CardModel[]{combat.CreateCard<FrostDefend>(player),combat.CreateCard<FrostDefend>(player)};
        foreach(var card in saved) await CardPileCmd.Add(card,PileType.Hand);
        selector.PrepareToSelect(saved);
        await ColdStorage.Store(context,player,source,2);
        int snow=player.Creature.GetPowerAmount<SnowPower>();
        Assert(ColdStorage.Pile(player).Cards.Count==2,"winter stay fixture stores two distinct cards");
        await player.Creature.GetPower<WinterStayPower>()!.BeforeSideTurnEndEarly(context,CombatSide.Player,[player.Creature]);
        Assert(player.Creature.GetPowerAmount<SnowPower>()==snow+2,"winter stay adds snow during early end-turn phase");
        await FinalChecks(context,player,enemy,combat,selector);
    }
    public static void Assert(bool condition, string message)
    {
        if(!condition) throw new Exception("FAILED: "+message);
        GD.Print("PASS: "+message);
    }
    public static CardPlay Play(CardModel card, MegaCrit.Sts2.Core.Entities.Creatures.Creature? target=null) => new()
    {
        Card=card, Player=card.Owner, Target=target, ResultPile=PileType.Discard,
        Resources=default, IsAutoPlay=false, PlayIndex=0, PlayCount=1
    };
}
