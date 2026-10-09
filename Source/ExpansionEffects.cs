using MegaCrit.Sts2.Core.Models.Powers;
namespace Frostsworn;
public static class ExpansionEvents
{
    public static async Task<int> Spend<T>(PlayerChoiceContext context,Player player,int maximum,CardModel source) where T:PowerModel
    {
        var power=player.Creature.GetPower<T>();
        if(power==null) return 0;
        int amount=Math.Min(maximum,power.Amount);
        if(amount>0) await PowerCmd.ModifyAmount(context,power,-amount,player.Creature,source);
        if(typeof(T)==typeof(IceArmorPower)) await FinalEvents.ArmorLost(player,amount);
        return amount;
    }
    public static Task Shattered(PlayerChoiceContext context,Player player)
    { FinalEvents.Ledger(player).Shattered=true;return Task.CompletedTask; }
    public static async Task Stored(PlayerChoiceContext context,Player player)
    {
        FinalEvents.Ledger(player).Stored=true;
        if(player.GetRelic<PolarGlobe>() is {} globe)await globe.Stored(context);
    }
    public static async Task Thawed(PlayerChoiceContext context,Player player)
    {
        if(player.Creature.GetPower<IceMirrorPower>() is {} mirror)
            await PowerCmd.Apply<IceArmorPower>(context,player.Creature,mirror.Amount,player.Creature,null);
        if(player.Creature.GetPower<IceCellarPower>() is {} cellar) await CardPileCmd.Draw(context,cellar.Amount,player);
    }
    public static async Task Frozen(PlayerChoiceContext context,Player player,Creature target)
    { if(player.GetRelic<IceKey>() is {} key)await key.Frozen(context,target); }
}

public abstract class LimitedTurnPower:FrostPowerBase
{
    private int _turn=-1;
    private int _used;
    public bool Use(int limit)
    {
        int turn=Owner.Player!.PlayerCombatState!.TurnNumber;
        if(_turn!=turn) { _turn=turn; _used=0; }
        if(_used>=limit) return false;
        _used++; return true;
    }
}
[RegisterPower] public sealed class GlacierBodyPower:FrostPowerBase
{
    public override async Task BeforeHandDrawLate(Player player,PlayerChoiceContext context,ICombatState combatState)
    {
        if(player.Creature==Owner && !Owner.IsDead)await PowerCmd.Apply<IceArmorPower>(context,Owner,Amount,Owner,null);
    }
}
[RegisterPower] public sealed class IceMirrorPower:FrostPowerBase;

[RegisterPower] public sealed class StormCorePower:FrostPowerBase
{
    public override async Task BeforeHandDrawLate(Player player,PlayerChoiceContext context,ICombatState combatState)
    {
        if(player.Creature==Owner && !Owner.IsDead) await PowerCmd.Apply<SnowPower>(context,Owner,Amount,Owner,null);
    }
}
[RegisterPower] public sealed class SnowEyePower:FrostPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerGainVar(false)];
    public override async Task AfterCardPlayed(PlayerChoiceContext context,CardPlay play)
    {
        if(play.Player.Creature==Owner && play.Card is IceCrystal && !Owner.IsDead)
            await PowerCmd.Apply<SnowPower>(context,Owner,Amount,Owner,play.Card);
    }
}

