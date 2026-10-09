using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Frostsworn;

public abstract class FrostPowerBase : ModPowerTemplate
{
    protected override IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip> AdditionalHoverTips
    {
        get
        {
            if(Description.GetRawText().Contains("冰晶"))yield return FrostKeywords.Crystal();
        }
    }
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType Type => PowerType.Buff;
    public override string CustomIconPath => "res://Frostsworn/icons/" + GetType().Name + ".svg";
    public override string CustomBigIconPath => CustomIconPath;
}

public sealed class FreezeState
{
    public int Count;
    public bool NeedsNormalMove;
}
public static class FrostActions
{
    private static readonly ConditionalWeakTable<Creature, FreezeState> Freezes = new();
    public static FreezeState Freeze(Creature c) => Freezes.GetOrCreateValue(c);
    public static int Threshold(Creature c) => Rules.FreezeThreshold(c.MaxHp, Freeze(c).Count);
    public static bool CanFreeze(Creature c) => !c.IsDead && c.Monster != null &&
        !Freeze(c).NeedsNormalMove && c.Monster.NextMove.Id != MonsterModel.stunnedMoveId &&
        c.Monster.NextMove.CanTransitionAway;

    public static async Task Frost(PlayerChoiceContext context, Creature target, int amount, CardModel source)
    {
        if (target.IsDead) return;
        if (!CanFreeze(target)) amount = Math.Min(amount, Threshold(target) - 1 - target.GetPowerAmount<FrostPower>());
        if (amount > 0) await PowerCmd.Apply<FrostPower>(context, target, amount, source.Owner.Creature, source);
    }
    public static async Task<int> Shatter(PlayerChoiceContext context, Creature target, int count, CardModel source)
    {
        var power = target.GetPower<FrostPower>();
        if (power == null) return 0;
        int taken = Math.Min(count, power.Amount);
        await PowerCmd.ModifyAmount(context, power, -taken, source.Owner.Creature, source);
        if (taken > 0) await ExpansionEvents.Shattered(context, source.Owner);
        return taken;
    }
    public static async Task Crystals(Player owner, int count, bool upgraded=false, bool sharp=false)
    {
        if(count<=0 || owner.Creature.IsDead) return;
        if(owner.Creature.GetPower<CrystalResonancePower>() is {} resonance) count+=resonance.Amount;
        for (int i = 0; i < count && !owner.Creature.IsDead; i++)
        {
            var card = owner.Creature.CombatState!.CreateCard<IceCrystal>(owner);
            if(upgraded) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
            if(sharp) CardCmd.Enchant<SharpEnchantment>(card,1);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
        }
    }
    public static Task Gain<T>(PlayerChoiceContext context, Player owner, int amount, CardModel source) where T : PowerModel
        => PowerCmd.Apply<T>(context, owner.Creature, amount, owner.Creature, source);
}

// Artifact ignores invisible powers in the base game. Restore only Frost's debuff interaction;
// let PowerCmd run Artifact's normal consumption and other receive hooks exactly once.
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Powers.ArtifactPower), nameof(MegaCrit.Sts2.Core.Models.Powers.ArtifactPower.TryModifyPowerAmountReceived))]
internal static class FrostArtifactPatch
{
    public static void Postfix(MegaCrit.Sts2.Core.Models.Powers.ArtifactPower __instance, PowerModel canonicalPower,
        Creature target, decimal amount, ref decimal modifiedAmount, ref bool __result)
    {
        if (__result || canonicalPower is not FrostPower || target != __instance.Owner || amount <= 0) return;
        modifiedAmount = 0;
        __result = true;
    }
}

[RegisterPower]
public sealed class FrostPower : FrostPowerBase
{
    protected override bool IsVisibleInternal => false;
    public override PowerType Type => PowerType.Debuff;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Threshold", 12)];
    public override Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay cardPlay)=>ResolveFreeze(context,cardPlay.Player);
    public async Task ResolveFreeze(PlayerChoiceContext context,Player player)
    {
        if (Owner.IsDead) return;
        int threshold = FrostActions.Threshold(Owner);
        if (!FrostActions.CanFreeze(Owner))
        {
            if (Amount >= threshold) SetAmount(threshold - 1);
            return;
        }
        if (Amount < threshold) return;
        string next = Owner.Monster!.NextMove.Id;
        var state = FrostActions.Freeze(Owner);
        await PowerCmd.Remove(this);
        // State changes only once the native stun actually takes effect.
        await CreatureCmd.Stun(Owner, next);
        if (Owner.Monster.NextMove.Id == MonsterModel.stunnedMoveId)
        {
            state.Count++;
            state.NeedsNormalMove = true;
            await ExpansionEvents.Frozen(context, player, Owner);
        }
    }
}

// Observe completion of the actual move, rather than assuming every enemy turn contains an action.
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.PerformMove))]
internal static class CompletedMovePatch
{
    public static void Prefix(MonsterModel __instance, out bool __state)
        => __state = __instance.NextMove.Id != MonsterModel.stunnedMoveId;
    public static void Postfix(MonsterModel __instance, bool __state, ref Task __result)
        => __result = Observe(__result, __instance, __state);
    private static async Task Observe(Task original, MonsterModel monster, bool wasNormal)
    {
        await original;
        if (wasNormal) FrostActions.Freeze(monster.Creature).NeedsNormalMove = false;
    }
}

[RegisterPower]
public sealed class IceArmorPower : FrostPowerBase
{
    protected override bool IsVisibleInternal => false;
    private int _pendingAbsorb;
    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // Constrict is Unpowered but blockable. Unblockable HP loss (including
        // SelfFrost) bypasses armor; preserve absorption of powered attacks.
        if (target != Owner || (!props.IsPoweredAttack() && props.HasFlag(ValueProp.Unblockable)) || amount <= 0) return amount;
        _pendingAbsorb = Rules.Absorb(Amount, amount);
        return Math.Max(0, amount - _pendingAbsorb);
    }
    public override async Task AfterModifyingHpLostBeforeOsty()
    {
        int spent = Math.Min(Amount, _pendingAbsorb);
        _pendingAbsorb = 0;
        if (spent == 0) return;
        SetAmount(Amount - spent);
        if (Amount <= 0) await PowerCmd.Remove(this);
        if (Owner.Player!=null) await FinalEvents.ArmorLost(Owner.Player,spent);
        IceArmorHitReceipt.Record(Owner);
    }
    public override async Task BeforeHandDrawLate(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature != Owner || Owner.IsDead) return;
        if (Owner.HasPower<GlacierBodyPower>()) return;
        int lost=Rules.Melt(Amount);
        await PowerCmd.ModifyAmount(context, this, -lost, Owner, null);
        await FinalEvents.ArmorLost(player,lost);
        if(lost>0 && player.GetRelic<FrozenSoil>() is {} soil)await soil.Melted(context);
    }
}

// Associate actual armor consumption with the next HP result, including when armor
// has just been removed. Previews never enter AfterModifyingHpLostBeforeOsty.
internal static class IceArmorHitReceipt
{
    private static readonly ConditionalWeakTable<Creature, object> Pending = new();
    private static readonly ConditionalWeakTable<DamageResult, object> Absorbed = new();
    public static void Record(Creature owner) => Pending.GetOrCreateValue(owner);
    public static void Complete(Creature owner, DamageResult result)
    {
        if (Pending.Remove(owner) && result.Props.IsPoweredAttack() && result.UnblockedDamage == 0)
            Absorbed.GetOrCreateValue(result);
    }
    public static bool FullyAbsorbed(DamageResult result) => Absorbed.TryGetValue(result, out _);
}

[HarmonyPatch(typeof(Creature), nameof(Creature.LoseHpInternal))]
internal static class IceArmorHitReceiptPatch
{
    public static void Postfix(Creature __instance, DamageResult __result)
        => IceArmorHitReceipt.Complete(__instance, __result);
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Powers.ImbalancedPower), nameof(PowerModel.AfterDamageGiven))]
internal static class IceArmorImbalancedPatch
{
    public static void Prefix(ref DamageResult result)
    {
        if (result.WasFullyBlocked || !IceArmorHitReceipt.FullyAbsorbed(result)) return;
        // Only Imbalanced sees this substitute. Preserve its native BowlbugRock
        // branch without changing shared damage history or unrelated block hooks.
        result = new DamageResult(result.Receiver, result.Props) { WasFullyBlocked = true };
    }
}

[RegisterPower]
public sealed class SelfFrostPower : FrostPowerBase
{
    // A payment marker, so Artifact does not negate the price of drawing/energy.
    public override PowerType Type => PowerType.None;
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature != Owner || Owner.IsDead) return;
        int debt = Amount;
        await PowerCmd.Remove(this);
        int before=Owner.CurrentHp;
        var results=await CreatureCmd.Damage(context, Owner, debt, ValueProp.Unblockable | ValueProp.Unpowered, Owner);
        int actual=Math.Min(before,results.Sum(r=>r.UnblockedDamage));
        await FinalEvents.DebtPaid(context,player,actual);
    }
}

[RegisterPower]
public sealed class SnowPower : FrostPowerBase
{
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner.IsDead || !participants.Contains(Owner)) return;
        int damage = Amount;
        await CreatureCmd.Damage(context, CombatState.GetOpponentsOf(Owner).Where(c => !c.IsDead).ToArray(), damage, ValueProp.Unpowered, Owner);
        SetAmount(Owner.GetPower<EndlessStormPower>() is {} storm ? Math.Max(0,damage-storm.Decay) : damage / 2);
        if (Amount == 0) await PowerCmd.Remove(this);
    }
}

[RegisterPower]
public sealed class ColdExpansionPower : FrostPowerBase;
[RegisterPower]
public sealed class SlowReleasePower : FrostPowerBase;

[RegisterPower]
public sealed class ColdStoragePower : FrostPowerBase
{
    protected override bool IsVisibleInternal => false;
    public override PowerType Type => PowerType.None;
    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext context, Player player)
    {
        if (player.Creature != Owner || Owner.IsDead) return;
        int extra=0;
        if(Owner.GetPower<ThawNextPower>() is {} next && player.PlayerCombatState!.TurnNumber>=next.DueTurn)
        { extra=next.Amount; await PowerCmd.Remove(next); }
        await ColdStorage.Thaw(context, player, 1 + Owner.GetPowerAmount<SlowReleasePower>() + extra);
    }
}
