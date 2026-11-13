using System.Reflection;
using HarmonyLib;

namespace Frostsworn;

// The Gambler overlay queries canonical card templates during compendium previews.
public static class FrostGamblerPreviewCompatibility
{
    const string AssemblyName = "Gambler";
    const string RegistryType = "Gambler.GamblerCode.Cards.GamblerSuitRegistry";
    const string PatchOwner = "Frostsworn.gambler-preview";
    static readonly object Gate = new();
    static bool initialized;

    public static void Initialize()
    {
        if (initialized) return;
        initialized = true;
        AppDomain.CurrentDomain.AssemblyLoad += (_, args) => TryInstall(args.LoadedAssembly);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            TryInstall(assembly);
    }

    public static void TryInstall(Assembly assembly)
    {
        if (assembly.GetName().Name != AssemblyName) return;
        var registry = assembly.GetType(RegistryType);
        if (registry == null) return;

        foreach (var method in registry.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (method.Name != "TryGetSuit" || method.ReturnType != typeof(bool)) continue;
            var parameters = method.GetParameters();
            if (parameters.Length != 2 || parameters[0].ParameterType != typeof(CardModel)
                || !parameters[1].IsOut || parameters[1].ParameterType.GetElementType()?.Name != "GamblerSuit")
                continue;
            lock (Gate)
            {
                if (Harmony.GetPatchInfo(method)?.Owners.Contains(PatchOwner) == true) continue;
                new Harmony(PatchOwner).Patch(method, prefix: new HarmonyMethod(typeof(FrostGamblerPreviewCompatibility), nameof(SkipCanonicalCard)));
            }
        }
    }

    static bool SkipCanonicalCard(CardModel __0, ref bool __result)
    {
        if (__0 is not FrostCard || __0.IsMutable) return true;
        __result = false;
        return false;
    }
}
