#if !STS2_STABLE_107
using System.IO;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Rngs;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
namespace Frostsworn;

// Only the RNG/shared relic-pool snapshot uses this versioned encoding. Never
// recover by dropping a relic or accepting an unresolved network model number.
public static class FrostRngWire097
{
    public const int Magic = 0x46535231;
    public const uint ProtocolHash = 0x46535231;
    const int MaxItems = 16384;

    public static void Write(PacketWriter writer, SyncRngMessage message)
    {
        writer.WriteInt(Magic);
        writer.WriteBool(message.rng.Seed != null);
        if (message.rng.Seed != null) writer.WriteString(message.rng.Seed);
        writer.WriteInt(message.rng.Rngs.Count);
        foreach (var pair in message.rng.Rngs.OrderBy(p => (int)p.Key))
        {
            writer.WriteInt((int)pair.Key);
            writer.Write(pair.Value);
        }
        writer.WriteInt(message.sharedRelicGrabBag.RelicIdLists.Count);
        foreach (var pair in message.sharedRelicGrabBag.RelicIdLists.OrderBy(p => (int)p.Key))
        {
            writer.WriteInt((int)pair.Key);
            writer.WriteInt(pair.Value.Count);
            foreach (var id in pair.Value)
            {
                writer.WriteString(id.Category);
                writer.WriteString(id.Entry);
            }
        }
    }

    static int Count(PacketReader reader)
    {
        int count = reader.ReadInt();
        if (count < 0 || count > MaxItems) throw new InvalidDataException("Invalid Frostsworn RNG snapshot count.");
        return count;
    }

    public static SyncRngMessage Read(PacketReader reader)
    {
        if (reader.ReadInt() != Magic) throw new InvalidDataException("Frostsworn RNG protocol mismatch. All players must use the same Frostsworn version.");
        var rng = new SerializableRunRngSet { Seed = reader.ReadBool() ? reader.ReadString() : null };
        int count = Count(reader);
        for (int i = 0; i < count; i++)
        {
            var key = (RunRngType)reader.ReadInt();
            if (!rng.Rngs.TryAdd(key, reader.Read<SerializableRng>())) throw new InvalidDataException("Duplicate RNG stream in Frostsworn snapshot.");
        }
        var bag = new SerializableRelicGrabBag();
        count = Count(reader);
        int total = 0;
        for (int i = 0; i < count; i++)
        {
            var rarity = (RelicRarity)reader.ReadInt();
            int length = Count(reader);
            total += length;
            if (total > MaxItems) throw new InvalidDataException("Frostsworn relic snapshot is too large.");
            var ids = new List<ModelId>(length);
            for (int j = 0; j < length; j++)
            {
                var id = new ModelId(reader.ReadString(), reader.ReadString());
                if (ModelDb.GetByIdOrNull<RelicModel>(id) == null) throw new InvalidDataException($"Unknown relic in Frostsworn RNG snapshot: {id}. Check matching gameplay mods.");
                ids.Add(id);
            }
            if (!bag.RelicIdLists.TryAdd(rarity, ids)) throw new InvalidDataException("Duplicate rarity in Frostsworn snapshot.");
        }
        return new SyncRngMessage { rng = rng, sharedRelicGrabBag = bag };
    }
}

[HarmonyPatch(typeof(SyncRngMessage), nameof(SyncRngMessage.Serialize))]
public static class FrostRngWrite097
{
    static bool Prefix(SyncRngMessage __instance, PacketWriter writer)
    { FrostRngWire097.Write(writer, __instance); return false; }
}
[HarmonyPatch(typeof(SyncRngMessage), nameof(SyncRngMessage.Deserialize))]
public static class FrostRngRead097
{
    static bool Prefix(ref SyncRngMessage __instance, PacketReader reader)
    { __instance = FrostRngWire097.Read(reader); return false; }
}

// Finalize the actual stored hash after every Init postfix. Patching only the
// getter is unsafe: callers can inline it or read the backing field directly.
[HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
public static class FrostRngProtocol097
{
    static uint? appliedHash;
    public static void Apply()
    {
        uint current = ModelIdSerializationCache.Hash;
        if (appliedHash == current) return;
        uint salted = current ^ FrostRngWire097.ProtocolHash;
        typeof(ModelIdSerializationCache).GetProperty(nameof(ModelIdSerializationCache.Hash))!.SetValue(null, salted);
        appliedHash = salted;
        GD.Print($"[Frostsworn] RNG protocol FSR1 stored hash finalized: {current} -> {salted}.");
    }
    static void Finalizer(Exception? __exception)
    { if (__exception == null) Apply(); }
}
#endif
