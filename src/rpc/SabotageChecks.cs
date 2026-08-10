using Hazel;
using InnerNet;
using System;

namespace WellsAntiCheat.Rpc
{
    // 验证 sabotge / 系统更新 RPC。这是最重要的一个：阻止反应堆强制修复
    // 和强制呼叫崩溃、蘑菇混合混乱欺骗、开关面板崩溃数据、非内鬼 sabotge、以及捉迷藏模式下的 sabotge
    internal class UpdateSystem : RpcCheck
    {
        private static readonly SystemTypes[] UpdatableWhenDead =
        {
            SystemTypes.MedBay, SystemTypes.Sabotage, SystemTypes.Security, SystemTypes.Ventilation
        };

        public override Type GetExpectedNetObject() => typeof(ShipStatus);

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            SystemTypes system = (SystemTypes)reader.ReadByte();
            player = reader.ReadNetObject<PlayerControl>();
            if (player == null) return;
            if (Anticheat.IsExempt(player)) return; // 本地+受信任玩家豁免

            if (ShipStatus.Instance == null || !ShipStatus.Instance.Systems.ContainsKey(system))
            {
                Anticheat.Flag(player, Strings.ViolationSystemNotFound(Anticheat.Name(player), system.ToString()));
                blockRpc = true;
                return;
            }

            if (player.Data.IsDead && Array.IndexOf(UpdatableWhenDead, system) < 0)
            {
                Anticheat.Flag(player, Strings.ViolationSystemDead(Anticheat.Name(player), system.ToString()));
                blockRpc = true;
                return;
            }

            switch (system)
            {
                case SystemTypes.Electrical:  ValidateSwitches(player, reader, ref blockRpc); break;
                case SystemTypes.MushroomMixupSabotage: ValidateMushroom(player, ref blockRpc); break;
                case SystemTypes.Reactor:
                case SystemTypes.Laboratory:
                case SystemTypes.HeliSabotage: ValidateReactor(player, reader, ref blockRpc); break;
                case SystemTypes.Sabotage:     ValidateSabotage(player, reader, ref blockRpc); break;
            }
        }

        // 蘑菇混合混乱在游戏逻辑中是仅主机的；玩家发送它总是无效的
        private static void ValidateMushroom(PlayerControl player, ref bool blockRpc)
        {
            Anticheat.Flag(player, Strings.ViolationMushroom(Anticheat.Name(player)));
            blockRpc = true;
        }

        private static void ValidateReactor(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            byte op = reader.ReadByte();
            if (op == 16)
            {
                Anticheat.Flag(player, Strings.ViolationReactorForceFix(Anticheat.Name(player)));
                blockRpc = true;
            }
            else if (op == 128)
            {
                Anticheat.Flag(player, Strings.ViolationReactorForceCall(Anticheat.Name(player)));
                blockRpc = true;
            }
        }

        private static void ValidateSabotage(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            SystemTypes target = (SystemTypes)reader.ReadByte();

            if (!MapUtil.ValidSabotages.Contains(target))
            {
                Anticheat.Flag(player, Strings.ViolationInvalidSabotageTarget(Anticheat.Name(player), target.ToString()));
                blockRpc = true;
            }
            if (player.Data != null && !RoleManager.IsImpostorRole(player.Data.RoleType))
            {
                Anticheat.Flag(player, Strings.ViolationSabotageNotImpostor(Anticheat.Name(player), target.ToString()));
                blockRpc = true;
            }
            if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek())
            {
                Anticheat.Flag(player, Strings.ViolationSabotageHideSeek(Anticheat.Name(player), target.ToString()));
                blockRpc = true;
            }
        }

        private static void ValidateSwitches(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            byte switches = reader.ReadByte();

            if ((switches & 128) != 0)
            {
                Anticheat.Flag(player, Strings.ViolationSwitchCrash(Anticheat.Name(player), switches));
                blockRpc = true;
            }
            else if (switches > 5)
            {
                Anticheat.Flag(player, Strings.ViolationInvalidSwitch(Anticheat.Name(player), switches));
                blockRpc = true;
            }

            // 当灯光未被 sabotge 或会议期间静默阻止开关更新 -
            // 这些可能在竞态条件下误报，所以我们阻止但不惩罚
            try
            {
                var sys = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                if (sys != null && sys.ExpectedSwitches == sys.ActualSwitches)
                    blockRpc = true;
            }
            catch { }

            if (MeetingHud.Instance != null)
                blockRpc = true;
        }
    }

    // 捉迷藏模式下不能关闭门。发送者未知（通过 ShipStatus 路由）
    internal class CloseDoorsOfType : RpcCheck
    {
        public override Type GetExpectedNetObject() => typeof(ShipStatus);

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek())
            {
                Anticheat.Flag(Strings.ViolationHideSeekMeeting("Someone"));
                blockRpc = true;
            }
        }
    }
}
