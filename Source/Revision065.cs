using MegaCrit.Sts2.Core.Models.Powers;
namespace Frostsworn;

// Native Draw checks ShouldDraw once per batch. This helper is checked again
// between cards, allowing Depleted to stop the remainder of the same batch.
[HarmonyLib.HarmonyPatch(typeof(CardPileCmd),"CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot")]
internal static class DepletedDrawBatchPatch
{
    public static bool Prefix(Player player,ref bool __result)
    {
        if(player.PlayerCombatState is {} state && player.Creature.HasPower<NoDrawPower>() && FinalEvents.Ledger(player).DepletedTurn==state.TurnNumber)
        { __result=false;return false; }
        return true;
    }
}

[HarmonyLib.HarmonyPatch(typeof(MegaCrit.Sts2.Core.Hooks.Hook),nameof(MegaCrit.Sts2.Core.Hooks.Hook.AfterPowerAmountChanged))]
internal static class FrostApplicationLedgerPatch
{
    public static void Prefix(PowerModel power,decimal amount,Creature? applier)
    {
        if(applier?.Player is not {} player || player.Character is not FrostswornCharacter || player.PlayerCombatState==null || power.Owner.Side!=CombatSide.Enemy || amount<=0 || power.TypeForCurrentAmount!=PowerType.Debuff)return;
        var ledger=FinalEvents.Ledger(player);
        ledger.DebuffLayers+=(int)amount;
        if(power is FrostPower)ledger.FrostApplications++;
    }
}

[RegisterPower] public sealed class FlowingPowerPower:FrostPowerBase
{
    public override PowerStackType StackType=>PowerStackType.Single;
    public override Task AfterCardGeneratedForCombat(CardModel card,Player? creator)
    {
        if(card.Owner.Creature==Owner && card is IceCrystal && !card.IsUpgraded)CardCmd.Upgrade(card);
        return Task.CompletedTask;
    }
}
[RegisterPower] public sealed class SelfFrostGuardPower:FrostPowerBase
{
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower,Creature target,decimal amount,Creature? applier,out decimal modifiedAmount)
    {
        modifiedAmount=amount;
        if(target!=Owner || canonicalPower is not SelfFrostPower || amount<=0)return false;
        modifiedAmount=0;return true;
    }
    public override Task AfterModifyingPowerAmountReceived(PowerModel power)=>PowerCmd.Decrement(this);
}
