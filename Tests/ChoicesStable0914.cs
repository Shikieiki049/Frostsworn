#if STS2_STABLE_107
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Frostsworn;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Actions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters.Mocks;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.ValueProps;
namespace Frostsworn.Tests;
public static partial class Suite
{
 static async Task CheckStableChoices0914()
 {
  var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
  var other=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,2);
  var run=RunState.CreateForTest([player,other]);player.ResetCombatState();other.ResetCombatState();
  var combat=new CombatState(runState:run);combat.AddPlayer(player);combat.AddPlayer(other);
  var monster=(MockAttackMonster)ModelDb.Monster<MockAttackMonster>().ToMutable();
  var enemy=combat.CreateCreature(monster,CombatSide.Enemy,"memory-stable0914");combat.AddCreature(enemy);
  CombatManager.Instance.SetUpCombat(combat);var turn=TestTurn107();turn.GetType().GetProperty("IsInProgress")!.SetValue(turn,true);
  combat.GetType().GetProperty("CurrentSide")!.SetValue(combat,CombatSide.Enemy);
  LocalContext.NetId=1;
  // Stable has no local-only selector. Use native replay choice delivery so
  // ReserveChoiceId/Signal/WaitForRemoteChoice all execute without a UI mock.
  var replay=new NetReplayGameService(2);var net=new NetSingleplayerGameService();
  using var sync=new PlayerChoiceSynchronizer(replay,run);
  var manager=RunManager.Instance;
  typeof(RunManager).GetProperty("NetService")!.SetValue(manager,replay);
  typeof(RunManager).GetProperty("PlayerChoiceSynchronizer")!.SetValue(manager,sync);
  var relic=(MemorySlough)ModelDb.Relic<MemorySlough>().ToMutable();player.AddRelicInternal(relic);
  uint next=0;
  async Task<CardModel[]> Reset()
  {
   relic.VoidUsed=false;
   foreach(var c in player.PlayerCombatState!.AllPiles.SelectMany(p=>p.Cards).ToArray())await CardPileCmd.RemoveFromCombat(c);
   var cards=new CardModel[]{combat.CreateCard<FrostStrike>(player),combat.CreateCard<FrostDefend>(player),combat.CreateCard<DeepCache>(player)};
   foreach(var c in cards)await CardPileCmd.Add(c,PileType.Draw);
   return cards;
  }
  void Deliver(params int[] indices)=>sync.ReceiveReplayChoice(player,next++,PlayerChoiceResult.FromIndexes(indices.ToList()).ToNetData());
  Task Damage(PlayerChoiceContext context)=>CreatureCmd.Damage(context,player.Creature,1,ValueProp.Unpowered|ValueProp.Unblockable,enemy);
  var cards=await Reset();var damage=Damage(new ThrowingPlayerChoiceContext());if(damage.IsFaulted)await damage;
  Assert(!damage.IsCompleted && relic.VoidUsed,"stable secondary damage waits for a real native choice without throwing");
  Assert(cards.All(c=>c.Pile?.Type==PileType.Draw),"stable cards stay in Draw until the native remote choice arrives");
  Deliver(1);await damage;
  Assert(cards[1].Pile?.Type==PileType.Hand && cards.Where((c,i)=>i!=1).All(c=>c.Pile?.Type==PileType.Exhaust),"stable native remote result keeps one and exhausts the remaining two");
  await Damage(new ThrowingPlayerChoiceContext());
  Assert(next==1 && player.PlayerCombatState!.Hand.Cards.Count==1,"stable repeated damage does not request another choice");
  typeof(RunManager).GetProperty("NetService")!.SetValue(manager,replay);
  cards=await Reset();damage=Damage(new ThrowingPlayerChoiceContext());Deliver();await damage;
  Assert(cards.All(c=>c.Pile?.Type==PileType.Draw),"stable cancelled choice does not exhaust the entire offered set");
  cards=await Reset();var normal=new TrackingChoice0914();damage=Damage(normal);Deliver(0);await damage;
  Assert(normal.Begun==1 && normal.Ended==1 && cards[0].Pile?.Type==PileType.Hand,"stable existing choice context retains its native begin/end signals");
  cards=await Reset();combat.GetType().GetProperty("CurrentSide")!.SetValue(combat,CombatSide.Player);
  var queues=new ActionQueueSet(run.Players);queues.CombatStarted();var executor=new ActionExecutor(queues);
  var buffer=new RunLocationTargetedMessageBuffer(net);var queueSync=new ActionQueueSynchronizer(run,queues,buffer,net);
  typeof(RunManager).GetProperty("ActionQueueSet")!.SetValue(manager,queues);
  typeof(RunManager).GetProperty("ActionExecutor")!.SetValue(manager,executor);
  typeof(RunManager).GetProperty("ActionQueueSynchronizer")!.SetValue(manager,queueSync);
  GenericHookGameAction? pending=null;executor.BeforeActionExecuted+=a=>pending=a as GenericHookGameAction;
  await Damage(new ThrowingPlayerChoiceContext());
  for(int frame=0;frame<20 && pending?.State!=GameActionState.GatheringPlayerChoice;frame++)await Godot.Engine.GetMainLoop().ToSignal(Godot.Engine.GetMainLoop(),Godot.SceneTree.SignalName.ProcessFrame);
  Assert(pending?.State==GameActionState.GatheringPlayerChoice && cards.All(c=>c.Pile?.Type==PileType.Draw),"stable player-turn secondary damage pauses a native owner hook action");
  Deliver(2);await queues.BecameEmpty();
  Assert(pending?.State==GameActionState.Finished && cards[2].Pile?.Type==PileType.Hand && cards.Take(2).All(c=>c.Pile?.Type==PileType.Exhaust),"stable native hook resumes and finishes with matching piles");
  combat.GetType().GetProperty("CurrentSide")!.SetValue(combat,CombatSide.Enemy);cards=await Reset();damage=Damage(new ThrowingPlayerChoiceContext());
  await CardPileCmd.Add(cards[1],PileType.Discard);Deliver(1);await damage;
  Assert(cards[1].Pile?.Type==PileType.Discard && cards.Where((c,i)=>i!=1).All(c=>c.Pile?.Type==PileType.Draw),"stable selection cannot resurrect a card moved by another effect");
  Assert(sync.ReserveChoiceId(player)==next++,"stable native choice IDs advance exactly once per actual selection");
  combat.GetType().GetProperty("CurrentSide")!.SetValue(combat,CombatSide.Player);
  typeof(RunManager).GetProperty("State",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(manager,run);
  typeof(RunManager).GetProperty("NetService")!.SetValue(manager,net);queues.UnpauseAllPlayerQueues();await CreatureCmd.SetMaxAndCurrentHp(enemy,1000);
  for(int i=0;i<4;i++)
  {
   var crystal=combat.CreateCard<IceCrystal>(player);await CardPileCmd.Add(crystal,PileType.Hand);
   var action=new PlayCardAction(crystal,enemy);queues.EnqueueWithoutSynchronizing(action);await queues.BecameEmpty();
   Assert(action.State==GameActionState.Finished && crystal.Pile?.Type==PileType.Exhaust,"stable native IceCrystal play finishes and exhausts #"+i);
  }
  combat.GetType().GetProperty("CurrentSide")!.SetValue(combat,CombatSide.Enemy);
  typeof(RunManager).GetProperty("NetService")!.SetValue(manager,replay);
  cards=await Reset();damage=Damage(new ThrowingPlayerChoiceContext());
  turn.GetType().GetProperty("IsInProgress")!.SetValue(turn,false);Deliver(0);await damage;
  Assert(cards.All(c=>c.Pile?.Type==PileType.Draw),"stable late choice cannot change piles after combat ends");
  executor.Cancel();queueSync.Dispose();LocalContext.NetId=null;
 }
}
#endif


