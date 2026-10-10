using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Networking.MessageExtensions;
using STS2RitsuLib.Networking.Sidecar;
namespace Frostsworn.Tests;
public static partial class Suite
{
    public static async Task Check086()
    {
        var tree=(SceneTree)Engine.GetMainLoop();
        var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
        var run=RunState.CreateForTest([player]);Eclipse.Data.Set(run,new EclipseRunData {Level=8});
        player.Creature.SetCurrentHpInternal(30);
        Assert(Eclipse.Healing(player.Creature,1)==1 && Eclipse.Healing(player.Creature,9)==6,"Eclipse5 positive healing floors at one");
        var rest=new HealRestSiteOption(player);var description=rest.Description;
        Assert(((DynamicVar)description.Variables["Heal"]).PreviewValue==14,"rest description previews reduced healing");
        var evt=ModelDb.Event<Neow>().ToMutable();typeof(EventModel).GetProperty("Owner")!.SetValue(evt,player);
        var heal=new HealVar(9);heal.SetOwner(evt);var damage=new HpLossVar(3);damage.SetOwner(evt);var maximum=new MaxHpVar(8);maximum.SetOwner(evt);
        var loc=new LocString("rest_site_ui","HEAL.description");loc.Add(heal);loc.Add(damage);loc.Add(maximum);
        run.PushRoom(new EventRoom(ModelDb.Event<Neow>()));
        var healthVars=new DynamicVarSet(new DynamicVar[]{heal,damage,maximum});healthVars.AddTo(loc);
        Assert(((DynamicVar)loc.Variables["HpLoss"]).BaseValue==3 && EclipseEventText.Adjust("失去3点生命。",player)=="失去5点生命。","event HP loss preview includes fifty percent penalty");
        Assert(ReferenceEquals(loc.Variables["MaxHp"],maximum),"maximum HP event variable remains unchanged");
        run.PopCurrentRoom();
        Assert(heal.BaseValue==9 && damage.BaseValue==3,"preview copies never change event mechanics or compound on repeated display");
        healthVars.AddTo(loc);Assert(((DynamicVar)loc.Variables["Heal"]).BaseValue==9 && EclipseEventText.Adjust("回复9点生命。",player)=="回复6点生命。","ordinary event healing preview uses two thirds");
        Assert(!Eclipse.Description(8).Contains("日食1：") && Eclipse.Description(0)=="无特殊效果。" && Eclipse.Description(8).StartsWith(Eclipse.Quote),"Eclipse tooltip contains effects only and preserves quotation");
        Assert(new LocString("characters",player.Character.Id.Entry+".banter.alive.endTurnPing").GetFormattedText()=="再给你一点时间。" && new LocString("characters",player.Character.Id.Entry+".banter.dead.endTurnPing").GetFormattedText()=="接下来就交给你们了。","alive and dead multiplayer ping are localized");
        var screen=new NCharacterSelectScreen();var net=DispatchProxy.Create<INetGameService,EclipseNetProxy085>();var proxy=(EclipseNetProxy085)(object)net;
        var lobby=new StartRunLobby(GameMode.Standard,net,screen,2);lobby.Players.Add(new StartRunLobbyPlayer{id=net.NetId,character=player.Character});
        proxy.Kind=NetGameType.Host;EclipseNetwork.Accept(lobby,8);EclipseNetwork.Publish(lobby);
        var writer=new PacketWriter();RitsuNetMessageTailExtensions.Write(writer,new LobbyBeginRunMessage());
        var outer=typeof(MegaCrit.Sts2.Core.Multiplayer.NetMessageBus).GetMethods().First(m=>m.Name=="SerializeMessage" && m.IsGenericMethodDefinition).MakeGenericMethod(typeof(LobbyBeginRunMessage));
        Assert(HarmonyLib.Harmony.GetPatchInfo(outer)?.Postfixes.Any(p=>p.PatchMethod.DeclaringType?.FullName?.Contains("SerializePatch")==true)==true,"native begin-run outer serialization owns registered extensions");
        var bytes=writer.Buffer.Take(writer.BytePosition).ToArray();Assert(bytes.Length>1,"begin-run packet contains registered Eclipse extension");
        proxy.Kind=NetGameType.Client;EclipseNetwork.Accept(lobby,0);
        var serialized=EclipseNetwork.Descriptor.Serialize(new EclipseLobbyMessage(false,8));var message=EclipseNetwork.Descriptor.Deserialize(serialized);EclipseNetwork.Accept(lobby,message.Level);
        Assert(Eclipse.LobbyLevel(lobby)==8 && !Eclipse.Choose(lobby,0),"guest receives host eight independent of own unlocks and cannot change it");
        var reader=new PacketReader();reader.Reset(bytes);RitsuNetMessageTailExtensions.Read<LobbyBeginRunMessage>(reader);
        var guest=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,2);var guestRun=RunState.CreateForTest([guest]);Eclipse.Data.Set(guestRun,new EclipseRunData());
        Eclipse.InitializeRun(guestRun);Assert(Eclipse.Level(guestRun)==8 && guest.Gold==79,"wire-decoded final host difficulty applied before client run effects");
        typeof(Node).Assembly.GetType("Godot.Bridge.ScriptManagerBridge")!.GetMethod("LookupScriptsInAssembly",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(null,new object[]{typeof(NTinyCard).Assembly});
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        var background=new ColorRect {Color=new Color("202A38"),Size=new Vector2(1000,640)};tree.Root.AddChild(background);
        var history=new RunHistory {StartTime=86,Players=[new RunHistoryPlayer {Id=player.NetId,Character=player.Character.Id}]};Eclipse.Progress.History[EclipseHistory.Key(history)]=8;
        var portrait=ResourceLoader.Load<PackedScene>(NRunHistoryPlayerIcon.scenePath).Instantiate<NRunHistoryPlayerIcon>();background.AddChild(portrait);portrait.Position=new Vector2(100,150);portrait.LoadRun(history.Players[0],history);Assert(EclipseHistory.Level(history)==8,"history difficulty remains eight");
        var texture=portrait.GetNode<TextureRect>("%Icon");Assert(texture.Texture==player.Character.IconTexture && texture.GetCombinedMinimumSize().X<150,"history displays small head with bounded layout");
        var tiny=ResourceLoader.Load<PackedScene>("res://scenes/cards/tiny_card.tscn").Instantiate<NTinyCard>();background.AddChild(tiny);tiny.Position=new Vector2(300,170);tiny.Scale=new Vector2(3,3);tiny.SetCard(ModelDb.Card<FreezeRay>());
        Assert(tiny.GetNode<TextureRect>("%CardBack").Material is ShaderMaterial && tiny.GetNode<TextureRect>("%Portrait").Modulate==Colors.White,"tiny deck icons retain character color without frame shader");
        for(int n=0;n<9;n++){int value=n;var badge=EclipseUi.Badge(background,()=>value,new Vector2(72,72));badge.Position=new Vector2(40+n*105,350);Assert(EclipseUi.Icon(n) is AtlasTexture,"Eclipse icon transparent margins normalized "+n);}
        if(DisplayServer.GetName()!="headless") {await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../work/ui086.png"));}
        GD.Print("REVISION086_COMPLETE");
    }
}



