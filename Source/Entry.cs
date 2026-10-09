using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Interop;
using STS2RitsuLib.CardPiles;

namespace Frostsworn;

[ModInitializer(nameof(Initialize))]
public static class Entry
{
    public const string ModId = "Frostsworn";
    public static PileType ColdPileType { get; private set; }

    public static void Initialize()
    {
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, Assembly.GetExecutingAssembly());
        RitsuLibFramework.RegisterDustyTomeCard<FrostswornCharacter,FatedStory>(ModId);
        RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<FreezeRay,AbsoluteBeam>(ModId);
        RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<WinterCore,WinterCrown>(ModId);
        ColdPileType = ModCardPileRegistry.For(ModId).RegisterOwned("cold_storage", new()
        {
            Scope = ModCardPileScope.CombatOnly,
            Style = ModCardPileUiStyle.BottomLeft,
            IconPath = "res://Frostsworn/icons/cold.png",
            VisibleWhen = c => c.Player?.Character is FrostswornCharacter || (c.Pile?.Cards.Count ?? 0) > 0,
            OnOpen = c => c.ShowDefaultPileScreen()
        }).PileType;
        Eclipse.Initialize();
        new Harmony("Frostsworn.mechanics").PatchAll(Assembly.GetExecutingAssembly());
        RitsuLibFramework.CreateLogger(ModId).Info($"Frostsworn {FrostVersion.Value}: 93 card definitions, 3 character potions, 9 relics, ancient mappings, Eclipse 0-8 and dynamic cold capacity registered.");
    }
}



