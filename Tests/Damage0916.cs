using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Frostsworn;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters.Mocks;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static async Task CheckDamage0916()
    {
        var player = Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player]);
        player.ResetCombatState();
        foreach (var relic in player.Relics.ToArray()) player.RemoveRelicInternal(relic);
        var combat = new CombatState(runState: run);
        combat.AddPlayer(player);
        var monster = (MockAttackMonster)ModelDb.Monster<MockAttackMonster>().ToMutable();
        var enemy = combat.CreateCreature(monster, CombatSide.Enemy, "damage-0916");
        combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        TestTurn107().GetType().GetProperty("IsInProgress")!.SetValue(TestTurn107(), true);
        if (monster.MoveStateMachine == null) monster.SetUpForCombat();
        monster.RollMove(combat.PlayerCreatures);
        var context = new ThrowingPlayerChoiceContext();
        var own = player.Creature;
        Eclipse.Data.Set(run, new EclipseRunData { Level = 8 });
        int maximum = own.MaxHp, hp = own.CurrentHp;
#if STS2_BETA_114
        run.PushRoom(new EventRoom(ModelDb.Event<Neow>()));
        for (int i = 0; i < 4; i++) own.LoseHpInternal(3, dryRun: true);
        Assert(own.CurrentHp == hp && own.MaxHp == maximum && Eclipse.Data.Get(run).MaxHpLost.Count == 0,
            "repeated native HP previews do not change HP or Eclipse scars");
        own.LoseHpInternal(3);
        Assert(own.CurrentHp == hp - 5 && own.MaxHp == maximum - 1,
            "actual event HP loss applies Eclipse rounding and one scar");
        run.PopCurrentRoom();
        own.SetMaxHpInternal(maximum);
        own.SetCurrentHpInternal(hp);
        Eclipse.Data.Set(run, new EclipseRunData { Level = 8 });
#endif
        await PowerCmd.Apply<IceArmorPower>(context, own, 5, own, null);
        await PowerCmd.Apply<ImbalancedPower>(context, enemy, 1, own, null);
        await CreatureCmd.Damage(context, own, 5, ValueProp.Move, enemy);
        Assert(own.CurrentHp == hp && own.MaxHp == maximum && !own.HasPower<IceArmorPower>(),
            "native fully absorbed attack consumes armor without losing HP or Max HP");
        Assert(monster.NextMove.Id == MonsterModel.stunnedMoveId,
            "native Imbalanced still stuns after ice armor absorbs an attack");
        await PowerCmd.Remove(enemy.GetPower<ImbalancedPower>()!);
        await CreatureCmd.Damage(context, own, 2, ValueProp.Unpowered | ValueProp.Unblockable, enemy);
        Assert(own.CurrentHp == hp - 2 && own.MaxHp == maximum - 1,
            "native applied damage creates exactly one Eclipse scar");
    }
}
