using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Rngs;
using MegaCrit.Sts2.Core.Entities.Relics;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Saves;
namespace Frostsworn.Tests;
public static partial class Suite
{
 static NetMessageBus earlyBus0913=null!;
 static SyncRngMessage EmptySnapshot0913()=>new(){rng=new SerializableRunRngSet {Seed="SEED12345678"},sharedRelicGrabBag=new SerializableRelicGrabBag()};
 [MethodImpl(MethodImplOptions.NoInlining)]
 static byte[] EarlySend0913(NetMessageBus bus,SyncRngMessage message,out int length)=>bus.SerializeMessage(17,message,out length);
 static void PrewarmNetwork0913()
 {
#if !STS2_STABLE_107
  MegaCrit.Sts2.Core.Modding.AssemblyInfo.Init();
#endif
  if(System.Environment.GetEnvironmentVariable("FROSTSWORN_FOCUSED_TEST")=="0913pre")
   AssemblyLoadContext.GetLoadContext(typeof(Suite).Assembly)!.LoadFromAssemblyPath("C:/fz/steam/steamapps/workshop/content/2868840/3747588205/Acheron.dll");
  MessageTypes.Initialize();
#if STS2_STABLE_107
  earlyBus0913=new NetMessageBus();
#else
  earlyBus0913=new NetMessageBus(new PacketReader(),new PacketWriter());
#endif
  var bytes=EarlySend0913(earlyBus0913,EmptySnapshot0913(),out int length);
  Assert(BitConverter.ToInt32(bytes,9)==12,"pre-initialization real network sender uses native 12-byte seed header, matching report");
 }
 static Task CheckCompat0913()
 {
  var input=EmptySnapshot0913();var bytes=EarlySend0913(earlyBus0913,input,out int length).Take(4096).ToArray();
  Assert(BitConverter.ToInt32(bytes,9)==FrostRngWire097.Magic,"precompiled actual NetMessageBus sender emits FSR header after mod initialization");
  Assert(earlyBus0913.TryDeserializeMessage(bytes.Take(length).ToArray(),out var output,out var sender) && output is SyncRngMessage decoded && decoded.rng.Seed==input.rng.Seed && sender==17,"actual network envelope roundtrips through native message dispatch after prewarming");
  var heartbeat=new MegaCrit.Sts2.Core.Multiplayer.Messages.HeartbeatRequestMessage {counter=73};
  var ping=earlyBus0913.SerializeMessage(31,heartbeat,out int pingLength).Take(128).ToArray();
  Assert(earlyBus0913.TryDeserializeMessage(ping.Take(pingLength).ToArray(),out var pingResult,out var pingSender) && pingResult is MegaCrit.Sts2.Core.Multiplayer.Messages.HeartbeatRequestMessage h && h.counter==73 && pingSender==31,"unrelated native message keeps its original wire encoding and sender");
  CheckFullEnvelope0913();
  CheckActualAcheron0913();
  return Task.CompletedTask;
 }
 static void CheckFullEnvelope0913()
 {
  var input=EmptySnapshot0913();
  foreach(var key in Enum.GetValues<RunRngType>())
#if STS2_STABLE_107
   input.rng.Counters[key]=(int)key+73;
#else
   input.rng.Rngs[key]=new SerializableRng {counter=(int)key+73,state0=ulong.MaxValue,state1=13,state2=17,state3=29};
#endif
#if STS2_STABLE_107
  input.sharedRelicGrabBag.RelicIdLists[RelicRarity.Common]=ModelDb.AllRelics.Select(r=>r.Id).Distinct().ToList();
#else
  input.sharedRelicGrabBag.RelicIdLists[RelicRarity.Common]=ModelDb.All.OfType<RelicModel>().Select(r=>r.Id).Distinct().ToList();
#endif
  input.sharedRelicGrabBag.RelicIdLists[VoidRelics.Rarity]=VoidRelics.Types.Select(ModelDb.GetId).ToList();
  int original=ModelIdSerializationCache.EntryIdBitSize;
  var bits=typeof(ModelIdSerializationCache).GetProperty(nameof(ModelIdSerializationCache.EntryIdBitSize))!;
  var core=new PacketWriter();FrostRngWire097.Write(core,input);var expected=core.Buffer.Take(core.BytePosition).ToArray();
  try {
   foreach(int width in new[]{11,12,13})
   {
    bits.SetValue(null,width);
    var bytes=EarlySend0913(earlyBus0913,input,out int length).Take(65536).ToArray();
    Assert(bytes.Skip(9).Take(length-9).SequenceEqual(expected),"real precompiled envelope preserves every RNG/relic byte at width "+width);
    Assert(earlyBus0913.TryDeserializeMessage(bytes.Take(length).ToArray(),out var output,out _) && output is SyncRngMessage,"real dispatch reads full snapshot at width "+width);
    var decoded=(SyncRngMessage)output!;var roundtrip=new PacketWriter();FrostRngWire097.Write(roundtrip,decoded);
    Assert(roundtrip.Buffer.Take(roundtrip.BytePosition).SequenceEqual(expected),"real dispatch retains full state and exact relic order at width "+width);
   }
  } finally {bits.SetValue(null,original);}
 }
 static void CheckActualAcheron0913()
 {
  var assembly=AssemblyLoadContext.GetLoadContext(typeof(Suite).Assembly)!.LoadFromAssemblyPath("C:/fz/steam/steamapps/workshop/content/2868840/3747588205/Acheron.dll");
  var method=assembly.GetType("Acheron.AcheronCode.Mechanics.MyriadVoidDescriptionPatch")!.GetMethod("Postfix",BindingFlags.Static|BindingFlags.NonPublic)!;
  var harmony=new Harmony("Frostsworn.preview-canonical-0913");
  Assert(Harmony.GetPatchInfo(method)?.Owners.Contains(harmony.Id)==true,"optional guard is already active before first preview, regardless of assembly load order");
  harmony.Unpatch(method,HarmonyPatchType.Prefix,harmony.Id);
  var canonical=ModelDb.Card<QuietMeditation>();
  bool reproduced=false;
  try {method.Invoke(null,new object?[]{canonical,PileType.Hand,"native description"});}
  catch(TargetInvocationException e) when(e.InnerException is MegaCrit.Sts2.Core.Models.Exceptions.CanonicalModelException){reproduced=true;}
  Assert(reproduced,"actual Acheron 0.4.11 postfix reproduces canonical Frostsworn Owner failure without guard");
  FrostPreviewCompatibility0913.TryInstall(assembly);FrostPreviewCompatibility0913.TryInstall(assembly);
  Assert(Harmony.GetPatchInfo(method)!.Prefixes.Count(p=>p.owner==harmony.Id)==1,"optional compatibility guard installs once, including late assembly loading");
  foreach(CardModel card in new CardModel[]{canonical,ModelDb.Card<TwinBlades>(),ModelDb.Card<SealSpell>()})
  {
   var args=new object?[]{card,PileType.Hand,"native description"};method.Invoke(null,args);
   Assert((string)args[2]! == "native description", "canonical "+card.GetType().Name+" keeps native preview description without mutable access");
  }
  var originalDescription=typeof(CardModel).GetMethod("GetDescriptionForPile",BindingFlags.Instance|BindingFlags.NonPublic)!;
  var integration=new Harmony("Frostsworn.Tests.AcheronInvocation0913");
  integration.Patch(originalDescription,postfix:new HarmonyMethod(method));
  try {
   foreach(var card in new CardModel[]{canonical,ModelDb.Card<TwinBlades>(),ModelDb.Card<SealSpell>()})
    Assert(!string.IsNullOrWhiteSpace(card.GetDescriptionForPile(PileType.Hand)),"native description with actual Acheron postfix supports canonical "+card.GetType().Name);
  } finally {integration.Unpatch(originalDescription,HarmonyPatchType.Postfix,integration.Id);}
  var mutable=canonical.ToMutable();var mutableArgs=new object?[]{mutable,PileType.Hand,"native description"};method.Invoke(null,mutableArgs);
  var guard=typeof(FrostPreviewCompatibility0913).GetMethod("Guard",BindingFlags.NonPublic|BindingFlags.Static)!;
  Assert((bool)guard.Invoke(null,new object[]{mutable})!,"mutable Frostsworn cards retain Acheron description behavior");
  Assert((bool)guard.Invoke(null,new object[]{ModelDb.Card<StrikeIronclad>()})!,"canonical cards from other characters are outside the guard scope");
 }

}
