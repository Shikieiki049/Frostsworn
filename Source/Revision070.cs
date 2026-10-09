using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.HoverTips;
namespace Frostsworn;

[RegisterPower] public sealed class FatedStoryPower:FrostPowerBase
{
    public static int PendingReplays(CardModel card)=>ColdStorage.State(card).Pending?card.Owner.Creature.GetPowerAmount<FatedStoryPower>():0;
    public override int ModifyCardPlayCount(CardModel card,Creature? target,int playCount)
        =>card.Owner.Creature==Owner?playCount+PendingReplays(card):playCount;
}
[RegisterPower] public sealed class IceReleasePower:FrostPowerBase
{
    public static bool Applies(CardModel card)=>card.IsMutable && !card.EnergyCost.CostsX && card.Owner.PlayerCombatState!=null && ColdStorage.State(card).Pending && card.Owner.Creature.HasPower<IceReleasePower>();
    public override bool TryModifyEnergyCostInCombatLate(CardModel card,decimal originalCost,out decimal modifiedCost)
    {
        bool applies=card.Owner.Creature==Owner && Applies(card);
        modifiedCost=applies?0:originalCost;return applies;
    }
}
[RegisterPower] public sealed class FractalSnowPower:FrostPowerBase
{
    public override PowerStackType StackType=>PowerStackType.Single;
    public override Task AfterCardGeneratedForCombat(CardModel card,Player? creator)
    {
        if(card.Owner.Creature==Owner && card is IceCrystal && card.Enchantment is not SharpEnchantment)
            CardCmd.Enchant<SharpEnchantment>(card,1);
        return Task.CompletedTask;
    }
}

// Live ability tooltips display each trigger's actual yield, without storing
// Dexterity/Strength inside the ability's base amount or applying it twice.
public sealed class PowerGainVar(bool armor):DynamicVar("EffectiveAmount",0)
{
    private PowerModel? _power;
    public override void SetOwner(AbstractModel owner){base.SetOwner(owner);_power=owner as PowerModel;}
    private decimal Value=>_power?.IsMutable==true?Math.Max(0,_power.Amount+(armor
        ?(_power.Owner.HasPower<BlessingWindPower>()?_power.Owner.GetPowerAmount<DexterityPower>():0)
        :(_power.Owner.HasPower<HeartInscriptionPower>()?_power.Owner.GetPowerAmount<StrengthPower>():0))):0;
    public override string ToString()=>Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    protected override decimal GetBaseValueForIConvertible()=>Value;
}

[RegisterRelic(typeof(FrostRelicPool))]
public sealed class WinterCrown:ModRelicTemplate
{
    public override RelicRarity Rarity=>RelicRarity.Ancient;
    public override string CustomIconPath=>"res://Frostsworn/art075/WinterCrown_small.tres";
    public override string CustomBigIconPath=>"res://Frostsworn/art075/WinterCrown.tres";
    public override string CustomIconOutlinePath=>CustomIconPath;
    private bool _opening;
    public override Task BeforeCombatStart(){_opening=true;return Task.CompletedTask;}
    protected override IEnumerable<IHoverTip> AdditionalHoverTips=>[new HoverTip(new LocString("static_hover_tips","FROSTSWORN_FROST.title"),new LocString("static_hover_tips","FROSTSWORN_FROST.description"),null)];
    public override async Task BeforeSideTurnStart(PlayerChoiceContext context,CombatSide side,IReadOnlyList<Creature> participants,ICombatState combatState)
    {
        if(side!=CombatSide.Player || Owner.Creature.IsDead || !participants.Contains(Owner.Creature))return;
        Flash();
        if(_opening){_opening=false;await PowerCmd.Apply<FrostPower>(context,combatState.HittableEnemies,5,Owner.Creature,null);}
        await PowerCmd.Apply<FrostPower>(context,combatState.HittableEnemies,2,Owner.Creature,null);
        foreach(var enemy in combatState.HittableEnemies.ToArray())
            if(enemy.GetPower<FrostPower>() is {} frost)await frost.ResolveFreeze(context,Owner);
    }
}
public abstract class FrostPotion:ModPotionTemplate
{
    public override PotionUsage Usage=>PotionUsage.CombatOnly;
    public override string CustomImagePath=>"res://Frostsworn/art075/"+GetType().Name+".tres";
    public override string CustomOutlinePath=>CustomImagePath;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips=>[new HoverTip(new LocString("static_hover_tips","FROSTSWORN_COLD.title"),new LocString("static_hover_tips","FROSTSWORN_COLD.description"),null)];
}
[RegisterPotion(typeof(FrostPotionPool))] public sealed class FrostBottle:FrostPotion
{
    public override PotionRarity Rarity=>PotionRarity.Common;
    public override TargetType TargetType=>TargetType.AnyEnemy;
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DynamicVar("Amount",16)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips=>[new HoverTip(new LocString("static_hover_tips","FROSTSWORN_FROST.title"),new LocString("static_hover_tips","FROSTSWORN_FROST.description"),null)];
    protected override async Task OnUse(PlayerChoiceContext context,Creature? target)
    {
        if(target==null || target.IsDead)return;
        await PowerCmd.Apply<FrostPower>(context,target,DynamicVars["Amount"].BaseValue,Owner.Creature,null);
        if(target.GetPower<FrostPower>() is {} frost)await frost.ResolveFreeze(context,Owner);
    }
}
[RegisterPotion(typeof(FrostPotionPool))] public sealed class LiquidNitrogen:FrostPotion
{
    public override PotionRarity Rarity=>PotionRarity.Uncommon;
    public override TargetType TargetType=>TargetType.Self;
    protected override async Task OnUse(PlayerChoiceContext context,Creature? target)
    {
        if(!Owner.Creature.HasPower<ColdStoragePower>())await PowerCmd.Apply<ColdStoragePower>(context,Owner.Creature,1,Owner.Creature,null);
        await ColdStorage.Store(context,Owner,this,3);
    }
}
[RegisterPotion(typeof(FrostPotionPool))] public sealed class FractalSnowflake:FrostPotion
{
    public override PotionRarity Rarity=>PotionRarity.Rare;
    public override TargetType TargetType=>TargetType.Self;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips=>[FrostKeywords.Crystal(false,true),ModelDb.Enchantment<SharpEnchantment>().HoverTip];
    protected override Task OnUse(PlayerChoiceContext context,Creature? target)=>PowerCmd.Apply<FractalSnowPower>(context,Owner.Creature,1,Owner.Creature,null);
}
