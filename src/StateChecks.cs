using System.Collections.Generic;

namespace WellsAntiCheat
{
    // 拒绝与当前游戏状态不可能的 RPC
    //
    // 重要：这从每个对象的 HandleRpc 钩子运行，其中 `player` 是 RPC 所关于的
    // 网络对象，而不是发送者。所以我们只能安全地检查路由到
    //  Actor 自己的 PlayerControl 的 RPC（玩家自己的操作）。主机->客户端广播 RPC（SetRole,
    // SetTasks, SetInfected, Exiled, VotingComplete, CloseMeeting, ...）路由到目标
    // 不是发送者，所以我们不能在这里处理它们 - 之前这样做会在角色分配时踢出整个大厅
    internal static class StateChecks
    {
        public static bool Enabled = true;
        public static bool CheckCosmetics  = true;
        public static bool CheckLobbyRpcs  = true;

        // 外观设置 RPC，路由到更改外观玩家的 own 对象。合法仅在
        // 大厅/自定义界面，绝不可能在游戏过程中
        private static readonly HashSet<byte> Cosmetic = Ids(
            RpcCalls.SetColor, RpcCalls.SetHatStr, RpcCalls.SetSkinStr,
            RpcCalls.SetVisorStr, RpcCalls.SetPetStr, RpcCalls.SetNamePlateStr);

        // 玩家自创动作 RPC，路由到 Actor 自己的 PlayerControl。这些
        // 需要游戏进行（ShipStatus）才合法，所以在大厅可见时
        // 异常。主机授权/广播 RPC 被故意排除
        private static readonly HashSet<byte> LobbyIllegal = Ids(
            RpcCalls.MurderPlayer, RpcCalls.CheckMurder,
            RpcCalls.EnterVent, RpcCalls.ExitVent, RpcCalls.BootFromVent,
            RpcCalls.ClimbLadder, RpcCalls.UsePlatform, RpcCalls.UseZipline, RpcCalls.CheckZipline,
            RpcCalls.CompleteTask,
            RpcCalls.Shapeshift, RpcCalls.CheckShapeshift, RpcCalls.RejectShapeshift,
            RpcCalls.ProtectPlayer, RpcCalls.CheckProtect,
            RpcCalls.StartVanish, RpcCalls.CheckVanish, RpcCalls.StartAppear, RpcCalls.CheckAppear,
            RpcCalls.TriggerSpores, RpcCalls.CheckSpore);

        public static void Check(PlayerControl player, byte callId, ref bool blockRpc)
        {
            if (!Enabled || player == null || blockRpc) return;

            bool inLobby = LobbyBehaviour.Instance != null;
            bool inGameplay = ShipStatus.Instance != null && LobbyBehaviour.Instance == null;

            if (CheckCosmetics && inGameplay && Cosmetic.Contains(callId))
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationCosmetics(Anticheat.Name(player), ((RpcCalls)callId).ToString()));
                return;
            }

            if (CheckLobbyRpcs && inLobby && LobbyIllegal.Contains(callId))
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationLobbyRpc(Anticheat.Name(player), callId));
            }
        }

        private static HashSet<byte> Ids(params RpcCalls[] calls)
        {
            var set = new HashSet<byte>();
            foreach (var c in calls) set.Add((byte)c);
            return set;
        }
    }
}
