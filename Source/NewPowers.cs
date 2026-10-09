using MegaCrit.Sts2.Core.Models.Powers;
namespace Frostsworn;
[RegisterPower] public sealed class ArmorNextPower:FrostPowerBase
{
    public int DueTurn;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context,Player player)
    {
        if(player.Creature!=Owner || Owner.IsDead || player.PlayerCombatState!.TurnNumber<DueTurn)return;
        int amount=Amount;await PowerCmd.Remove(this);
        await PowerCmd.Apply<IceArmorPower>(context,Owner,amount,Owner,null);
    }
}
[RegisterPower] public sealed class ThawNextPower:FrostPowerBase { public int DueTurn; }
[RegisterPower] public sealed class BlessingWindPower:FrostPowerBase
{
    public override PowerStackType StackType=>PowerStackType.Single;
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower,Creature target,decimal amount,Creature? applier,out decimal modifiedAmount)
    {
        modifiedAmount=amount;
        if(target!=Owner || canonicalPower is not IceArmorPower || amount<=0)return false;
        modifiedAmount=Math.Max(0,amount+Owner.GetPowerAmount<DexterityPower>());return modifiedAmount!=amount;
    }
}
[RegisterPower] public sealed class HeartInscriptionPower:FrostPowerBase
{
    public override PowerStackType StackType=>PowerStackType.Single;
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower,Creature target,decimal amount,Creature? applier,out decimal modifiedAmount)
    {
        modifiedAmount=amount;
        if(target!=Owner || canonicalPower is not SnowPower || amount<=0)return false;
        modifiedAmount=Math.Max(0,amount+Owner.GetPowerAmount<StrengthPower>());return modifiedAmount!=amount;
    }
}
[RegisterEnchantment]
public sealed class SharpEnchantment:ModEnchantmentTemplate
{
    public override string CustomIconPath=>"res://Frostsworn/art075/sharp.png";
    public override bool HasExtraCardText=>true;
    public override bool CanEnchantCardType(CardType cardType)=>cardType==CardType.Attack;
    public override async Task OnPlay(PlayerChoiceContext context,CardPlay? play)
    {
        if(play?.Target is {IsDead:false} target)
            await PowerCmd.Apply<VulnerablePower>(context,target,1,Card.Owner.Creature,Card);
    }
}
