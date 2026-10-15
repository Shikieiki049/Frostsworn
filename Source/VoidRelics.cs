using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using System.Reflection;
using STS2RitsuLib.RunData;
namespace Frostsworn;

public sealed class VoidRelicRunData
{public List<string> ExcludedOriginals {get;set;}=new();}
public static class VoidRelics
{
    public static readonly Color Color=new("B780B0");
    public static RelicRarity Rarity {get;private set;}
    public static RunSavedData<VoidRelicRunData> Data {get;private set;}=null!;
    public static void Initialize()
    {
        Rarity=RitsuLibFramework.RegisterDynamicEnumValue<RelicRarity>(Entry.ModId,"void");
        using(RitsuLibFramework.BeginModDataRegistration(Entry.ModId))
            Data=RitsuLibFramework.GetRunSavedDataStore(Entry.ModId).Register("void_relics",()=>new VoidRelicRunData(),new RunSavedDataOptions {SyncLobbyOnChange=false});
    }
    public static readonly Type[] Types={typeof(BloodlessCord),typeof(DrownedFloat),typeof(RuminationSac),typeof(MirrorParasite),typeof(MemorySlough),typeof(LightMothCocoon),typeof(CurledPolyp),typeof(TriocularWax),typeof(FaceborrowMembrane),typeof(MeristemTeeth),typeof(HangingStarSac),typeof(MarrowTouch),typeof(MoldyCookie),typeof(GymnospermPendant),typeof(OverdriveGearbox),typeof(LighthouseJelly)};
    public static readonly Type[] Originals={typeof(BloodVial),typeof(Anchor),typeof(BagOfPreparation),typeof(BronzeScales),typeof(CentennialPuzzle),typeof(Lantern),typeof(Orichalcum),typeof(Candelabra),typeof(SparklingRouge),typeof(StoneCracker),typeof(Chandelier),typeof(MeatOnTheBone),typeof(LastingCandy),typeof(ReptileTrinket),typeof(TuningFork),typeof(LizardTail)};
    public static RelicModel Canonical(Type t)=>ModelDb.GetById<RelicModel>(ModelDb.GetId(t));
    public static bool Suppressed(RelicModel r)=>r.IsMutable && r.Owner!=null && r.Owner.Relics.OfType<VoidRelic>().Any(v=>v.OriginalType==r.GetType());
    public static bool OriginalBlocked(Type t,IRunState run)=>(run is RunState saved && Data.Get(saved).ExcludedOriginals.Contains(ModelDb.GetId(t).Entry)) || run.Players.SelectMany(p=>p.Relics).OfType<VoidRelic>().Any(v=>v.OriginalType==t);
    public static IEnumerable<RelicModel> Candidates(Player p)=>p.Relics.Where(r=>!r.IsMelted && Originals.Contains(r.GetType()) && !p.Relics.OfType<VoidRelic>().Any(v=>v.OriginalType==r.GetType()));
    public static IEnumerable<RelicModel> NewCandidates(Player p)=>Types.Select(Canonical).Where(r=>p.RunState.Players.All(x=>x.Relics.All(held=>held.Id!=r.Id)));
}
public abstract class VoidRelic:ModRelicTemplate
{
    public Type OriginalType=>VoidRelics.Originals[Array.IndexOf(VoidRelics.Types,GetType())];
    public override RelicRarity Rarity=>VoidRelics.Rarity;
    public override bool IsAllowedInShops=>false;
    public override bool IsAllowed(IRunState runState)=>false;
    public override int MerchantCost=>999999999;
    public override string CustomIconPath=>$"res://Frostsworn/void/{GetType().Name}_small.png";
    public override string CustomBigIconPath=>$"res://Frostsworn/void/{GetType().Name}.png";
    public override string CustomIconOutlinePath=>CustomIconPath;
    [SavedProperty] public int VoidCount {get;set;}
    [SavedProperty] public int VoidFloor {get;set;}
    [SavedProperty] public bool VoidUsed {get;set;}
    [SavedProperty] public bool VoidDisabled {get;set;}
    [SavedProperty] public int VoidTurn {get;set;}
    private CardModel? mothCard;
    private bool pendingFloat;
    public override bool ShowCounter=>this is MoldyCookie or OverdriveGearbox;
    public override int DisplayAmount=>this is MoldyCookie?VoidCount%2:VoidCount;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if(this is FaceborrowMembrane)yield return HoverTipFactory.FromPower<StrengthPower>();
            if(this is FaceborrowMembrane or GymnospermPendant)yield return HoverTipFactory.FromPower<DexterityPower>();
            if(this is MemorySlough or LightMothCocoon)yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust);
            if(this is MirrorParasite or LighthouseJelly)yield return HoverTipFactory.Static(StaticHoverTip.Block);
            if(this is OverdriveGearbox)yield return HoverTipFactory.FromPower<VigorPower>();
        }
    }
    public override Task AfterObtained()
    {
        VoidFloor=Owner.RunState.TotalFloor;
        var original=VoidRelics.Canonical(OriginalType);
        if(Owner.RunState is RunState run)VoidRelics.Data.Modify(run,data=>{if(!data.ExcludedOriginals.Contains(original.Id.Entry)){data.ExcludedOriginals.Add(original.Id.Entry);data.ExcludedOriginals.Sort(StringComparer.Ordinal);}});
        foreach(var p in Owner.RunState.Players)p.RelicGrabBag.Remove(original);
        Owner.RunState.SharedRelicGrabBag.Remove(original);
        foreach(var relic in Owner.Relics.Where(r=>r.GetType()==OriginalType))relic.Status=RelicStatus.Disabled;
        return Task.CompletedTask;
    }
    public override Task AfterRemoved()
    {foreach(var relic in Owner.Relics.Where(r=>r.GetType()==OriginalType && !r.IsUsedUp))relic.Status=RelicStatus.Normal;return Task.CompletedTask;}
    public override Task BeforeCombatStart()
    {
        VoidUsed=false;VoidDisabled=false;VoidTurn=0;mothCard=null;pendingFloat=false;Status=RelicStatus.Normal;

        return Task.CompletedTask;
    }
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if(this is not BloodlessCord || Owner.Creature.IsDead)return;
        int floor=Owner.RunState.TotalFloor,steps=Math.Max(0,floor-VoidFloor);
        VoidFloor=floor;
        if(steps>0){Flash();await CreatureCmd.Heal(Owner.Creature,steps);}
    }
    public override async Task AfterShuffle(PlayerChoiceContext context,Player shuffler)
    {
        if(this is RuminationSac && shuffler==Owner && !VoidUsed)
        {VoidUsed=true;Flash();await CardPileCmd.Draw(context,3,Owner);}
    }
    public override decimal ModifyHpLostAfterOstyLate(Creature target,decimal amount,ValueProp props,Creature? dealer,CardModel? cardSource)
    {
        pendingFloat=this is DrownedFloat && !VoidUsed && target==Owner.Creature && amount>0 && dealer?.Side==CombatSide.Enemy && props.IsPoweredAttack();
        return pendingFloat?Math.Max(0,amount-15):amount;
    }
    public override Task AfterModifyingHpLostAfterOsty()
    {if(pendingFloat){VoidUsed=true;pendingFloat=false;Flash();}return Task.CompletedTask;}
    public override async Task AfterDamageReceived(PlayerChoiceContext context,Creature target,DamageResult result,ValueProp props,Creature? dealer,CardModel? cardSource)
    {
        if(target!=Owner.Creature || !CombatManager.Instance.IsInProgress)return;
        bool enemy=dealer?.Side==CombatSide.Enemy;
        if(this is CurledPolyp && enemy && result.UnblockedDamage>0){VoidDisabled=true;Status=RelicStatus.Disabled;}
        if(this is MirrorParasite && !VoidUsed && enemy && props.IsPoweredAttack() && result.BlockedDamage>0)
        {VoidUsed=true;Flash();if(!dealer!.IsDead)await CreatureCmd.Damage(context,dealer,Math.Min(11,result.BlockedDamage),ValueProp.Unpowered,Owner.Creature);}
        if(this is MemorySlough && !VoidUsed && result.UnblockedDamage>0 && !Owner.Creature.IsDead)
        {
            VoidUsed=true;var top=PileType.Draw.GetPile(Owner).Cards.Take(3).ToArray();if(top.Length==0)return;
            Flash();var selected=(await CardSelectCmd.FromSimpleGrid(context,top,Owner,new CardSelectorPrefs(new LocString("static_hover_tips","FROSTSWORN_VOID_PICK.prompt"),1))).FirstOrDefault();
            if(selected!=null)await CardPileCmd.Add(selected,PileType.Hand);
            foreach(var c in top.Where(c=>c!=selected))await CardCmd.Exhaust(context,c);
        }
    }
    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext context,Player player)
    {
        if(player!=Owner || Owner.Creature.IsDead)return;
        int turn=Owner.PlayerCombatState!.TurnNumber;
        if(this is MirrorParasite)VoidUsed=false;
        if(this is LightMothCocoon && turn==1 && !VoidUsed)
        {
            VoidUsed=true;var card=(await CardSelectCmd.FromHand(context,Owner,new CardSelectorPrefs(new LocString("static_hover_tips","FROSTSWORN_VOID_PICK.prompt"),1),null,this)).FirstOrDefault();
            if(card!=null){Flash();card.EnergyCost.AddThisTurn(-2);mothCard=card;VoidTurn=turn;}
        }
        if(this is TriocularWax && turn==2 && !VoidUsed)
        {
            VoidUsed=true;var cards=PileType.Hand.GetPile(Owner).Cards.Where(c=>!c.EnergyCost.CostsX).ToList();
            for(int n=0;n<3 && cards.Count>0;n++){var c=Owner.RunState.Rng.CombatCardSelection.NextItem(cards)!;cards.Remove(c);c.EnergyCost.AddThisTurn(-1);}Flash();
        }
        if(this is HangingStarSac && turn==4 && !VoidUsed)
        {VoidUsed=true;foreach(var c in PileType.Hand.GetPile(Owner).Cards.Where(c=>!c.EnergyCost.CostsX))c.EnergyCost.SetThisTurn(0);Flash();}
        if(this is FaceborrowMembrane && turn==1 && !VoidUsed)
        {VoidUsed=true;Flash();await GainDecay<VoidFadingStrengthPower,StrengthPower>(context,3);await GainDecay<VoidFadingDexterityPower,DexterityPower>(context,3);}
    }
    public override bool TryModifyKeywordsInCombat(CardModel card,ISet<CardKeyword> keywords)
    {
        if(this is LightMothCocoon && card==mothCard && Owner.PlayerCombatState?.TurnNumber==VoidTurn){keywords.Add(CardKeyword.Exhaust);return true;}
        return false;
    }
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context,CombatSide side,IEnumerable<Creature> participants)
    {if(this is LightMothCocoon && side==CombatSide.Player && participants.Contains(Owner.Creature))mothCard=null; if(this is CurledPolyp && side==CombatSide.Enemy && !VoidDisabled && !Owner.Creature.IsDead){Flash();await CreatureCmd.Heal(Owner.Creature,4);}}
    public override async Task AfterPotionUsed(PotionModel potion,Creature? target)
    {if(this is GymnospermPendant && potion.Owner==Owner && CombatManager.Instance.IsInProgress){Flash();await GainDecay<VoidFadingDexterityPower,DexterityPower>(new ThrowingPlayerChoiceContext(),2);}}
    async Task GainDecay<TDecay,TStat>(PlayerChoiceContext context,int n) where TDecay:PowerModel,new() where TStat:PowerModel,new()
    {await PowerCmd.Apply<TStat>(context,Owner.Creature,n,Owner.Creature,null);await PowerCmd.Apply<TDecay>(context,Owner.Creature,n,Owner.Creature,null);}
    public override async Task AfterCardPlayed(PlayerChoiceContext context,CardPlay play)
    {
        if(this is not OverdriveGearbox || play.Player!=Owner)return;
        VoidCount++;if(VoidCount>=7){VoidCount-=7;Flash();await PowerCmd.Apply<VigorPower>(context,Owner.Creature,Math.Min(20,Owner.PlayerCombatState!.TurnNumber*2),Owner.Creature,null);}InvokeDisplayAmountChanged();
    }
    public override async Task AfterCombatVictory(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {if(this is MoldyCookie){VoidCount++;InvokeDisplayAmountChanged();} if(this is MarrowTouch && !Owner.Creature.IsDead && Owner.Creature.CurrentHp*2>=Owner.Creature.MaxHp){Flash();await CreatureCmd.GainMaxHp(Owner.Creature,5);}}
    public override decimal ModifyCardRewardUpgradeOdds(Player p,CardModel c,decimal odds)=>this is MoldyCookie && p==Owner && VoidCount>0 && VoidCount%2==0?1:odds;
    public override async Task BeforeHandDraw(Player player,PlayerChoiceContext context,ICombatState combatState)
    {
        if(this is not MeristemTeeth || player!=Owner || VoidUsed)return;
        VoidUsed=true;var pool=PileType.Draw.GetPile(Owner).Cards.Where(c=>c.IsUpgradable).ToArray();int count=Math.Min(2,pool.Length);if(count==0)return;
        var chosen=(await CardSelectCmd.FromSimpleGrid(context,pool,Owner,new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt,count))).ToArray();
        if(chosen.Length==0)return;Flash();CardCmd.Upgrade(chosen,CardPreviewStyle.HorizontalLayout);
        var discarded=(await CardSelectCmd.FromSimpleGrid(context,chosen,Owner,new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt,1))).FirstOrDefault();
        if(discarded!=null)await CardPileCmd.Add(discarded,PileType.Discard);
    }
    public override bool ShouldDieLate(Creature c)=>!(this is LighthouseJelly && c==Owner.Creature && c.MaxHp>10);
    public override async Task AfterPreventingDeath(Creature c)
    {
        if(this is not LighthouseJelly || c!=Owner.Creature)return;Flash();
        decimal heal=Math.Max(1m,c.MaxHp*.15m);await CreatureCmd.Heal(c,heal);
        await CreatureCmd.GainBlock(c,16,ValueProp.Unpowered,null);await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(),c,11,false);
    }
}
[RegisterRelic(typeof(SharedRelicPool))] public sealed class BloodlessCord:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class DrownedFloat:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class RuminationSac:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class MirrorParasite:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class MemorySlough:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class LightMothCocoon:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class CurledPolyp:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class TriocularWax:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class FaceborrowMembrane:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class MeristemTeeth:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class HangingStarSac:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class MarrowTouch:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class MoldyCookie:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class GymnospermPendant:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class OverdriveGearbox:VoidRelic;
[RegisterRelic(typeof(SharedRelicPool))] public sealed class LighthouseJelly:VoidRelic;

public abstract class VoidFadingPower:ModPowerTemplate
{
    public override PowerType Type=>PowerType.Buff;
    public override PowerStackType StackType=>PowerStackType.Counter;
    public override string CustomIconPath=>$"res://Frostsworn/void/{GetType().Name}.png";
    public override string CustomBigIconPath=>CustomIconPath;
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context,CombatSide side,IEnumerable<Creature> participants)
    {
        if(side!=Owner.Side || !participants.Contains(Owner))return;
        if(this is VoidFadingStrengthPower)await PowerCmd.Apply<StrengthPower>(context,Owner,-1,Owner,null);
        else await PowerCmd.Apply<DexterityPower>(context,Owner,-1,Owner,null);
        await PowerCmd.Decrement(this);
    }
}
[RegisterPower]public sealed class VoidFadingStrengthPower:VoidFadingPower;
[RegisterPower]public sealed class VoidFadingDexterityPower:VoidFadingPower;

[HarmonyPatch]public static class VoidSuppressHooksPatch
{
    static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(RunState),"IterateHookListeners"),AccessTools.Method(typeof(CombatState),"IterateHookListeners")};
    static void Postfix(ref IEnumerable<AbstractModel> __result)=>__result=__result.Where(m=>m is not RelicModel r || !VoidRelics.Suppressed(r));
}
[HarmonyPatch]public static class VoidAllowedPatch
{
    static IEnumerable<MethodBase> TargetMethods()=>VoidRelics.Originals.Append(typeof(RelicModel)).Select(t=>AccessTools.Method(t,nameof(RelicModel.IsAllowed))).Distinct();
    static void Postfix(RelicModel __instance,IRunState runState,ref bool __result)
    {if(__instance is VoidRelic || VoidRelics.OriginalBlocked(__instance.GetType(),runState))__result=false;}
}
[HarmonyPatch(typeof(RelicModel),nameof(RelicModel.Status),MethodType.Getter)]public static class VoidStatusPatch
{static void Postfix(RelicModel __instance,ref RelicStatus __result){if(VoidRelics.Suppressed(__instance))__result=RelicStatus.Disabled;}}



