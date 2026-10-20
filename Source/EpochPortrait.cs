using HarmonyLib;
using MegaCrit.Sts2.Core.Timeline;
namespace Frostsworn;

// Ritsu 0.6.7 redirects the texture paths, but the native HasRealPortrait getter
// still probes the vanilla path and incorrectly labels a custom image as missing.
[HarmonyPatch(typeof(EpochModel), "HasRealPortrait", MethodType.Getter)]
public static class FrostEpochHasPortrait
{
    static bool Prefix(EpochModel __instance, ref bool __result)
    {
        if (__instance is not FrostEpoch epoch) return true;
        __result = ResourceLoader.Exists(epoch.CustomBigPortraitPath);
        return false;
    }
}
