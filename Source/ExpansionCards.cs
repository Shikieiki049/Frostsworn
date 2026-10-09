// Generated from tools/cards_data.py; edit that file.
namespace Frostsworn;

[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_FROST_STRIKE"), RegisterCharacterStarterCard(typeof(FrostswornCharacter), 4, Order = 10)]
public sealed class FrostStrike() : FrostCard(new(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy, Damage: 6, DamageUpgrade: 3)) { protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike]; }
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_FROST_DEFEND"), RegisterCharacterStarterCard(typeof(FrostswornCharacter), 4, Order = 20)]
public sealed class FrostDefend() : FrostCard(new(1, CardType.Skill, CardRarity.Basic, TargetType.Self, Block: 5, BlockUpgrade: 3)) { protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend]; }
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_CRYSTAL_RITE"), RegisterCharacterStarterCard(typeof(FrostswornCharacter), Order = 40)]
public sealed class CrystalRite() : FrostCard(new(1, CardType.Skill, CardRarity.Basic, TargetType.Self, Block: 5, BlockUpgrade: 3));
[RegisterCard(typeof(MegaCrit.Sts2.Core.Models.CardPools.TokenCardPool), FullPublicEntry = "FROSTSWORN_CARD_ICE_CRYSTAL")]
public sealed class IceCrystal() : FrostCard(new(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, Damage: 2, DamageUpgrade: 1, Amount: 1, AmountUpgrade: 1, Exhaust: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_FROST_SHATTER")]
public sealed class FrostShatter() : FrostCard(new(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, Damage: 10, Amount: 1, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_FREEZE_RAY"), RegisterCharacterStarterCard(typeof(FrostswornCharacter), Order = 30)]
public sealed class FreezeRay() : FrostCard(new(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy, Damage: 3, DamageUpgrade: 1, Amount: 10, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_RIME_COAT")]
public sealed class RimeCoat() : FrostCard(new(1, CardType.Skill, CardRarity.Common, TargetType.Self, Block: 3, BlockUpgrade: 1, Amount: 4, AmountUpgrade: 2));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_CRYSTALLIZE")]
public sealed class Crystallize() : FrostCard(new(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, Damage: 8, DamageUpgrade: 2, Amount: 2, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_CRYSTAL_CLUSTER")]
public sealed class CrystalCluster() : FrostCard(new(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, Amount: 3, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SNOW_CACHE")]
public sealed class SnowCache() : FrostCard(new(0, CardType.Skill, CardRarity.Common, TargetType.Self, Amount: 3, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_MELT_SEAL")]
public sealed class MeltSeal() : FrostCard(new(0, CardType.Skill, CardRarity.Common, TargetType.Self, Block: 3, BlockUpgrade: 2));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_WAKE_ICE")]
public sealed class WakeIce() : FrostCard(new(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, Exhaust: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_OVERDRAW_WARMTH")]
public sealed class OverdrawWarmth() : FrostCard(new(0, CardType.Skill, CardRarity.Common, TargetType.Self, Amount: 2, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_RAPID_CALCULATION")]
public sealed class RapidCalculation() : FrostCard(new(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, Amount: 2, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_REWARM")]
public sealed class Rewarm() : FrostCard(new(1, CardType.Skill, CardRarity.Common, TargetType.Self, Block: 9, BlockUpgrade: 2, Amount: 2, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_EXPAND_COLD_STORE")]
public sealed class ExpandColdStore() : FrostCard(new(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, Amount: 2, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SLOW_RELEASE")]
public sealed class SlowRelease() : FrostCard(new(2, CardType.Power, CardRarity.Rare, TargetType.Self, Amount: 1, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SHIELD_COUNTER")]
public sealed class ShieldCounter() : FrostCard(new(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, CostUpgrade: -1, Damage: 5));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_ICE_HARVEST")]
public sealed class IceHarvest() : FrostCard(new(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, Damage: 7, Amount: 1, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SNOW_ROLL")]
public sealed class SnowRoll() : FrostCard(new(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies, Damage: 5, DamageUpgrade: 1, Amount: 4, AmountUpgrade: 2));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_AVALANCHE")]
public sealed class Avalanche() : FrostCard(new(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, Damage: 30, DamageUpgrade: 10, Amount: 20, AmountUpgrade: 5, Exhaust: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SNOW_SWEEP")]
public sealed class SnowSweep() : FrostCard(new(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies, Damage: 7, Amount: 2, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_TWIN_BLADES")]
public sealed class TwinBlades() : FrostCard(new(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, Damage: 4, Amount: 1, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_COLD_HAMMER")]
public sealed class ColdHammer() : FrostCard(new(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, Damage: 15, Amount: 4, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_MELT_SLASH")]
public sealed class MeltSlash() : FrostCard(new(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, Damage: 8, DamageUpgrade: 1, Amount: 1, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_WHITE_MIST")]
public sealed class WhiteMist() : FrostCard(new(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, Damage: 9, DamageUpgrade: 2, Amount: 1, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_ICE_BIND")]
public sealed class IceBind() : FrostCard(new(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy, Amount: 18, AmountUpgrade: 6));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_ABSOLUTE_ZERO")]
public sealed class AbsoluteZero() : FrostCard(new(3, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies, CostUpgrade: -1, Amount: 35, Exhaust: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SHELL_RECYCLE")]
public sealed class ShellRecycle() : FrostCard(new(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, Retain: true, Exhaust: true, RemoveExhaustUpgrade: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SEAL_SPELL")]
public sealed class SealSpell() : FrostCard(new(1, CardType.Skill, CardRarity.Common, TargetType.Self, Amount: 3, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_BLIZZARD_COMING")]
public sealed class BlizzardComing() : FrostCard(new(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self, Amount: 10, AmountUpgrade: 2, Extra: 1, ExtraUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SNOWLINE")]
public sealed class Snowline() : FrostCard(new(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies, Damage: 7, DamageUpgrade: 3));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_THAW_FROST")]
public sealed class ThawFrost() : FrostCard(new(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy, Amount: 5, AmountUpgrade: 1, Extra: 2, ExtraUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_COLD_RETURN")]
public sealed class ColdReturn() : FrostCard(new(2, CardType.Skill, CardRarity.Rare, TargetType.Self, Amount: 4, AmountUpgrade: 1, Extra: 2, ExtraUpgrade: 1, Retain: true, Exhaust: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_STEP_SNOW")]
public sealed class StepSnow() : FrostCard(new(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, Damage: 7, DamageUpgrade: 1, Amount: 2, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_THIN_ICE")]
public sealed class ThinIce() : FrostCard(new(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, Damage: 5, DamageUpgrade: 2));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SPRING_FLOOD")]
public sealed class SpringFlood() : FrostCard(new(2, CardType.Skill, CardRarity.Rare, TargetType.Self, CostUpgrade: -1, Exhaust: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_DEEP_CACHE")]
public sealed class DeepCache() : FrostCard(new(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, Damage: 4, Amount: 2, CostUpgrade: -1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_GLACIER_BODY")]
public sealed class GlacierBody() : FrostCard(new(3, CardType.Power, CardRarity.Rare, TargetType.Self, Amount: 7, AmountUpgrade: 3));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_STORM_CORE")]
public sealed class StormCore() : FrostCard(new(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, Amount: 3, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SNOW_EYE")]
public sealed class SnowEye() : FrostCard(new(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, Amount: 1, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_ICE_MIRROR")]
public sealed class IceMirror() : FrostCard(new(1, CardType.Power, CardRarity.Rare, TargetType.Self, Amount: 3, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_GLACIAL_CORE")]
public sealed class GlacialCore() : FrostCard(new(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, Amount: 1, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_PRISM_BLAST")]
public sealed class PrismBlast() : FrostCard(new(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, Damage: 16, DamageUpgrade: 2, Amount: 3, AmountUpgrade: 1, Extra: 3, ExtraUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_ICE_CHISEL")]
public sealed class IceChisel() : FrostCard(new(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, Damage: 7, DamageUpgrade: 1, Amount: 1, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_FROSTFIRE")]
public sealed class Frostfire() : FrostCard(new(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, Damage: 10, DamageUpgrade: 3));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_CRYSTAL_VOLLEY")]
public sealed class CrystalVolley() : FrostCard(new(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, Damage: 19));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_ICE_DUST")]
public sealed class IceDust() : FrostCard(new(1, CardType.Skill, CardRarity.Common, TargetType.Self, Amount: 6, AmountUpgrade: 1, Extra: 2, ExtraUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_PERMAFROST_WARD")]
public sealed class PermafrostWard() : FrostCard(new(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self, Amount: 12, AmountUpgrade: 4));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_WARMTH_RECOVERY")]
public sealed class WarmthRecovery() : FrostCard(new(0, CardType.Skill, CardRarity.Common, TargetType.Self, Amount: 2, AmountUpgrade: 1, Exhaust: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_THAW_ENERGY")]
public sealed class ThawEnergy() : FrostCard(new(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, Amount: 1, AmountUpgrade: 1, Exhaust: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_CRYSTAL_FURNACE")]
public sealed class CrystalFurnace() : FrostCard(new(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, Exhaust: true, RetainUpgrade: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_CRYSTAL_EDGE")]
public sealed class CrystalEdge() : FrostCard(new(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, Amount: 3, AmountUpgrade: 2));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_CRYSTAL_AMULET")]
public sealed class CrystalAmulet() : FrostCard(new(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, Amount: 2, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_ICE_CELLAR")]
public sealed class IceCellar() : FrostCard(new(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, Amount: 1, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_DELAYED_CHILL")]
public sealed class DelayedChill() : FrostCard(new(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, Damage: 9, DamageUpgrade: 2, Amount: 8, AmountUpgrade: 2, Extra: 12, ExtraUpgrade: 4));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_PIERCING_COLD")]
public sealed class PiercingCold() : FrostCard(new(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, Damage: 6, Amount: 2, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SNOW_FINALE")]
public sealed class SnowFinale() : FrostCard(new(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, Damage: 10, Exhaust: true, RemoveExhaustUpgrade: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SPREAD_FROST")]
public sealed class SpreadFrost() : FrostCard(new(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies, Amount: 3, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_FROST_CARVING")]
public sealed class FrostCarving() : FrostCard(new(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, Amount: 1, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_FROZEN_MOMENT")]
public sealed class FrozenMoment() : FrostCard(new(1, CardType.Skill, CardRarity.Rare, TargetType.Self, Amount: 2, AmountUpgrade: 1, Exhaust: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_PHASE_SHIFT")]
public sealed class PhaseShift() : FrostCard(new(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy, Amount: 2, AmountUpgrade: 1, Exhaust: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_COLD_SEARCH")]
public sealed class ColdSearch() : FrostCard(new(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, Block: 7, Amount: 1, AmountUpgrade: 1, Exhaust: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_BLOOD_WINTER")]
public sealed class BloodWinter() : FrostCard(new(0, CardType.Skill, CardRarity.Rare, TargetType.Self, Amount: 3, AmountUpgrade: 1, Extra: 2, ExtraUpgrade: 1, Exhaust: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_DEBT_SETTLEMENT")]
public sealed class DebtSettlement() : FrostCard(new(2, CardType.Skill, CardRarity.Rare, TargetType.Self, Retain: true, Exhaust: true, CostUpgrade: -1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_QUIET_MEDITATION")]
public sealed class QuietMeditation() : FrostCard(new(1, CardType.Skill, CardRarity.Common, TargetType.Self, Amount: 2, AmountUpgrade: 1, Extra: 4, ExtraUpgrade: 2));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_CRYSTAL_RESONANCE")]
public sealed class CrystalResonance() : FrostCard(new(2, CardType.Power, CardRarity.Rare, TargetType.Self, Amount: 1, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_WINTER_ARCHIVE")]
public sealed class WinterArchive() : FrostCard(new(2, CardType.Power, CardRarity.Rare, TargetType.Self, Amount: 1, CostUpgrade: -1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_ENDLESS_STORM")]
public sealed class EndlessStorm() : FrostCard(new(3, CardType.Power, CardRarity.Rare, TargetType.Self, Amount: 2, AmountUpgrade: -1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SIXFOLD_SNOW")]
public sealed class SixfoldSnow() : FrostCard(new(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, Amount: 4, AmountUpgrade: 2));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_HIDDEN_BLADE")]
public sealed class HiddenBlade() : FrostCard(new(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, Amount: 4, AmountUpgrade: 2));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_COLD_BLOOD_ECHO")]
public sealed class ColdBloodEcho() : FrostCard(new(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, Amount: 2, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_HEAT_EXCHANGE")]
public sealed class HeatExchange() : FrostCard(new(2, CardType.Power, CardRarity.Rare, TargetType.Self, Amount: 1, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SNOW_BLOOM")]
public sealed class SnowBloom() : FrostCard(new(2, CardType.Skill, CardRarity.Rare, TargetType.Self, Exhaust: true, RemoveExhaustUpgrade: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_DISSOLVE")]
public sealed class Dissolve() : FrostCard(new(0, CardType.Skill, CardRarity.Rare, TargetType.Self, Exhaust: true, RetainUpgrade: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SILVER_FROST")]
public sealed class SilverFrost() : FrostCard(new(1, CardType.Skill, CardRarity.Rare, TargetType.Self, Amount: 13, AmountUpgrade: 4, Innate: true, Exhaust: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_FROZEN_WISH")]
public sealed class FrozenWish() : FrostCard(new(1, CardType.Skill, CardRarity.Rare, TargetType.Self, Amount: 2, AmountUpgrade: 1, Innate: true, Exhaust: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_GLITTER")]
public sealed class Glitter() : FrostCard(new(1, CardType.Skill, CardRarity.Rare, TargetType.Self));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_BLESSING_WIND")]
public sealed class BlessingWind() : FrostCard(new(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, InnateUpgrade: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_HEART_INSCRIPTION")]
public sealed class HeartInscription() : FrostCard(new(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, InnateUpgrade: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_FLOWING_POWER")]
public sealed class FlowingPower() : FrostCard(new(2, CardType.Power, CardRarity.Rare, TargetType.Self, CostUpgrade: -1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_FROST_BITE")]
public sealed class FrostBite() : FrostCard(new(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, Damage: 5, Amount: 1, Extra: 2, ExtraUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_ICE_BREAK")]
public sealed class IceBreak() : FrostCard(new(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies, Damage: 10, Amount: 10, AmountUpgrade: 2, Extra: 2, ExtraUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SNOW_UNSHEATHED")]
public sealed class SnowUnsheathed() : FrostCard(new(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, Damage: 9, DamageUpgrade: 2, Amount: 1, AmountUpgrade: 1));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_HYPOTHERMIA_STRIKE")]
public sealed class HypothermiaStrike() : FrostCard(new(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, Damage: 7, DamageUpgrade: 2));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_WHITE_NIGHT_RUSH")]
public sealed class WhiteNightRush() : FrostCard(new(5, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies, Damage: 18, DamageUpgrade: 6));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_PIERCE")]
public sealed class Pierce() : FrostCard(new(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies, Damage: 7, Amount: 4, AmountUpgrade: 1, Exhaust: true));
[RegisterCard(typeof(MegaCrit.Sts2.Core.Models.CardPools.TokenCardPool), FullPublicEntry = "FROSTSWORN_CARD_DEPLETED")]
public sealed class Depleted() : FrostCard(new(-1, CardType.Status, CardRarity.Status, TargetType.None, Unplayable: true, Ethereal: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_SNOW_CHARGE")]
public sealed class SnowCharge() : FrostCard(new(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, Damage: 17, DamageUpgrade: 4, Exhaust: true));
[RegisterCard(typeof(MegaCrit.Sts2.Core.Models.CardPools.EventCardPool), FullPublicEntry = "FROSTSWORN_CARD_FATED_STORY")]
public sealed class FatedStory() : FrostCard(new(2, CardType.Power, CardRarity.Ancient, TargetType.Self, InnateUpgrade: true));
[RegisterCard(typeof(MegaCrit.Sts2.Core.Models.CardPools.EventCardPool), FullPublicEntry = "FROSTSWORN_CARD_ABSOLUTE_BEAM")]
public sealed class AbsoluteBeam() : FrostCard(new(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy, Damage: 9, DamageUpgrade: 2));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_GUARD_ADVANCE")]
public sealed class GuardAdvance() : FrostCard(new(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, Amount: 6, AmountUpgrade: 2, Extra: 6, ExtraUpgrade: 2, CostsX: true));
[RegisterCard(typeof(FrostCardPool), FullPublicEntry = "FROSTSWORN_CARD_ICE_RELEASE")]
public sealed class IceRelease() : FrostCard(new(0, CardType.Skill, CardRarity.Rare, TargetType.Self, CostsX: true));
