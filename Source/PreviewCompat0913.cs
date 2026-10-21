using System.Reflection;
using HarmonyLib;
namespace Frostsworn;

// No hard dependency: patch only the identified optional Acheron postfix. Native
// descriptions already support canonical cards; they have no runtime Owner.
public static class FrostPreviewCompatibility0913
{
    const string Owner = "Frostsworn.preview-canonical-0913";
    const string PatchType = "Acheron.AcheronCode.Mechanics.MyriadVoidDescriptionPatch";
    static readonly object Gate = new();
    static bool initialized;
    public static void Initialize()
    {
        if (initialized) return;
        initialized = true;
        AppDomain.CurrentDomain.AssemblyLoad += (_, e) =>
        {
            if (e.LoadedAssembly.GetName().Name == "Acheron") TryInstall(e.LoadedAssembly);
        };
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            if (assembly.GetName().Name == "Acheron") TryInstall(assembly);
    }
    public static void TryInstall(Assembly assembly)
    {
        var method = assembly.GetType(PatchType)?.GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null) return;
        var args = method.GetParameters();
        if (method.ReturnType != typeof(void) || args.Length != 3 || args[0].ParameterType != typeof(CardModel)
            || args[1].ParameterType != typeof(PileType) || args[2].ParameterType != typeof(string).MakeByRefType())
        { GD.Print("[Frostsworn] Acheron description signature changed; compatibility guard not applied."); return; }
        lock (Gate)
        {
            if (Harmony.GetPatchInfo(method)?.Owners.Contains(Owner) == true) return;
            new Harmony(Owner).Patch(method, prefix: new HarmonyMethod(typeof(FrostPreviewCompatibility0913), nameof(Guard)));
            GD.Print("[Frostsworn] Acheron description guard enabled for canonical Frostsworn cards.");
        }
    }
    static bool Guard([HarmonyArgument(0)] CardModel card) => card is not FrostCard || card.IsMutable;
}
