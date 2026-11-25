using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Godot;
using HarmonyLib;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;

namespace Frostsworn.Tests;

public static partial class Suite
{
    static void CheckSkinSelect0916()
    {
        var assembly = AssemblyLoadContext.GetLoadContext(typeof(Suite).Assembly)!.LoadFromAssemblyPath(
            "C:/fz/steam/steamapps/workshop/content/2868840/3787302680/Gurio.SkinChanger.dll");
        var method = assembly.GetType("STS2SkinChanger.Core.SkinService")!
            .GetMethod("LoadCharacterSelectIcon", BindingFlags.Static | BindingFlags.Public)!;
        var harmony = new Harmony("Frostsworn.skin-select-icon");
        Assert(Harmony.GetPatchInfo(method)?.Owners.Contains(harmony.Id) == true,
            "real skin changer receives the icon guard when loaded after Frostsworn");
        var character = ModelDb.Character<FrostswornCharacter>();
        var id = character.Id.Entry.ToLowerInvariant();
        string missingPath = $"res://images/packed/character_select/char_select_{id}.png";
        Assert(!ResourceLoader.Exists(missingPath), "reported conventional Frostsworn icon path is absent");
        harmony.Unpatch(method, HarmonyPatchType.Prefix, harmony.Id);
        Assert(method.Invoke(null, new object[] { id, false }) == null,
            "real skin changer cannot resolve the Frostsworn icon without compatibility");
        FrostSkinSelectCompatibility.TryInstall(assembly);
        FrostSkinSelectCompatibility.TryInstall(assembly);
        Assert(Harmony.GetPatchInfo(method)!.Prefixes.Count(p => p.owner == harmony.Id) == 1,
            "skin icon compatibility installs once");
        foreach (bool locked in new[] { false, true })
        {
            var expected = ResourceLoader.Load<Texture2D>(locked
                ? character.CustomCharacterSelectLockedIconPath : character.CustomCharacterSelectIconPath);
            var actual = method.Invoke(null, new object[] { id.ToUpperInvariant(), locked }) as Texture2D;
            Assert(expected != null && actual == expected, "skin changer loads supplied Frostsworn icon, locked=" + locked);
        }
        Assert(method.Invoke(null, new object[] { "unknown_test_character", false }) == null,
            "other characters retain the skin changer's own fallback behavior");

        // A runtime skin at the conventional path must take priority over our fallback.
        var skinTexture = new ImageTexture();
        skinTexture.TakeOverPath(missingPath);
        try
        {
            Assert(ReferenceEquals(method.Invoke(null, new object[] { id, false }), skinTexture),
                "an explicit skin replacement keeps precedence over the Frostsworn fallback");
        }
        finally { skinTexture.ResourcePath = ""; skinTexture.Dispose(); }
    }
}
