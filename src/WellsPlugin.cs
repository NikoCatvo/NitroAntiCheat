using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;
using UnityEngine;

namespace WellsAntiCheat
{
    [BepInPlugin(Guid, "Nitro Anti Cheat - Well", "1.1.0")]
    [BepInProcess("Among Us.exe")]
    public class WellsPlugin : BasePlugin
    {
        public const string Guid = "com.well.nitroanticheat";

        public new static ManualLogSource Log;
        private static Harmony _harmony;
        private static ConfigFile _config;

        // ========== 存储所有 ConfigEntry 引用 ==========
        private static ConfigEntry<bool>  _antiCheat, _cheatClients, _nameFilter, _meetingTimer,
                                          _moddedLobby, _discardRpc, _rainbow, _bannedList,
                                          _sendNotify, _stateMaster, _stCosmetics, _stLobby,
                                          _crash, _malformed, _flood, _unknownRpc,
                                          _pVent, _pTp, _pVote, _pLarge, _voteKick,
                                          _muteMajority, _muteConsensus, _emergencyOnly;
        private static ConfigEntry<int>   _floodCfg, _pMaxLen, _selectedMap;
        private static ConfigEntry<float> _floodWindow, _graceCfg, _displaySec;
        private static ConfigEntry<string> _punishment, _keyCfg;

        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("Nitro Anti Cheat - Well loading...");
            _config = Config;

            // ===== 通用 =====
            _antiCheat     = Config.Bind("General", "AntiCheatEnabled", true, "反作弊总开关");
            _cheatClients  = Config.Bind("General", "DetectCheatClients", true, "踢出已知作弊客户端（SickoMenu/AUM/KillNetwork）");
            _nameFilter    = Config.Bind("General", "NameFilterEnabled", true, "踢出辱骂/被封禁的名字");
            _meetingTimer  = Config.Bind("General", "MeetingTimerEnabled", true, "惩罚在Grace时间窗口内发起的会议");
            _moddedLobby   = Config.Bind("General", "ModdedLobby", false, "放宽修改版大厅的角色/游戏逻辑检测");
            _discardRpc    = Config.Bind("General", "DiscardRpc", true, "丢弃违规RPC，使其效果不生效");
            _punishment    = Config.Bind("General", "Punishment", "Kick", "对违规玩家的惩罚：无/踢出/封禁");
            _keyCfg        = Config.Bind("General", "ToggleKey", "F8", "打开/关闭面板的按键");
            _rainbow       = Config.Bind("General", "RainbowGui", true, "GUI颜色循环彩虹效果");
            _bannedList    = Config.Bind("General", "BannedListEnabled", true, "自动踢出封禁名单中的玩家");
            _sendNotify    = Config.Bind("General", "SendNotification", true, "检测到违规时发送屏幕通知");

            // ===== 崩溃/洪水 =====
            _crash         = Config.Bind("Crash", "CrashProtection", true, "崩溃/洪水防护总开关");
            _malformed     = Config.Bind("Crash", "CheckMalformed", true, "检测畸形（过短）RPC数据");
            _flood         = Config.Bind("Crash", "CheckFlood", true, "检测RPC洪水攻击");
            _unknownRpc    = Config.Bind("Crash", "DetectUnknownRpc", true, "检测未注册/未知RPC（修改版大厅自动关闭）");
            _floodCfg      = Config.Bind("Crash", "FloodThreshold", 50, "在窗口期内触发洪水检测的RPC数量");
            _floodWindow   = Config.Bind("Crash", "FloodWindowSeconds", 1.0f, "洪水检测的滑动窗口时间（秒）");

            // ===== 状态检测 =====
            _stateMaster   = Config.Bind("State", "StateChecks", true, "基于状态的RPC拒绝总开关");
            _stCosmetics   = Config.Bind("State", "CheckCosmetics", true, "拒绝游戏中更改外观");
            _stLobby       = Config.Bind("State", "CheckLobbyRpcs", true, "在大厅中拒绝游戏内RPC");

            // ===== 会议 =====
            _graceCfg      = Config.Bind("Meeting", "GraceSeconds", 10f, "每轮开始后可允许开会的秒数");
            _emergencyOnly = Config.Bind("Meeting", "EmergencyOnly", false, "仅限制紧急按钮开会");
            _muteMajority  = Config.Bind("Mute", "MuteOnMajorityVote", false, "玩家获得多数票时禁言");
            _muteConsensus = Config.Bind("Mute", "MuteOnChatConsensus", false, "多数玩家通过聊天颜色投票禁言");

            // ===== 防护 =====
            _pVent   = Config.Bind("Protections", "BlockVentKickExploit", true, "阻止通风口踢出/封禁漏洞");
            _pTp     = Config.Bind("Protections", "BlockServerTeleports", true, "阻止强制传送（大量通风）");
            _pVote   = Config.Bind("Protections", "BlockVotingOverload", true, "阻止投票完成时内存溢出崩溃");
            _pLarge  = Config.Bind("Protections", "BlockLargeMessages", true, "丢弃超大游戏数据消息");
            _pMaxLen = Config.Bind("Protections", "MaxMessageLength", 1400, "超过此长度的游戏数据消息将被丢弃");

            // ===== GUI =====
            _displaySec = Config.Bind("Gui", "DisplaySeconds", 10f, "通知消息在屏幕上停留的秒数");

            // ===== 主机工具 =====
            _selectedMap = Config.Bind("HostTools", "SelectedMap", 0, "默认生成的地图（0=Skeld,1=Mira,2=Polus,3=Dleks,4=Airship,5=Fungle）");

            // ===== 投票踢出 =====
            _voteKick = Config.Bind("VoteKick", "Enabled", true, "阻止未知客户端、已死玩家或会议外投票");

            // ===== 应用加载的值 =====
            ApplyLoadedValues();

            // 加载 [Rpc] 段里的每个 RPC 检测开关
            Anticheat.LoadRpcStates();

            NameFilter.Load();
            BannedPlayers.Load();
            _harmony = new Harmony(Guid);
            _harmony.PatchAll();
            AddComponent<WellsGui>();

            Log.LogInfo("Nitro Anti Cheat - Well 已加载。在游戏中按 F8 打开面板。");
        }

        private static void ApplyLoadedValues()
        {
            Anticheat.Enabled          = _antiCheat.Value;
            Anticheat.CrashProtection  = _crash.Value;
            Anticheat.CheckMalformed   = _malformed.Value;
            Anticheat.CheckFlood       = _flood.Value;
            Anticheat.DetectUnknownRpc = _unknownRpc.Value;
            Anticheat.ModdedLobby      = _moddedLobby.Value;
            Anticheat.DiscardRpc       = _discardRpc.Value;
            Anticheat.SendNotification = _sendNotify.Value;
            Anticheat.Punishment       = ParsePunishment(_punishment.Value);
            Anticheat.FloodThreshold   = _floodCfg.Value;
            Anticheat.FloodWindow      = _floodWindow.Value;
            CheatClients.Enabled       = _cheatClients.Value;
            StateChecks.Enabled        = _stateMaster.Value;
            StateChecks.CheckCosmetics = _stCosmetics.Value;
            StateChecks.CheckLobbyRpcs = _stLobby.Value;
            NameFilter.Enabled         = _nameFilter.Value;
            MeetingTimer.Enabled       = _meetingTimer.Value;
            MeetingTimer.GraceSeconds  = _graceCfg.Value;
            MeetingTimer.EmergencyOnly = _emergencyOnly.Value;
            MuteManager.MuteOnMajorityVote = _muteMajority.Value;
            MuteManager.MuteOnChatConsensus = _muteConsensus.Value;
            Protections.BlockVentKickExploit = _pVent.Value;
            Protections.BlockServerTeleports = _pTp.Value;
            Protections.BlockVotingOverload = _pVote.Value;
            Protections.BlockLargeMessages = _pLarge.Value;
            Protections.MaxMessageLength   = _pMaxLen.Value;
            WellsGui.RainbowGui            = _rainbow.Value;
            BannedPlayers.Enabled          = _bannedList.Value;
            Notifier.DisplaySeconds        = _displaySec.Value;
            HostTools.SelectedMap          = (byte)_selectedMap.Value;
            Rpc.VoteKickGuard.Enabled      = _voteKick.Value;

            if (System.Enum.TryParse<KeyCode>(_keyCfg.Value, true, out var key))
                WellsGui.ToggleKey = key;
        }

        // 将当前 GUI 中修改过的静态字段值同步回 ConfigEntry
        public static void SyncToConfig()
        {
            _antiCheat.Value     = Anticheat.Enabled;
            _cheatClients.Value  = CheatClients.Enabled;
            _nameFilter.Value    = NameFilter.Enabled;
            _meetingTimer.Value  = MeetingTimer.Enabled;
            _moddedLobby.Value   = Anticheat.ModdedLobby;
            _discardRpc.Value    = Anticheat.DiscardRpc;
            _rainbow.Value       = WellsGui.RainbowGui;
            _bannedList.Value    = BannedPlayers.Enabled;
            _sendNotify.Value    = Anticheat.SendNotification;

            _punishment.Value = Anticheat.Punishment switch
            {
                Anticheat.Punishments.None => "None",
                Anticheat.Punishments.Ban  => "Ban",
                _                          => "Kick",
            };

            _stateMaster.Value   = StateChecks.Enabled;
            _stCosmetics.Value   = StateChecks.CheckCosmetics;
            _stLobby.Value       = StateChecks.CheckLobbyRpcs;

            _crash.Value         = Anticheat.CrashProtection;
            _malformed.Value     = Anticheat.CheckMalformed;
            _flood.Value         = Anticheat.CheckFlood;
            _unknownRpc.Value    = Anticheat.DetectUnknownRpc;
            _floodCfg.Value      = Anticheat.FloodThreshold;
            _floodWindow.Value   = Anticheat.FloodWindow;

            _muteMajority.Value  = MuteManager.MuteOnMajorityVote;
            _muteConsensus.Value = MuteManager.MuteOnChatConsensus;
            _emergencyOnly.Value = MeetingTimer.EmergencyOnly;

            _pVent.Value    = Protections.BlockVentKickExploit;
            _pTp.Value      = Protections.BlockServerTeleports;
            _pVote.Value    = Protections.BlockVotingOverload;
            _pLarge.Value   = Protections.BlockLargeMessages;
            _pMaxLen.Value  = Protections.MaxMessageLength;

            _voteKick.Value = Rpc.VoteKickGuard.Enabled;
        }

        // 保存全部配置：同步GUI -> BepInEx -> 保存 + 保存 [Rpc] 段
        public static void SaveAllConfig()
        {
            try
            {
                SyncToConfig();
                _config?.Save();
                Anticheat.SaveRpcStates();
                BannedPlayers.Save();
                Log.LogInfo(Strings.LogConfigSaved);
            }
            catch (Exception e)
            {
                Log.LogWarning(Strings.LogConfigSaveFailed(e.Message));
            }
        }

        // 供 WellsGui 调用，保存 BepInEx 配置
        public static void SaveConfig() => _config?.Save();

        private static Anticheat.Punishments ParsePunishment(string s)
            => System.Enum.TryParse<Anticheat.Punishments>(s, true, out var p) ? p : Anticheat.Punishments.Kick;
    }
}
