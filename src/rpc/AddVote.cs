using HarmonyLib;

namespace WellsAntiCheat.Rpc
{
    // 投票踢出验证。直接修补 VoteBanSystem.AddVote（每个参考客户端修补的方法
    // - VoteBanSystem.HandleRpc 不可靠修补）。主机端运行
    // 并阻止来自未知客户端、已死玩家或会议外投票
    [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
    internal static class VoteKickGuard
    {
        public static bool Enabled = true;

        private static bool Prefix(int srcClient, int clientId)
        {
            if (!Anticheat.Enabled || !Enabled) return true;
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return true;

            var client = AmongUsClient.Instance.FindClientById(srcClient);
            if (client == null || client.Character == null)
            {
                Anticheat.Flag(Strings.ViolationUnknownClientVote(srcClient.ToString()));
                return false; // 阻止
            }

            var voter = client.Character;
            if (Anticheat.IsExempt(voter)) return true; // 本地+受信任豁免

            if (voter.Data != null && voter.Data.IsDead)
            {
                Anticheat.Flag(voter, Strings.ViolationDeadVote(Anticheat.Name(voter)));
                return false;
            }

            if (MeetingHud.Instance == null)
            {
                Anticheat.Flag(voter, Strings.ViolationOutsideMeetingVote(Anticheat.Name(voter)));
                return false;
            }

            return true;
        }
    }
}
