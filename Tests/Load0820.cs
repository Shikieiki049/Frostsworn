using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Frostsworn;
using STS2RitsuLib;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Monsters.Mocks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Context;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static void PublishLoaded(RunState run)=>typeof(RitsuLibFramework).GetMethod("PublishLifecycleEvent",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(RunLoadedEvent)).Invoke(null,new object[]{new RunLoadedEvent(run,run.Players.Count>1,false,DateTimeOffset.UtcNow),"test SL load"});
    static SerializableRun SaveNative(RunState run)=>new() {
        Acts=run.Acts.Select(a=>a.ToSave()).ToList(),Modifiers=run.Modifiers.Select(m=>m.ToSerializable()).ToList(),
        Players=run.Players.Select(p=>p.ToSerializable()).ToList(),SerializableRng=run.Rng.ToSerializable(),
        SerializableOdds=run.Odds.ToSerializable(),SerializableSharedRelicGrabBag=run.SharedRelicGrabBag.ToSerializable(),
        GameMode=run.GameMode,CurrentActIndex=run.CurrentActIndex,Ascension=run.AscensionLevel,
        ExtraFields=run.ExtraFields.ToSerializable(),EventsSeen=run.VisitedEventIds.ToList(),VisitedMapCoords=run.VisitedMapCoords.ToList(),
        MapPointHistory=run.MapPointHistory.Select(p=>p.ToList()).ToList()
    };
    static async Task CheckLoad0820()
    {
        MegaCrit.Sts2.Core.Modding.AssemblyInfo.Init();
        using(var stream=System.IO.File.OpenRead(Godot.ProjectSettings.GlobalizePath("res://../../dist/Frostsworn/mod_manifest.json")))
            MegaCrit.Sts2.Core.Modding.AssemblyInfo.ModMap![typeof(Entry).Assembly]=new MegaCrit.Sts2.Core.Modding.Mod {path="test/Frostsworn",state=MegaCrit.Sts2.Core.Modding.ModLoadState.Loaded,manifest=MegaCrit.Sts2.Core.Modding.ModManifest.ReadFromStream(stream,out _)};
        MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache.Init();
        foreach(bool multiplayer in new[]{false,true})
        {
            var players=Enumerable.Range(1,multiplayer?2:1).Select(i=>Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,(ulong)i)).ToArray();
            var run=RunState.CreateForTest(players,seed:multiplayer?"SLMULTI0820":"SLSINGLE0820");
            Eclipse.Data.Set(run,new EclipseRunData {Level=8});Eclipse.InitializeRun(run);
            var registry=typeof(RitsuLibFramework).Assembly.GetType("STS2RitsuLib.RunData.RunSavedDataRegistry")!;
            Eclipse.Data.Modify(run,d=>d.MaxHpLost[1]=3);players[0].Creature.SetMaxHpInternal(67);
            string payload=(string)registry.GetMethod("BuildPayload")!.Invoke(null,new object[]{run})!;
            var save=SaveNative(run);
            for(int i=0;i<3;i++)
            {
                var restored=RunState.FromSerializable(save);
                // Exercise the real loaded lifecycle, not the new-run callback.
                PublishLoaded(restored);PublishLoaded(restored);
                Assert(Eclipse.Level(restored)==8,"SL lifecycle restores difficulty on load for party size "+players.Length);
                Assert(restored.Players.All(p=>p.Gold==79),"SL never reapplies starting gold penalty");
                await CheckLoadedCombat(restored);
            }
            // Simulate restarting the process: no in-memory recovery cache.
            foreach(string field in new[]{"activeRunKey","activeRunLevel","receivedHostLevel","current","pendingStart"})
                typeof(EclipseNetwork).GetField(field,BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,null);
            registry.GetMethod("AttachDocumentFromJson")!.Invoke(null,new object[]{save,payload});
            var full=RunState.FromSerializable(save);
            PublishLoaded(full);
            Assert(Eclipse.Data.Get(full).MaxHpLost[1]==3 && full.Players[0].Creature.MaxHp==67,"complete SL payload preserves Eclipse8 scars and native maximum HP");
            int currentHp=full.Players[0].Creature.CurrentHp;Eclipse.Restore(full,1);
            Assert(full.Players[0].Creature.MaxHp==70 && Eclipse.Data.Get(full).MaxHpLost.Count==0,"loaded Eclipse8 scars restore at next act");
            Assert(full.Players[0].Creature.CurrentHp==currentHp,"next-act maximum HP restoration never heals current HP");
        }
        LocalContext.NetId=null;
    }
    static async Task CheckLoadedCombat(RunState run)
    {
        var combat=new CombatState(runState:run);
        foreach(var p in run.Players){p.ResetCombatState();combat.AddPlayer(p);}
        var monster=(MockAttackMonster)ModelDb.Monster<MockAttackMonster>().ToMutable();
        var enemy=combat.CreateCreature(monster,CombatSide.Enemy,"SL0820");combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        var turn=typeof(CombatManager).GetField("_turnState",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(CombatManager.Instance)!;
        turn.GetType().GetProperty("IsInProgress")!.SetValue(turn,true);
        if(monster.MoveStateMachine==null)monster.SetUpForCombat();monster.RollMove(combat.PlayerCreatures);
        LocalContext.NetId=run.Players.Last().NetId;
        combat.RoundNumber=1;await Hook.AfterSideTurnStart(combat,CombatSide.Player,combat.PlayerCreatures);
        Assert(enemy.GetPowerAmount<StrengthPower>()==1,"next combat after SL has Eclipse7 strength on guest");
        combat.RoundNumber=2;await Hook.AfterSideTurnStart(combat,CombatSide.Player,combat.PlayerCreatures);
        Assert(enemy.Block==3,"next combat after SL has Eclipse4 Block");
        turn.GetType().GetMethod("Cancel")!.Invoke(turn,null);
        typeof(CombatManager).GetField("_turnState",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(CombatManager.Instance,null);
    }
}
