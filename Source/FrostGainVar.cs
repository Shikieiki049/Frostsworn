using MegaCrit.Sts2.Core.Models.Powers;
namespace Frostsworn;

// Preview only: effects still use BaseValue and the power-receive hooks apply the bonus once.
public sealed class FrostGainVar(string name,decimal amount,bool armor):DynamicVar(name,amount)
{
    public static DynamicVar Create(FrostCard card,string name,int amount)
    {
        // Power cards describe their base ability; stats apply only when gains resolve.
        if(card.Type==CardType.Power)return new DynamicVar(name,amount);
        bool armor=name=="Amount" && card is RimeCoat or ThawFrost or GlacierBody or IceDust or PermafrostWard or IceMirror or CrystalAmulet or SixfoldSnow or GuardAdvance
            || name=="Extra" && card is QuietMeditation;
        bool snow=name=="Amount" && card is SnowRoll or BlizzardComing or ColdReturn or StormCore or SnowEye or SilverFrost
            || name=="Extra" && card is IceDust or GuardAdvance or PrismBlast;
        return armor||snow ? new FrostGainVar(name,amount,armor) : new DynamicVar(name,amount);
    }
    public override void UpdateCardPreview(CardModel card,CardPreviewMode previewMode,Creature? target,bool runGlobalHooks)
    {
        PreviewValue=BaseValue;
        if(!runGlobalHooks || BaseValue<=0)return;
        var owner=card.Owner.Creature;
        if(armor && owner.HasPower<BlessingWindPower>())PreviewValue=Math.Max(0,BaseValue+owner.GetPowerAmount<DexterityPower>());
        if(!armor && owner.HasPower<HeartInscriptionPower>())PreviewValue=Math.Max(0,BaseValue+owner.GetPowerAmount<StrengthPower>());
    }
}
