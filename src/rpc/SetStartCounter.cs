using Hazel;

namespace WellsAntiCheat.Rpc
{
    // 非主机客户端只能发送值为 -1 的 SetStartCounter。其他值都是试图
    // 强制/伪造大厅倒计时
    internal class SetStartCounter : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            reader.ReadPackedInt32(); // 序列 id
            sbyte counter = reader.ReadSByte();

            if (player.OwnerId != AmongUsClient.Instance.HostId && counter != -1)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationStartCounterSpoof(Anticheat.Name(player), counter));

                // 撤销伪造的倒计时值
                if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer != null)
                    PlayerControl.LocalPlayer.RpcSetStartCounter(-1);
            }
        }
    }
}
