using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib;
using System.Reflection;
using System.Collections.Generic;
using System.Text.Json;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters.Mocks;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Models.Capabilities;

namespace Frostsworn.Tests;
public static partial class Suite
{
    public static async Task Run()
    {
        TestMode.IsOn = true;
        typeof(ModManager).GetProperty("State")!.SetValue(null,ModManagerState.Initialized);
        GD.Print("TEST_BOOT: Godot + game + RitsuLib assemblies loaded");
        GD.Print("TEST_USER_DIR=" + OS.GetUserDataDir());
        SaveManager.Instance.InitSettingsDataForTest();
        ProjectSettings.LoadResourcePack("C:/fz/steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.pck", false);
        ProjectSettings.LoadResourcePack(ProjectSettings.GlobalizePath("res://../../dist/Frostsworn/Frostsworn.pck"), true);
        RitsuLibFramework.Initialize();
        Assert(RitsuLibFramework.IsActive,"RitsuLib framework initialization");
        Entry.Initialize();
        typeof(RitsuLibFramework).Assembly.GetType("STS2RitsuLib.Interop.Patches.ModTypeDiscoveryPatch")!
            .GetMethod("Prefix",BindingFlags.Public|BindingFlags.Static)!.Invoke(null,null);
        ModelDb.Init(ModelDb.AllAbstractModelSubtypes.Concat(typeof(FrostCard).Assembly.GetTypes().Where(t=>!t.IsAbstract && typeof(AbstractModel).IsAssignableFrom(t))).Distinct().ToArray());
        LocManager.Initialize();
        var tables=(Dictionary<string,LocTable>)typeof(LocManager).GetField("_tables",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(LocManager.Instance)!;
        foreach(var table in new[]{"cards","powers","relics","characters","epochs","static_hover_tips","enchantments","ancients","potions"})
        {
            var text=Godot.FileAccess.GetFileAsString($"res://Frostsworn/localization/zhs/{table}.json");
            tables[table]=new LocTable(table,JsonSerializer.Deserialize<Dictionary<string,string>>(text)!,tables.GetValueOrDefault(table));
        }
        Assert(Rules.FreezeThreshold(72,0)==12&&Rules.FreezeThreshold(72,1)==21,"72 HP threshold starts at twelve and rises by nine");
        Assert(Rules.FreezeThreshold(300,0)==36&&Rules.FreezeThreshold(300,1)==48,"300 HP threshold starts at thirty-six and rises by twelve");
        Assert(Rules.FreezeThreshold(20,0)==12&&Rules.FreezeThreshold(20,2)==20,"low HP uses minimum initial threshold and minimum four-point increment");
        Assert(Rules.FreezeThreshold(1000,0)==54&&Rules.FreezeThreshold(1000,2)==78,"boss initial threshold caps at fifty-four while subsequent thresholds can exceed it");
        Assert(Rules.FreezeThreshold(101,0)==13&&Rules.FreezeThreshold(101,1)==25,"percentage rounds up before independent clamps");
        Assert(Rules.Capacity(20)==10,"capacity cap");
        using(var manifestStream=System.IO.File.OpenRead(ProjectSettings.GlobalizePath("res://../../dist/Frostsworn/mod_manifest.json")))
        {
            var manifest=ModManifest.ReadFromStream(manifestStream,out var errors);
            Assert(manifest!=null && (errors?.Count ?? 0)==0 && manifest.id=="Frostsworn" && manifest.dependencies!.Single().id=="STS2-RitsuLib","native game manifest reader accepts package and dependency");
        }
        if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "0821") await CheckShop0821();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "0820") await CheckLoad0820();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "0819") await CheckRebuild0819();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "0818") await CheckMend0818();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "0817") await CheckMultiplayer0817();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "0816") await CheckKeyword0816();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "0815") await CheckEnglish0815();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "0814") await CheckVisual0814();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "0813") await CheckIdle0813();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "0811") await CheckArt0811();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "075") CheckArt075();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "079") CheckArt079();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "080") CheckArt080();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "081") CheckToken081();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "082") await CheckThermometer082();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "083") await Check083();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "084") await Check084();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "089") await Check089();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "088") await Check088();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "087") await Check087();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "086") await Check086();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") == "085") await Check085();
        else if (System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST") is "076" or "077") await CheckArt076();
        else await CheckRevision();
    }
    public static void Assert(bool condition,string message)
    {
        if(!condition)throw new Exception("FAILED: "+message);
        GD.Print("PASS: "+message);
    }
    public static CardPlay Play(CardModel card,MegaCrit.Sts2.Core.Entities.Creatures.Creature? target=null)=>new()
    { Card=card,Player=card.Owner,Target=target,ResultPile=PileType.Discard,Resources=default,IsAutoPlay=false,PlayIndex=0,PlayCount=1 };
}


