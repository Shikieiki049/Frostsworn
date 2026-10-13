using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Frostsworn;
using STS2RitsuLib.Telemetry;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static async Task CheckErrorReports0829()
    {
        string root=Path.Combine(Path.GetTempPath(),"frost-report-tests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Assert(FrostReportSettings.Supported,"native consent bridge matches installed RitsuLib methods");
            FrostReportSettings.Register();
            var settings=STS2RitsuLib.Settings.ModSettingsRegistry.GetPages().Single(x=>x.ModId==Entry.ModId);
            Assert(settings.Sections.SelectMany(x=>x.Entries).Any(x=>x.Id=="allow_error_reports" && x is STS2RitsuLib.Settings.ToggleModSettingsEntryDefinition),"real RitsuLib settings registry contains the report toggle under Frostsworn");
            int defaults=0;
            FrostReportSettings.ApplyDefault(TelemetryConsentState.Unknown,value=>{Assert(value,"first install enables reports");defaults++;});
            FrostReportSettings.ApplyDefault(TelemetryConsentState.Denied,value=>defaults++);
            FrostReportSettings.ApplyDefault(TelemetryConsentState.Granted,value=>defaults++);
            Assert(defaults==1,"startup default never overrides an existing allow or deny choice");
            bool allowed=false;
            var binding=FrostReportSettings.CreateBinding(()=>allowed,value=>allowed=value);
            binding.Write(true);Assert(binding.Read(),"settings toggle immediately enables native permission binding");
            allowed=false;Assert(!binding.Read(),"external native consent changes are reflected by settings toggle");
            binding.Write(false);binding.Save();Assert(!binding.Read(),"disabling reports remains disabled when settings save");
            Assert(!FrostReportSettings.IsAllowed(TelemetryConsentState.Denied,FrostReportSettings.Requests) && !FrostReportSettings.IsAllowed(TelemetryConsentState.Granted,new[]{"diagnostics"}),"denial and incomplete permission never display fully allowed");
            var scrub=new FrostReportScrubber();
            string raw="ERROR: 76561198000000001 76561198000000002 email@example.com 192.168.1.2 ::1 ghp_exampleSECRET C:\\Users\\TestPlayer\\save.json";
            string cleaned=scrub.Text(raw);
            Assert(!new[]{"76561198","example.com","192.168","::1","exampleSECRET","TestPlayer"}.Any(cleaned.Contains),"public text strips IDs, email, both IP families, credentials and home paths");
            Assert(cleaned.Contains("player-1") && cleaned.Contains("player-2"),"different peers retain distinct anonymous labels");
            Assert(scrub.Text("res://Frostsworn/scene.tscn user://logs/error.log").Contains("res://Frostsworn/scene.tscn"),"resource paths remain useful");
            var state=scrub.Node(JsonNode.Parse("{\"76561198000000001\":{\"hp\":48,\"username\":\"PrivateName\"},\"peer\":76561198000000002,\"password\":\"private-password\"}"))!.ToJsonString();
            Assert(state.Contains("player-1") && state.Contains("player-2") && state.Contains("48") && !state.Contains("PrivateName") && !state.Contains("private-password"),"state diff keeps gameplay values and consistent peer correspondence");
            Assert(!scrub.DiagnosticLines("[INFO] chat hello\nERROR: actual fault\n   at Frostsworn.Method()\n[INFO] account private").Contains("private"),"only diagnostic lines are retained");
            foreach(string url in new[]{"http://example.com/reports","https://name:pass@example.com/reports","https://example.com/reports?token=x"})
            {
                bool rejected=false;try {_=new FrostReportAdapter(url,root,root);}catch(ArgumentException){rejected=true;}
                Assert(rejected,"unsafe endpoint rejected");
            }
            var local=new FrostReportAdapter("",Path.Combine(root,"local"),root);
            var applicant=FrostErrorReports.CreateApplicant(local);
            Assert(applicant.Requests.Count==2 && applicant.Requests.All(x=>x.Category==TelemetryDataCategory.Diagnostics),"only consent-controlled error and divergence requests registered");
            TelemetryEnvelope Envelope(string message,string request="diagnostics")=>new() {ApplicantId=FrostErrorReports.ApplicantId,EventName="test.error",RequestId=request,Category=TelemetryDataCategory.Diagnostics,Payload=new JsonObject {["message"]=message}};
            var first=Envelope(raw);var second=Envelope("second");
            var result=await local.SendAsync(applicant,new[]{first,second});
            Assert(!result.Success && Directory.GetFiles(Path.Combine(root,"local"),"*.json").Length==2,"unconfigured relay saves every report and retains queue");
            Assert(!File.ReadAllText(Directory.GetFiles(Path.Combine(root,"local"),"*.json")[0]).Contains("exampleSECRET"),"cached diagnostic is sanitized before writing");
            int sent=0;byte[]? attempt=null;
            var retry=new FrostReportAdapter("",Path.Combine(root,"retry"),root,(bytes,ct)=> {sent++;attempt??=bytes;Assert(attempt.SequenceEqual(bytes),"retry sends identical cached bytes");return Task.FromResult(sent>1);});
            result=await retry.SendAsync(FrostErrorReports.CreateApplicant(retry),new[]{first});
            Assert(!result.Success,"failed upload stays queued");
            result=await retry.SendAsync(FrostErrorReports.CreateApplicant(retry),new[]{first});
            Assert(result.Success && sent==2,"next attempt succeeds");
            await retry.SendAsync(FrostErrorReports.CreateApplicant(retry),new[]{first});
            Assert(sent==2,"delivered envelope is not uploaded again");
            int batchCalls=0;bool online=false;
            var partial=new FrostReportAdapter("",Path.Combine(root,"partial"),root,(bytes,ct)=>Task.FromResult(++batchCalls==1 || online));
            result=await partial.SendAsync(FrostErrorReports.CreateApplicant(partial),new[]{first,second});
            Assert(!result.Success && batchCalls==2,"partial delivery keeps remaining batch queued");
            online=true;result=await partial.SendAsync(FrostErrorReports.CreateApplicant(partial),new[]{first,second});
            Assert(result.Success && batchCalls==3,"partial batch retries only undelivered report");
            File.WriteAllText(Path.Combine(root,"godot-test.log"),"[INFO] chat private-chat\nERROR: "+raw+"\n   at Frostsworn.Method()\n[INFO] account private-account");
            string difference="State divergence diagnostics\nLocal progress\nPRIVATE_PROGRESS\nOverview\n--------\nchecksum.value: local=123; remote=456\nPlayers\n-------\nplayers[0].maxHp: local=48; remote=64\nplayers[0].powers\n  local:\n    FROSTSWORN_POWER_ICE_ARMOR: 2\n  remote:\n    FROSTSWORN_POWER_ICE_ARMOR: 3\nplayers[0].username\n  local:\n    PRIVATE_PLAYER\nnetwork.remotePeer: local=76561198000000001; remote=76561198000000002\ncontext.path: local=C:/Users/PrivatePerson/run.json\nLOCAL STATE DUMP\nRAW_FULL_STATE\nREMOTE STATE DUMP\nRAW_REMOTE_STATE";
            string diff=scrub.DivergenceReport(difference);
            Assert(diff.Contains("local=48; remote=64") && diff.Contains("ICE_ARMOR: 2") && diff.Contains("ICE_ARMOR: 3"),"plain divergence summary keeps scalar and multiline gameplay differences");
            Assert(!new[]{"PRIVATE_PROGRESS","PRIVATE_PLAYER","PrivatePerson","76561198","RAW_FULL_STATE","RAW_REMOTE_STATE"}.Any(diff.Contains),"difference summary removes identities, paths, preamble progress and complete state dumps");
            Assert(scrub.DivergenceReport("unrecognized report format\nPRIVATE_PLAYER")=="","unknown report format does not publish unrestricted text");
            Assert(scrub.DivergenceReport("Overview\n"+string.Join('\n',Enumerable.Repeat(new string('x',5000),2000))).Length<100000,"plain summary is bounded");
            using var zip=new MemoryStream();
            using(var archive=new ZipArchive(zip,ZipArchiveMode.Create,true))
            {
                foreach(var pair in new[]{("diff.json","{\"hp\":48,\"steam_id\":\"76561198000000001\"}"),("../escape.json","{}"),("save.bin","RAW_SAVE"),("trace.log",raw),("state-divergence-report.txt",difference)})
                {using var writer=new StreamWriter(archive.CreateEntry(pair.Item1).Open());writer.Write(pair.Item2);}
            }
            var divergence=Envelope("difference","state_divergence");
            ((JsonObject)divergence.Payload!)["bundle"]=new JsonObject {["content"]=Convert.ToBase64String(zip.ToArray())};
            byte[]? captured=null;
            var zipped=new FrostReportAdapter("",Path.Combine(root,"bundle"),root,(bytes,ct)=> {captured=bytes;return Task.FromResult(true);});
            await zipped.SendAsync(FrostErrorReports.CreateApplicant(zipped),new[]{divergence});
            string bundle=Encoding.UTF8.GetString(captured!);
            Assert(bundle.Contains("diff.json") && bundle.Contains("48") && !bundle.Contains("escape.json") && !bundle.Contains("RAW_SAVE") && !bundle.Contains("76561198"),"divergence ZIP permits only sanitized diagnostic entries and excludes traversal/binary saves");
            Assert(JsonNode.Parse(captured!)!["bundle_files"]!.AsArray().Single(x=>x?["name"]?.GetValue<string>()=="state-divergence-report.txt")!["data"]!.GetValue<string>().Contains("local=48; remote=64"),"actual ZIP report filename uses summary parsing instead of log filtering");
            Assert(!bundle.Contains("RAW_FULL_STATE") && !bundle.Contains("PRIVATE_PLAYER"),"ZIP summary never transmits full states or personal names");
            Assert(bundle.Contains("Frostsworn.Method") && !bundle.Contains("private-chat") && !bundle.Contains("private-account"),"real log tail selection preserves diagnostic stack and filters personal lines");
            ((JsonObject)divergence.Payload!)["bundle"]=new JsonObject {["content"]="invalid-base64"};
            result=await zipped.SendAsync(FrostErrorReports.CreateApplicant(zipped),new[]{divergence});
            Assert(result.Success,"damaged ZIP does not poison the diagnostic queue");
            using var largeZip=new MemoryStream();
            using(var archive=new ZipArchive(largeZip,ZipArchiveMode.Create,true))
            {
                using(var writer=new StreamWriter(archive.CreateEntry("diff.json").Open()))writer.Write("{\"hp\":17,\"path\":\"C:/Users/PrivatePerson/run.json\"}");
                var noise=new byte[100000];new Random(42).NextBytes(noise);
                using(var writer=new StreamWriter(archive.CreateEntry("noise.txt").Open()))writer.Write(Convert.ToBase64String(noise));
            }
            string encoded=Convert.ToBase64String(largeZip.ToArray());
            Assert(encoded.Length>65536,"nested attachment regression exceeds old string truncation limit");
            var nested=Envelope("nested","state_divergence");
            ((JsonObject)nested.Payload!)["applicant_payload"]=new JsonObject {["bundle"]=new JsonObject {["content"]=encoded}};
            await zipped.SendAsync(FrostErrorReports.CreateApplicant(zipped),new[]{nested});
            var extracted=JsonNode.Parse(captured!)!;
            Assert(extracted["bundle_files"]!.AsArray().Any(x=>x?["name"]?.GetValue<string>()=="diff.json") && !Encoding.UTF8.GetString(captured!).Contains("PrivatePerson") && extracted["payload"]?["applicant_payload"]?["bundle"]==null,"native nested ZIP extracts sanitized differences without raw attachment");
            string pendingFolder=Path.Combine(root,"pending");
            var pending=new FrostReportAdapter("",pendingFolder,root,(bytes,ct)=>{captured=bytes;return Task.FromResult(false);});
            await pending.SendAsync(FrostErrorReports.CreateApplicant(pending),new[]{nested});
            string pendingPath=Directory.GetFiles(pendingFolder,"*.json").Single();
            File.WriteAllText(pendingPath,"{\"attachment_parser\":2,\"payload\":\"old broken cache\"}");
            await pending.SendAsync(FrostErrorReports.CreateApplicant(pending),new[]{nested});
            Assert(JsonNode.Parse(captured!)?["attachment_parser"]?.GetValue<int>()==3,"pending old cache is regenerated from queued original envelope");
            using var cancellation=new CancellationTokenSource();cancellation.Cancel();bool canceled=false;
            try {await local.SendAsync(applicant,new[]{first},cancellation.Token);}catch(OperationCanceledException){canceled=true;}
            Assert(canceled,"shutdown cancellation propagates without marking delivery");
        }
        finally {Directory.Delete(root,true);}
    }
}
