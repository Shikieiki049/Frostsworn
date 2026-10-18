using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Frostsworn;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rngs;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static Task CheckSyncRng097()
    {
        var rng = new SerializableRunRngSet { Seed = "NETWORK097" };
        foreach(var key in Enum.GetValues<RunRngType>())
            rng.Rngs[key] = new SerializableRng { counter = (int)key+53, state0=ulong.MaxValue, state1=1, state2=0x123456789abcdefUL, state3=37 };
        rng.Rngs[(RunRngType)0x60001234] = new SerializableRng { counter=500, state0=31,state1=32,state2=33,state3=34 };
        var bag = new SerializableRelicGrabBag();
        var ids = ModelDb.All.OfType<RelicModel>().Select(r=>r.Id).Distinct().ToList();
        bag.RelicIdLists[RelicRarity.Common] = ids;
        bag.RelicIdLists[RelicRarity.Rare] = new();
        bag.RelicIdLists[VoidRelics.Rarity] = VoidRelics.Types.Select(ModelDb.GetId).Reverse().ToList();
        var input = new SyncRngMessage { rng = rng, sharedRelicGrabBag=bag };
        var writer = new PacketWriter(); input.Serialize(writer);
        var bytes = writer.Buffer.Take(writer.BytePosition).ToArray();
        Assert(BitConverter.ToInt32(bytes)==FrostRngWire097.Magic,"native SyncRngMessage.Serialize dispatches versioned wire patch");
        int originalBits=ModelIdSerializationCache.EntryIdBitSize;
        var bitProperty=typeof(ModelIdSerializationCache).GetProperty(nameof(ModelIdSerializationCache.EntryIdBitSize))!;
        var map=(List<string>)typeof(ModelIdSerializationCache).GetField("_netIdToEntryNameMap",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
        var saved=map.ToArray();
        try
        {
            // Simulate the observed 11/12-bit transition, an even wider table,
            // and an unrelated local entry order. Name-based snapshots must not
            // consult any of these network indices during read OR write.
            foreach(int bits in new[]{11,12,13})
            {
                bitProperty.SetValue(null,bits);map.Clear();map.AddRange(Enumerable.Range(0,bits==11?1983:bits==12?2438:4190).Reverse().Select(i=>"DIFFERENT_LOCAL_ENTRY_"+i));
                var reader=new PacketReader();reader.Reset(bytes);
                var output=new SyncRngMessage();output.Deserialize(reader);
                Assert(output.rng!.Seed==rng.Seed && output.rng.Rngs.Count==rng.Rngs.Count && rng.Rngs.All(p=>output.rng.Rngs[p.Key]==p.Value),"all RNG counters and 256-bit states preserved at entry width "+bits);
                Assert(bag.RelicIdLists.Count==output.sharedRelicGrabBag!.RelicIdLists.Count && bag.RelicIdLists.All(p=>output.sharedRelicGrabBag!.RelicIdLists[p.Key].SequenceEqual(p.Value)),"all relics, empty/custom rarity buckets and exact queue order preserved at width "+bits);
                var roundtrip=new PacketWriter();output.Serialize(roundtrip);
                Assert(bytes.SequenceEqual(roundtrip.Buffer.Take(roundtrip.BytePosition)),"network bytes independent of receiver entry table at width "+bits);
            }
        }
        finally {bitProperty.SetValue(null,originalBits);map.Clear();map.AddRange(saved);}
        var bad=(byte[])bytes.Clone();bad[0]^=1;
        bool rejected=false;try{var reader=new PacketReader();reader.Reset(bad);new SyncRngMessage().Deserialize(reader);}catch(System.IO.InvalidDataException){rejected=true;}
        Assert(rejected,"incompatible RNG wire revision rejected without accepting partial state");
        var nullableInput=new SyncRngMessage {rng=new SerializableRunRngSet(),sharedRelicGrabBag=new SerializableRelicGrabBag()};
        var nullableWriter=new PacketWriter();nullableWriter.Write(nullableInput);
        var nullableReader=new PacketReader();nullableReader.Reset(nullableWriter.Buffer.Take(nullableWriter.BytePosition).ToArray());
        var nullableOutput=nullableReader.Read<SyncRngMessage>();
        Assert(nullableOutput.rng!.Seed==null && nullableOutput.rng.Rngs.Count==0 && nullableOutput.sharedRelicGrabBag!.RelicIdLists.Count==0,"generic packet interface preserves null seed and empty snapshots");
        var invalidWriter=new PacketWriter();invalidWriter.WriteInt(FrostRngWire097.Magic);invalidWriter.WriteBool(false);invalidWriter.WriteInt(-1);
        var invalidReader=new PacketReader();invalidReader.Reset(invalidWriter.Buffer.Take(invalidWriter.BytePosition).ToArray());
        var unchanged=input;rejected=false;try{unchanged.Deserialize(invalidReader);}catch(System.IO.InvalidDataException){rejected=true;}
        Assert(rejected && ReferenceEquals(unchanged.rng,input.rng) && ReferenceEquals(unchanged.sharedRelicGrabBag,input.sharedRelicGrabBag),"malformed packet rejected before assigning any partial snapshot");
        var unknown=new SyncRngMessage {rng=rng,sharedRelicGrabBag=new SerializableRelicGrabBag()};
        unknown.sharedRelicGrabBag.RelicIdLists[RelicRarity.Common]=new(){new ModelId("RELIC","MISSING_RELIC_097")};
        var unknownWriter=new PacketWriter();unknown.Serialize(unknownWriter);
        var unknownReader=new PacketReader();unknownReader.Reset(unknownWriter.Buffer.Take(unknownWriter.BytePosition).ToArray());
        unchanged=input;rejected=false;try{unchanged.Deserialize(unknownReader);}catch(System.IO.InvalidDataException){rejected=true;}
        Assert(rejected && ReferenceEquals(unchanged.sharedRelicGrabBag,input.sharedRelicGrabBag),"unknown relic is explicitly rejected, never dropped or substituted");
        var hashField=typeof(ModelIdSerializationCache).GetField("<Hash>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static)!;
        uint native=(uint)hashField.GetValue(null)!;
        Assert(ModelIdSerializationCache.Hash==(native^FrostRngWire097.ProtocolHash) && ModelIdSerializationCache.Hash!=native,"native handshake hash distinguishes previous wire protocol");
        GD.Print($"PASS: RNG097 snapshot {bytes.Length} bytes, {ids.Count} relics, all RNG streams preserved");
        return Task.CompletedTask;
    }
}
