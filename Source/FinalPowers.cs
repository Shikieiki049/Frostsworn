using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Frostsworn;

public sealed class WinterLedger
{
    public int DebtLifeLost;
    public int FrostApplications,DebuffLayers;
    public int DepletedTurn=-1;
    public int Turn=-1;
    public bool Shattered,Stored,CrystalExhausted;
}
public static class FinalEvents
{
    private static readonly ConditionalWeakTable<PlayerCombatState,WinterLedger> Ledgers=new();
    public static WinterLedger Ledger(Player player)
    {
        var ledger=Ledgers.GetOrCreateValue(player.PlayerCombatState!);
        int turn=player.PlayerCombatState!.TurnNumber;
        if(ledger.Turn!=turn) { ledger.Turn=turn; ledger.DebuffLayers=0; ledger.Shattered=false; ledger.Stored=false; ledger.CrystalExhausted=false; }
        return ledger;
    }
    public static Task ArmorLost(Player player,int amount)=>Task.CompletedTask;
    public static async Task DebtPaid(PlayerChoiceContext context,Player player,int actualLoss)
    {
        Ledger(player).DebtLifeLost+=actualLoss;
        if(actualLoss>0 && !player.Creature.IsDead && player.Creature.GetPower<ColdBloodEchoPower>() is {} echo)
            await PowerCmd.Apply<IceArmorPower>(context,player.Creature,actualLoss*echo.Amount,player.Creature,null);
    }
    public static Task PlayedThawed(CardPlay play)
    {
        var state=ColdStorage.State(play.Card);
        state.ActivePlay=play;
        state.AttackBonus=0;
        if(play.Card.Type==CardType.Attack && play.Player.Creature.GetPower<HiddenBladePower>() is {} blade) state.AttackBonus=blade.Amount;
        return Task.CompletedTask;
    }
    public static async Task Thawed(PlayerChoiceContext context,Player player,CardModel card)
    {
        var state=ColdStorage.State(card);
        int crystals=state.CarvedCrystals;
        state.CarvedCrystals=0;
        if(player.Creature.GetPower<HeatExchangePower>() is {} heat && heat.Use(1))
            await PlayerCmd.GainEnergy(heat.Amount,player);
        if(crystals>0) await FrostActions.Crystals(player,crystals);
        if(player.Creature.GetPower<SixfoldSnowPower>() is {} six && six.Use(1))await PowerCmd.Apply<IceArmorPower>(context,player.Creature,six.Amount,player.Creature,null);
    }
    public static async Task QueueDraw(PlayerChoiceContext context,Player player,int amount)
    {
        await PowerCmd.Apply<DrawNextPower>(context,player.Creature,amount,player.Creature,null);
        player.Creature.GetPower<DrawNextPower>()!.DueTurn=player.PlayerCombatState!.TurnNumber+1;
    }
}

[HarmonyPatch(typeof(CardCmd),nameof(CardCmd.Exhaust))]
internal static class CrystalExhaustLedgerPatch
{
    public static void Postfix(CardModel card,ref Task<CardPileAddResult?> __result)
    {
        if(card is IceCrystal) __result=Observe(__result,card);
    }
    private static async Task<CardPileAddResult?> Observe(Task<CardPileAddResult?> original,CardModel card)
    {
        var result=await original;
        if(result!=null && card.Owner.PlayerCombatState!=null && card.Pile?.Type==PileType.Exhaust) FinalEvents.Ledger(card.Owner).CrystalExhausted=true;
        return result;
    }
}

[RegisterPower] public sealed class CrystalResonancePower:FrostPowerBase;
[RegisterPower] public sealed class SixfoldSnowPower:LimitedTurnPower;
[RegisterPower] public sealed class HeatExchangePower:LimitedTurnPower;
[RegisterPower] public sealed class ColdBloodEchoPower:LimitedTurnPower;
[RegisterPower] public sealed class HiddenBladePower:FrostPowerBase
{
    public override decimal ModifyDamageAdditive(Creature? target,decimal amount,ValueProp props,Creature? dealer,CardModel? cardSource,CardPlay? cardPlay)
    {
        if(dealer!=Owner || cardSource==null || cardPlay==null || !props.IsPoweredAttack()) return 0;
        var state=ColdStorage.State(cardSource);
        return ReferenceEquals(state.ActivePlay,cardPlay) ? state.AttackBonus : 0;
    }
}
[RegisterPower] public sealed class EndlessStormPower:FrostPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DynamicVar("Decay",2)];
    public void Improve(int decay)=>DynamicVars["Decay"].BaseValue=Math.Min(DynamicVars["Decay"].BaseValue,decay);
    public int Decay=>(int)DynamicVars["Decay"].BaseValue;
}
[RegisterPower] public sealed class WinterArchivePower:FrostPowerBase
{
    public override async Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext context,CombatSide side,IEnumerable<Creature> participants)
    {
        if(side!=CombatSide.Player || Owner.IsDead || !participants.Contains(Owner)) return;
        if(!Owner.HasPower<ColdStoragePower>()) await PowerCmd.Apply<ColdStoragePower>(context,Owner,1,Owner,null);
        await ColdStorage.Store(context,Owner.Player!,this,Amount);
    }
}
[RegisterPower] public sealed class DrawNextPower:FrostPowerBase
{
    public int DueTurn;
    public override decimal ModifyHandDraw(Player player,decimal count)=>player.Creature==Owner && player.PlayerCombatState!.TurnNumber>=DueTurn ? count+Amount : count;
    public override Task AfterModifyingHandDraw()=>PowerCmd.Remove(this);
}
