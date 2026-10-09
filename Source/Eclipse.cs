using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using STS2RitsuLib.RunData;
using STS2RitsuLib.Utils.Persistence;
using System.Runtime.CompilerServices;
namespace Frostsworn;

public sealed class EclipseProgress
{
    public int Unlocked {get;set;} // 0.8.4 migration only
    public bool SplitProgress {get;set;}
    public bool AllUnlocked {get;set;}
    public Dictionary<string,int> Singleplayer {get;set;}=new();
    public int Multiplayer {get;set;}
    public Dictionary<string,int> History {get;set;}=new();
}
public sealed class EclipseRunData
{
    public int Level {get;set;}
    public bool GoldApplied {get;set;}
    public Dictionary<ulong,int> MaxHpLost {get;set;}=new();
}
public static class Eclipse
{
    public static RunSavedData<EclipseRunData> Data {get;private set;}=null!;
    public static readonly string[] RulesText={
        "无特殊效果。","先古之民只会回复你已损失生命值的70%。","每一幕首个Boss战的前3层不可见。","初始金币-20。",
        "偶数回合开始时，敌人获得3点格挡。","从除先古之民外获得的回复量减少三分之一。","从事件所受的伤害增加50%。",
        "奇数回合开始时，敌人获得1点临时力量（不被人工制品抵消）。","友方受到永久伤害（最多50%），下一幕开始时恢复。"};
    public const string Quote="\"你唯能在光亮处庆祝......全因我允许你这么做。\"";
    public static void Initialize()
    {
        using(RitsuLibFramework.BeginModDataRegistration(Entry.ModId))
        {
            RitsuLibFramework.GetDataStore(Entry.ModId).Register("eclipse_progress","eclipse_progress.json",SaveScope.Profile,()=>new EclipseProgress(),autoCreateIfMissing:true);
            Data=RitsuLibFramework.GetRunSavedDataStore(Entry.ModId).Register("eclipse",()=>new EclipseRunData(),new RunSavedDataOptions {SyncLobbyOnChange=false});
        }
        EclipseNetwork.Initialize();
        RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(e=>InitializeRun(e.RunState));
        RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(e=>InitializeLoadedRun(e.RunState));
        RitsuLibFramework.SubscribeLifecycle<RunSavedDataLobbyStagingEvent>(e=>
        {
            if(e.IsHost && e.Reason==RunSavedDataLobbyStagingReason.Committing && e.Lobby.Players.Count>0)
            {
                EclipseNetwork.Commit(e.Lobby);
                int level=LobbyLevel(e.Lobby),limit=LobbyLimit(e.Lobby);
                if(level>limit)Data.Lobby.Set(e.Lobby,new EclipseRunData {Level=limit});
                EclipseNetwork.Publish(e.Lobby);
            }
        });
    }
    public static EclipseProgress Progress
    {
        get
        {
            var store=RitsuLibFramework.GetDataStore(Entry.ModId);
            if(!store.IsProfileInitialized)store.InitializeProfileScoped();
            var p=store.Get<EclipseProgress>("eclipse_progress");
            if(!p.SplitProgress)
            {
                p.Singleplayer[ModelDb.Character<FrostswornCharacter>().Id.ToString()]=Math.Clamp(p.Unlocked,0,8);
                p.Multiplayer=Math.Max(p.Multiplayer,Math.Clamp(p.Unlocked,0,8));p.SplitProgress=true;
                store.Save("eclipse_progress");
            }
            return p;
        }
    }
    public static int Unlocked=>UnlockedFor(ModelDb.Character<FrostswornCharacter>(),false);
    public static int UnlockedFor(CharacterModel character,bool multiplayer)
    {var p=Progress;return p.AllUnlocked?8:Math.Clamp(multiplayer?p.Multiplayer:p.Singleplayer.GetValueOrDefault(character.Id.ToString()),0,8);}
    public static int LobbyLimit(StartRunLobby lobby)=>UnlockedFor(lobby.LocalPlayer.character,lobby.NetService.Type!=NetGameType.Singleplayer);
    public static void UnlockAll()
    {
        var p=Progress;p.AllUnlocked=true;p.Multiplayer=8;
        foreach(var c in ModelDb.AllCharacters)p.Singleplayer[c.Id.ToString()]=8;
        RitsuLibFramework.GetDataStore(Entry.ModId).Save("eclipse_progress");
    }
    public static bool Available(IRunState? run)=>run?.Players.Count>0;
    public static int Level(IRunState? run)=>Available(run) && run is RunState r && Data.TryGet(r,out var data)?Math.Clamp(data.Level,0,8):0;
    public static string Description(int level)
    {
        level=Math.Clamp(level,0,8);
        return (level==8?FrostText.Get("FROSTSWORN_ECLIPSE_QUOTE.description")+"\n":"")+(level==0?FrostText.Get("FROSTSWORN_ECLIPSE_0.description"):string.Join("\n",Enumerable.Range(1,level).Select(i=>FrostText.Get($"FROSTSWORN_ECLIPSE_{i}.description"))));
    }
    public static bool CanChoose(StartRunLobby lobby)=>lobby.NetService.Type!=NetGameType.Client && !lobby.IsAboutToBeginGame();
    public static bool Choose(StartRunLobby lobby,int level)
    {
        if(!CanChoose(lobby) || lobby.Players.Count==0)return false;
        Data.Lobby.Set(lobby,new EclipseRunData {Level=Math.Clamp(level,0,LobbyLimit(lobby))});EclipseNetwork.Publish(lobby);return true;
    }
    public static int LobbyLevel(StartRunLobby lobby)=>Data.Lobby.TryGet(lobby,out var d)?Math.Clamp(d.Level,0,8):0;
    public static void InitializeRun(RunState run)
    {
        if(!Available(run))return;
        var d=Data.Get(run);
        EclipseNetwork.ApplyStartLevel(run);
        GD.Print("[Frostsworn] Eclipse run initialized: level="+Level(run)+", goldApplied="+d.GoldApplied);
        if(d.GoldApplied)return;
        if(Level(run)>=3)foreach(var p in run.Players)p.Gold=Math.Max(0,p.Gold-20);
        Data.Modify(run,d=>d.GoldApplied=true);
    }
    public static void InitializeLoadedRun(RunState run)
    {
        if(!Available(run))return;
        EclipseNetwork.ApplyStartLevel(run,loaded:true);
        // Native saved gold is already restored. Loading must never run the
        // new-run penalty, even when the attached mod payload was missing.
        Data.Modify(run,d=>d.GoldApplied=true);
        GD.Print("[Frostsworn] Eclipse saved run loaded: level="+Level(run));
    }
    public static void Victory(IRunState? run)
    {
        if(!Available(run))return;
        int next=Math.Min(8,Level(run)+1);
        var p=Progress;
        if(run!.Players.Count>1)p.Multiplayer=Math.Max(p.Multiplayer,next);
        else {string key=run.Players[0].Character.Id.ToString();p.Singleplayer[key]=Math.Max(p.Singleplayer.GetValueOrDefault(key),next);}
        RitsuLibFramework.GetDataStore(Entry.ModId).Save("eclipse_progress");
    }
    public static bool Ancient(IRunState? run)=>run?.CurrentRoom is EventRoom {CanonicalEvent:AncientEventModel};
    public static decimal Healing(Creature creature,decimal amount)
    {
        var run=creature.Player?.RunState;int level=Level(run);
        if(amount<=0 || level==0)return amount;
        if(Ancient(run))return Math.Floor(Math.Min(amount,(creature.MaxHp-creature.CurrentHp)*0.7m));
        return level>=5?Math.Max(1,Math.Floor(amount*2/3)):amount;
    }
    public static decimal EventDamage(Creature creature,decimal amount)
        =>amount>0 && Level(creature.Player?.RunState)>=6 && creature.Player!.RunState.CurrentRoom is EventRoom?Math.Ceiling(amount*1.5m):amount;
    public static void Scar(Creature creature,int damage)
    {
        if(damage<=0 || creature.IsDead || creature.Player is not {} p || p.RunState is not RunState run || Level(run)<8)return;
        var data=Data.Get(run);int lost=data.MaxHpLost.GetValueOrDefault(p.NetId);
        int full=creature.MaxHp+lost;
        if(lost>=full/2 || creature.MaxHp<=1)return;
        creature.SetMaxHpInternal(creature.MaxHp-1);
        Data.Modify(run,d=>d.MaxHpLost[p.NetId]=lost+1);
        EclipseNetwork.RememberScars(run);
    }
    public static void Restore(RunState run,int nextAct)
    {
        if(nextAct<=run.CurrentActIndex || !Data.TryGet(run,out var data))return;
        foreach(var p in run.Players)
        {
            int lost=data.MaxHpLost.GetValueOrDefault(p.NetId);
            if(lost>0)p.Creature.SetMaxHpInternal(p.Creature.MaxHp+lost);
        }
        Data.Modify(run,d=>d.MaxHpLost.Clear());
    }
    private sealed class TurnState {public int AppliedRound=-1;}
    private static readonly ConditionalWeakTable<ICombatState,TurnState> Turns=new();
    public static async Task TurnStart(ICombatState combat,CombatSide side)
    {
        int level=Level(combat.RunState);if(level<4 || side!=CombatSide.Player)return;
        var state=Turns.GetOrCreateValue(combat);if(state.AppliedRound==combat.RoundNumber)return;
        state.AppliedRound=combat.RoundNumber;
        foreach(var enemy in combat.HittableEnemies.ToArray())
        {
            if(combat.RoundNumber%2==0)await CreatureCmd.GainBlock(enemy,3,ValueProp.Unpowered,null);
            else if(level>=7)await PowerCmd.Apply<EclipseStrengthPower>(new ThrowingPlayerChoiceContext(),enemy,1,enemy,null);
        }
    }
}
[RegisterPower]
public sealed class EclipseStrengthPower:FrostPowerBase
{
    public override string CustomIconPath=>"res://Frostsworn/eclipse084/Eclipse7.png";
    public override async Task BeforeApplied(Creature target,decimal amount,Creature? applier,CardModel? cardSource)
        =>await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(),target,amount,applier,cardSource);
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context,CombatSide side,IEnumerable<Creature> participants)
    {
        if(side!=CombatSide.Enemy || !participants.Contains(Owner))return;
        if(Owner.GetPower<StrengthPower>() is {} strength)
        {strength.SetAmount(strength.Amount-Amount);if(strength.Amount==0)await PowerCmd.Remove(strength);}
        await PowerCmd.Remove(this);
    }
}
[HarmonyPatch(typeof(CreatureCmd),nameof(CreatureCmd.Heal))]
public static class EclipseHealPatch {public static void Prefix(Creature creature,ref decimal amount)=>amount=Eclipse.Healing(creature,amount);}
[HarmonyPatch(typeof(Creature),nameof(Creature.LoseHpInternal))]
public static class EclipseDamagePatch
{
    public static void Prefix(Creature __instance,ref decimal amount)=>amount=Eclipse.EventDamage(__instance,amount);
    public static void Postfix(Creature __instance,DamageResult __result)=>Eclipse.Scar(__instance,__result.UnblockedDamage);
}
// Genuine max HP rewards/costs may occur after damage and before saving.
// Keep the native HP fingerprint current without treating those changes as scars.
[HarmonyPatch(typeof(Creature),nameof(Creature.SetMaxHpInternal))]
public static class EclipseMaxHpCachePatch
{
    public static void Postfix(Creature __instance)
    {
        if(__instance.Player?.RunState is RunState run)EclipseNetwork.RememberScars(run);
    }
}
[HarmonyPatch(typeof(RunState),nameof(RunState.CurrentActIndex),MethodType.Setter)]
public static class EclipseActPatch
{
    public static void Prefix(RunState __instance,int value)=>Eclipse.Restore(__instance,value);
    public static void Postfix(RunState __instance)
    {
        if(Eclipse.Data.TryGet(__instance,out _))EclipseNetwork.RememberScars(__instance);
    }
}
[HarmonyPatch(typeof(RunManager),nameof(RunManager.OnEnded))]
public static class EclipseWinPatch
{public static void Prefix(RunManager __instance,bool isVictory){if(isVictory)Eclipse.Victory(__instance.DebugOnlyGetState());}}
// BeforeSideTurnStart schedules player-choice hooks independently on each peer;
// its callbacks can pause while synchronized actions execute. Apply team effects
// at the unconditional, awaited boundary before the native turn checksum instead.
[HarmonyPatch(typeof(Hook),nameof(Hook.AfterSideTurnStart))]
public static class EclipseTurnPatch
{
    public static void Postfix(ICombatState combatState,CombatSide side,ref Task __result)=>__result=Run(__result,combatState,side);
    private static async Task Run(Task original,ICombatState combat,CombatSide side){await original;await Eclipse.TurnStart(combat,side);}
}
