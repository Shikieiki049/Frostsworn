using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Frostsworn;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static async Task CheckCrystals0832()
    {
        var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
        await FrostActions.Crystals(player,2);
        Assert(player.Creature.CombatState==null,"crystals outside combat safely do nothing");
        var run=RunState.CreateForTest([player]);player.ResetCombatState();
        var combat=new CombatState(runState:run);combat.AddPlayer(player);
        var monster=(MegaCrit.Sts2.Core.Models.Monsters.Mocks.MockAttackMonster)MegaCrit.Sts2.Core.Models.ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Mocks.MockAttackMonster>().ToMutable();
        var enemy=combat.CreateCreature(monster,CombatSide.Enemy,"crystals-test");combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        var turn=TestTurn107();
        var progress=turn.GetType().GetProperty("IsInProgress")!;
        progress.SetValue(turn,true);
        await FrostActions.Crystals(player,2,true,true);
        var hand=player.PlayerCombatState!.Hand;
        Assert(hand.Cards.OfType<IceCrystal>().Count()==2 && hand.Cards.All(x=>x.IsUpgraded && x.Enchantment is SharpEnchantment),"active combat generates upgraded sharp crystals through native hooks");
        progress.SetValue(turn,false);
        await FrostActions.Crystals(player,3);
        Assert(hand.Cards.Count==2,"ended combat stops generation without native single-card empty-result exception");
    }
}
