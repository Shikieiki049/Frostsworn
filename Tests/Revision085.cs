using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using STS2RitsuLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Monsters.Mocks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Saves;
namespace Frostsworn.Tests;
public class EclipseNetProxy085:DispatchProxy
{
    public INetGameService Backing=new NetSingleplayerGameService();
    public NetGameType Kind=NetGameType.Singleplayer;
    protected override object? Invoke(MethodInfo? method,object?[]? args)=>method!.Name=="get_Type"?Kind:method.Invoke(Backing,args);
}
public static partial class Suite
{
    sealed class EclipseTestMap085:ActMap
    {
        public override MapPoint BossMapPoint {get;}=new(0,15);
        public override MapPoint StartingMapPoint {get;}=new(0,0);
        protected override MapPoint?[,] Grid {get;}=new MapPoint?[1,16];
    }
    static async Task Check085()
    {
        var tree=(SceneTree)Engine.GetMainLoop();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        Assert(ModelDb.Card<FatedStory>().Pool is EventCardPool && ModelDb.Card<AbsoluteBeam>().Pool is EventCardPool,"both ancient cards use native event pool");
        Assert(ModelDb.Card<IceCrystal>().Pool is TokenCardPool,"token pool unchanged");
        Assert(Rules.Melt(3)==1 && Rules.Melt(5)==2 && Rules.Melt(1)==0,"remaining armor rounds up");
        var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
        var second=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,2);
        var run=RunState.CreateForTest([player,second]);player.ResetCombatState();second.ResetCombatState();
        var combat=new CombatState(runState:run);combat.AddPlayer(player);combat.AddPlayer(second);
        var monster=(MockAttackMonster)ModelDb.Monster<MockAttackMonster>().ToMutable();
        var enemy=combat.CreateCreature(monster,CombatSide.Enemy,"084");combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        var turn=typeof(CombatManager).GetField("_turnState",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(CombatManager.Instance)!;
        turn.GetType().GetProperty("IsInProgress")!.SetValue(turn,true);
        if(monster.MoveStateMachine==null)monster.SetUpForCombat();monster.RollMove(combat.PlayerCreatures);
        var context=new ThrowingPlayerChoiceContext();var own=player.Creature;
        foreach(var p in run.Players)foreach(var r in p.Relics.ToArray())p.RemoveRelicInternal(r);
        await PowerCmd.Apply<IceArmorPower>(context,own,3,own,null);
        await own.GetPower<IceArmorPower>()!.BeforeHandDrawLate(player,context,combat);
        Assert(own.GetPowerAmount<IceArmorPower>()==2,"actual pre-draw melt leaves two of three");
        await PowerCmd.Remove(own.GetPower<IceArmorPower>()!);
        Assert(Eclipse.Level(run)==0,"old/default run starts at Eclipse zero");
        for(int n=0;n<=8;n++)Assert(EclipseUi.Icon(n)!=null && Eclipse.Description(n).Split('\n').Length==(n==0?1:n==8?9:n),"Eclipse icon and cumulative tooltip "+n);
        Assert(Eclipse.Description(8).StartsWith(Eclipse.Quote) && !Eclipse.Description(8).Contains("无特殊效果"),"level eight preserves quoted introduction without zero rule");
        var store=RitsuLibFramework.GetDataStore(Entry.ModId);store.InitializeProfileScoped();string initial=System.Text.Json.JsonSerializer.Serialize(Eclipse.Progress);
        store.Modify<EclipseProgress>("eclipse_progress",p=>{p.Unlocked=2;p.SplitProgress=false;p.Singleplayer.Clear();p.Multiplayer=0;p.AllUnlocked=false;});
        Assert(Eclipse.UnlockedFor(player.Character,false)==2 && Eclipse.Progress.Multiplayer==2 && Eclipse.UnlockedFor(ModelDb.Character<Ironclad>(),false)==0,"legacy shared progress migrates without unlocking unrelated solo characters");
        store.Modify<EclipseProgress>("eclipse_progress",p=>{p.Singleplayer.Clear();p.Multiplayer=0;p.AllUnlocked=false;p.SplitProgress=true;});
        Eclipse.Victory(run);Assert(Eclipse.Progress.Multiplayer==1,"winning zero unlocks one");
        Eclipse.Data.Set(run,new EclipseRunData{Level=4});Eclipse.Victory(run);Assert(Eclipse.Progress.Multiplayer==5,"winning four unlocks five");
        Assert(Eclipse.UnlockedFor(player.Character,false)==0,"multiplayer victory does not unlock solo character progress");
        var iron=Player.CreateForNewRun<Ironclad>(UnlockState.all,3);var solo=RunState.CreateForTest([iron]);
        Eclipse.Data.Set(solo,new EclipseRunData{Level=2});Eclipse.Victory(solo);
        Assert(Eclipse.Available(solo) && Eclipse.UnlockedFor(iron.Character,false)==3 && Eclipse.UnlockedFor(player.Character,false)==0 && Eclipse.Progress.Multiplayer==5,"other characters use Eclipse and solo progression stays separate by character and mode");
        
        int gold=player.Gold,otherGold=second.Gold;Eclipse.InitializeRun(run);Eclipse.InitializeRun(run);
        Assert(player.Gold==gold-20 && second.Gold==otherGold-20,"initial gold penalty applies once to both players");
        own.SetCurrentHpInternal(30);run.PushRoom(new EventRoom(ModelDb.Event<Neow>()));
        Assert(Eclipse.Healing(own,40)==28,"ancient restores seventy percent of missing HP");
        Eclipse.Data.Modify(run,d=>d.Level=6);
        Assert(Eclipse.Healing(own,40)==28 && Eclipse.EventDamage(own,3)==5,"ancient heal not reduced twice and event damage rounds up");
        run.PopCurrentRoom();Assert(Eclipse.Healing(own,9)==6 && Eclipse.EventDamage(own,3)==3,"ordinary healing reduced and non-event damage unchanged");
        Eclipse.Data.Modify(run,d=>d.Level=8);
        if(enemy.GetPower<ArtifactPower>() is {} artifact)await PowerCmd.Remove(artifact);await PowerCmd.Apply<ArtifactPower>(context,enemy,2,enemy,null);
        int artifactBefore=enemy.GetPowerAmount<ArtifactPower>();combat.RoundNumber=1;await Eclipse.TurnStart(combat,CombatSide.Player);await Eclipse.TurnStart(combat,CombatSide.Player);
        Assert(enemy.GetPowerAmount<StrengthPower>()==1 && enemy.GetPowerAmount<ArtifactPower>()==artifactBefore,"odd turn temporary strength applies once across multiplayer hooks without artifact loss");
        await enemy.GetPower<EclipseStrengthPower>()!.AfterSideTurnEnd(context,CombatSide.Enemy,new[]{enemy});
        Assert(enemy.GetPowerAmount<StrengthPower>()==0 && enemy.GetPowerAmount<ArtifactPower>()==artifactBefore,"temporary strength expires without artifact loss");
        combat.RoundNumber=2;await Eclipse.TurnStart(combat,CombatSide.Player);await Eclipse.TurnStart(combat,CombatSide.Player);
        Assert(enemy.Block==3,"even turn block applies once for whole team");
        int max=own.MaxHp;own.LoseHpInternal(1,ValueProp.Unpowered);
        Assert(own.MaxHp==max-1 && second.Creature.MaxHp==70,"actual damage records one max HP loss for its owner");
        own.LoseHpInternal(0,ValueProp.Unpowered);Assert(own.MaxHp==max-1,"zero damage does not scar");
        second.Creature.LoseHpInternal(1,ValueProp.Unpowered);
        var registry=typeof(RitsuLibFramework).Assembly.GetType("STS2RitsuLib.RunData.RunSavedDataRegistry")!;
        var payload=(string)registry.GetMethod("BuildPayload")!.Invoke(null,new object[]{run})!;
        Eclipse.Data.Remove(run);registry.GetMethod("ImportPayloadIntoRun")!.Invoke(null,new object[]{run,payload});
        Assert(Eclipse.Level(run)==8 && Eclipse.Data.Get(run).MaxHpLost[1]==1 && Eclipse.Data.Get(run).MaxHpLost[2]==1 && Eclipse.Data.Get(run).GoldApplied,"native Ritsu network/save payload roundtrip retains shared difficulty and separate scars");
        var screen=new NCharacterSelectScreen();
        var net=DispatchProxy.Create<INetGameService,EclipseNetProxy085>();var proxy=(EclipseNetProxy085)(object)net;
        var lobby=new StartRunLobby(GameMode.Standard,net,screen,2);proxy.Kind=NetGameType.Host;
        lobby.Players.Add(new StartRunLobbyPlayer{id=net.NetId,character=player.Character,isReady=false});
        ulong peer=net.NetId+1;lobby.Players.Add(new StartRunLobbyPlayer{id=peer,character=second.Character,isReady=false});
        store.Modify<EclipseProgress>("eclipse_progress",p=>p.Multiplayer=4);
        Assert(Eclipse.Choose(lobby,8) && Eclipse.LobbyLevel(lobby)==4,"host selection capped to unlocked difficulty");
        proxy.Kind=NetGameType.Client;
        Assert(!Eclipse.Choose(lobby,1) && Eclipse.LobbyLevel(lobby)==4,"client cannot change shared difficulty");
        proxy.Kind=NetGameType.Host;
        registry.GetMethod("MergeLobbyContribution")!.Invoke(null,new object[]{lobby,peer,payload});
        Assert(Eclipse.LobbyLevel(lobby)==4,"client payload cannot overwrite host difficulty");
        proxy.Kind=NetGameType.Singleplayer;
        Assert(Eclipse.LobbyLimit(lobby)==0,"same lobby reads solo character cap when switched to solo mode");
        proxy.Kind=NetGameType.Host;
        var staging=(string)registry.GetMethod("BuildLobbyStagingPayload")!.Invoke(null,new object[]{lobby})!;
        registry.GetMethod("ImportPayloadIntoRun")!.Invoke(null,new object[]{run,staging});
        Assert(Eclipse.Level(run)==4,"lobby selection transfers through native new-run payload");
        store.Modify<EclipseProgress>("eclipse_progress",p=>p.Multiplayer=1);
        typeof(RitsuLibFramework).GetMethod("PublishLifecycleEvent",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(STS2RitsuLib.RunData.RunSavedDataLobbyStagingEvent)).Invoke(null,new object[]{new STS2RitsuLib.RunData.RunSavedDataLobbyStagingEvent(lobby,true,true,STS2RitsuLib.RunData.RunSavedDataLobbyStagingReason.Committing,DateTimeOffset.UtcNow),"test"});
        Assert(Eclipse.LobbyLevel(lobby)==1,"final commit clamps stale selection even before UI timer refresh");
        Eclipse.UnlockAll();Assert(ModelDb.AllCharacters.All(c=>Eclipse.UnlockedFor(c,false)==8) && Eclipse.Progress.Multiplayer==8,"unlock all enables all characters and multiplayer through level eight");
        var unlockMethod=HarmonyLib.AccessTools.Method(typeof(MegaCrit.Sts2.Core.DevConsole.ConsoleCommands.UnlockConsoleCmd),"UnlockAscensions");
        Assert(HarmonyLib.Harmony.GetPatchInfo(unlockMethod)!.Postfixes.Any(p=>p.PatchMethod.DeclaringType==typeof(EclipseUnlockAllPatch)),"native unlock all path has Eclipse integration attached");
        Eclipse.Data.Modify(run,d=>{d.Level=8;d.MaxHpLost[1]=1;d.MaxHpLost[2]=1;});
        
        for(int n=0;n<80;n++)Eclipse.Scar(own,1);
        Assert(own.MaxHp==35,"scar capped at half original maximum");
        int hp=own.CurrentHp;run.CurrentActIndex=1;
        Assert(own.MaxHp==70 && own.CurrentHp==hp && second.Creature.MaxHp==70 && Eclipse.Data.Get(run).MaxHpLost.Count==0,"act transition restores all max HP without healing current HP");
        run.Map=new EclipseTestMap085();
        Assert(Enumerable.Range(0,17).Where(r=>EclipseUi.HiddenMapPoint(run,new MapPoint(0,r))).SequenceEqual(new[]{12,13,14}),"only three rows before first boss are masked");
        Assert(ResourceLoader.Exists("res://images/atlases/ui_atlas.sprites/map/icons/map_unknown.tres") && ResourceLoader.Exists("res://images/atlases/compressed.sprites/map/map_unknown_outline.tres"),"native unknown map textures exist");
        typeof(Node).Assembly.GetType("Godot.Bridge.ScriptManagerBridge")!.GetMethod("LookupScriptsInAssembly",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(null,new object[]{typeof(NHealthBar).Assembly});
        var bg=new ColorRect {Color=new Color("202A38"),Size=new Vector2(1000,640)};tree.Root.AddChild(bg);
        var snapshot=new SerializableRun {StartTime=85,SerializableRng=run.Rng.ToSerializable(),Players=run.Players.Select(p=>new MegaCrit.Sts2.Core.Saves.Runs.SerializablePlayer {NetId=p.NetId,CharacterId=p.Character.Id}).ToList()};
        registry.GetMethod("AttachDocumentFromJson")!.Invoke(null,new object[]{snapshot,payload});
        EclipseHistory.Capture(snapshot);
        var history=new RunHistory {StartTime=85,Seed=snapshot.SerializableRng.Seed??"",Players=run.Players.Select(p=>new RunHistoryPlayer {Id=p.NetId,Character=p.Character.Id}).ToList()};
        Assert(EclipseHistory.Level(history)==8,"history captures Eclipse from exact serialized run snapshot");
        var restored=System.Text.Json.JsonSerializer.Deserialize<EclipseProgress>(System.Text.Json.JsonSerializer.Serialize(Eclipse.Progress))!;
        Assert(restored.History[EclipseHistory.Key(history)]==8 && EclipseHistory.Level(new RunHistory())==null,"history survives serialization and unknown old runs are not mislabeled zero");
        var portrait=ResourceLoader.Load<PackedScene>(NRunHistoryPlayerIcon.scenePath).Instantiate<NRunHistoryPlayerIcon>();tree.Root.AddChild(portrait);portrait.Position=new Vector2(800,440);
        portrait.LoadRun(history.Players[0],history);
        Assert(portrait.GetNode<TextureRect>("%Icon").ExpandMode==TextureRect.ExpandModeEnum.IgnoreSize && portrait.GetNode<TextureRect>("%Icon").GetCombinedMinimumSize().X<150 && portrait.GetNodeOrNull<Control>("EclipseHistory")!=null,"native history portrait ignores large source dimensions and has Eclipse badge");
        typeof(NCharacterSelectScreen).GetField("_lobby",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(screen,lobby);
        EclipseUi.Select(screen);var panel=screen.GetNode<Control>("EclipseSelect");screen.RemoveChild(panel);bg.AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);panel.Position=new Vector2(400,430);panel.Size=new Vector2(184,94);
        await PowerCmd.Apply<IceArmorPower>(context,own,5,own,null);await CreatureCmd.GainBlock(own,3,ValueProp.Unpowered,null);
        var health=ResourceLoader.Load<PackedScene>("res://scenes/combat/health_bar.tscn").Instantiate<NHealthBar>();tree.Root.AddChild(health);health.Position=new Vector2(300,140);health.Scale=new Vector2(2,2);health.SetCreature(own);health.RefreshValues();
        var badge=health.GetNode<Control>("FrostArmorBadge");
        Assert(Math.Abs(badge.Position.X+badge.Size.X/2-health.HpBarContainer.Position.X-health.HpBarContainer.Size.X)<0.1f,"armor icon overlaps exact health bar end");
        for(int n=0;n<9;n++){int value=n;var icon=EclipseUi.Badge(bg,()=>value,new Vector2(72,72));icon.Position=new Vector2(40+n*105,300);}
        if(DisplayServer.GetName()!="headless")
        {await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../work/ui085.png"));}
        var original=System.Text.Json.JsonSerializer.Deserialize<EclipseProgress>(initial)!;store.Modify<EclipseProgress>("eclipse_progress",p=>{p.Unlocked=original.Unlocked;p.SplitProgress=original.SplitProgress;p.AllUnlocked=original.AllUnlocked;p.Singleplayer=original.Singleplayer;p.Multiplayer=original.Multiplayer;p.History=original.History;});store.Save("eclipse_progress");health.Free();GD.Print("REVISION085_COMPLETE");
    }
}










