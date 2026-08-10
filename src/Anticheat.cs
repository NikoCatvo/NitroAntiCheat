using HarmonyLib;
using Hazel;
using WellsAntiCheat.Rpc;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WellsAntiCheat
{
    // RPC-validation dispatcher. Detection always runs and notifies; DISCARDING an RPC and
    // PUNISHING a player only happen when you are the host. Your own RPCs are never checked.
    internal static class Anticheat
    {
        public static bool Enabled = true;

        public static bool ModdedLobby = false;
        public static bool IsModded() => ModdedLobby || Constants.IsVersionModded();

        // crash / flood protection
        public static bool CrashProtection = true;
        public static bool CheckMalformed  = true;
        public static bool CheckFlood      = true;
        public static int  FloodThreshold  = 50;
        public static float FloodWindow    = 1.0f;

        // unknown/unregistered RPC detection (off in modded lobbies, which use custom RPCs)
        public static bool DetectUnknownRpc = true;

        public static readonly Dictionary<RpcCalls, RpcCheck> RpcHandlers = new()
        {
            { RpcCalls.CompleteTask,     new CompleteTask()     { Name = "CompleteTask",     DisplayName = "完成任务" } },
            { RpcCalls.CheckName,        new CheckName()        { Name = "CheckName",        DisplayName = "检查名字" } },
            { RpcCalls.SetName,          new SetName()          { Name = "SetName",          DisplayName = "设置名字" } },
            { RpcCalls.SendChat,         new SendChat()         { Name = "SendChat",         DisplayName = "发送聊天" } },
            { RpcCalls.ReportDeadBody,   new ReportDeadBody()   { Name = "ReportDeadBody",   DisplayName = "报告尸体" } },
            { RpcCalls.SetStartCounter,  new SetStartCounter()  { Name = "SetStartCounter",  DisplayName = "设置开始倒计时" } },
            { RpcCalls.EnterVent,        new EnterVent()        { Name = "EnterVent",        DisplayName = "进入通风口" } },
            { RpcCalls.ExitVent,         new ExitVent()         { Name = "ExitVent",         DisplayName = "离开通风口" } },
            { RpcCalls.SnapTo,           new SnapTo()           { Name = "SnapTo",           DisplayName = "传送（SnapTo）" } },
            { RpcCalls.ClimbLadder,      new ClimbLadder()      { Name = "ClimbLadder",      DisplayName = "爬梯子" } },
            // role-exploit checks (auto-relaxed on modded lobbies)
            { RpcCalls.CheckMurder,      new CheckMurder()      { Name = "CheckMurder",      DisplayName = "检测击杀" } },
            { RpcCalls.MurderPlayer,     new MurderPlayer()     { Name = "MurderPlayer",     DisplayName = "击杀玩家" } },
            { RpcCalls.Shapeshift,       new Shapeshift()       { Name = "Shapeshift",       DisplayName = "变形" } },
            { RpcCalls.StartVanish,      new StartVanish()      { Name = "StartVanish",      DisplayName = "消失" } },
            { RpcCalls.ProtectPlayer,    new ProtectPlayer()    { Name = "ProtectPlayer",    DisplayName = "保护玩家" } },
            // sabotage / system checks (ShipStatus + VoteBanSystem net objects)
            { RpcCalls.UpdateSystem,     new UpdateSystem()     { Name = "UpdateSystem",     DisplayName = "更新系统（sabotge）" } },
            { RpcCalls.CloseDoorsOfType, new CloseDoorsOfType() { Name = "CloseDoorsOfType", DisplayName = "关闭门" } },
            // cosmetic / task exploit checks
            { RpcCalls.PlayAnimation,    new PlayAnimation()    { Name = "PlayAnimation",    DisplayName = "播放任务动画" } },
            { RpcCalls.Exiled,           new Exiled()           { Name = "Exiled",           DisplayName = "流放（Exiled）" } },
            { RpcCalls.SetColor,         new SetColor()         { Name = "SetColor",         DisplayName = "设置颜色" } },
            { RpcCalls.SetScanner,       new SetScanner()       { Name = "SetScanner",       DisplayName = "医学扫描" } },
            { RpcCalls.UsePlatform,      new UsePlatform()      { Name = "UsePlatform",      DisplayName = "使用传送台" } },
            { RpcCalls.SetLevel,         new SetLevel()         { Name = "SetLevel",         DisplayName = "设置等级" } },
        };

        private static readonly Dictionary<RpcCalls, int> MinBytes = new()
        {
            { RpcCalls.PlayAnimation, 1 }, { RpcCalls.CompleteTask, 1 }, { RpcCalls.SyncSettings, 1 },
            { RpcCalls.SetInfected, 1 }, { RpcCalls.CheckName, 1 }, { RpcCalls.SetName, 1 },
            { RpcCalls.CheckColor, 1 }, { RpcCalls.SetColor, 1 }, { RpcCalls.ReportDeadBody, 1 },
            { RpcCalls.MurderPlayer, 1 }, { RpcCalls.SendChat, 1 }, { RpcCalls.StartMeeting, 1 },
            { RpcCalls.SetScanner, 2 }, { RpcCalls.SendChatNote, 2 }, { RpcCalls.SetStartCounter, 1 },
            { RpcCalls.EnterVent, 1 }, { RpcCalls.ExitVent, 1 }, { RpcCalls.SnapTo, 8 },
            { RpcCalls.VotingComplete, 1 }, { RpcCalls.CastVote, 2 }, { RpcCalls.AddVote, 1 },
            { RpcCalls.CloseDoorsOfType, 1 }, { RpcCalls.SetTasks, 1 }, { RpcCalls.ClimbLadder, 2 },
        };

        private static HashSet<byte> _knownRpcIds;
        private static bool _knownRpcTried;
        private static HashSet<byte> KnownRpcIds()
        {
            if (_knownRpcTried) return _knownRpcIds;
            _knownRpcTried = true;
            try
            {
                var set = new HashSet<byte>();
                foreach (RpcCalls r in Enum.GetValues(typeof(RpcCalls))) set.Add((byte)r);
                if (set.Count > 0) _knownRpcIds = set;
            }
            catch { _knownRpcIds = null; }
            return _knownRpcIds;
        }

        public enum Punishments { None, Kick, Ban }

        public static Punishments Punishment = Punishments.Kick;
        public static bool SendNotification = true;
        public static bool DiscardRpc = true;

        private static readonly RateTracker _rpcRate = new();

        private static bool AmHost => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
        private static bool IsSelf(PlayerControl player)
            => player != null && (player.AmOwner || player == PlayerControl.LocalPlayer);

        public static bool IsExempt(PlayerControl player)
            => IsSelf(player) || TrustedPlayers.IsTrusted(player);

        // --- 配置文件路径 ---
        private static string ConfigPath => Path.Combine(BepInEx.Paths.ConfigPath, "com.well.nitroanticheat.cfg");

        // --- 保存所有设置到配置文件 ---
        // BepInEx 自动保存通过 Config.Bind 注册的设置。
        // 我们只负责保存 [Rpc] 段里的每个 RPC 检测开关状态。
        public static void SaveAllSettings()
        {
            try
            {
                SaveRpcStates();
            }
            catch (Exception e)
            {
                WellsPlugin.Log.LogWarning(Strings.LogConfigSaveFailed(e.Message));
            }
        }

        // --- 从配置文件加载所有设置 ---
        public static void LoadAllSettings()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;
                var lines = File.ReadAllLines(ConfigPath);
                bool inRpcSection = false;
                bool inGeneralSection = false;
                bool inCrashSection = false;
                bool inStateSection = false;
                bool inMeetingSection = false;
                bool inProtectionsSection = false;
                bool inMuteSection = false;
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("["))
                    {
                        inRpcSection = trimmed.Equals("[Rpc]", System.StringComparison.OrdinalIgnoreCase);
                        inGeneralSection = trimmed.Equals("[General]", System.StringComparison.OrdinalIgnoreCase);
                        inCrashSection = trimmed.Equals("[Crash]", System.StringComparison.OrdinalIgnoreCase) ||
                                         trimmed.Equals("[CrashFlood]", System.StringComparison.OrdinalIgnoreCase);
                        inStateSection = trimmed.Equals("[State]", System.StringComparison.OrdinalIgnoreCase);
                        inMeetingSection = trimmed.Equals("[Meeting]", System.StringComparison.OrdinalIgnoreCase);
                        inProtectionsSection = trimmed.Equals("[Protections]", System.StringComparison.OrdinalIgnoreCase);
                        inMuteSection = trimmed.Equals("[Mute]", System.StringComparison.OrdinalIgnoreCase) ||
                                        trimmed.Equals("[AutoMute]", System.StringComparison.OrdinalIgnoreCase);
                        continue;
                    }
                    if (trimmed.Length == 0 || trimmed.StartsWith("#")) continue;
                    var eqIdx = trimmed.IndexOf('=');
                    if (eqIdx <= 0) continue;
                    var key = trimmed.Substring(0, eqIdx).Trim();
                    var valStr = trimmed.Substring(eqIdx + 1).Trim();

                    if (inGeneralSection)
                    {
                        if (key == "Enabled" && bool.TryParse(valStr, out var b)) Enabled = b;
                        else if (key == "ModdedLobby" && bool.TryParse(valStr, out b)) ModdedLobby = b;
                        else if (key == "CrashProtection" && bool.TryParse(valStr, out b)) CrashProtection = b;
                        else if (key == "CheckMalformed" && bool.TryParse(valStr, out b)) CheckMalformed = b;
                        else if (key == "CheckFlood" && bool.TryParse(valStr, out b)) CheckFlood = b;
                        else if (key == "DetectUnknownRpc" && bool.TryParse(valStr, out b)) DetectUnknownRpc = b;
                        else if (key == "DiscardRpc" && bool.TryParse(valStr, out b)) DiscardRpc = b;
                        else if (key == "SendNotification" && bool.TryParse(valStr, out b)) SendNotification = b;
                        else if (key == "Punishment") { if (valStr == "True") Punishment = Punishments.Ban; else if (valStr == "Kick") Punishment = Punishments.Kick; else Punishment = Punishments.None; }
                    }
                    else if (inCrashSection)
                    {
                        if (key == "FloodThreshold" && int.TryParse(valStr, out var i)) FloodThreshold = i;
                        else if (key == "FloodWindow" && float.TryParse(valStr, out var f)) FloodWindow = f;
                    }
                    else if (inStateSection)
                    {
                        if (key == "CheckCosmetics" && bool.TryParse(valStr, out var b)) StateChecks.CheckCosmetics = b;
                        else if (key == "CheckLobbyRpcs" && bool.TryParse(valStr, out b)) StateChecks.CheckLobbyRpcs = b;
                    }
                    else if (inMeetingSection)
                    {
                        if (key == "GraceSeconds" && float.TryParse(valStr, out var f)) MeetingTimer.GraceSeconds = f;
                        else if (key == "EmergencyOnly" && float.TryParse(valStr, out var f2)) MeetingTimer.EmergencyOnly = f2 > 0f;
                    }
                    else if (inProtectionsSection)
                    {
                        if (key == "BlockVentKickExploit" && bool.TryParse(valStr, out var b)) Protections.BlockVentKickExploit = b;
                        else if (key == "BlockServerTeleports" && bool.TryParse(valStr, out b)) Protections.BlockServerTeleports = b;
                        else if (key == "BlockVotingOverload" && bool.TryParse(valStr, out b)) Protections.BlockVotingOverload = b;
                        else if (key == "BlockLargeMessages" && bool.TryParse(valStr, out b)) Protections.BlockLargeMessages = b;
                        else if (key == "MaxMessageLength" && int.TryParse(valStr, out var i)) Protections.MaxMessageLength = i;
                    }
                    else if (inMuteSection)
                    {
                        if (key == "MuteOnMajorityVote" && bool.TryParse(valStr, out var b)) MuteManager.MuteOnMajorityVote = b;
                        else if (key == "MuteOnChatConsensus" && bool.TryParse(valStr, out b)) MuteManager.MuteOnChatConsensus = b;
                    }
                    else if (inRpcSection)
                    {
                        if (Enum.TryParse<bool>(valStr, true, out bool val))
                        {
                            foreach (var kvp in RpcHandlers)
                                if (kvp.Value.Name == key) { kvp.Value.Enabled = val; break; }
                        }
                    }
                }
            }
            catch { }
        }

        // --- 保存/加载 RPC 检测状态到配置文件 ---
        // 注意：只读写 [Rpc] 段，不破坏 BepInEx 管理的其他段
        public static void SaveRpcStates()
        {
            try
            {
                var lines = File.Exists(ConfigPath) ? File.ReadAllLines(ConfigPath) : new string[0];
                var sb = new System.Text.StringBuilder();
                bool inRpcSection = false;
                var rpcStateMap = new Dictionary<string, bool>();
                foreach (var kvp in RpcHandlers) rpcStateMap[kvp.Value.Name] = kvp.Value.Enabled;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("["))
                    {
                        inRpcSection = trimmed.Equals("[Rpc]", System.StringComparison.OrdinalIgnoreCase);
                        sb.AppendLine(line);
                        continue;
                    }
                    if (inRpcSection)
                    {
                        if (trimmed.Length == 0 || trimmed.StartsWith("#"))
                        {
                            sb.AppendLine(line);
                            continue;
                        }
                        var eqIdx = trimmed.IndexOf('=');
                        if (eqIdx > 0)
                        {
                            var key = trimmed.Substring(0, eqIdx).Trim();
                            if (rpcStateMap.TryGetValue(key, out bool val))
                            {
                                sb.AppendLine($"{key}={val}");
                                rpcStateMap.Remove(key);
                                continue;
                            }
                        }
                        // 不属于我们管理的行，原样保留
                        sb.AppendLine(line);
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
                // 追加缺失的 RPC 条目到 [Rpc] 段
                if (rpcStateMap.Count > 0)
                {
                    bool hasRpcSection = false;
                    foreach (var line in lines)
                    {
                        if (line.Trim().Equals("[Rpc]", System.StringComparison.OrdinalIgnoreCase))
                        { hasRpcSection = true; break; }
                    }
                    if (!hasRpcSection)
                        sb.AppendLine("[Rpc]");
                    foreach (var kvp in rpcStateMap)
                        sb.AppendLine($"{kvp.Key}={kvp.Value}");
                }
                File.WriteAllText(ConfigPath, sb.ToString());
            }
            catch (Exception e)
            {
                WellsPlugin.Log.LogWarning(Strings.LogSaveRpcStatesFailed(e.Message));
            }
        }

        public static void LoadRpcStates()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;
                var lines = File.ReadAllLines(ConfigPath);
                bool inRpcSection = false;
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("["))
                    {
                        inRpcSection = trimmed.Equals("[Rpc]", System.StringComparison.OrdinalIgnoreCase);
                        continue;
                    }
                    if (inRpcSection && trimmed.Length > 0 && !trimmed.StartsWith("#"))
                    {
                        var eqIdx = trimmed.IndexOf('=');
                        if (eqIdx > 0)
                        {
                            var key = trimmed.Substring(0, eqIdx).Trim();
                            var valStr = trimmed.Substring(eqIdx + 1).Trim();
                            if (Enum.TryParse<bool>(valStr, true, out bool val))
                            {
                                foreach (var kvp in RpcHandlers)
                                    if (kvp.Value.Name == key) { kvp.Value.Enabled = val; break; }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // --- Harmony hooks ---

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
        private static class OnPlayerControlRpc
        {
            private static bool Prefix(PlayerControl __instance, byte callId, MessageReader reader)
                => HandleRpc(typeof(PlayerControl), __instance, callId, reader);
        }

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
        private static class OnPlayerPhysicsRpc
        {
            private static bool Prefix(PlayerPhysics __instance, byte callId, MessageReader reader)
                => HandleRpc(typeof(PlayerPhysics), __instance.myPlayer, callId, reader);
        }

        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.HandleRpc))]
        private static class OnNetTransformRpc
        {
            private static bool Prefix(CustomNetworkTransform __instance, byte callId, MessageReader reader)
                => HandleRpc(typeof(CustomNetworkTransform), __instance.myPlayer, callId, reader);
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.HandleRpc))]
        private static class OnShipStatusRpc
        {
            private static bool Prefix(byte callId, MessageReader reader)
                => HandleRpc(typeof(ShipStatus), null, callId, reader);
        }

        private static bool HandleRpc(Type sourceNetObj, PlayerControl player, byte callId, MessageReader reader)
        {
            if (Protections.ShouldBlock(sourceNetObj, player, callId, reader))
                return false;

            if (!Enabled) return true;
            if (IsExempt(player)) return true;

            RpcCalls rpc = (RpcCalls)callId;
            bool blockRpc = false;

            if (player != null && CheatClients.Check(player, callId, reader))
                return AmHost ? false : true;

            if (player != null)
            {
                if (CrashProtection)
                {
                    if (CheckMalformed && MinBytes.TryGetValue(rpc, out int min) && reader.Length < min)
                    {
                        Flag(player, Strings.ViolationMalformedRpc(Name(player), rpc.ToString()));
                        blockRpc = true;
                    }
                    if (CheckFlood && !blockRpc)
                    {
                        int count = _rpcRate.Record(player.OwnerId, Time.realtimeSinceStartup, FloodWindow);
                        if (count > FloodThreshold)
                        {
                            Flag(player, Strings.ViolationFlood(Name(player), count, FloodWindow));
                            blockRpc = true;
                        }
                    }
                }

                if (!blockRpc && DetectUnknownRpc && !IsModded())
                {
                    var known = KnownRpcIds();
                    if (known != null && !known.Contains(callId))
                    {
                        Flag(player, Strings.ViolationUnknownRpc(Name(player), callId));
                        blockRpc = true;
                    }
                }

                if (!blockRpc)
                    StateChecks.Check(player, callId, ref blockRpc);
            }

            if (!blockRpc && RpcHandlers.TryGetValue(rpc, out var check) && check != null && check.Enabled)
            {
                if (check.GetExpectedNetObject() != sourceNetObj)
                    return AmHost ? false : true;

                if (AmHost && check.IsHostOnly())
                {
                    Flag(player, Strings.ViolationHostOnlyRpc(Name(player), rpc.ToString()));
                    blockRpc = true;
                }
                else
                {
                    int savedPos = reader.Position;
                    try { check.Validate(player, reader, ref blockRpc); }
                    catch (Exception e)
                    {
                        WellsPlugin.Log.LogWarning(Strings.LogWarning($"Wells check for {rpc} threw: {e.Message}"));
                        blockRpc = false;
                    }
                    reader.Position = savedPos;
                }
            }

            if (AmHost && DiscardRpc && blockRpc) return false;
            return true;
        }

        public static void Flag(PlayerControl player, string reason, bool shouldPunish = true)
        {
            WellsPlugin.Log.LogMessage($"[Nitro] {reason}");
            if (SendNotification) Notifier.Show(reason);
            if (AmHost && shouldPunish && !IsExempt(player)) Punish(player);
        }

        public static void Flag(string reason)
        {
            WellsPlugin.Log.LogMessage($"[Nitro] {reason}");
            if (SendNotification) Notifier.Show(reason);
        }

        private static void Punish(PlayerControl player)
        {
            if (player == null) return;
            switch (Punishment)
            {
                case Punishments.None: break;
                case Punishments.Kick: AmongUsClient.Instance.KickPlayer(player.OwnerId, false); break;
                case Punishments.Ban:  AmongUsClient.Instance.KickPlayer(player.OwnerId, true);  break;
            }
        }

        public static string Name(PlayerControl p) => p?.Data?.PlayerName ?? "<unknown>";
    }
}
