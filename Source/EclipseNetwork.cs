using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Networking.Sidecar;
using STS2RitsuLib.Networking.MessageExtensions;
namespace Frostsworn;

public sealed record EclipseLobbyMessage(bool Request,int Level);
public static class EclipseNetwork
{
    private static readonly RitsuLibSidecarJsonSerializer<EclipseLobbyMessage> Serializer=new();
    public static readonly RitsuLibSidecarMessageDescriptor<EclipseLobbyMessage> Descriptor=new(
        Entry.ModId,"eclipse.lobby.v1",Serializer.Serialize,Serializer.Deserialize);
    private static IDisposable? subscription;
    private static WeakReference<StartRunLobby>? current;
    private static long nextRequest;
    private static int? pendingStart;
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<StartRunLobby,HostChoice> HostChoices=new();
    private sealed class HostChoice {public int Level;}
    public static int HostLevel(StartRunLobby lobby)=>HostChoices.TryGetValue(lobby,out var choice)?choice.Level:Eclipse.LobbyLevel(lobby);
    public static void Commit(StartRunLobby lobby)
    {if(lobby.NetService.Type==NetGameType.Host)Accept(lobby,HostLevel(lobby));}
    public static void Initialize()
    {
        subscription=RitsuLibSidecarTypedMessageRegistry.Subscribe(Descriptor,context=>
        {
            if(current==null || !current.TryGetTarget(out var lobby))return;
            if(lobby.NetService.Type==NetGameType.Host)
            {
                if(context.IsHostIngest && context.Message.Request){Commit(lobby);Publish(lobby);}
            }
            else if(lobby.NetService.Type==NetGameType.Client && !context.IsHostIngest && !context.Message.Request)
                Accept(lobby,context.Message.Level);
        });
        // Ritsu owns this message's outer serialization boundary. Include the host's final
        // choice in the begin-run packet as well, independent of lobby sidecar timing.
        RitsuNetMessageTailExtensions.RegisterBytes<LobbyBeginRunMessage>(Entry.ModId+".eclipse",1,
            _=>current!=null && current.TryGetTarget(out var lobby) && lobby.NetService.Type==NetGameType.Host
                ?new[]{(byte)HostLevel(lobby)}:null,
            (version,payload)=>{if(version==1 && payload.Length==1 && payload.Span[0]<=8)pendingStart=payload.Span[0];});
    }
    public static void Accept(StartRunLobby lobby,int level)
    {
        // Guests accept the host's setting even when their own progression is lower.
        Eclipse.Data.Lobby.Set(lobby,new EclipseRunData {Level=Math.Clamp(level,0,8)});
    }
    public static void Poll(StartRunLobby lobby)
    {
        if(current==null || !current.TryGetTarget(out var previous) || previous!=lobby)
        {current=new(lobby);nextRequest=0;pendingStart=null;}
        if(lobby.NetService.Type!=NetGameType.Client || lobby.IsAboutToBeginGame())return;
        long now=System.Environment.TickCount64;if(now<nextRequest)return;nextRequest=now+2000;
        RitsuLibSidecarTypedMessageRegistry.SendToHost(lobby.NetService,Descriptor,new EclipseLobbyMessage(true,0));
    }
    public static void Publish(StartRunLobby lobby)
    {
        current=new(lobby);HostChoices.GetOrCreateValue(lobby).Level=Eclipse.LobbyLevel(lobby);
        if(lobby.NetService.Type==NetGameType.Host)
            RitsuLibSidecarTypedMessageRegistry.Broadcast(lobby.NetService,Descriptor,new EclipseLobbyMessage(false,Eclipse.LobbyLevel(lobby)));
    }
    public static void ApplyStartLevel(RunState run)
    {
        int? level=pendingStart;pendingStart=null;
        if(level==null && current!=null && current.TryGetTarget(out var lobby) && lobby.NetService.Type!=NetGameType.Client)
            level=Eclipse.LobbyLevel(lobby);
        if(level.HasValue)Eclipse.Data.Modify(run,d=>d.Level=level.Value);
    }
}

