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
    private static int? receivedHostLevel;
    private static string? activeRunKey;
    private static int? activeRunLevel;
    private static string RunKey(RunState run)=>run.Rng.StringSeed+"|"+string.Join(",",run.Players.Select(p=>p.NetId).OrderBy(id=>id));
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
        if(lobby.NetService.Type==NetGameType.Client)receivedHostLevel=Math.Clamp(level,0,8);
    }
    public static void Poll(StartRunLobby lobby)
    {
        if(current==null || !current.TryGetTarget(out var previous) || previous!=lobby)
        {current=new(lobby);nextRequest=0;pendingStart=null;receivedHostLevel=null;activeRunKey=null;activeRunLevel=null;}
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
    public static void ApplyStartLevel(RunState run,bool loaded=false)
    {
        int? level=loaded?null:pendingStart;if(!loaded)pendingStart=null;
        // Some replay/rebuild tools recreate RunState through ordinary JSON.
        // Ritsu's attached run payload is then absent, although the native gold
        // and seed are already restored. Retain only the committed difficulty
        // for this session and exact seed/party; never reapply starting gold.
        if(level==null && activeRunLevel.HasValue && activeRunKey==RunKey(run)
            && (!Eclipse.Data.TryGet(run,out var saved) || !saved.GoldApplied))
        {
            Eclipse.Data.Modify(run,d=>{d.Level=activeRunLevel.Value;d.GoldApplied=true;});
            GD.Print("[Frostsworn] Restored committed Eclipse level after run reconstruction: "+activeRunLevel.Value);
            return;
        }
        bool hasSavedRun=Eclipse.Data.TryGet(run,out var existing) && existing.GoldApplied;
        if(!loaded && level==null && !hasSavedRun && current!=null && current.TryGetTarget(out var lobby) && lobby.NetService.Type!=NetGameType.Client)
            level=Eclipse.LobbyLevel(lobby);
        // Use only a setting actually received from the host, never a guest's
        // default/unlock limit, when the optional begin-run extension is lost.
        if(!loaded && level==null && !hasSavedRun)level=receivedHostLevel;
        if(level.HasValue)Eclipse.Data.Modify(run,d=>d.Level=level.Value);
        activeRunKey=RunKey(run);activeRunLevel=Eclipse.Level(run);
    }
}

