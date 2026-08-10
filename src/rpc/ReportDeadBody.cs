using Hazel;

namespace WellsAntiCheat.Rpc
{
    // 会议呼叫（紧急按钮和报告尸体）通过此 RPC 路由。我们在此强制执行
    // 倒计时规则。这能捕获直接触发会议 RPC 以跳过紧急按钮冷却/限制的修改客户端
    // （例如 HyperMenu 的"呼叫会议"按钮）
    internal class ReportDeadBody : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            // 第一个字节是被报告的玩家 id；0xFF（255）表示紧急按钮会议
            byte targetId = reader.ReadByte();
            bool isEmergency = targetId == byte.MaxValue;

            // 捉迷藏模式中会议从不合法
            if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek())
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationHideSeekMeeting(Anticheat.Name(player)));
                return;
            }

            // 倒计时规则
            bool restricted = MeetingTimer.EmergencyOnly ? isEmergency : true;
            if (restricted && MeetingTimer.InGracePeriod(out float remaining))
            {
                blockRpc = true;
                string kind = isEmergency ? "紧急会议" : "报告尸体";
                Anticheat.Flag(player,
                    Strings.ViolationEarlyMeeting(Anticheat.Name(player), kind, remaining, MeetingTimer.GraceSeconds));
            }
        }
    }
}
