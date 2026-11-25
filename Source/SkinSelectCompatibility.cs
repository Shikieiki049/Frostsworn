using System.Reflection;
using HarmonyLib;

namespace Frostsworn;

public static class FrostSkinSelectCompatibility
{
    const string PatchOwner = "Frostsworn.skin-select-icon";
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
        if (assembly.GetName().Name != "Gurio.SkinChanger") return;
        var type = assembly.GetType("STS2SkinChanger.Core.SkinService");
        var method = type?.GetMethod("LoadCharacterSelectIcon",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null, [typeof(string), typeof(bool)], null);
        if (method?.ReturnType != typeof(Texture2D)) return;
        lock (Gate)
        {
            if (Harmony.GetPatchInfo(method)?.Owners.Contains(PatchOwner) == true) return;
            new Harmony(PatchOwner).Patch(method,
                prefix: new HarmonyMethod(typeof(FrostSkinSelectCompatibility), nameof(LoadFrostIcon)));
        }
    }

    static bool LoadFrostIcon(string __0, bool __1, ref Texture2D? __result)
    {
        var character = ModelDb.Character<FrostswornCharacter>();
        if (!string.Equals(__0, character.Id.Entry, StringComparison.OrdinalIgnoreCase)) return true;

        // Preserve an explicitly supplied skin texture at the conventional path.
        string skinPath = $"res://images/packed/character_select/char_select_{__0.ToLowerInvariant()}{(__1 ? "_locked" : "")}.png";
        if (ResourceLoader.HasCached(skinPath))
        {
            __result = ResourceLoader.Load<Texture2D>(skinPath);
            return false;
        }
        if (ResourceLoader.Exists(skinPath)) return true;
        string fallback = __1 ? character.CustomCharacterSelectLockedIconPath : character.CustomCharacterSelectIconPath;
        __result = ResourceLoader.Load<Texture2D>(fallback);
        return false;
    }
}
