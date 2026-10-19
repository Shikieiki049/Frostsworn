#if STS2_STABLE_107
using System;
using System.Linq;
using System.Threading.Tasks;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Monsters.Mocks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Commands;
namespace Frostsworn.Tests
{
 public static partial class Suite
 {
  static async Task CheckDebugSummon010()
  {
   var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);var run=RunState.CreateForTest([player]);player.ResetCombatState();
   var combat=new CombatState(runState:run);combat.AddPlayer(player);CombatManager.Instance.SetUpCombat(combat);
   var turn=TestTurn107();turn.GetType().GetProperty("IsInProgress")!.SetValue(turn,true);
   foreach(var canonical in new MonsterModel[]{ModelDb.Monster<Crusher>(),ModelDb.Monster<Rocket>()})
   {
    var monster=canonical.ToMutable();int before=combat.Creatures.Count();bool rejected=false;
    try {await Loadout.Services.Actions.LoadoutSummonMonsterService.TestAdd(monster,combat);}catch(InvalidOperationException e){rejected=e.Message.Contains("No creature was added");}
    Assert(rejected && combat.Creatures.Count()==before,"actual CreatureCmd rejects unsupported debug "+monster.GetType().Name+" before mutating combat");
    Assert(!FrostDebugSummonGuard010.Unsupported(monster,true,true),"dedicated crab boss scene keeps original spawn behavior for "+monster.GetType().Name);
    Assert(!FrostDebugSummonGuard010.Unsupported(monster,false,false),"non-Loadout paths remain untouched for "+monster.GetType().Name);
   }
   Assert(!FrostDebugSummonGuard010.Unsupported(ModelDb.Monster<MockAttackMonster>(),true,false),"ordinary debug monsters remain eligible");
   var enemy=combat.CreateCreature(ModelDb.Monster<MockAttackMonster>().ToMutable(),CombatSide.Enemy,"valid");combat.AddCreature(enemy); await CreatureCmd.SetMaxAndCurrentHp(enemy,10000); MegaCrit.Sts2.Core.Context.LocalContext.NetId=player.NetId;
   var ctx=new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext();
   foreach(var type in new[]{typeof(FrostStrike),typeof(FreezeRay),typeof(IceCrystal)})
   {
    var card=combat.CreateCard(ModelDb.GetById<MegaCrit.Sts2.Core.Models.CardModel>(ModelDb.GetId(type)),player);
    await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card,MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand);
    await MegaCrit.Sts2.Core.Commands.CardCmd.AutoPlay(ctx,card,enemy);
    Assert(card.Pile?.Type!=MegaCrit.Sts2.Core.Entities.Cards.PileType.Play,"native AutoPlay completes and leaves play pile for "+type.Name+" after rejected debug summons");
   }
  }
 }
}
namespace Loadout.Services.Actions
{
 // Test-only caller scope; no Loadout dependency is shipped with Frostsworn.
 public static class LoadoutSummonMonsterService
 {
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
  public static Task<MegaCrit.Sts2.Core.Entities.Creatures.Creature> TestAdd(MonsterModel monster,ICombatState state)
    => CreatureCmd.Add(monster,state,CombatSide.Enemy,null);
 }
}
#endif

