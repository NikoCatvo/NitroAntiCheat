using HarmonyLib;
using Hazel;
using InnerNet;

namespace WellsAntiCheat
{
    // 针对已知大规模恶意漏洞的自我保护。与主反作弊（仅在主机上丢弃 RPC）不同
    // ，这些在任何客户端上都阻止危险传入 RPC，因此它们
    // 保护你不会被踢出/封禁/崩溃，即使你不是主机
    internal static class Protections
    {
        public static bool BlockVentKickExploit = true; // "所有人被封禁"（通风口漏洞）
        public static bool BlockServerTeleports = true; // 大量传送到通风口（SnapTo 针对我们）
        public static bool BlockVotingOverload  = true; // VotingComplete 内存溢出崩溃
        public static bool BlockLargeMessages   = true; // 超大游戏数据消息攻击
        public static int  MaxMessageLength     = 1400;

        private static bool AmHost => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;

        // 在 RPC 分发器的最顶部调用（在反作弊开关/豁免之前），
        // 用于分发器已经挂钩的网络对象。如果必须阻止 RPC，返回 true。
        public static bool ShouldBlock(System.Type netObj, PlayerControl player, byte callId, MessageReader reader)
        {
            var rpc = (RpcCalls)callId;

            // 大量传送到通风口：在 vanilla 服务器上你永远不会合法接收针对
            // 你自己玩家的 SnapTo（你的移动是客户端权威的），所以阻止它
            if (BlockServerTeleports && netObj == typeof(CustomNetworkTransform)
                && rpc == RpcCalls.SnapTo && player != null && player == PlayerControl.LocalPlayer)
            {
                Notifier.Show(Strings.NotifyBlockedTeleport);
                return true;
            }

            // 通风口踢出/封禁漏洞：黑客向非主机客户端发送 UpdateSystem(Ventilation)
            // 以触发 InnerSloth 的反作弊封禁他们。非主机时阻止它
            if (BlockVentKickExploit && netObj == typeof(ShipStatus)
                && rpc == RpcCalls.UpdateSystem && !AmHost)
            {
                int pos = reader.Position;
                try
                {
                    var system = (SystemTypes)reader.ReadByte();
                    reader.Position = pos;
                    if (system == SystemTypes.Ventilation)
                    {
                        Notifier.Show(Strings.NotifyBlockedVentKick);
                        return true;
                    }
                }
                catch { reader.Position = pos; }
            }

            return false;
        }

        // VotingComplete 带巨大数组长度会强制客户端分配数GB -> 崩溃
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.HandleRpc))]
        private static class VotingOverloadGuard
        {
            private static bool Prefix(byte callId, MessageReader reader)
            {
                if (!BlockVotingOverload || callId != (byte)RpcCalls.VotingComplete) return true;

                int pos = reader.Position;
                try
                {
                    int arrayLength = reader.ReadPackedInt32();
                    if (arrayLength > 1024 || arrayLength > reader.BytesRemaining)
                    {
                        Notifier.Show(Strings.NotifyBlockedVotingOverload);
                        return false;
                    }
                    reader.Position = pos;
                }
                catch { reader.Position = pos; }
                return true;
            }
        }

        // 丢弃超大游戏数据消息（大消息崩溃/大规模封禁向量）
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleGameData))]
        private static class LargeMessageGuard
        {
            private static bool Prefix(MessageReader parentReader)
            {
                if (!BlockLargeMessages) return true;
                if (parentReader != null && parentReader.Length > MaxMessageLength)
                {
                    parentReader.Recycle();
                    return false;
                }
                return true;
            }
        }
    }
}
