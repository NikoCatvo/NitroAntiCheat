using Hazel;
using System;
using UnityEngine;

namespace WellsAntiCheat.Rpc
{
    // 任务完成：不可能上下文和内鬼完成任务检测
    internal class CompleteTask : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            uint taskIndex = reader.ReadPackedUInt32();

            if (ShipStatus.Instance == null)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationTaskNoShip(Anticheat.Name(player), taskIndex));
                return;
            }

            // 修改版大厅的角色语义不同；跳过角色检测以避免误报
            if (!Anticheat.IsModded())
            {
                if (RoleManager.IsImpostorRole(player.Data.RoleType))
                {
                    blockRpc = true;
                    Anticheat.Flag(player, Strings.ViolationTaskAsImpostor(Anticheat.Name(player), taskIndex));
                    return;
                }

                if (taskIndex + 1 > (uint)player.Data.Tasks.Count)
                {
                    blockRpc = true;
                    Anticheat.Flag(player, Strings.ViolationTaskCount(Anticheat.Name(player), taskIndex, player.Data.Tasks.Count));
                }
            }
        }
    }

    internal class EnterVent : RpcCheck
    {
        public override Type GetExpectedNetObject() => typeof(PlayerPhysics);

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (ShipStatus.Instance == null)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationVentNoShip(Anticheat.Name(player)), false);
                return;
            }

            if (Anticheat.IsModded()) return; // 自定义角色可能可以使用通风口

            if (!player.Data.IsDead && !player.Data.Role.CanVent)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationVentNoRole(Anticheat.Name(player), player.Data.RoleType.ToString()), false);
            }
        }
    }

    internal class ExitVent : RpcCheck
    {
        public override Type GetExpectedNetObject() => typeof(PlayerPhysics);

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (ShipStatus.Instance == null)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationExitVentNoShip(Anticheat.Name(player)), false);
                return;
            }

            if (Anticheat.IsModded()) return;

            if (!player.Data.IsDead && !player.Data.Role.CanVent)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationExitVentNoRole(Anticheat.Name(player), player.Data.RoleType.ToString()), false);
            }
        }
    }

    internal class ClimbLadder : RpcCheck
    {
        public override Type GetExpectedNetObject() => typeof(PlayerPhysics);

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (ShipStatus.Instance == null)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationClimbLadderNoShip(Anticheat.Name(player)), false);
                return;
            }

            if (!player.Data.IsDead) return; // 活人爬梯子正常

            blockRpc = true;
            Anticheat.Flag(player, Strings.ViolationClimbLadderDead(Anticheat.Name(player)), false);
        }
    }

    // SnapTo（传送）仅在游戏进行中合法，在大厅中不合法
    internal class SnapTo : RpcCheck
    {
        public override Type GetExpectedNetObject() => typeof(CustomNetworkTransform);

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            NetHelpers.ReadVector2(reader); // 位置 - 读取以推进，值未使用

            if (LobbyBehaviour.Instance != null)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationSnapToLobby(Anticheat.Name(player)), false);

                // 将其传送回原位，防止非法移动在其他客户端中保持
                if (AmongUsClient.Instance.AmHost && player?.NetTransform != null)
                    player.NetTransform.RpcSnapTo(player.transform.position);
            }
        }
    }
}
