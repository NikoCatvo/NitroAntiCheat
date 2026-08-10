using Hazel;

namespace WellsAntiCheat.Rpc
{
    // 虚假视觉任务动画：仅限 crewmate，游戏中，且视觉任务开启时
    internal class PlayAnimation : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            reader.ReadByte(); // 任务类型
            if (player?.Data == null) return;

            if (LobbyBehaviour.Instance != null)
            {
                Anticheat.Flag(player, Strings.ViolationTaskAnimLobby(Anticheat.Name(player)));
                blockRpc = true;
            }
            if (RoleManager.IsImpostorRole(player.Data.RoleType))
            {
                Anticheat.Flag(player, Strings.ViolationTaskAnimImpostor(Anticheat.Name(player)));
                blockRpc = true;
            }
            if (GameManager.Instance != null && !GameManager.Instance.LogicOptions.GetVisualTasks())
            {
                Anticheat.Flag(player, Strings.ViolationTaskAnimNoVisual(Anticheat.Name(player)));
                blockRpc = true;
            }
        }
    }

    // Exiled RPC 在游戏中永远不会合法发送
    internal class Exiled : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            Anticheat.Flag(player, Strings.ViolationExiled(Anticheat.Name(player)));
            blockRpc = true;
        }
    }

    // SetColor 数据验证（无效 netId / 无效颜色）。注意：不标记为仅主机
    // - SetColor 是主机到客户端的广播，路由到目标，所以仅主机检查
    // 这里会踢出合法玩家（SetRole bug）。这些数据验证是发送者无关的
    internal class SetColor : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            uint netId = reader.ReadUInt32();
            byte color = reader.ReadByte();
            if (player?.Data == null) return;

            if (netId != player.Data.NetId)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationSetColorNetId(Anticheat.Name(player), netId), false);
            }
            if (color >= Palette.ColorNames.Length)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationSetColorColor(Anticheat.Name(player), color), false);
            }
        }
    }

    // 虚假医学扫描：需要地图已生成、有扫描任务的 crewmate、视觉任务开启
    internal class SetScanner : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            bool scanning = reader.ReadBoolean();
            if (player?.Data == null || !scanning) return;

            // 当视觉任务关闭时，扫描不是可靠信号（扫描任务可能不是视觉任务）
            // 验证它会产生误报。完全跳过
            if (GameManager.Instance != null && !GameManager.Instance.LogicOptions.GetVisualTasks())
                return;

            if (ShipStatus.Instance == null)
            {
                Anticheat.Flag(player, Strings.ViolationScannerNoMap(Anticheat.Name(player)));
                blockRpc = true;
                return;
            }

            if (RoleManager.IsImpostorRole(player.Data.RoleType))
            {
                Anticheat.Flag(player, Strings.ViolationScannerImpostor(Anticheat.Name(player)));
                blockRpc = true;
                return;
            }

            // 只有在我们能看到玩家任务列表时才标记"无扫描任务"；如果它是
            // 空的/不可用，我们无法确认，所以不标记
            bool hasTasks = player.Data.Tasks != null && player.Data.Tasks.Count > 0;
            if (!hasTasks) return;

            bool hasScanTask = false;
            foreach (NetworkedPlayerInfo.TaskInfo task in player.Data.Tasks)
            {
                if (task.TypeId == (byte)TaskTypes.SubmitScan) { hasScanTask = true; break; }
            }
            if (!hasScanTask)
            {
                Anticheat.Flag(player, Strings.ViolationScannerNoTask(Anticheat.Name(player)));
                blockRpc = true;
            }
        }
    }

    // Airship 传送台：仅 Airship 合法，游戏中，且不在捉迷藏模式
    internal class UsePlatform : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (player?.Data == null) return;

            if (MapUtil.GetCurrentMap() != MapNames.Airship)
            {
                Anticheat.Flag(player, Strings.ViolationPlatformWrongMap(Anticheat.Name(player)));
                blockRpc = true;
                return;
            }
            if (ShipStatus.Instance == null)
            {
                Anticheat.Flag(player, Strings.ViolationPlatformNoMap(Anticheat.Name(player)));
                blockRpc = true;
                return;
            }
            if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek())
            {
                Anticheat.Flag(player, Strings.ViolationPlatformHideSeek(Anticheat.Name(player)));
                blockRpc = true;
            }
        }
    }

    // 等级欺骗：vanilla 在 10 万以上封禁；我们在 1 万以上标记。永不阻止（仅外观）
    internal class SetLevel : RpcCheck
    {
        private const uint MaxLevel = 10000;

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            uint level = reader.ReadPackedUInt32();
            if (player?.Data == null) return;

            if (level > MaxLevel)
            {
                Anticheat.Flag(player, Strings.ViolationLevelTooHigh(Anticheat.Name(player), level));
                blockRpc = true;
            }
            if (ShipStatus.Instance != null)
                Anticheat.Flag(player, Strings.ViolationLevelAfterStart(Anticheat.Name(player)), false);
        }
    }
}
