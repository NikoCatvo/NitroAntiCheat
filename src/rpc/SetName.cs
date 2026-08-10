using Hazel;

namespace WellsAntiCheat.Rpc
{
    // 名字实际提交时触发。HyperMenu 在修改版大厅中完全跳过
    // （`if (Anticheat.IsModded()) return;`），这正是名字绕过工具所在的地方
    // 我们保持所有大厅类型的辱骂名字检查有效，仅对修改版大厅放宽长度/格式
    // 规则（在修改版大厅中自定义长/彩色名字是合法的）
    internal class SetName : RpcCheck
    {
        public const int MaxNameLength = 12; // +2 用于 vanilla 追加的区分数字

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            reader.ReadUInt32(); // netId - 我们的检查不需要
            string requested = reader.ReadString();

            // 辱骂名字检查始终运行，无论是否修改版
            if (NameFilter.IsOffensive(requested, out var term))
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationBlockedTerm(requested, term ?? ""));
                return;
            }

            // 在修改版大厅中，长/彩色名字是正常的；跳过外观检查
            if (Anticheat.IsModded()) return;

            if (requested.Length > MaxNameLength)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationNameTooLong(requested, requested.Length));
                return;
            }

            if (requested.Contains('<'))
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationNameInvalidChars(requested));
            }
        }

        // 在修改版服务器上，主机永远不应从客户端收到 SetName
        public override bool IsHostOnly() => Anticheat.IsModded();
    }
}
