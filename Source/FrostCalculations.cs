namespace Frostsworn;
public static class FrostCalculations
{
    public static IEnumerable<DynamicVar> Vars(FrostCard card,CardSpec spec)
    {
        if(card is AbsoluteBeam)yield return new BeamFrostVar();
        if(card is ShellRecycle or SnowFinale or SnowUnsheathed or ColdReturn or SnowCharge or PhaseShift)yield return new FrostLiveVar();
        if(card is not (FrostBite or ShieldCounter or ColdHammer))yield break;
        yield return new CalculationBaseVar(spec.Damage);
        yield return new ExtraDamageVar(card is FrostBite?spec.Extra:card is ColdHammer?spec.Amount:1);
        yield return new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (c,_)=>c switch
        {
            FrostBite=>FinalEvents.Ledger(c.Owner).FrostApplications,
            ShieldCounter=>c.Owner.Creature.GetPowerAmount<IceArmorPower>(),
            ColdHammer=>ColdStorage.Pile(c.Owner).Cards.Sum(RevisedEffects.BaseCost),
            _=>0
        });
    }
    public static void Upgrade(FrostCard card,CardSpec spec)
    {
        if(card is not (FrostBite or ShieldCounter or ColdHammer))return;
        if(spec.DamageUpgrade!=0)card.DynamicVars.CalculationBase.UpgradeValueBy(spec.DamageUpgrade);
        int extra=card is FrostBite?spec.ExtraUpgrade:card is ColdHammer?spec.AmountUpgrade:0;
        if(extra!=0)card.DynamicVars.ExtraDamage.UpgradeValueBy(extra);
    }
}
public sealed class FrostLiveVar():DynamicVar("LiveAmount",0)
{
    public override void UpdateCardPreview(CardModel card,CardPreviewMode previewMode,Creature? target,bool runGlobalHooks)
    {
        PreviewValue=0;
        if(!runGlobalHooks)return;
        var own=card.Owner.Creature;
        decimal armor(decimal n)=>n<=0?0:Math.Max(0,n+(own.HasPower<BlessingWindPower>()?own.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.DexterityPower>():0));
        PreviewValue=card switch
        {
            ShellRecycle=>own.GetPowerAmount<IceArmorPower>()<=0?0:MegaCrit.Sts2.Core.Hooks.Hook.ModifyBlock(card.CombatState!,own,own.GetPowerAmount<IceArmorPower>()*2,ValueProp.Move,card,null,out _),
            SnowFinale=>FinalEvents.Ledger(card.Owner).DebtLifeLost,
            SnowUnsheathed=>FinalEvents.Ledger(card.Owner).DebuffLayers*card.DynamicVars["Amount"].BaseValue,
            SnowCharge=>own.GetPowerAmount<SnowPower>()*2,
            PhaseShift=>armor(Math.Min(10,target?.GetPowerAmount<FrostPower>()??0)*card.DynamicVars["Amount"].BaseValue),
            ColdReturn=>armor((own.GetPowerAmount<SnowPower>()+Math.Max(0,card.DynamicVars["Amount"].BaseValue+(own.HasPower<HeartInscriptionPower>()?own.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>():0)))*card.DynamicVars["Extra"].BaseValue),
            _=>0
        };
    }
}

public sealed class BeamFrostVar():DynamicVar("BeamFrost",0)
{
    public override void UpdateCardPreview(CardModel card,CardPreviewMode previewMode,Creature? target,bool runGlobalHooks)
    {PreviewValue=target==null?0:card.IsUpgraded?FrostActions.Threshold(target):(int)Math.Ceiling(FrostActions.Threshold(target)/2m);}
}
