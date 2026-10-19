#if STS2_STABLE_107
using System;
using System.Linq;
using System.Reflection;
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
 static void CheckNativeNetwork107()
 {
  var rng=new SerializableRunRngSet {Seed="FROST_STABLE_SAVE_RELOAD"};
  foreach(var key in Enum.GetValues<RunRngType>())rng.Counters[key]=(int)key+57;
  var bag=new SerializableRelicGrabBag();
  bag.RelicIdLists[RelicRarity.Common]=ModelDb.AllRelics.Select(r=>r.Id).Distinct().Reverse().ToList();
  bag.RelicIdLists[RelicRarity.Rare]=new();
  var input=new SyncRngMessage {rng=rng,sharedRelicGrabBag=bag};
  uint hash=ModelIdSerializationCache.Hash;
  ModelIdSerializationCache.Init();
  Assert(hash==ModelIdSerializationCache.Hash,"native stable model hash remains unchanged on repeated initialization");
  var width=typeof(ModelIdSerializationCache).GetProperty(nameof(ModelIdSerializationCache.EntryIdBitSize))!;
  int savedWidth=ModelIdSerializationCache.EntryIdBitSize;
  try
  {
   foreach(int bits in new[]{savedWidth,11,12,13}.Distinct())
   {
    width.SetValue(null,bits);
    var writer=new PacketWriter();writer.Write(input);
    var bytes=writer.Buffer.Take(writer.BytePosition).ToArray();
    var reader=new PacketReader();reader.Reset(bytes);
    var output=reader.Read<SyncRngMessage>();
    Assert(output.rng.Seed==rng.Seed && output.rng.Counters.Count==rng.Counters.Count && rng.Counters.All(p=>output.rng.Counters[p.Key]==p.Value),"native RNG seed and every counter roundtrip at shared entry width "+bits);
    Assert(bag.RelicIdLists.All(p=>output.sharedRelicGrabBag.RelicIdLists[p.Key].SequenceEqual(p.Value)),"all native/custom relic IDs and empty reward queues retain exact order at width "+bits);
    var rewritten=new PacketWriter();rewritten.Write(output);
    Assert(bytes.SequenceEqual(rewritten.Buffer.Take(rewritten.BytePosition)),"native host/client snapshot bytes agree at shared width "+bits);
   }
  }
  finally {width.SetValue(null,savedWidth);}
  var empty=new SyncRngMessage {rng=new SerializableRunRngSet {Seed=""},sharedRelicGrabBag=new SerializableRelicGrabBag()};
  var ew=new PacketWriter();ew.Write(empty);var er=new PacketReader();er.Reset(ew.Buffer.Take(ew.BytePosition).ToArray());var eo=er.Read<SyncRngMessage>();
  Assert(eo.rng.Seed=="" && eo.rng.Counters.Count==0 && eo.sharedRelicGrabBag.RelicIdLists.Count==0,"native empty/new-run synchronization preserves empty seed and empty queues");
  bool missingSeedRejected=false;
  try {new SyncRngMessage {rng=new SerializableRunRngSet(),sharedRelicGrabBag=new SerializableRelicGrabBag()}.Serialize(new PacketWriter());}
  catch(ArgumentNullException){missingSeedRejected=true;}
  Assert(missingSeedRejected,"stable native writer rejects an uninitialized null seed; active runs use a concrete seed");
  Assert(typeof(Entry).Assembly.GetTypes().All(t=>t.Name!="FrostRngWrite097" && t.Name!="FrostRngRead097" && t.Name!="FrostRngProtocol097"),"beta RNG wire and hash patches absent from stable assembly");
 }
}
#endif
