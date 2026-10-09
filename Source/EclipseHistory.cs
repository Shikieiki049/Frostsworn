using HarmonyLib;
using System.Reflection;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
namespace Frostsworn;

public static class EclipseHistory
{
    public static string Key(long start,string seed,IEnumerable<string> players)=>$"{start}:{seed}:{string.Join(",",players.OrderBy(x=>x,StringComparer.Ordinal))}";
    public static string Key(RunHistory history)=>Key(history.StartTime,history.Seed,history.Players.Select(p=>$"{p.Id}/{p.Character}"));
    public static string Key(SerializableRun run)=>Key(run.StartTime,run.SerializableRng.Seed??"",run.Players.Select(p=>$"{p.NetId}/{p.CharacterId}"));
    public static int? Level(RunHistory history)=>Eclipse.Progress.History.TryGetValue(Key(history),out int level)?Math.Clamp(level,0,8):null;
    public static int? ReadPayload(string? payload)
    {
        if(string.IsNullOrWhiteSpace(payload))return null;
        var level=JsonNode.Parse(payload)?["_ritsulib"]?["run_saved_data"]?[Entry.ModId]?["eclipse"]?["data"]?["Level"];
        return level==null?null:Math.Clamp(level.GetValue<int>(),0,8);
    }
    public static void Capture(SerializableRun run)
    {
        // Read the extension attached to this exact snapshot, including abandoned saved runs.
        var registry=typeof(RitsuLibFramework).Assembly.GetType("STS2RitsuLib.RunData.RunSavedDataRegistry");
        string? payload=(string?)registry?.GetMethod("BuildPayloadFromSerializable",BindingFlags.Static|BindingFlags.Public)?.Invoke(null,new object[]{run});
        int level=ReadPayload(payload)??0;
        Eclipse.Progress.History[Key(run)]=level;
        RitsuLibFramework.GetDataStore(Entry.ModId).Save("eclipse_progress");
    }
    public static void FitPortrait(TextureRect icon)
    {
        icon.ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize;
        icon.StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered;
    }
    public static void Show(NRunHistoryPlayerIcon control,RunHistoryPlayer player,RunHistory history)
    {
        if(player.Character==ModelDb.Character<FrostswornCharacter>().Id)
        {var portrait=control.GetNode<TextureRect>("%Icon");FitPortrait(portrait);portrait.Texture=ModelDb.Character<FrostswornCharacter>().IconTexture;}
        if(control.GetNodeOrNull<Control>("EclipseHistory") is {} old){control.RemoveChild(old);old.QueueFree();}
        var parent=new Control {Name="EclipseHistory",MouseFilter=Control.MouseFilterEnum.Ignore};control.AddChild(parent);
        int? level=Level(history);
        if(level.HasValue)
        {
            var icon=EclipseUi.Badge(parent,()=>level.Value,new Vector2(32,32));icon.Position=new Vector2(-8,-8);icon.ZIndex=5;
        }
        else
        {
            var label=new Label {Text=FrostText.Get("FROSTSWORN_ECLIPSE_UNRECORDED.title"),Position=new Vector2(-10,70),MouseFilter=Control.MouseFilterEnum.Ignore};
            label.AddThemeFontSizeOverride("font_size",13);parent.AddChild(label);
        }
    }
}
[HarmonyPatch(typeof(UnlockConsoleCmd),"UnlockAscensions")]
public static class EclipseUnlockAllPatch {public static void Postfix()=>Eclipse.UnlockAll();}
[HarmonyPatch(typeof(RunHistoryUtilities),nameof(RunHistoryUtilities.CreateRunHistoryEntry))]
public static class EclipseCaptureHistoryPatch {public static void Prefix(SerializableRun run)=>EclipseHistory.Capture(run);}
[HarmonyPatch(typeof(NRunHistoryPlayerIcon),nameof(NRunHistoryPlayerIcon.LoadRun))]
public static class EclipseHistoryUiPatch
{
    public static void Prefix(NRunHistoryPlayerIcon __instance,RunHistoryPlayer player)
    {if(player.Character==ModelDb.Character<FrostswornCharacter>().Id)EclipseHistory.FitPortrait(__instance.GetNode<TextureRect>("%Icon"));}
    public static void Postfix(NRunHistoryPlayerIcon __instance,RunHistoryPlayer player,RunHistory history)=>EclipseHistory.Show(__instance,player,history);
}

