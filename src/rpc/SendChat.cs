using Hazel;
using UnityEngine;

namespace WellsAntiCheat.Rpc
{
    // 检测聊天刷屏（短时间内过多消息）和可能导致 lag 或崩溃的超大消息
    internal class SendChat : RpcCheck
    {
        public static int SpamThreshold = 5;      // SpamWindow 内消息数 => 刷屏
        public static float SpamWindow = 3.0f;    // 秒
        public static int MaxMessageLength = 300; // 超过此长度视为崩溃尝试

        private static readonly RateTracker _chatRate = new();

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (player == null) return;

            string message = reader.ReadString();

            // 被禁言玩家（本次会议）：静默丢弃他们的聊天，使其永不传播
            if (MuteManager.IsMuted(player))
            {
                blockRpc = true;
                return;
            }

            // 提供聊天共识禁言触发（除非启用+主机，否则不做任何事）
            MuteManager.RecordChatColorVote(player, message);

            if (message != null && message.Length > MaxMessageLength)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationChatOversized(Anticheat.Name(player), message.Length));
                return;
            }

            int count = _chatRate.Record(player.OwnerId, Time.realtimeSinceStartup, SpamWindow);
            if (count > SpamThreshold)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationChatSpam(Anticheat.Name(player), count, SpamWindow));
            }
        }
    }
}
