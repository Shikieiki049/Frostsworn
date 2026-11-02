using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Frostsworn;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Actions;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters.Mocks;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
namespace Frostsworn.Tests;
public static partial class Suite
{
 static async Task CheckChoices0914()
 {
#if STS2_STABLE_107
  await CheckStableChoices0914();
#else
  var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
  var other=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,2);
  var run=RunState.CreateForTest([player,other]);player.ResetCombatState();other.ResetCombatState();
  var combat=new CombatState(runState:run);combat.AddPlayer(player);combat.AddPlayer(other);
  var monster=(MockAttackMonster)ModelDb.Monster<MockAttackMonster>().ToMutable();
  var enemy=combat.CreateCreature(monster,CombatSide.Enemy,"memory-0914");combat.AddCreature(enemy);
  CombatManager.Instance.SetUpCombat(combat);TestTurn107().GetType().GetProperty("IsInProgress")!.SetValue(TestTurn107(),true);
  combat.GetType().GetProperty("CurrentSide")!.SetValue(combat,CombatSide.Enemy);
  LocalContext.NetId=1;
  var property=typeof(RunManager).GetProperty("PlayerChoiceSynchronizer")!;
  var previous=property.GetValue(RunManager.Instance);
  var net=new NetSingleplayerGameService();
  typeof(RunManager).GetProperty("NetService")!.SetValue(RunManager.Instance,net);
  using var sync=new PlayerChoiceSynchronizer(net,run);
  property.SetValue(RunManager.Instance,sync);
  foreach(var card in player.PlayerCombatState!.AllPiles.SelectMany(p=>p.Cards).ToArray())await CardPileCmd.RemoveFromCombat(card);
  var relic=(MemorySlough)ModelDb.Relic<MemorySlough>().ToMutable();player.AddRelicInternal(relic);
  var cards=new CardModel[]{combat.CreateCard<FrostStrike>(player),combat.CreateCard<FrostDefend>(player),combat.CreateCard<DeepCache>(player)};
  foreach(var card in cards)await CardPileCmd.Add(card,PileType.Draw);
  var selector=new TestCardSelector();using var scope=CardSelectCmd.UseSelector(selector,localOnly:true);
  try
  {
   var selected=selector.SetupForAsyncCardSelection();
   var damage=CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),player.Creature,1,ValueProp.Unpowered|ValueProp.Unblockable,enemy);
   if(damage.IsFaulted)await damage;
   Assert(!damage.IsCompleted && relic.VoidUsed,"enemy damage waits for native player choice rather than throwing or selecting automatically");
   Assert(cards.All(c=>c.Pile?.Type==PileType.Draw),"Memory Slough does not move or exhaust any card before the choice completes");
   selected.SetResult([cards[1]]);await damage;
   Assert(player.PlayerCombatState!.Hand.Cards.Single()==cards[1] && player.PlayerCombatState.ExhaustPile.Cards.SequenceEqual(new[]{cards[0],cards[2]}),"native signal/resume path keeps the selected card and exhausts the other two");
   await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),player.Creature,1,ValueProp.Unpowered|ValueProp.Unblockable,enemy);
   Assert(player.PlayerCombatState.Hand.Cards.Count==1 && player.PlayerCombatState.ExhaustPile.Cards.Count==2,"subsequent damage does not request another choice in the same combat");
   async Task<CardModel[]> Reset()
   {
    selector.Cleanup();relic.VoidUsed=false;
    foreach(var c in player.PlayerCombatState!.AllPiles.SelectMany(p=>p.Cards).ToArray())await CardPileCmd.RemoveFromCombat(c);
    var result=new CardModel[]{combat.CreateCard<FrostStrike>(player),combat.CreateCard<FrostDefend>(player),combat.CreateCard<DeepCache>(player)};
    foreach(var c in result)await CardPileCmd.Add(c,PileType.Draw);
    return result;
   }
   var cancelCards=await Reset();selector.PrepareToSelect(Array.Empty<CardModel>());
   await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),player.Creature,1,ValueProp.Unpowered|ValueProp.Unblockable,enemy);
   Assert(cancelCards.All(c=>c.Pile?.Type==PileType.Draw),"empty/cancelled choice never exhausts all three cards");
   var normalCards=await Reset();selector.PrepareToSelect([normalCards[0]]);var normal=new TrackingChoice0914();
   await CreatureCmd.Damage(normal,player.Creature,1,ValueProp.Unpowered|ValueProp.Unblockable,enemy);
   Assert(normal.Begun==1 && normal.Ended==1 && normalCards[0].Pile?.Type==PileType.Hand,"an existing choice-capable context is preserved and signalled exactly once");
   var playerCards=await Reset();combat.GetType().GetProperty("CurrentSide")!.SetValue(combat,CombatSide.Player);
   var queues=new ActionQueueSet(run.Players);queues.SetUpForCombat();queues.CombatStarted();
   var executor=new ActionExecutor(queues);
   GenericHookGameAction? pendingHook=null;executor.BeforeActionExecuted+=a=>pendingHook=a as GenericHookGameAction;
   var buffer=new RunLocationTargetedMessageBuffer(net);
   var queueSync=new ActionQueueSynchronizer(run,queues,buffer,net);
   typeof(RunManager).GetProperty("ActionQueueSet")!.SetValue(RunManager.Instance,queues);
   typeof(RunManager).GetProperty("ActionExecutor")!.SetValue(RunManager.Instance,executor);
   typeof(RunManager).GetProperty("ActionQueueSynchronizer")!.SetValue(RunManager.Instance,queueSync);
   selected=selector.SetupForAsyncCardSelection();
   await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),player.Creature,1,ValueProp.Unpowered|ValueProp.Unblockable,enemy);
   for(int frame=0;frame<20 && pendingHook?.State!=GameActionState.GatheringPlayerChoice;frame++)await Godot.Engine.GetMainLoop().ToSignal(Godot.Engine.GetMainLoop(),Godot.SceneTree.SignalName.ProcessFrame);
   Assert(pendingHook?.State==GameActionState.GatheringPlayerChoice && relic.VoidUsed,"player-turn secondary damage creates and pauses a native owner hook action");
   Assert(playerCards.All(c=>c.Pile?.Type==PileType.Draw),"paused native hook does not apply the selection before resumption");
   selected.SetResult([playerCards[2]]);await queues.BecameEmpty();
   Assert(pendingHook?.State==GameActionState.Finished && playerCards[2].Pile?.Type==PileType.Hand && playerCards.Take(2).All(c=>c.Pile?.Type==PileType.Exhaust),"native hook action resumes and completes the original keep-one/exhaust-rest effect");

   // Exercise the real hand -> native PlayCardAction -> exhaust path, rather
   // than invoking IceCrystal's effect directly.
   typeof(RunManager).GetProperty("State",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(RunManager.Instance,run);
   queues.UnpauseAllPlayerQueues();
   await CreatureCmd.SetMaxAndCurrentHp(enemy,1000);
   for(int i=0;i<4;i++)
   {
    var crystal=combat.CreateCard<IceCrystal>(player);await CardPileCmd.Add(crystal,PileType.Hand);
    var action=new PlayCardAction(crystal,enemy);queues.EnqueueWithoutSynchronizing(action);await queues.BecameEmpty();
    Assert(action.State==GameActionState.Finished && crystal.Pile?.Type==PileType.Exhaust,"native IceCrystal play finishes and exhausts without remaining in the play pile #"+i);
   }
   Assert(enemy.GetPowerAmount<FrostPower>()>=4,"repeated native IceCrystal actions retain their Frost effect");

   var remoteCards=await Reset();
   combat.GetType().GetProperty("CurrentSide")!.SetValue(combat,CombatSide.Enemy);
   var replay=new NetReplayGameService(2);using var remoteSync=new PlayerChoiceSynchronizer(replay,run);
   typeof(RunManager).GetProperty("NetService")!.SetValue(RunManager.Instance,replay);
   property.SetValue(RunManager.Instance,remoteSync);LocalContext.NetId=2;
   var remoteDamage=CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),player.Creature,1,ValueProp.Unpowered|ValueProp.Unblockable,enemy);
   if(remoteDamage.IsFaulted)await remoteDamage;
   Assert(!remoteDamage.IsCompleted && remoteCards.All(c=>c.Pile?.Type==PileType.Draw),"remote peer waits for the owner's native synchronized choice instead of choosing locally");
   remoteSync.ReceiveReplayChoice(player,0,PlayerChoiceResult.FromIndexes([1]).ToNetData());await remoteDamage;
   Assert(remoteCards[1].Pile?.Type==PileType.Hand && remoteCards.Where((c,i)=>i!=1).All(c=>c.Pile?.Type==PileType.Exhaust),"remote choice replay produces the same keep-one/exhaust-rest piles");
   Assert(remoteSync.ReserveChoiceId(player)==1,"remote selection advances exactly one native choice ID");
   LocalContext.NetId=1;typeof(RunManager).GetProperty("NetService")!.SetValue(RunManager.Instance,net);property.SetValue(RunManager.Instance,sync);
   var movedCards=await Reset();selected=selector.SetupForAsyncCardSelection();
   var movedDamage=CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),player.Creature,1,ValueProp.Unpowered|ValueProp.Unblockable,enemy);
   await CardPileCmd.Add(movedCards[1],PileType.Discard);selected.SetResult([movedCards[1]]);await movedDamage;
   Assert(movedCards[1].Pile?.Type==PileType.Discard && movedCards.Where((c,i)=>i!=1).All(c=>c.Pile?.Type==PileType.Draw),"a card moved by another effect while choosing is not resurrected or used to exhaust the others");
   var endingCards=await Reset();selected=selector.SetupForAsyncCardSelection();
   var endingDamage=CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),player.Creature,1,ValueProp.Unpowered|ValueProp.Unblockable,enemy);
   TestTurn107().GetType().GetProperty("IsInProgress")!.SetValue(TestTurn107(),false);selected.SetResult([endingCards[0]]);await endingDamage;
   Assert(endingCards.All(c=>c.Pile?.Type==PileType.Draw),"a choice completing after combat ends cannot move or exhaust cards");
   executor.Cancel();queueSync.Dispose();

  }
  finally{property.SetValue(RunManager.Instance,previous);LocalContext.NetId=null;}
#endif
 }
 sealed class TrackingChoice0914 : PlayerChoiceContext
 {
  public int Begun,Ended;
#if STS2_STABLE_107
  public override Task SignalPlayerChoiceBegun(PlayerChoiceOptions options){Begun++;return Task.CompletedTask;}
#else
  public override ulong? OwnerId=>1;
  public override Task SignalPlayerChoiceBegun(Player player,PlayerChoiceOptions options){Begun++;return Task.CompletedTask;}
#endif
  public override Task SignalPlayerChoiceEnded(){Ended++;return Task.CompletedTask;}
 }
}
