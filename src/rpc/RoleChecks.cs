using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using UnityEngine;

namespace WellsAntiCheat.Rpc
{
    internal static class RoleUtil
    {
        public static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead;
        public static bool Impostor(PlayerControl p) => p?.Data != null && RoleManager.IsImpostorRole(p.Data.RoleType);
        public static bool IsRole(PlayerControl p, RoleTypes r) => p?.Data != null && p.Data.RoleType == r;
        public static bool InVent(PlayerControl p) => p != null && (p.inVent || p.walkingToVent);
        public static bool InRange(PlayerControl a, PlayerControl b, float range)
            => a != null && b != null && Vector2.Distance(a.GetTruePosition(), b.GetTruePosition()) <= range;
    }

    // 击杀验证：击杀者必须是活体内鬼，不在通风口内，且在击杀范围内对活体非内鬼目标
    internal class CheckMurder : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (Anticheat.IsModded()) return; // 自定义角色会改变击杀规则
            PlayerControl target = reader.ReadNetObject<PlayerControl>();
            if (target == null) return;

            bool killerOk = RoleUtil.Alive(player) && RoleUtil.Impostor(player) && !RoleUtil.InVent(player)
                            && RoleUtil.InRange(player, target, 3f);
            bool targetOk = RoleUtil.Alive(target) && !RoleUtil.Impostor(target);

            if (!killerOk || !targetOk)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationInvalidKill(Anticheat.Name(player), Anticheat.Name(target)));
            }
        }
    }

    internal class MurderPlayer : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (Anticheat.IsModded()) return;
            PlayerControl target = reader.ReadNetObject<PlayerControl>();
            if (target == null) return;

            bool killerOk = RoleUtil.Alive(player) && RoleUtil.Impostor(player) && !RoleUtil.InVent(player);
            if (!killerOk)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationIllegalMurder(Anticheat.Name(player)));
            }
        }
    }

    internal class Shapeshift : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (Anticheat.IsModded()) return;
            if (!RoleUtil.IsRole(player, RoleTypes.Shapeshifter) || !RoleUtil.Alive(player))
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationShapeshift(Anticheat.Name(player)));
            }
        }
    }

    internal class StartVanish : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (Anticheat.IsModded()) return;
            if (!RoleUtil.IsRole(player, RoleTypes.Phantom) || !RoleUtil.Alive(player))
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationVanish(Anticheat.Name(player)));
            }
        }
    }

    internal class ProtectPlayer : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (Anticheat.IsModded()) return;
            if (!RoleUtil.IsRole(player, RoleTypes.GuardianAngel))
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationProtect(Anticheat.Name(player)));
            }
        }
    }
}
