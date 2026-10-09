namespace Frostsworn;

public static class FrostPowerArt
{
    private static readonly HashSet<string> Supplied = new()
    {
        nameof(ArmorNextPower), nameof(IceArmorPower), nameof(ColdStoragePower), nameof(EndlessStormPower),
        nameof(SixfoldSnowPower), nameof(SnowPower), nameof(ThawNextPower), nameof(IceCellarPower),
        nameof(HeatExchangePower), nameof(SlowReleasePower), nameof(CrystalAmuletPower), nameof(ColdExpansionPower),
        nameof(FrostPower), nameof(GlacialCorePower), nameof(BlessingWindPower), nameof(HeartInscriptionPower),
        nameof(CrystalEdgePower), nameof(WinterArchivePower), nameof(HiddenBladePower), nameof(StormCorePower),
        nameof(GlacierBodyPower), nameof(IceMirrorPower), nameof(ColdBloodEchoPower), nameof(SelfFrostGuardPower),
        nameof(FractalSnowPower), nameof(CrystalResonancePower), nameof(FlowingPowerPower), nameof(SelfFrostPower),
        nameof(SnowEyePower), nameof(DrawNextPower), nameof(IceReleasePower), nameof(FatedStoryPower)
    };

    public static string PathFor(string powerName) => Supplied.Contains(powerName)
        ? $"res://Frostsworn/buffs/{powerName}.tres"
        : $"res://Frostsworn/icons/{powerName}.svg";
}
