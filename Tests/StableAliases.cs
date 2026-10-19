#if STS2_STABLE_107
global using StartRunLobbyPlayer = MegaCrit.Sts2.Core.Entities.Multiplayer.LobbyPlayer;
#endif
namespace Frostsworn.Tests;
public static class TestEventCompat107
{
 public static System.Threading.Tasks.Task BeginEventForTest107(this MegaCrit.Sts2.Core.Models.EventModel ev,MegaCrit.Sts2.Core.Entities.Players.Player player)
#if STS2_STABLE_107
 => ev.BeginEvent(player,false);
#else
 => ev.BeginEvent(player,null,false);
#endif
}
public static partial class Suite
{
 static System.Threading.Tasks.Task LoseTestBlock107(MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext ctx,MegaCrit.Sts2.Core.Entities.Creatures.Creature c)
#if STS2_STABLE_107
 => MegaCrit.Sts2.Core.Commands.CreatureCmd.LoseBlock(c,c.Block);
#else
 => MegaCrit.Sts2.Core.Commands.CreatureCmd.LoseBlock(ctx,c,c.Block,null);
#endif
}
public static partial class Suite
{
 static object TestTurn107()
 {
#if STS2_STABLE_107
  return new StableTurnState107();
#else
  return typeof(MegaCrit.Sts2.Core.Combat.CombatManager).GetField("_turnState",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(MegaCrit.Sts2.Core.Combat.CombatManager.Instance)!;
#endif
 }
 static void ClearTestTurn107()
 {
#if STS2_STABLE_107
  // Headless tests have no room/UI cleanup context. Release only their test combat slot.
  new StableTurnState107().Cancel();
  typeof(MegaCrit.Sts2.Core.Combat.CombatManager).GetField("_state",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(MegaCrit.Sts2.Core.Combat.CombatManager.Instance,null);
#else
  typeof(MegaCrit.Sts2.Core.Combat.CombatManager).GetField("_turnState",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(MegaCrit.Sts2.Core.Combat.CombatManager.Instance,null);
#endif
 }
#if STS2_STABLE_107
 sealed class StableTurnState107
 {
  public bool IsInProgress {get=>MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsInProgress;set=>typeof(MegaCrit.Sts2.Core.Combat.CombatManager).GetProperty("IsInProgress")!.SetValue(MegaCrit.Sts2.Core.Combat.CombatManager.Instance,value);}
  public void Cancel()=>IsInProgress=false;
 }
#endif
}