using System;
using System.Collections.Generic;

namespace WellsAntiCheat
{
    // 中文本地化 —— 所有面板 UI 文字、反作弊提示、日志消息均通过此类返回中文。
    internal static class Strings
    {
        // ========== 窗口标题 ==========
        public static string WindowTitle => "Nitro 反作弊 - Well";

        // ========== 状态行 ==========
        public static string StatusHost => "状态：主机 - 完全激活";
        public static string StatusNotHost => "状态：非主机 - 仅警报，控制已锁定";

        // ========== 面板分区标题 ==========
        public static string SectionLobby => "游戏大厅";
        public static string SectionDetection => "检测（总开关）";
        public static string SectionCrash => "崩溃 / 洪水防护";
        public static string SectionStateChecks => "状态检测";
        public static string SectionChat => "聊天";
        public static string SectionMeeting => "会议倒计时";
        public static string SectionExploitProtect => "漏洞防护（自我保护）";
        public static string SectionAutoMute => "会议自动禁言";
        public static string SectionOnViolation => "违规处理";
        public static string SectionRpcChecks => "独立 RPC 检测";
        public static string SectionAppearance => "外观设置";
        public static string SectionBanPlayer => "在此大厅封禁玩家";
        public static string SectionBannedEntries => "已封禁名单";
        public static string SectionHostTools => "主机工具";

        // ========== 大厅 ==========
        public static string ToggleModdedLobby => "  修改版大厅（放宽角色检测）";

        // ========== 检测总开关 ==========
        public static string ToggleMasterSwitch => "  反作弊总开关";
        public static string ToggleCheatClients => "  检测作弊客户端（Sicko/AUM/KN）";
        public static string ToggleNameFilter => "  踢出辱骂/被封禁的名字";
        public static string ButtonReloadBlocklist => "重新加载黑名单文件";

        // ========== 崩溃/洪水 ==========
        public static string ToggleCrashProtection => "  崩溃防护（总开关）";
        public static string ToggleCheckMalformed => "    - 畸形 RPC 数据";
        public static string ToggleCheckFlood => "    - RPC 洪水攻击";
        public static string ToggleDetectUnknownRpc => "  未注册/未知 RPC";

        // ========== 状态检测 ==========
        public static string ToggleStateChecks => "  状态检测（总开关）";
        public static string ToggleCheckCosmetics => "    - 游戏中更改外观";
        public static string ToggleCheckLobbyRpcs => "    - 在大厅中使用游戏内 RPC";

        // ========== 聊天 ==========
        public static string ToggleChatSpam => "  聊天刷屏 / 超大消息";
        public static string ToggleVoteKickGuard => "  投票滥用（已死玩家/会议外投票）";

        // ========== 会议倒计时 ==========
        public static string ToggleBlockEarlyMeetings => "  阻止过早开会";
        public static string ToggleEmergencyOnly => "  仅紧急按钮";
        public static string LabelGrace => "Grace: {0}s";

        // ========== 漏洞防护 ==========
        public static string ToggleBlockVentKick => "  阻止通风口踢出/封禁漏洞";
        public static string ToggleBlockTeleports => "  阻止强制传送（大量通风）";
        public static string ToggleBlockVotingOverload => "  阻止投票过载崩溃";
        public static string ToggleBlockLargeMessages => "  阻止超大消息";

        // ========== 自动禁言 ==========
        public static string ToggleMuteMajorityVote => "  多数票时禁言玩家";
        public static string ToggleMuteChatConsensus => "  按聊天颜色投票禁言";

        // ========== 违规处理 ==========
        public static string ToggleSendNotification => "  发送通知";
        public static string ToggleDiscardRpc => "  丢弃该 RPC";
        public static string LabelPunish => "惩罚：{0}";
        public static string PunishmentNone => "无";
        public static string PunishmentKick => "踢出";
        public static string PunishmentBan => "封禁";

        // ========== 外观 ==========
        public static string ToggleRainbowGui => "  彩虹 GUI";

        // ========== 封禁玩家 ==========
        public static string ToggleAutoKickBanned => "  自动踢出封禁名单玩家（仅主机）";
        public static string ButtonBan => "封禁";
        public static string ButtonUnban => "解封";
        public static string ButtonReloadFile => "重新加载文件";

        // ========== 主机工具 ==========
        public static string LabelMap => "地图：{0}";
        public static string ButtonSpawnMap => "生成地图";
        public static string ButtonDespawnMap => "移除地图";
        public static string ButtonSpawnLobby => "生成大厅";
        public static string ButtonDespawnLobby => "移除大厅";
        public static string ButtonForceCrewWin => "强制胜利（crew）";
        public static string ButtonForceImpostorWin => "强制胜利（impostor）";

        // ========== 通知消息 ==========
        public static string NotifyBlockedTeleport => "已阻止针对你的强制传送。";
        public static string NotifyBlockedVentKick => "已阻止针对你的通风口踢出/封禁漏洞。";
        public static string NotifyBlockedVotingOverload => "已阻止投票过载崩溃尝试。";
        public static string NotifyBannedListSaved => "封禁名单已保存（共 {0} 条）。";
        public static string NotifyMapSpawned => "地图 {(MapNames)mapId} 已生成。";
        public static string NotifyCurrentMapDespawned => "当前地图已移除。";
        public static string NotifyNoMapSpawned => "当前没有地图。";
        public static string NotifyLobbyDespawned => "大厅已移除。";
        public static string NotifyLobbyAlreadyDespawned => "大厅已经移除。";
        public static string NotifyLobbySpawned => "大厅已生成。";
        public static string NotifyCrewVictory => "已强制 crew 胜利。";
        public static string NotifyImpostorVictory => "已强制 impostor 胜利。";
        public static string NotifyConfigSaved => "配置已保存。";
        public static string NotifyConfigSaveFailed => "保存配置失败：{0}";
        public static string NotifyLoadedBlocklist => "已加载 {0} 条黑名单术语。";

        // ========== 通用违规消息前缀 ==========
        public static string ViolationPrefix(string name) => $"{name}";

        // ========== 反作弊违规消息 ==========
        public static string ViolationMalformedRpc(string name, string rpc)
            => $"{name} 发送了畸形 {rpc} RPC（崩溃尝试）。";
        public static string ViolationFlood(string name, int count, float window)
            => $"{name} 正在洪水发送 RPC（{count}次/{window:0.#}秒）- 崩溃尝试。";
        public static string ViolationUnknownRpc(string name, byte callId)
            => $"{name} 发送了未注册的 RPC（{callId}）- 可能是作弊/崩溃。";
        public static string ViolationHostOnlyRpc(string name, string rpc)
            => $"{name} 以非主机身份发送了仅主机可用的 RPC {rpc}。";
        public static string ViolationCheatClient(string name, string client)
            => $"{name} 正在运行 {client}（已检测到作弊客户端）。";
        public static string ViolationCosmetics(string name, string rpc)
            => $"{name} 在游戏中更改了外观（{rpc}）。";
        public static string ViolationLobbyRpc(string name, byte rpc)
            => $"{name} 在大厅中发送了游戏内 RPC {(RpcCalls)rpc}。";
        public static string ViolationChatOversized(string name, int len)
            => $"{name} 发送了超大聊天消息（{len}字符）- 崩溃尝试。";
        public static string ViolationChatSpam(string name, int count, float window)
            => $"{name} 正在刷屏聊天（{count}条消息/{window:0.#}秒）。";
        public static string ViolationTaskNoShip(string name, uint taskIndex)
            => $"{name} 在没有飞船状态的情况下完成了任务 {taskIndex}。";
        public static string ViolationTaskAsImpostor(string name, uint taskIndex)
            => $"{name} 以内鬼身份完成了任务 {taskIndex}。";
        public static string ViolationTaskCount(string name, uint taskIndex, int count)
            => $"{name} 完成了任务 {taskIndex}，但只有 {count} 个任务。";
        public static string ViolationVentNoShip(string name)
            => $"{name} 在没有飞船状态的情况下进入通风口。";
        public static string ViolationVentNoRole(string name, string roleType)
            => $"{name} 进入了通风口，但角色（{roleType}）无法使用通风口。";
        public static string ViolationExitVentNoShip(string name)
            => $"{name} 在没有飞船状态的情况下离开通风口。";
        public static string ViolationExitVentNoRole(string name, string roleType)
            => $"{name} 离开了通风口，但角色（{roleType}）无法使用通风口。";
        public static string ViolationClimbLadderNoShip(string name)
            => $"{name} 在没有飞船状态的情况下爬梯子。";
        public static string ViolationClimbLadderDead(string name)
            => $"{name} 在死亡状态下爬梯子。";
        public static string ViolationSnapToLobby(string name)
            => $"{name} 在大厅中使用了 SnapTo（传送）。";
        public static string ViolationInvalidKill(string name, string targetName)
            => $"{name} 对 {targetName} 发送了无效击杀。";
        public static string ViolationIllegalMurder(string name)
            => $"{name} 执行了非法的击杀。";
        public static string ViolationShapeshift(string name)
            => $"{name} 在非活体变形者状态下变形。";
        public static string ViolationVanish(string name)
            => $"{name} 在非活体幻影状态下消失。";
        public static string ViolationProtect(string name)
            => $"{name} 在非守护天使状态下保护玩家。";
        public static string ViolationSystemNotFound(string name, string system)
            => $"{name} 更新了该地图不存在的系统 {system}。";
        public static string ViolationSystemDead(string name, string system)
            => $"{name} 死亡时更新了系统 {system}。";
        public static string ViolationMushroom(string name)
            => $"{name} 试图强制触发蘑菇混合混乱 sabotge。";
        public static string ViolationReactorForceFix(string name)
            => $"{name} 试图强制修复反应堆 sabotge。";
        public static string ViolationReactorForceCall(string name)
            => $"{name} 试图强制呼叫反应堆 sabotge。";
        public static string ViolationInvalidSabotageTarget(string name, string target)
            => $"{name} 试图 sabotge 无效系统：{target}。";
        public static string ViolationSabotageNotImpostor(string name, string target)
            => $"{name} 不是内鬼却试图 sabotge {target}。";
        public static string ViolationSabotageHideSeek(string name, string target)
            => $"{name} 在捉迷藏模式中试图 sabotge {target}。";
        public static string ViolationSwitchCrash(string name, byte switches)
            => $"{name} 发送了批量开关崩溃数据（{switches}）。";
        public static string ViolationInvalidSwitch(string name, byte switches)
            => $"{name} 切换了无效开关（{switches}）。";
        public static string ViolationTaskAnimLobby(string name)
            => $"{name} 在大厅中播放了任务动画。";
        public static string ViolationTaskAnimImpostor(string name)
            => $"{name} 以内鬼身份播放了任务动画。";
        public static string ViolationTaskAnimNoVisual(string name)
            => $"{name} 在关闭视觉任务时播放了任务动画。";
        public static string ViolationExiled(string name)
            => $"{name} 发送了无效的 Exiled RPC。";
        public static string ViolationSetColorNetId(string name, uint netId)
            => $"{name} 的颜色设置具有错误的 net id（应为{netId}）。";
        public static string ViolationSetColorColor(string name, byte color)
            => $"{name} 的颜色设置使用了无效颜色（{color}）。";
        public static string ViolationScannerNoMap(string name)
            => $"{name} 在地图生成前进行了医学扫描。";
        public static string ViolationScannerImpostor(string name)
            => $"{name} 以内鬼身份进行了医学扫描。";
        public static string ViolationScannerNoTask(string name)
            => $"{name} 没有医学扫描任务却进行了扫描。";
        public static string ViolationPlatformWrongMap(string name)
            => $"{name} 在错误的地图上使用了传送台。";
        public static string ViolationPlatformNoMap(string name)
            => $"{name} 在没有地图的情况下使用了传送台。";
        public static string ViolationPlatformHideSeek(string name)
            => $"{name} 在捉迷藏模式中使用了传送台。";
        public static string ViolationLevelTooHigh(string name, uint level)
            => $"{name} 发送了不可能的高等级（{level}）。";
        public static string ViolationLevelAfterStart(string name)
            => $"{name} 在游戏开始后发送了 SetLevel。";
        public static string ViolationUnknownClientVote(string id)
            => $"未知客户端（{id}）试图发起投票。";
        public static string ViolationDeadVote(string name)
            => $"{name} 在死亡状态下试图发起投票。";
        public static string ViolationOutsideMeetingVote(string name)
            => $"{name} 在会议外试图发起投票。";
        public static string ViolationHideSeekMeeting(string name)
            => $"{name} 在捉迷藏模式中试图发起会议。";
        public static string ViolationEarlyMeeting(string name, string kind, float remaining, float grace)
            => $"{name} 过早发起{kind}（早了 {remaining:0.0}秒，Grace 时间为 {grace:0}秒）。";
        public static string ViolationBlockedTerm(string name, string term)
            => $"'{name}' 包含被屏蔽的词（'{term}'）。";
        public static string ViolationNameTooLong(string name, int len)
            => $"'{name}' 过长（{len}字符）。";
        public static string ViolationNameInvalidChars(string name)
            => $"'{name}' 包含非法格式字符。";
        public static string ViolationStartCounterSpoof(string name, sbyte counter)
            => $"{name} 篡改了开始倒计时（{counter}）。";
        public static string ViolationBannedList(string name)
            => $"{name} 在封禁名单上。";
        public static string MuteMajorityVotes(string name)
            => $"{name} 获得多数票 - 本次会议禁言。";
        public static string MuteChatConsensus(string name, int count)
            => $"{name} 被聊天投票禁言（{count}票）。";
        public static string LogYourIdentifiers(string friendCode, string playerName)
            => $"[Nitro] 你的标识 -> 好友码：'{friendCode}'  名字：'{playerName}'";
        public static string LogConfigSaved => "[Nitro] 配置已保存。";
        public static string LogConfigSaveFailed(string msg) => $"[Nitro] 保存配置失败：{msg}";
        public static string LogLoadBlocklist(int count) => $"[WellsAntiCheat] 已加载 {count} 条黑名单术语。";
        public static string LogLoadRpcStatesFailed(string msg) => $"[Wells] 加载 RPC 状态失败：{msg}";
        public static string LogSaveRpcStatesFailed(string msg) => $"[Wells] 保存 RPC 状态失败：{msg}";
        public static string LogLoaded(string msg) => $"[Nitro] {msg}";
        public static string LogWarning(string msg) => $"[Nitro] {msg}";
        public static string LogFailedToSaveBannedList(string msg) => $"无法保存封禁名单：{msg}";
        public static string ConfigHeaderBlocklist =>
            "# WellsAntiCheat 辱骂名字黑名单。\n" +
            "# 每行一个词。以 # 开头的行为注释。\n" +
            "# 匹配不区分大小写，支持 Leetspeak：\n" +
            "#   'nigger' 也能匹配 'N1GG3R'、'n i g g e r'、'niiiggerr' 等。\n" +
            "# 请使用纯小写字母输入每个词。\n\n";
        public static string ConfigHeaderBanned =>
            "# Nitro 封禁玩家名单。\n" +
            "# 每行一个条目 - 玩家名字或好友码（例如 name#1234）。\n" +
            "# 匹配不区分大小写。以 # 开头的行为注释。\n" +
            "# 匹配到的玩家在你作为主机时会被自动踢出。\n\n";

        // ========== 帮助方法 ==========
        public static string Format(string format, params object[] args) => string.Format(format, args);

        // 获取惩罚枚举的中文显示
        public static string GetPunishmentName(Anticheat.Punishments p) => p switch
        {
            Anticheat.Punishments.None => PunishmentNone,
            Anticheat.Punishments.Kick => PunishmentKick,
            Anticheat.Punishments.Ban  => PunishmentBan,
            _                          => PunishmentKick,
        };
    }
}
