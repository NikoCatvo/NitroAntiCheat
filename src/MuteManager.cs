using HarmonyLib;
using System.Collections.Generic;

namespace WellsAntiCheat
{
    // "禁言"在 Among Us 中 = 主机端阻止玩家的聊天 RPC，使其消息永不传播
    // （游戏没有原生禁言）。禁言持续到当前会议结束，会议结束时清除
    //
    // 两个可选触发器：
    //   A) 多数票  - 一旦玩家持有大多数票（即将被驱逐），禁言
    //      他们，这样垂死的内鬼无法在聊天中泄露信息
    //   B) 聊天共识 - 如果大多数活玩家输入一个颜色名字，穿
    //      该颜色衣服的玩家在本会议剩余时间内被禁言
    internal static class MuteManager
    {
        public static bool MuteOnMajorityVote = false;
        public static bool MuteOnChatConsensus = false;

        private static readonly HashSet<byte> _muted = new();
        // colourId -> 为此颜色投票的不同活玩家集合
        private static readonly Dictionary<int, HashSet<byte>> _colorVotes = new();
        private static Dictionary<string, int> _colorLookup;

        private static bool AmHost => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;

        public static void ClearMeeting()
        {
            _muted.Clear();
            _colorVotes.Clear();
        }

        public static bool IsMuted(PlayerControl p)
            => p != null && p.Data != null && _muted.Contains(p.PlayerId);

        private static void Mute(byte playerId, string reason)
        {
            if (_muted.Add(playerId))
                Notifier.Show(reason);
        }

        private static int AliveCount()
        {
            int n = 0;
            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                if (p?.Data != null && !p.Data.IsDead && !p.Data.Disconnected) n++;
            return n;
        }

        // --- 功能 A: 多数票 ---
        // 会议期间每帧调用（仅主机）
        public static void CheckMajorityVotes()
        {
            if (!MuteOnMajorityVote || !AmHost || MeetingHud.Instance == null) return;

            try
            {
                var tally = new Dictionary<byte, int>();
                foreach (PlayerVoteArea state in MeetingHud.Instance.playerStates)
                {
                    if (state == null || !state.DidVote) continue;
                    byte v = state.VotedFor;
                    if (v == 253 || v == 254 || v == 255) continue; // 跳过/错过/无投票
                    tally.TryGetValue(v, out int c);
                    tally[v] = c + 1;
                }

                int needed = AliveCount() / 2 + 1;
                foreach (var kv in tally)
                {
                    if (kv.Value >= needed)
                    {
                        var target = FindPlayerById(kv.Key);
                        if (target != null && !Anticheat.IsExempt(target))
                            Mute(kv.Key, Strings.MuteMajorityVotes(Anticheat.Name(target)));
                    }
                }
            }
            catch { }
        }

        // --- 功能 B: 聊天颜色共识 ---
        public static void RecordChatColorVote(PlayerControl sender, string message)
        {
            if (!MuteOnChatConsensus || !AmHost || sender?.Data == null) return;
            if (sender.Data.IsDead) return; // 死玩家不计入
            if (string.IsNullOrEmpty(message)) return;

            int colorId = MatchColor(message);
            if (colorId < 0) return;

            if (!_colorVotes.TryGetValue(colorId, out var voters))
            {
                voters = new HashSet<byte>();
                _colorVotes[colorId] = voters;
            }
            voters.Add(sender.PlayerId);

            int needed = AliveCount() / 2 + 1;
            if (voters.Count >= needed)
            {
                var target = FindPlayerByColor(colorId);
                if (target != null && !Anticheat.IsExempt(target))
                    Mute(target.PlayerId, Strings.MuteChatConsensus(Anticheat.Name(target), voters.Count));
            }
        }

        private static PlayerControl FindPlayerById(byte id)
        {
            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                if (p?.Data != null && p.PlayerId == id) return p;
            return null;
        }

        private static PlayerControl FindPlayerByColor(int colorId)
        {
            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                if (p?.Data != null && p.Data.DefaultOutfit != null && p.Data.DefaultOutfit.ColorId == colorId)
                    return p;
            return null;
        }

        // 如果消息命名恰好一个已知颜色则返回颜色 id，否则返回 -1
        private static int MatchColor(string message)
        {
            BuildColorLookup();
            var words = message.ToLowerInvariant().Split(new[] { ' ', ',', '.', '!', '?', ';', ':' },
                System.StringSplitOptions.RemoveEmptyEntries);

            int found = -1;
            foreach (var w in words)
            {
                if (_colorLookup.TryGetValue(w, out int id))
                {
                    if (found >= 0 && found != id) return -1; // 歧义（命名了两个颜色）
                    found = id;
                }
            }
            return found;
        }

        private static void BuildColorLookup()
        {
            if (_colorLookup != null) return;
            // Among Us 颜色顺序固定（索引 = ColorId）。我们直接匹配英文名字
            // 而不是通过翻译层（玩家无论如何都输入英文）
            _colorLookup = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["red"] = 0, ["blue"] = 1, ["green"] = 2, ["pink"] = 3, ["orange"] = 4,
                ["yellow"] = 5, ["black"] = 6, ["white"] = 7, ["purple"] = 8, ["brown"] = 9,
                ["cyan"] = 10, ["lime"] = 11, ["maroon"] = 12, ["rose"] = 13, ["banana"] = 14,
                ["gray"] = 15, ["grey"] = 15, ["tan"] = 16, ["coral"] = 17,
                // 常见缩写
                ["purp"] = 8, ["cyian"] = 10, ["darkgreen"] = 2, ["lightgreen"] = 11,
            };
        }

        // 在每个会议开始时清除禁言
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
        private static class OnMeetingStart
        {
            private static void Postfix() => ClearMeeting();
        }
    }
}
