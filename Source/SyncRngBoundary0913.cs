using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
namespace Frostsworn;

// SyncRngMessage.Serialize is small enough to be inlined into this generic sender
// before mods initialize. Redirect the closed value-type entry, not other packets.
[HarmonyPatch]
public static class FrostRngSendBoundary0913
{
    static MethodBase TargetMethod() => typeof(NetMessageBus).GetMethod(nameof(NetMessageBus.SerializeMessage))!.MakeGenericMethod(typeof(SyncRngMessage));
    [HarmonyPriority(Priority.First)]
    static bool Prefix(ulong senderId, SyncRngMessage message, ref int length, PacketWriter ____writer, ref byte[] __result)
    {
        ____writer.Reset();
        ____writer.WriteByte((byte)message.ToId());
        ____writer.WriteULong(senderId);
        FrostRngWire097.Write(____writer, message);
        length = ____writer.BytePosition;
        __result = ____writer.Buffer;
        return false;
    }
}

// The packet helpers can inline the same tiny struct methods independently of
// NetMessageBus. Pin both closed generic helpers to the identical wire codec.
[HarmonyPatch]
public static class FrostRngPacketWrite0913
{
    static MethodBase TargetMethod() => typeof(PacketWriter).GetMethods().Single(m=>m.Name==nameof(PacketWriter.Write) && m.IsGenericMethodDefinition && m.GetParameters().Length==1).MakeGenericMethod(typeof(SyncRngMessage));
    static bool Prefix(PacketWriter __instance, SyncRngMessage val)
    { FrostRngWire097.Write(__instance,val); return false; }
}
[HarmonyPatch]
public static class FrostRngPacketRead0913
{
    static MethodBase TargetMethod() => typeof(PacketReader).GetMethods().Single(m=>m.Name==nameof(PacketReader.Read) && m.IsGenericMethodDefinition && m.GetParameters().Length==0).MakeGenericMethod(typeof(SyncRngMessage));
    static bool Prefix(PacketReader __instance, ref SyncRngMessage __result)
    { __result=FrostRngWire097.Read(__instance); return false; }
}
