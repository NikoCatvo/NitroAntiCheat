using Hazel;
using System.Collections.Generic;

namespace WellsAntiCheat
{
    // 通过他们广播的自定义 RPC ID 检测已知作弊客户端。这些 ID 位于
    // vanilla RpcCalls 范围之外，所以正常客户端永远不会发送它们。指纹和
    // 检测细节（Sicko 的空数据、AUM 的自识别字节）来自
    // BetterAmongUs 的作弊客户端处理器
    internal static class CheatClients
    {
        public static bool Enabled = true;

        // 作弊客户端自定义 RPC ID，简化为字节值（它们在网络上到达的方式）
        private const byte Sicko           = 164; // CustomRPC 420 -> byte 164，空数据
        private const byte AUM             = 85;  // CustomRPC 42069 -> byte 85，首字节 == 发送者 PlayerId
        private const byte AUMChat         = 101;
        private const byte KillNetwork     = 250;
        private const byte KillNetworkChat = 119;

        // 已识别的玩家，这样我们不会对每个数据包都刷屏通知
        private static readonly HashSet<int> _known = new();

        // 如果此 RPC 标识作弊客户端则返回 true（调用者应该阻止+玩家在此处
        // 被标记/惩罚）
        public static bool Check(PlayerControl player, byte callId, MessageReader reader)
        {
            if (!Enabled || player == null) return false;

            string client = null;

            switch (callId)
            {
                case Sicko:
                    if (reader.BytesRemaining == 0) client = "SickoMenu";
                    break;

                case AUM:
                    // AUM 的指纹：数据的第一个字节是发送者自己的 PlayerId
                    if (reader.BytesRemaining >= 1)
                    {
                        int savedPos = reader.Position;
                        byte id = reader.ReadByte();
                        reader.Position = savedPos;
                        if (id == player.PlayerId) client = "AmongUsMenu (AUM)";
                    }
                    break;

                case AUMChat:        client = "AmongUsMenu (AUM) 聊天"; break;
                case KillNetwork:    client = "KillNetwork"; break;
                case KillNetworkChat: client = "KillNetwork 聊天"; break;
            }

            if (client == null) return false;

            // 每个玩家只通知/惩罚一次，但始终报告匹配，以便调用者阻止
            if (_known.Add(player.OwnerId))
                Anticheat.Flag(player, Strings.ViolationCheatClient(Anticheat.Name(player), client));

            return true;
        }

        public static void ResetKnown() => _known.Clear();
    }
}
