namespace Frostsworn;
[RegisterPower] public sealed class CrystalEdgePower:FrostPowerBase
{
    // This game version has no public builder method for unblockable attacks.
    // Set the native command's flags before damage, retaining attack history and all native hooks.
    private static readonly System.Reflection.MethodInfo SetDamageProps = HarmonyLib.AccessTools.PropertySetter(
        typeof(MegaCrit.Sts2.Core.Commands.Builders.AttackCommand), "DamageProps");
    public override Task BeforeAttack(MegaCrit.Sts2.Core.Commands.Builders.AttackCommand command)
    {
        if(command.Attacker == Owner && command.ModelSource is IceCrystal)
            SetDamageProps.Invoke(command, new object[] { command.DamageProps | ValueProp.Unblockable });
        return Task.CompletedTask;
    }
    public override decimal ModifyDamageAdditive(Creature? target,decimal amount,ValueProp props,Creature? dealer,CardModel? cardSource,CardPlay? cardPlay)
        => dealer==Owner && cardSource is IceCrystal && props.IsPoweredAttack() ? Amount : 0;
}
[RegisterPower] public sealed class CrystalAmuletPower:FrostPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerGainVar(true)];
    public override async Task AfterCardExhausted(PlayerChoiceContext context,CardModel card,bool causedByEthereal)
    {
        if(!Owner.IsDead && card.Owner.Creature==Owner && card is IceCrystal)
            await PowerCmd.Apply<IceArmorPower>(context,Owner,Amount,Owner,card);
    }
}
[RegisterPower] public sealed class GlacialCorePower:FrostPowerBase;
public sealed class CrystalFrostVar(decimal amount):DynamicVar("Amount",amount)
{
    public override void UpdateCardPreview(CardModel card,CardPreviewMode previewMode,Creature? target,bool runGlobalHooks)
        => PreviewValue = BaseValue + (runGlobalHooks ? card.Owner.Creature.GetPowerAmount<GlacialCorePower>() : 0);
}
[RegisterPower] public sealed class IceCellarPower:FrostPowerBase;

