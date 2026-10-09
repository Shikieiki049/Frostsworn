using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Monsters.Mocks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Context;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static async Task CheckMultiplayer0817()
    {
        foreach(ulong? local in new ulong?[]{1,2,null})
        {
            var host=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
            var guest=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,2);
            var run=RunState.CreateForTest([host,guest]);Eclipse.Data.Set(run,new EclipseRunData {Level=8,GoldApplied=true});
            host.ResetCombatState();guest.ResetCombatState();
            var combat=new CombatState(runState:run);combat.AddPlayer(host);combat.AddPlayer(guest);
            var monster=(MockAttackMonster)ModelDb.Monster<MockAttackMonster>().ToMutable();
            var enemy=combat.CreateCreature(monster,CombatSide.Enemy,"0817");combat.AddCreature(enemy);
            CombatManager.Instance.SetUpCombat(combat);
            var turn=typeof(CombatManager).GetField("_turnState",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(CombatManager.Instance)!;
            turn.GetType().GetProperty("IsInProgress")!.SetValue(turn,true);
            if(monster.MoveStateMachine==null)monster.SetUpForCombat();monster.RollMove(combat.PlayerCreatures);
            var context=new ThrowingPlayerChoiceContext();
            LocalContext.NetId=local;
            await PowerCmd.Apply<ArtifactPower>(context,enemy,2,enemy,null);
            int artifactBefore=enemy.GetPowerAmount<ArtifactPower>();
            Assert(artifactBefore>0,"test enemy starts with Artifact");
            combat.RoundNumber=1;
            await Hook.AfterSideTurnStart(combat,CombatSide.Player,combat.PlayerCreatures);
            await Hook.AfterSideTurnStart(combat,CombatSide.Player,combat.PlayerCreatures);
            Assert(enemy.GetPowerAmount<StrengthPower>()==1 && enemy.GetPowerAmount<EclipseStrengthPower>()==1,"native turn hook gives identical single strength for local peer "+local);
            Assert(enemy.GetPowerAmount<ArtifactPower>()==artifactBefore,"Eclipse strength bypasses Artifact");
            await enemy.GetPower<EclipseStrengthPower>()!.AfterSideTurnEnd(context,CombatSide.Enemy,new[]{enemy});
            Assert(enemy.GetPowerAmount<StrengthPower>()==0,"temporary strength expires for peer "+local);
            combat.RoundNumber=2;
            await Hook.AfterSideTurnStart(combat,CombatSide.Player,combat.PlayerCreatures);
            await Hook.AfterSideTurnStart(combat,CombatSide.Player,combat.PlayerCreatures);
            Assert(enemy.Block==3,"native even turn hook gives exactly three Block for peer "+local);
            combat.RoundNumber=3;
            await Hook.AfterSideTurnStart(combat,CombatSide.Enemy,new[]{enemy});
            Assert(enemy.GetPowerAmount<StrengthPower>()==0,"enemy start does not receive player turn bonus");
            await Hook.AfterSideTurnStart(combat,CombatSide.Player,combat.PlayerCreatures);
            Assert(enemy.GetPowerAmount<StrengthPower>()==1,"next odd turn refreshes strength for peer "+local);
            // Standalone harness has no lobby action synchronizer. Drop the test
            // turn state directly instead of invoking UI/network teardown.
            turn.GetType().GetMethod("Cancel")!.Invoke(turn,null);
            typeof(CombatManager).GetField("_turnState",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(CombatManager.Instance,null);
        }
        LocalContext.NetId=null;
    }
}
