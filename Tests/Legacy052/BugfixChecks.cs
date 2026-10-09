using System;
using System.Linq;
using System.Threading.Tasks;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace Frostsworn.Tests;
public static partial class Suite
{
    private static async Task BugfixChecks(ThrowingPlayerChoiceContext context, Player player, Creature enemy, CombatState combat)
    {
        var own=player.Creature;
        foreach(var type in typeof(FrostCard).Assembly.GetTypes().Where(t=>!t.IsAbstract && (typeof(FrostCard).IsAssignableFrom(t) || typeof(PhaseOption).IsAssignableFrom(t))))
        {
            var card=ModelDb.GetById<CardModel>(ModelDb.GetId(type));
            if(!card.GetDescriptionForPile(MegaCrit.Sts2.Core.Entities.Cards.PileType.None).Contains("碎冰")) continue;
            Assert(card.HoverTips.OfType<HoverTip>().Any(t=>t.Title=="碎冰" && t.Description.Contains("实际移除")),type.Name+" includes Shatter keyword definition");
        }
        foreach(var power in own.Powers.Concat(enemy.Powers).ToArray()) await PowerCmd.Remove(power);
        foreach(var type in typeof(FrostPowerBase).Assembly.GetTypes().Where(t=>!t.IsAbstract && typeof(FrostPowerBase).IsAssignableFrom(t)))
        {
            var canonical=ModelDb.GetById<PowerModel>(ModelDb.GetId(type));
            Assert(!canonical.GetDumbHoverTip().Description.Contains('{'),type.Name+" generic tooltip resolves placeholders");
            var power=canonical.ToMutable();
            await PowerCmd.Apply(context,power,own,2,enemy,null);
            Assert(power.HoverTips.OfType<HoverTip>().All(t=>!t.Description.Contains('{')),type.Name+" live tooltip resolves placeholders");
            if(power is RimeErosionPower or PolarCyclePower)
                Assert(power.HoverTips.OfType<HoverTip>().Any(t=>t.Title=="碎冰"),type.Name+" includes Shatter keyword definition");
            await PowerCmd.Remove(power);
        }
        await PowerCmd.Apply<MeditationPower>(context,own,2,enemy,null);
        var meditation=own.GetPower<MeditationPower>()!;
        meditation.DynamicVars["Snow"].BaseValue=1;
        string Tip()=>meditation.HoverTips.OfType<HoverTip>().First().Description;
        Assert(Tip().Contains("2点能量") && Tip().Contains("1层雪势"),"meditation displays actual energy and snow");
        await PowerCmd.Apply<MeditationPower>(context,own,2,enemy,null);
        meditation.DynamicVars["Snow"].BaseValue+=2;
        Assert(Tip().Contains("4点能量") && Tip().Contains("3层雪势"),"meditation tooltip refreshes stacked upgraded values");
        await PowerCmd.Remove(meditation);
        own.LoseBlockInternal(own.Block);
        await CreatureCmd.SetMaxAndCurrentHp(own,100);
        await PowerCmd.Apply<ImbalancedPower>(context,enemy,1,own,null);
        async Task ResetMove()
        {
            if(enemy.Monster!.NextMove.Id==MonsterModel.stunnedMoveId)
            {
                await enemy.Monster.PerformMove();
                enemy.PrepareForNextTurn(combat.PlayerCreatures);
            }
        }
        await ResetMove();
        await PowerCmd.Apply<IceArmorPower>(context,own,5,own,null);
        var hit=(await CreatureCmd.Damage(context,own,5,ValueProp.Move,enemy)).Single();
        Assert(own.CurrentHp==100 && !own.HasPower<IceArmorPower>() && enemy.Monster!.NextMove.Id==MonsterModel.stunnedMoveId,"fully consumed ice armor triggers native Imbalanced stun");
        Assert(!hit.WasFullyBlocked,"ice armor does not change shared block history");
        await ResetMove();
        await CreatureCmd.Damage(context,own,1,ValueProp.Move,enemy);
        Assert(own.CurrentHp==99 && enemy.Monster!.NextMove.Id!=MonsterModel.stunnedMoveId,"next hit cannot reuse ice armor stun receipt");
        await PowerCmd.Apply<IceArmorPower>(context,own,3,own,null);
        await CreatureCmd.Damage(context,own,5,ValueProp.Move,enemy);
        Assert(own.CurrentHp==97 && enemy.Monster!.NextMove.Id!=MonsterModel.stunnedMoveId,"partial armor absorption does not stun");
        await PowerCmd.Apply<IceArmorPower>(context,own,5,own,null);
        own.GetPower<IceArmorPower>()!.ModifyHpLostBeforeOsty(own,3,ValueProp.Move,enemy,null);
        await CreatureCmd.Damage(context,own,0,ValueProp.Move,enemy);
        Assert(enemy.Monster!.NextMove.Id!=MonsterModel.stunnedMoveId && own.GetPowerAmount<IceArmorPower>()==5,"preview and zero damage do not grant armor stun");
        await CreatureCmd.GainBlock(own,2,ValueProp.Unpowered,null);
        await CreatureCmd.Damage(context,own,5,ValueProp.Move,enemy);
        Assert(own.CurrentHp==97 && own.GetPowerAmount<IceArmorPower>()==2 && enemy.Monster!.NextMove.Id==MonsterModel.stunnedMoveId,"block plus armor triggers Imbalanced");
        await ResetMove();
        await CreatureCmd.Damage(context,own,1,ValueProp.Unpowered,enemy);
        Assert(own.CurrentHp==96 && enemy.Monster!.NextMove.Id!=MonsterModel.stunnedMoveId,"non attack damage does not trigger armor stun");
        var rock=(BowlbugRock)ModelDb.Monster<BowlbugRock>().ToMutable();
        var rockEnemy=combat.CreateCreature(rock,CombatSide.Enemy,"armor-rock-test");
        combat.AddCreature(rockEnemy);
        await PowerCmd.Apply<ImbalancedPower>(context,rockEnemy,1,own,null);
        await CreatureCmd.Damage(context,own,2,ValueProp.Move,rockEnemy);
        Assert(rock.IsOffBalance,"ice armor preserves BowlbugRock native off balance behavior");
        foreach(var power in enemy.Powers.Concat(rockEnemy.Powers).ToArray()) await PowerCmd.Remove(power);
        Assert(typeof(WinterCore).Assembly.GetTypes().Count(t=>!t.IsAbstract && typeof(RelicModel).IsAssignableFrom(t))==1,"only the starter relic remains in the mod");
        var core=(WinterCore)player.Relics.Single();
        await PowerCmd.Apply<ArtifactPower>(context,rockEnemy,1,own,null);
        await core.BeforeCombatStart();
        await core.BeforeSideTurnStart(context,CombatSide.Enemy,[enemy,rockEnemy],combat);
        Assert(!enemy.HasPower<FrostPower>() && rockEnemy.HasPower<ArtifactPower>(),"starter relic waits for player opening turn");
        await core.BeforeSideTurnStart(context,CombatSide.Player,[own],combat);
        Assert(enemy.GetPowerAmount<FrostPower>()==2 && !rockEnemy.HasPower<FrostPower>() && !rockEnemy.HasPower<ArtifactPower>(),"starter frost affects all enemies and respects Artifact");
        await core.BeforeSideTurnStart(context,CombatSide.Player,[own],combat);
        Assert(enemy.GetPowerAmount<FrostPower>()==2 && !rockEnemy.HasPower<FrostPower>(),"starter frost does not repeat on later turns");
        await PowerCmd.Remove(enemy.GetPower<FrostPower>()!);
        await core.BeforeCombatStart();
        await core.BeforeSideTurnStart(context,CombatSide.Player,[own],combat);
        Assert(enemy.GetPowerAmount<FrostPower>()==2 && rockEnemy.GetPowerAmount<FrostPower>()==2,"starter frost resets for next combat and affects multiple enemies");
    }
}

