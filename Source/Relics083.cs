using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.HoverTips;
namespace Frostsworn;

public abstract class FrostRelic : ModRelicTemplate
{
    public override string CustomIconPath => "res://Frostsworn/relics083/"+GetType().Name+"_small.tres";
    public override string CustomBigIconPath => "res://Frostsworn/relics083/"+GetType().Name+".tres";
    public override string CustomIconOutlinePath => CustomIconPath;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            foreach(var (term,key) in new[]{("寒霜","FROSTSWORN_FROST"),("冰封","FROSTSWORN_FROST"),("冷藏","FROSTSWORN_COLD"),("冰甲","FROSTSWORN_ARMOR"),("雪势","FROSTSWORN_SNOW")}.DistinctBy(x=>x.Item2))
                if(DynamicDescription.GetRawText().Contains(term) || (key=="FROSTSWORN_FROST" && DynamicDescription.GetRawText().Contains("冰封")))
                    yield return new HoverTip(new LocString("static_hover_tips",key+".title"),new LocString("static_hover_tips",key+".description"),null);
        }
    }
}
[RegisterRelic(typeof(FrostRelicPool))]
public sealed class SnowBookmark : FrostRelic
{
    public override RelicRarity Rarity=>RelicRarity.Uncommon;
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context,CombatSide side,IEnumerable<Creature> participants)
    {
        if(side!=CombatSide.Player || !participants.Contains(Owner.Creature) || Owner.Creature.IsDead)return;
        int n=ColdStorage.Pile(Owner).Cards.Count;
        if(n>0){Flash();await PowerCmd.Apply<IceArmorPower>(context,Owner.Creature,n,Owner.Creature,null);}
    }
}
[RegisterRelic(typeof(FrostRelicPool))]
public sealed class PolarGlobe : FrostRelic
{
    public override RelicRarity Rarity=>RelicRarity.Rare;
    [SavedProperty] public int StoredCount {get;set;}
    public override bool ShowCounter=>true;
    public override int DisplayAmount=>StoredCount;
    public async Task Stored(PlayerChoiceContext context)
    {
        StoredCount++;InvokeDisplayAmountChanged();
        if(StoredCount<3)return;
        StoredCount-=3;InvokeDisplayAmountChanged();Flash();
        await ColdStorage.SelectThaw(context,Owner,1);
    }
}
[RegisterRelic(typeof(FrostRelicPool))]
public sealed class WinterBottle : FrostRelic
{
    public override RelicRarity Rarity=>RelicRarity.Rare;
    private bool _used;
    public override Task BeforeCombatStart(){_used=false;return Task.CompletedTask;}
    public override async Task BeforeHandDraw(Player player,PlayerChoiceContext context,ICombatState combatState)
    {
        if(player!=Owner || _used || Owner.Creature.IsDead)return;
        _used=true;Flash();await ColdStorage.StoreFromPile(context,Owner,Owner.PlayerCombatState!.DrawPile,1,required:true);
    }
}
[RegisterRelic(typeof(FrostRelicPool))]
public sealed class IceKey : FrostRelic
{
    public override RelicRarity Rarity=>RelicRarity.Uncommon;
    private bool _used;
    public override Task BeforeCombatStart(){_used=false;return Task.CompletedTask;}
    public override Task BeforeSideTurnStart(PlayerChoiceContext context,CombatSide side,IReadOnlyList<Creature> participants,ICombatState combatState)
    {if(side==CombatSide.Player && participants.Contains(Owner.Creature))_used=false;return Task.CompletedTask;}
    public async Task Frozen(PlayerChoiceContext context,Creature target)
    {
        if(_used || Owner.Creature.IsDead || target.IsDead)return;
        _used=true;Flash();await CreatureCmd.Damage(context,target,12,ValueProp.Unpowered,Owner.Creature);
    }
}
[RegisterRelic(typeof(FrostRelicPool))]
public sealed class FrozenSoil : FrostRelic
{
    public override RelicRarity Rarity=>RelicRarity.Common;
    private bool _used;
    public override Task BeforeCombatStart(){_used=false;return Task.CompletedTask;}
    public async Task Melted(PlayerChoiceContext context)
    {
        if(_used || Owner.Creature.IsDead)return;
        _used=true;Flash();await PowerCmd.Apply<IceArmorPower>(context,Owner.Creature,7,Owner.Creature,null);
    }
}
[RegisterRelic(typeof(FrostRelicPool))]
public sealed class SnowPrimer : FrostRelic
{
    public override RelicRarity Rarity=>RelicRarity.Shop;
    private bool _used;
    public override Task BeforeCombatStart(){_used=false;return Task.CompletedTask;}
    public override Task BeforeSideTurnStart(PlayerChoiceContext context,CombatSide side,IReadOnlyList<Creature> participants,ICombatState combatState)
    {if(side==CombatSide.Player && participants.Contains(Owner.Creature))_used=false;return Task.CompletedTask;}
    public override Task AfterPowerAmountChanged(PlayerChoiceContext context,PowerModel power,decimal amount,Creature? applier,CardModel? cardSource)
    {
        // Double the effective gain after Strength and other gain modifiers, once, without treating it as a second gain.
        if(!_used && power is SnowPower && power.Owner==Owner.Creature && amount>0)
        {_used=true;Flash();power.SetAmount(power.Amount+(int)amount);}
        return Task.CompletedTask;
    }
}
[RegisterRelic(typeof(FrostRelicPool))]
public sealed class FrostDiploma : FrostRelic
{
    public override RelicRarity Rarity=>RelicRarity.Rare;
    public override async Task AfterDamageGiven(PlayerChoiceContext context,Creature? dealer,DamageResult result,ValueProp props,Creature target,CardModel? cardSource)
    {
        if(dealer!=Owner.Creature || target.IsDead || target.Side==Owner.Creature.Side || result.BlockedDamage+result.UnblockedDamage<=0)return;
        int n=FrostActions.CanFreeze(target)?1:Math.Min(1,FrostActions.Threshold(target)-1-target.GetPowerAmount<FrostPower>());
        if(n<=0)return;
        Flash();await PowerCmd.Apply<FrostPower>(context,target,n,Owner.Creature,cardSource);
        if(cardSource==null && target.GetPower<FrostPower>() is {} frost)await frost.ResolveFreeze(context,Owner);
    }
}
