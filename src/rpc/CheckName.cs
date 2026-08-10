using Hazel;

namespace WellsAntiCheat.Rpc
{
    // 当客户端提议名字时触发（加入/重命名）。这是在名字提交前最早能检测到
    // 辱骂名字的地方。与 HyperMenu 不同，我们在所有修改版本中都运行
    internal class CheckName : RpcCheck
    {
        public const int MaxNameLength = 10;

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            string requested = reader.ReadString();

            if (NameFilter.IsOffensive(requested, out var term))
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationBlockedTerm(requested, term ?? ""));
                return;
            }

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
    }
}
