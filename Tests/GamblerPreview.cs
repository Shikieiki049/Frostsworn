using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Exceptions;
using Frostsworn;

namespace Frostsworn.Tests;

public static partial class Suite
{
    static void CheckGamblerPreview()
    {
        // A fixture reproduces the reported Owner access at the same method boundary.
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Gambler"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Gambler");
        var suit = module.DefineEnum("Gambler.GamblerCode.Cards.GamblerSuit", TypeAttributes.Public, typeof(int));
        suit.DefineLiteral("None", 0);
        var suitType = suit.CreateTypeInfo()!.AsType();
        var registry = module.DefineType("Gambler.GamblerCode.Cards.GamblerSuitRegistry", TypeAttributes.Public);
        var builder = registry.DefineMethod("TryGetSuit", MethodAttributes.Public | MethodAttributes.Static, typeof(bool),
            new[] { typeof(CardModel), suitType.MakeByRefType() });
        builder.DefineParameter(2, ParameterAttributes.Out, "suit");
        var il = builder.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, typeof(CardModel).GetProperty(nameof(CardModel.Owner))!.GetMethod!);
        il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Stind_I4);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ret);
        var method = registry.CreateType()!.GetMethod("TryGetSuit")!;
        var card = ModelDb.Card<SnowCache>();
        bool reproduced = false;
        try { method.Invoke(null, new object?[] { card, null }); }
        catch (TargetInvocationException exception) when (exception.InnerException is CanonicalModelException) { reproduced = true; }
        Assert(reproduced, "reported Gambler-style Owner access fails for a canonical Frostsworn preview");

        FrostGamblerPreviewCompatibility.TryInstall(method.DeclaringType!.Assembly);
        FrostGamblerPreviewCompatibility.TryInstall(method.DeclaringType!.Assembly);
        Assert(Harmony.GetPatchInfo(method)?.Prefixes.Count(p => p.owner == "Frostsworn.gambler-preview") == 1,
            "Gambler guard installs once");
        Assert(method.Invoke(null, new object?[] { card, null }) is false, "canonical Frostsworn preview has no Gambler suit");
        bool otherStillRuns = false;
        try { method.Invoke(null, new object?[] { ModelDb.Card<StrikeIronclad>(), null }); }
        catch (TargetInvocationException exception) when (exception.InnerException is CanonicalModelException) { otherStillRuns = true; }
        Assert(otherStillRuns, "native card remains in Gambler's own suit lookup");
    }
}
