using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using STS2RitsuLib.Telemetry;
using STS2RitsuLib.Settings;
namespace Frostsworn;

public static class FrostErrorReports
{
    public const string ApplicantId="Frostsworn.error_reports";
    public const int MaxReportBytes=1024*1024;
    public static void Initialize()
    {
        // RitsuLib owns engine/exception hooks, consent, persistent queue and deduplication.
        // Tests never register a real collector or read a player's logs.
        if(MegaCrit.Sts2.Core.TestSupport.TestMode.IsOn)return;
        string endpoint="";
        try {endpoint=JsonNode.Parse(Godot.FileAccess.GetFileAsString("res://Frostsworn/diagnostics/config.json"))?["endpoint"]?.GetValue<string>()??"";}
        catch { /* Missing configuration means local reports only. */ }
        FrostReportAdapter adapter;
        string folder=Path.Combine(OS.GetUserDataDir(),"Frostsworn","error-reports"),logs=Path.Combine(OS.GetUserDataDir(),"logs");
        try {adapter=new FrostReportAdapter(endpoint,folder,logs);}
        catch(ArgumentException){adapter=new FrostReportAdapter("",folder,logs);}
        TelemetryRegistry.RegisterApplicant(CreateApplicant(adapter));
    }
    public static TelemetryApplicant CreateApplicant(ITelemetryAdapter adapter)=>new() {
            ApplicantId=ApplicantId,OwnerModId=Entry.ModId,DisplayName="Frostsworn error reports",
            DisplayNameText=Text("霜誓者错误报告","Frostsworn error reports"),Adapter=adapter,
            Requests=[
                TelemetryRequest.Diagnostics(Text("自动保存脱敏错误堆栈和近期诊断日志；作者配置上传服务后，这些报告会公开保存到 GitHub。可随时在此关闭。不会上传原始存档。","Automatically save sanitized errors and recent diagnostic logs. Once the author configures the upload service, reports are public on GitHub. You can disable this here at any time. Raw saves are not uploaded.")),
                TelemetryRequest.StateDivergence(Text("自动收集并保存脱敏的多人不同步报告。配置上传服务后公开上传到 GitHub，帮助定位双方状态差异。","Automatically collect sanitized multiplayer divergence reports. With an upload service configured, publish them to GitHub to diagnose state differences."))
            ]
        };
    static ModSettingsText Text(string zh,string en)=>ModSettingsText.Dynamic(()=>LocManager.Instance?.Language is "zhs" or "zht" ? zh : en);
}

// One report is cached before network I/O. Retries reuse the exact same bytes,
// and delivered markers prevent a partially successful batch from reposting.
public sealed class FrostReportAdapter : ITelemetryAdapter
{
    static readonly System.Net.Http.HttpClient Client=new(new HttpClientHandler {AllowAutoRedirect=false}) {Timeout=TimeSpan.FromSeconds(12)};
    readonly Uri? endpoint;
    readonly string folder,logFolder;
    readonly Func<byte[],CancellationToken,Task<bool>>? sender;
    public string AdapterId=>"frostsworn_github_reports";
    public string EndpointDescription=>endpoint?.ToString()??"Local reports only; GitHub upload service is not configured.";
    public FrostReportAdapter(string url,string folder,string logFolder,Func<byte[],CancellationToken,Task<bool>>? sender=null)
    {
        if(!string.IsNullOrWhiteSpace(url))
        {
            if(!Uri.TryCreate(url,UriKind.Absolute,out var target) || target.Scheme!="https" || !string.IsNullOrEmpty(target.UserInfo) || !string.IsNullOrEmpty(target.Query) || !string.IsNullOrEmpty(target.Fragment))
                throw new ArgumentException("Report endpoint must be an HTTPS URL without credentials, query or fragment.");
            endpoint=target;
        }
        this.folder=folder;this.logFolder=logFolder;this.sender=sender;
    }
    public async ValueTask<TelemetrySendResult> SendAsync(TelemetryApplicant applicant,IReadOnlyList<TelemetryEnvelope> events,CancellationToken cancellationToken=default)
    {
        if(applicant.ApplicantId!=FrostErrorReports.ApplicantId)return TelemetrySendResult.Fail("Unexpected report applicant.");
        try
        {
            // All file and compression work stays off the game thread.
            bool localOnly=endpoint==null && sender==null;
            foreach(var envelope in events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if(envelope.Category!=TelemetryDataCategory.Diagnostics || envelope.RequestId is not ("diagnostics" or "state_divergence"))continue;
                var cached=await Task.Run(()=>Cache(envelope),cancellationToken).ConfigureAwait(false);
                if(System.IO.File.Exists(cached.Path+".uploaded"))continue;
                if(localOnly)continue;
                bool success;
                if(sender!=null)success=await sender(cached.Bytes,cancellationToken).ConfigureAwait(false);
                else
                {
                    using var request=new HttpRequestMessage(HttpMethod.Post,endpoint);
                    request.Content=new ByteArrayContent(cached.Bytes);
                    request.Content.Headers.ContentType=new("application/json");
                    using var response=await Client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,cancellationToken).ConfigureAwait(false);
                    success=false;
                    if(response.IsSuccessStatusCode)
                    {
                        using var receiptStream=await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                        var receipt=new byte[4097];int length=0,count;
                        while(length<receipt.Length && (count=await receiptStream.ReadAsync(receipt.AsMemory(length),cancellationToken).ConfigureAwait(false))>0)length+=count;
                        if(length<=4096)success=JsonNode.Parse(receipt.AsSpan(0,length))?["saved"]?.GetValue<bool>()==true;
                    }
                }
                if(!success)return TelemetrySendResult.Fail("Report saved locally; upload failed. It remains queued for retry.");
                await System.IO.File.WriteAllTextAsync(cached.Path+".uploaded","delivered",cancellationToken).ConfigureAwait(false);
            }
            return localOnly?TelemetrySendResult.Fail("Reports saved locally; upload service is not configured."):TelemetrySendResult.Ok();
        }
        catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested){throw;}
        catch {return TelemetrySendResult.Fail("Unable to save or send diagnostic report. Gameplay continues normally.");}
    }
    (string Path,byte[] Bytes) Cache(TelemetryEnvelope envelope)
    {
        Directory.CreateDirectory(folder);
        // The envelope identity survives queue reloads, independent of current mod version.
        string identity=envelope.TimestampUtc.ToString("O")+"\n"+envelope.EventName+"\n"+envelope.RequestId+"\n"+envelope.Payload?.ToJsonString();
        string id=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))).ToLowerInvariant();
        string path=Path.Combine(folder,id+".json");
        if(System.IO.File.Exists(path))return(path,System.IO.File.ReadAllBytes(path));
        var scrub=new FrostReportScrubber();
        var payload=envelope.Payload?.DeepClone();
        JsonArray? bundle=null;
        if(payload is JsonObject obj && obj["bundle"] is JsonObject zipped)
        {
            try {bundle=ReadBundle(zipped,scrub);}
            catch(Exception ex) when(ex is InvalidDataException or FormatException or JsonException){bundle=new JsonArray();}
            obj.Remove("bundle");
        }
        var report=new JsonObject {
            ["schema"]="frostsworn.error-report.v1",["mod_version"]=FrostVersion.Value,
            ["timestamp_utc"]=envelope.TimestampUtc.ToString("O"),["event"]=scrub.Text(envelope.EventName),
            ["request"]=envelope.RequestId,["payload"]=scrub.Node(payload),["bundle_files"]=bundle,
            ["diagnostic_log_tail"]=scrub.DiagnosticLines(ReadLogTail(logFolder)),
            ["redaction_note"]="Diagnostic excerpts only. Account IDs, contacts, credentials and absolute paths are removed. No raw saves or raw ZIP attachments."
        };
        byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(report);
        if(bytes.Length>FrostErrorReports.MaxReportBytes)
        {
            report["bundle_files"]=null;report["truncated"]=true;bytes=JsonSerializer.SerializeToUtf8Bytes(report);
        }
        if(bytes.Length>FrostErrorReports.MaxReportBytes)
        {
            report["payload"]=new JsonObject {["summary"]="Oversized payload omitted; diagnostic log excerpt retained."};
            bytes=JsonSerializer.SerializeToUtf8Bytes(report);
        }
        string temp=path+".tmp";System.IO.File.WriteAllBytes(temp,bytes);System.IO.File.Move(temp,path,true);
        return(path,bytes);
    }
    static JsonArray ReadBundle(JsonObject zipped,FrostReportScrubber scrub)
    {
        var files=new JsonArray();
        string content=zipped["content"]?.GetValue<string>()??"";
        if(content.Length>4*1024*1024)return files;
        using var stream=new MemoryStream(Convert.FromBase64String(content));
        using var archive=new ZipArchive(stream,ZipArchiveMode.Read);
        long total=0;
        foreach(var entry in archive.Entries.Take(32))
        {
            string name=entry.FullName.Replace('\\','/');
            if(name.Contains("../",StringComparison.Ordinal) || name.StartsWith('/') || name.Contains(':') || entry.Length>512*1024)continue;
            if(!new[]{".json",".log",".txt"}.Contains(Path.GetExtension(name),StringComparer.OrdinalIgnoreCase))continue;
            total+=entry.Length;if(total>768*1024)break;
            using var input=entry.Open();using var bounded=new MemoryStream();
            var buffer=new byte[8192];int count;
            while((count=input.Read(buffer,0,buffer.Length))>0 && bounded.Length+count<=512*1024)bounded.Write(buffer,0,count);
            if(count>0)continue;
            string text=Encoding.UTF8.GetString(bounded.ToArray());
            JsonNode? data;
            if(Path.GetExtension(name).Equals(".json",StringComparison.OrdinalIgnoreCase))
            {try {data=scrub.Node(JsonNode.Parse(text));}catch(JsonException){continue;}}
            else data=JsonValue.Create(scrub.DiagnosticLines(text));
            files.Add(new JsonObject {["name"]=scrub.Text(Path.GetFileName(name)),["data"]=data});
        }
        return files;
    }
    static string ReadLogTail(string folder)
    {
        try
        {
            var file=new DirectoryInfo(folder).EnumerateFiles("godot*.log").Where(x=>x.Length>0).OrderByDescending(x=>x.LastWriteTimeUtc).FirstOrDefault();
            if(file==null)return "";
            using var stream=new FileStream(file.FullName,FileMode.Open,System.IO.FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
            long length=Math.Min(stream.Length,65536);stream.Seek(-length,SeekOrigin.End);
            var bytes=new byte[(int)length];int count=stream.Read(bytes,0,bytes.Length);
            return Encoding.UTF8.GetString(bytes,0,count);
        }
        catch{return "";}
    }
}

public sealed class FrostReportScrubber
{
    static Regex Rx(string pattern)=>new(pattern,RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
    static readonly Regex Paths=Rx(@"(?<keep>(?:res|user)://[^\s\""'<>]+)|(?:file://)?[a-z]:[\\/][^\r\n\""'<>]+|(?<![:\w/])/(?:home|Users|root|tmp|var|mnt)/[^\r\n\""'<>]+|\\\\[^\r\n\""'<>]+"),
        Emails=Rx(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b"),
        Ids=Rx(@"(?<!\d)\d{17}(?!\d)"),
        Addresses=Rx(@"\b(?:\d{1,3}\.){3}\d{1,3}(?::\d+)?\b|(?<!\w)(?:[0-9a-f]{0,4}:){2,}[0-9a-f:]{0,39}(?!\w)"),
        Secrets=Rx(@"\b(?:github_pat_|gh[pousr]_)[A-Za-z0-9_]+|Bearer\s+[^\s\""'<>]+|(?:(?:password|token|secret|api[_-]?key)\s*[=:]\s*)[^\s\""'<>]+"),
        SensitiveLine=Rx(@"\b(?:chat|username|display.?name|player.?name|account|profile|authorization|password|api[_-]?key)\b"),
        DiagnosticLine=Rx(@"^\s*(?:ERROR:|WARNING:|at\s|C# backtrace|\[\d+\])|\[(?:ERROR|WARN)\]|\[(?:INFO|DEBUG)\].*(?:Frostsworn|Eclipse|divergence|checksum)");
    readonly Dictionary<string,string> aliases=new(StringComparer.Ordinal);
    readonly string[] localNames=new[]{System.Environment.UserName,System.Environment.MachineName}.Where(x=>x.Length>=3).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    string Alias(string raw)
    {if(!aliases.TryGetValue(raw,out var value))aliases[raw]=value="player-"+(aliases.Count+1);return value;}
    public string Text(string text)
    {
        try
        {
            text=Paths.Replace(text,m=>m.Groups["keep"].Success?m.Value:"[absolute-path]");
            text=Emails.Replace(text,"[email]");text=Ids.Replace(text,m=>Alias(m.Value));
            text=Addresses.Replace(text,"[ip]");text=Secrets.Replace(text,"[credential]");
            foreach(string name in localNames)text=Rx(@"(?<!\w)"+Regex.Escape(name)+@"(?!\w)").Replace(text,"[local-identity]");
            return text;
        }
        catch(RegexMatchTimeoutException){return "[redacted]";}
    }
    public JsonNode? Node(JsonNode? node,int depth=0)
    {
        if(node==null)return null;if(depth>40)return JsonValue.Create("[depth-limit]");
        if(node is JsonObject obj)
        {
            var result=new JsonObject();
            foreach(var pair in obj.Take(1000))
            {
                string key=pair.Key.ToLowerInvariant().Replace("_","").Replace("-","");
                if(new[]{"username","playername","displayname","steamid","accountid","installid","anonymousinstallid","profile","ipaddress","email","authorization","password","token","secret","apikey","chat","hostname","computername"}.Any(x=>key.Contains(x,StringComparison.Ordinal)))continue;
                result[Text(pair.Key)]=Node(pair.Value,depth+1);
            }
            return result;
        }
        if(node is JsonArray array)return new JsonArray(array.Take(1000).Select(x=>Node(x,depth+1)).ToArray());
        if(node is JsonValue value && value.TryGetValue<string>(out var text))return JsonValue.Create(Text(text.Length>65536?text[..65536]+" [truncated]":text));
        // Large numeric account/peer IDs must also be aliased, without losing party correspondence.
        string raw=node.ToJsonString();if(Ids.IsMatch(raw))return JsonValue.Create(Text(raw));
        return node.DeepClone();
    }
    public string DiagnosticLines(string log)
    {
        try {return string.Join('\n',log.Split('\n').Where(line=>DiagnosticLine.IsMatch(line) && !SensitiveLine.IsMatch(line)).TakeLast(240).Select(Text));}
        catch(RegexMatchTimeoutException){return "[redacted]";}
    }
}

