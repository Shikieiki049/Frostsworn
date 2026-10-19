using System.IO;
using System.Text.Json;
using System.Threading;
namespace Frostsworn;

// Keep this startup path independent of version-specific game/Ritsu APIs.
// JIT the gameplay initializer only after its exact build target matches.
public static class FrostVersionGuard
{
#if STS2_STABLE_107
    public const string RequiredGameVersion = "0.107.1";
#else
    public const string RequiredGameVersion = "0.111.0";
#endif
    static int warningQueued;
    public static bool IsMismatch(string? actual,string required)
        => Normalize(actual) is string version && version != Normalize(required);
    static string? Normalize(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return null;
        string text=value.Trim().TrimStart('v','V');
        return Version.TryParse(text,out var parsed)?parsed.ToString():null;
    }
    public static string? ReadVersion(string releaseFile)
    {
        try
        {
            using var data=JsonDocument.Parse(File.ReadAllText(releaseFile));
            return data.RootElement.TryGetProperty("version",out var version) && version.ValueKind==JsonValueKind.String ? Normalize(version.GetString()) : null;
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or JsonException) {return null;}
    }
    public static string Message(string actual,string required,bool english)
        => english
        ? $"Frostsworn version mismatch\n\nCurrent game: {actual}\nRequired game: {required}\nInstalled mod: {FrostVersion.Value}\n\nExit the game, unsubscribe from Frostsworn in Steam Workshop, then subscribe again and wait for the download to finish. If it still does not match, choose the matching game branch or mod build.\n\nFrostsworn gameplay has not been initialized for this launch."
        : $"霜誓者版本不匹配\n\n当前游戏版本：{actual}\n模组所需游戏版本：{required}\n当前模组版本：{FrostVersion.Value}\n\n请退出游戏，在 Steam 创意工坊取消订阅“霜誓者”，再重新订阅，等待下载完成后重启游戏。若仍不匹配，请选择对应的游戏分支或模组版本。\n\n本次启动未初始化霜誓者玩法。";
    public static AcceptDialog CreateDialog(string actual,string required,bool english)
    {
        var dialog=new AcceptDialog
        {
            Name="FrostswornVersionMismatch",
            Title=english?"Frostsworn — Version mismatch":"霜誓者 — 版本不匹配",
            DialogText=Message(actual,required,english),
            OkButtonText=english?"OK":"我知道了",
            Exclusive=true,
            Unresizable=true
        };
        var font=new SystemFont {FontNames=["Microsoft YaHei","Noto Sans CJK SC","Noto Sans","Arial"]};
        dialog.GetLabel().AddThemeFontOverride("font",font);
        dialog.GetLabel().AddThemeFontSizeOverride("font_size",20);
        dialog.GetOkButton().AddThemeFontOverride("font",font);
        dialog.GetOkButton().AddThemeFontSizeOverride("font_size",18);
        dialog.GetLabel().AutowrapMode=TextServer.AutowrapMode.WordSmart;
        dialog.GetLabel().CustomMinimumSize=new Vector2(620,0);
        dialog.Confirmed+=dialog.QueueFree;
        dialog.CloseRequested+=dialog.QueueFree;
        return dialog;
    }
    public static bool CheckStartup(string? releaseFile=null)
    {
        string? actual=ReadVersion(releaseFile ?? Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!,"release_info.json"));
        if(!IsMismatch(actual,RequiredGameVersion))return true;
        GD.Print($"[Frostsworn] Version mismatch: game={actual}, required={RequiredGameVersion}, mod={FrostVersion.Value}. Gameplay initialization skipped.");
        if(Interlocked.Exchange(ref warningQueued,1)==0)
            Callable.From(()=>ShowWarning(actual!)).CallDeferred();
        return false;
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static bool UseEnglish() => LocManager.Instance?.Language is not ("zhs" or "zht");
    static void ShowWarning(string actual)
    {
        if(Engine.GetMainLoop() is not SceneTree tree)return;
        bool english;
        try {english=UseEnglish();}
        catch(Exception e) when(e is TypeLoadException or MissingMethodException or FileNotFoundException or TypeInitializationException)
        {english=!TranslationServer.GetLocale().StartsWith("zh",StringComparison.OrdinalIgnoreCase);}
        var dialog=CreateDialog(actual,RequiredGameVersion,english);
        tree.Root.AddChild(dialog);
        dialog.PopupCentered(new Vector2I(680,340));
    }
}
