using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace WellsAntiCheat
{
    // 持久化的封禁玩家列表，主机在面板中以纯文本方式编辑（记事本风格）
    // 每个非注释行与加入玩家的名字或好友码匹配
    // （不区分大小写）。匹配的玩家在你作为主机时自动踢出
    internal static class BannedPlayers
    {
        public static bool Enabled = true;

        // 记事本中显示的可编辑缓冲区。在 Save() 时持久化到磁盘
        public static string RawText = "";

        private static readonly HashSet<string> _entries = new(StringComparer.OrdinalIgnoreCase);
        private static string Path_ => System.IO.Path.Combine(Paths.ConfigPath, "NitroAntiCheat_banned.txt");

        public static void Load()
        {
            if (!File.Exists(Path_))
            {
                RawText = Strings.ConfigHeaderBanned;
                File.WriteAllText(Path_, RawText);
            }
            else
            {
                RawText = File.ReadAllText(Path_);
            }
            Reparse();
        }

        public static void Save()
        {
            try { File.WriteAllText(Path_, RawText); }
            catch (Exception e) { WellsPlugin.Log.LogWarning(Strings.LogFailedToSaveBannedList(e.Message)); }
            Reparse();
            Notifier.Show(string.Format(Strings.NotifyBannedListSaved, _entries.Count));
        }

        private static void Reparse()
        {
            _entries.Clear();
            foreach (var raw in RawText.Replace("\r", "").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                _entries.Add(line);
            }
        }

        // 从游戏内动作添加条目并立即持久化
        public static void Add(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry)) return;
            if (!RawText.EndsWith("\n")) RawText += "\n";
            RawText += entry.Trim() + "\n";
            Save();
        }

        public static bool IsBanned(PlayerControl p)
        {
            if (!Enabled || p == null || p.Data == null || _entries.Count == 0) return false;
            var name = p.Data.PlayerName ?? "";
            var code = p.Data.FriendCode ?? "";
            return (name.Length > 0 && _entries.Contains(name))
                || (code.Length > 0 && _entries.Contains(code));
        }

        // 供 GUI 使用：当前封禁条目，以及删除
        public static List<string> EntryList()
        {
            var list = new List<string>(_entries);
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        public static void Remove(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry)) return;
            var kept = new List<string>();
            foreach (var raw in RawText.Replace("\r", "").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) { kept.Add(raw); continue; }
                if (string.Equals(line, entry.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                kept.Add(raw);
            }
            RawText = string.Join("\n", kept);
            Save();
        }
    }
}
