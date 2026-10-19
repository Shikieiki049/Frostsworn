#if STS2_STABLE_107
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rngs;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;
namespace Frostsworn.Tests;
public static partial class Suite
{
 static uint rawStableHash010;
 public static void CaptureRawStableHash010()=>rawStableHash010=ModelIdSerializationCache.Hash;
 static void CheckNativeNetwork107()
 {
  var rng=new SerializableRunRngSet {Seed="STABLE010_HOST"};
  foreach(var k in Enum.GetValues<RunRngType>())rng.Counters[k]=(int)k+57;
  rng.Counters[(RunRngType)0x60001234]=500;
  var bag=new SerializableRelicGrabBag();
  var ids=ModelDb.AllRelics.Select(r=>r.Id).Distinct().Reverse().ToList();
  bag.RelicIdLists[RelicRarity.Common]=ids;bag.RelicIdLists[RelicRarity.Rare]=new();
  bag.RelicIdLists[VoidRelics.Rarity]=VoidRelics.Types.Select(ModelDb.GetId).ToList();
  var input=new SyncRngMessage {rng=rng,sharedRelicGrabBag=bag};
  var writer=new PacketWriter();writer.Write(input);var bytes=writer.Buffer.Take(writer.BytePosition).ToArray();
  Assert(BitConverter.ToInt32(bytes)==0x46535232,"stable SyncRngMessage uses FSR2, not native compressed relic indices or beta FSR1");
  var map=(List<string>)typeof(ModelIdSerializationCache).GetField("_netIdToEntryNameMap",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  var saved=map.ToArray();var width=typeof(ModelIdSerializationCache).GetProperty(nameof(ModelIdSerializationCache.EntryIdBitSize))!;int oldWidth=ModelIdSerializationCache.EntryIdBitSize;
  try
  {
   foreach(var pair in new[]{(11,1983),(12,2108),(12,2438),(13,4190)})
   {
    width.SetValue(null,pair.Item1);map.Clear();map.AddRange(Enumerable.Range(0,pair.Item2).Reverse().Select(i=>"UNRELATED_LOCAL_ID_"+i));
    var reader=new PacketReader();reader.Reset(bytes);var output=reader.Read<SyncRngMessage>();
    Assert(output.rng.Seed==rng.Seed && output.rng.Counters.Count==rng.Counters.Count && rng.Counters.All(p=>output.rng.Counters[p.Key]==p.Value),"every stable RNG counter including custom keys retained with receiver table "+pair);
    Assert(bag.RelicIdLists.Count==output.sharedRelicGrabBag.RelicIdLists.Count && bag.RelicIdLists.All(p=>output.sharedRelicGrabBag.RelicIdLists[p.Key].SequenceEqual(p.Value)),"all relics, custom rarity, empty queues and exact order retained with receiver table "+pair);
    var rewritten=new PacketWriter();rewritten.Write(output);Assert(bytes.SequenceEqual(rewritten.Buffer.Take(rewritten.BytePosition)),"snapshot writing also ignores the receiver's unrelated ID table "+pair);
   }
  }
  finally {map.Clear();map.AddRange(saved);width.SetValue(null,oldWidth);}
  // Negative control reproduces the old encoding's dependency on shared bit width.
  var legacy=new SerializableRelicGrabBag();legacy.RelicIdLists[RelicRarity.Common]=ids;
  width.SetValue(null,12);var lw=new PacketWriter();legacy.Serialize(lw);var lr=new PacketReader();lr.Reset(lw.Buffer.Take(lw.BytePosition).ToArray());
  bool legacyBroken=false;
  try {width.SetValue(null,11);var lo=new SerializableRelicGrabBag();lo.Deserialize(lr);legacyBroken=!lo.RelicIdLists[RelicRarity.Common].SequenceEqual(ids);}
  catch(ArgumentOutOfRangeException){legacyBroken=true;}
  finally {width.SetValue(null,oldWidth);}
  Assert(legacyBroken,"negative control: native 12-bit sender / 11-bit receiver corrupts the relic pool, matching the observed failure class");
  var empty=new SyncRngMessage {rng=new SerializableRunRngSet(),sharedRelicGrabBag=new SerializableRelicGrabBag()};
  var ew=new PacketWriter();ew.Write(empty);var er=new PacketReader();er.Reset(ew.Buffer.Take(ew.BytePosition).ToArray());var eo=er.Read<SyncRngMessage>();
  Assert(eo.rng.Seed==null && eo.rng.Counters.Count==0 && eo.sharedRelicGrabBag.RelicIdLists.Count==0,"FSR2 preserves a null seed and empty snapshot without silently inventing state");
  var bad=(byte[])bytes.Clone();bad[0]^=1;var br=new PacketReader();br.Reset(bad);var unchanged=input;bool rejected=false;
  try {unchanged.Deserialize(br);}catch(InvalidDataException){rejected=true;}
  Assert(rejected && ReferenceEquals(unchanged.rng,input.rng),"wrong protocol marker rejected before assigning partial state");
  var malformed=new PacketWriter();malformed.WriteInt(FrostRngWire097.Magic);malformed.WriteBool(false);malformed.WriteInt(-1);
  var mr=new PacketReader();mr.Reset(malformed.Buffer.Take(malformed.BytePosition).ToArray());rejected=false;
  try {unchanged.Deserialize(mr);}catch(InvalidDataException){rejected=true;}
  Assert(rejected && ReferenceEquals(unchanged.sharedRelicGrabBag,input.sharedRelicGrabBag),"negative count rejected atomically");
  var unknown=new SyncRngMessage {rng=rng,sharedRelicGrabBag=new SerializableRelicGrabBag()};unknown.sharedRelicGrabBag.RelicIdLists[RelicRarity.Common]=new(){new ModelId("RELIC","NOT_INSTALLED_010")};
  var uw=new PacketWriter();uw.Write(unknown);var ur=new PacketReader();ur.Reset(uw.Buffer.Take(uw.BytePosition).ToArray());rejected=false;
  try {unchanged.Deserialize(ur);}catch(InvalidDataException){rejected=true;}
  Assert(rejected && ReferenceEquals(unchanged.rng,input.rng),"missing gameplay relic rejects snapshot instead of dropping rewards or substituting NONE");
  new HarmonyLib.Harmony("Frostsworn.Tests010").Patch(typeof(ModelIdSerializationCache).GetMethod("Init")!,postfix:new HarmonyLib.HarmonyMethod(typeof(Suite).GetMethod(nameof(CaptureRawStableHash010))!));
  var field=typeof(ModelIdSerializationCache).GetField("<Hash>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static)!;
  var method=new System.Reflection.Emit.DynamicMethod("StableEarlyHashRead",typeof(uint),Type.EmptyTypes,typeof(ModelIdSerializationCache),true);
  var il=method.GetILGenerator();il.Emit(System.Reflection.Emit.OpCodes.Ldsfld,field);il.Emit(System.Reflection.Emit.OpCodes.Ret);var direct=(Func<uint>)method.CreateDelegate(typeof(Func<uint>));
  foreach(int repeat in new[]{0,1})
  {
   ModelIdSerializationCache.Init();uint expected=rawStableHash010^FrostRngWire097.ProtocolHash;
   Assert(ModelIdSerializationCache.Hash==expected && direct()==expected,"actual native Init finalizes stored FSR2 hash for getter and direct/inlined readers "+repeat);
   FrostRngProtocol097.Apply();Assert(ModelIdSerializationCache.Hash==expected,"repeated finalization cannot toggle hash "+repeat);
  }
 }
}
#endif
