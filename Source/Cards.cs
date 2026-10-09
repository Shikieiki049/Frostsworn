using MegaCrit.Sts2.Core.HoverTips;
using HarmonyLib;
namespace Frostsworn;
public sealed record CardSpec(int Cost, CardType Type, CardRarity Rarity, TargetType Target,
    int Damage=0,int DamageUpgrade=0,int Block=0,int BlockUpgrade=0,int Amount=0,int AmountUpgrade=0,
    bool Exhaust=false,int CostUpgrade=0,int Extra=0,int ExtraUpgrade=0,int Third=0,int ThirdUpgrade=0,
    bool Retain=false,bool Innate=false,bool RemoveExhaustUpgrade=false,bool RetainUpgrade=false,bool InnateUpgrade=false,bool Unplayable=false,bool Ethereal=false,bool CostsX=false);
public abstract class FrostCard(CardSpec spec):ModCardTemplate(spec.Cost,spec.Type,spec.Rarity,spec.Target)
{
    public CardSpec Spec=>spec;
    public override CardPoolModel VisualCardPool => this is IceCrystal or Depleted or FatedStory or AbsoluteBeam ? ModelDb.CardPool<FrostCardPool>() : base.VisualCardPool;
    protected override bool HasEnergyCostX=>spec.CostsX;
    public override int MaxUpgradeLevel=>this is Depleted?0:base.MaxUpgradeLevel;
    public override bool GainsBlock=>spec.Block>0 || this is ShellRecycle;
    public override string CustomPortraitPath
    {
        get
        {
            var supplied="res://Frostsworn/card_art/"+GetType().Name+".tres";
            return ResourceLoader.Exists(supplied) ? supplied
                : (this is FrostStrike or FrostDefend or FreezeRay ? "res://Frostsworn/art075/" : "res://Frostsworn/art/")+GetType().Name+".png";
        }
    }
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { if(spec.Exhaust)yield return CardKeyword.Exhaust; if(spec.Retain)yield return CardKeyword.Retain; if(spec.Innate)yield return CardKeyword.Innate; if(spec.Unplayable)yield return CardKeyword.Unplayable; if(spec.Ethereal)yield return CardKeyword.Ethereal; }
    }
    protected override IEnumerable<DynamicVar> CanonicalVars=>new DynamicVar[]{new DamageVar(spec.Damage,ValueProp.Move),new BlockVar(spec.Block,ValueProp.Move),
        this is IceCrystal ? new CrystalFrostVar(spec.Amount) : this is OverdrawWarmth or QuietMeditation or HeatExchange ? new EnergyVar("Amount",spec.Amount) : FrostGainVar.Create(this,"Amount",spec.Amount),FrostGainVar.Create(this,"Extra",spec.Extra),new DynamicVar("Third",spec.Third)}.Concat(FrostCalculations.Vars(this,spec));
    protected override void OnUpgrade()
    {
        if(spec.DamageUpgrade!=0)DynamicVars.Damage.UpgradeValueBy(spec.DamageUpgrade);
        if(spec.BlockUpgrade!=0)DynamicVars.Block.UpgradeValueBy(spec.BlockUpgrade);
        if(spec.AmountUpgrade!=0)DynamicVars["Amount"].UpgradeValueBy(spec.AmountUpgrade);
        if(spec.ExtraUpgrade!=0)DynamicVars["Extra"].UpgradeValueBy(spec.ExtraUpgrade);
        if(spec.ThirdUpgrade!=0)DynamicVars["Third"].UpgradeValueBy(spec.ThirdUpgrade);
        FrostCalculations.Upgrade(this,spec);
        if(spec.CostUpgrade!=0)EnergyCost.UpgradeBy(spec.CostUpgrade);
        if(spec.RemoveExhaustUpgrade)RemoveKeyword(CardKeyword.Exhaust);
        if(spec.RetainUpgrade)AddKeyword(CardKeyword.Retain);
        if(spec.InnateUpgrade)AddKeyword(CardKeyword.Innate);
    }
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            string text=Description.GetRawText();
            if(FrostText.Contains(text,"力量"))yield return HoverTipFactory.FromPower<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>();
            if(FrostText.Contains(text,"敏捷"))yield return HoverTipFactory.FromPower<MegaCrit.Sts2.Core.Models.Powers.DexterityPower>();
            if(FrostText.Contains(text,"易伤"))yield return HoverTipFactory.FromPower<MegaCrit.Sts2.Core.Models.Powers.VulnerablePower>();
            if(FrostText.Contains(text,"虚弱"))yield return HoverTipFactory.FromPower<MegaCrit.Sts2.Core.Models.Powers.WeakPower>();
            if(FrostText.Contains(text,"消耗")&&!Keywords.Contains(CardKeyword.Exhaust))yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust);
            if(text.Contains("energyIcons"))yield return HoverTipFactory.ForEnergy(this);
            foreach(var pair in new[]{("寒霜","FROST"),("碎冰","SHATTER"),("冰甲","ARMOR"),("自霜","SELF"),("雪势","SNOW")})
                if(FrostText.Contains(text,pair.Item1)) yield return Tip(pair.Item2);
            if(FrostText.Contains(text,"冷藏")||FrostText.Contains(text,"解冻"))yield return Tip("COLD");
            if(this is not IceCrystal && FrostText.Contains(text,"冰晶"))
                yield return FrostKeywords.Crystal(IsUpgraded && this is CrystalVolley or Glitter,this is Glitter);
            if(this is Glitter) yield return ModelDb.Enchantment<SharpEnchantment>().HoverTip;
            if(this is Pierce)yield return HoverTipFactory.FromCard<Depleted>();
        }
    }
    private static HoverTip Tip(string key)=>new(new LocString("static_hover_tips","FROSTSWORN_"+key+".title"),new LocString("static_hover_tips","FROSTSWORN_"+key+".description"),null);
    protected override Task OnPlay(PlayerChoiceContext context,CardPlay play)=>RevisedEffects.Play(this,context,play);
    public override Task AfterCardChangedPiles(CardModel card,PileType oldPileType,AbstractModel? clonedBy)
    {
        if(card!=this)return Task.CompletedTask;
        if(this is WhiteNightRush && card.Pile?.Type==Entry.ColdPileType && oldPileType!=Entry.ColdPileType)EnergyCost.AddThisCombat(-1);
        if(this is not Depleted || card.Pile?.Type!=PileType.Hand || oldPileType==PileType.Hand)return Task.CompletedTask;
        FinalEvents.Ledger(Owner).DepletedTurn=Owner.PlayerCombatState!.TurnNumber;
        return PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.NoDrawPower>(new ThrowingPlayerChoiceContext(),Owner.Creature,1,Owner.Creature,this);
    }
}
[HarmonyPatch(typeof(CardModel),nameof(CardModel.Description),MethodType.Getter)]
internal static class FrostUpgradeDescriptionPatch
{
    public static void Postfix(CardModel __instance,ref LocString __result)
    {
        if(__instance is not FrostCard)return;
        string key=__instance.Id.Entry+(__instance.IsUpgraded?".upgradeDescription":".description");
        if(__instance.Pile?.Type is PileType.Hand or PileType.Play && LocString.Exists("cards",key+".combat")
            && (__instance is not AbsoluteBeam || __instance.DynamicVars["BeamFrost"].PreviewValue>0))key+=".combat";
        __result=new LocString("cards",key);
    }
}
