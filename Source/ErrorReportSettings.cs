using System.Reflection;
using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;
namespace Frostsworn;

public static class FrostReportSettings
{
    // RitsuLib 0.6.7 exposes native consent through its settings page, but the
    // store type is internal. Keep this small compatibility bridge in one place;
    // never edit consent.json or maintain a second, contradictory permission.
    static readonly Type? Store=typeof(TelemetryApi).Assembly.GetType("STS2RitsuLib.Telemetry.TelemetryConsentStore");
    static readonly MethodInfo? Get=Store?.GetMethod("GetApplicantConsent",BindingFlags.Static|BindingFlags.NonPublic);
    static readonly MethodInfo? Set=Store?.GetMethod("SetApplicantConsent",BindingFlags.Static|BindingFlags.Public);
    public static bool Supported=>Get!=null && Set!=null;
    public static string[] Requests=>["diagnostics","state_divergence"];
    public static bool IsAllowed(TelemetryConsentState state,IEnumerable<string> requests)=>
        state==TelemetryConsentState.Granted && Requests.All(x=>requests.Contains(x,StringComparer.OrdinalIgnoreCase));
    public static void ApplyDefault(TelemetryConsentState state,Action<bool> write)
    {
        // Unknown is a first-time install. Explicit refusals always survive updates.
        if(state==TelemetryConsentState.Unknown)write(true);
    }
    public static void InitializeDefault()
    {
        if(!Supported)return;
        var consent=Get!.Invoke(null,[FrostErrorReports.ApplicantId]);
        if(consent?.GetType().GetProperty("Consent")?.GetValue(consent) is TelemetryConsentState state)ApplyDefault(state,Write);
    }
    public static bool Read()
    {
        if(!Supported)return false;
        var consent=Get!.Invoke(null,[FrostErrorReports.ApplicantId]);
        if(consent==null)return false;
        var type=consent.GetType();
        return type.GetProperty("Consent")?.GetValue(consent) is TelemetryConsentState state &&
            type.GetProperty("GrantedRequests")?.GetValue(consent) is IEnumerable<string> requests && IsAllowed(state,requests);
    }
    public static void Write(bool enabled)
    {
        if(!Supported)throw new InvalidOperationException("RitsuLib consent API is unavailable. Use its native data sharing settings.");
        Set!.Invoke(null,[FrostErrorReports.ApplicantId,enabled?TelemetryConsentState.Granted:TelemetryConsentState.Denied,enabled?Requests:Array.Empty<string>()]);
    }
    public static IModSettingsValueBinding<bool> CreateBinding(Func<bool> read,Action<bool> write)=>
        ModSettingsBindings.Callback(Entry.ModId,"allow_error_reports",read,write,()=>{});
    public static void Register()
    {
        RitsuLibFramework.RegisterModSettings(Entry.ModId,page=> {
            page.WithTitle(Text("霜誓者","Frostsworn"));
            page.AddSection("error_reports",section=> {
                section.WithTitle(Text("错误报告","Error reports"));
                section.AddToggle("allow_error_reports",Text("允许发送错误报告","Allow sending error reports"),CreateBinding(Read,Write),
                    Text("默认开启。自动收集脱敏错误日志和多人不同步报告，公开保存到作者的 GitHub，用于排查问题；无需登录。不会发送完整存档。关闭后停止收集和发送，后续更新会保留关闭状态。", "On by default. Automatically collect sanitized errors and multiplayer divergence reports and publish them to the author's GitHub for troubleshooting. No login required. Full saves are not sent. Turning this off stops collection and sending; updates preserve this choice."),()=>Supported);
                section.AddParagraph("upload_status",ModSettingsText.Dynamic(()=> {
                    string endpoint="";
                    try {endpoint=System.Text.Json.Nodes.JsonNode.Parse(Godot.FileAccess.GetFileAsString("res://Frostsworn/diagnostics/config.json"))?["endpoint"]?.GetValue<string>()??"";}catch{}
                    return string.IsNullOrWhiteSpace(endpoint)?Text("作者尚未配置上传接口：开启后仅保存本地脱敏报告。","The upload service is not configured: enabling this currently saves sanitized reports locally.").Resolve():
                        Text("上传服务已配置。报告发送权限与 RitsuLib 数据共享页面同步。","An upload service is configured. Permission is shared with RitsuLib's data sharing page.").Resolve();
                }));
                if(!Supported)section.AddParagraph("compatibility",Text("当前 RitsuLib 接口不兼容，请使用 RitsuLib 的数据共享页面管理授权。","This RitsuLib version is incompatible. Manage consent on RitsuLib's data sharing page."));
            });
        });
    }
    static ModSettingsText Text(string zh,string en)=>ModSettingsText.Dynamic(()=>LocManager.Instance?.Language is "zhs" or "zht"?zh:en);
}
