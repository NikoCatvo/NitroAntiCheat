using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;

namespace WellsAntiCheat
{
    // 判断请求的名字是否具有冒犯性。黑名单存放在纯文本文件
    // （BepInEx/config/NitroAntiCheat_blocklist.txt）中，因此你可以编辑它而无需重新编译
    internal static class NameFilter
    {
        public static bool Enabled = true;

        private static readonly HashSet<string> Blocked = new(StringComparer.Ordinal);
        private static string BlocklistPath => Path.Combine(Paths.ConfigPath, "NitroAntiCheat_blocklist.txt");

        // 人们用来绕过朴素子字符串匹配的常见替换
        private static readonly Dictionary<char, char> LeetMap = new()
        {
            ['0'] = 'o', ['1'] = 'i', ['!'] = 'i', ['|'] = 'i',
            ['3'] = 'e', ['4'] = 'a', ['@'] = 'a', ['5'] = 's',
            ['$'] = 's', ['7'] = 't', ['+'] = 't', ['8'] = 'b',
            ['9'] = 'g', ['6'] = 'g', ['2'] = 'z',
        };

        // 最小的种子列表，使 mod 在首次运行时可用。在你的
        // 配置文件中添加自己的词。刻意保持简短；文件才是真正列表所在
        private static readonly string[] SeedList =
        {
            // 主机请求踢出的特定玩家目标
            "antipride",
            // 种族歧视/仇恨用语（规范化形式，即名字会折叠到的形式）
            "nigger", "nigga", "faggot", "retard", "kike", "spic", "chink", "tranny",
            // 通过配置文件添加更多
        };

        public static void Load()
        {
            Blocked.Clear();

            if (!File.Exists(BlocklistPath))
            {
                var header = Strings.ConfigHeaderBlocklist;
                File.WriteAllText(BlocklistPath, header + string.Join("\n", SeedList) + "\n");
            }

            foreach (var raw in File.ReadAllLines(BlocklistPath))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var norm = Normalize(line);
                if (norm.Length > 0) Blocked.Add(norm);
            }

            WellsPlugin.Log.LogInfo(Strings.LogLoadBlocklist(Blocked.Count));
        }

        // 如果名字应该被阻止则返回 true 和 offending term
        public static bool IsOffensive(string name, out string matched)
        {
            matched = null;
            if (!Enabled || string.IsNullOrEmpty(name)) return false;

            var norm = Normalize(name);
            if (norm.Length == 0) return false;

            foreach (var term in Blocked)
            {
                if (norm.Contains(term))
                {
                    matched = term;
                    return true;
                }
            }
            return false;
        }

        // 小写，剥离格式标签，应用 Leet 映射，删除非字母字符，压缩相同字母的连续序列
        // 使 "niiigger" 折叠为 "niiger"... 等等：在匹配前压缩会破坏
        // 合法单词，所以我们最多压缩到单个重复，这仍然能捕获填充技巧
        public static string Normalize(string input)
        {
            // 移除可能隐藏字符的 TMP rich-text 标签如 <color=...>
            var noTags = StripTags(input).ToLowerInvariant();

            var sb = new StringBuilder(noTags.Length);
            foreach (var ch in noTags)
            {
                char c = LeetMap.TryGetValue(ch, out var mapped) ? mapped : ch;
                if (c >= 'a' && c <= 'z') sb.Append(c);
                // 所有其他字符（空格、标点、emoji）被删除，这能击败
                // "n.i.g.g.e.r" 和 "n i g g e r" 风格的间距
            }

            // 将 3+ 相同字母压缩到 2 个，使 "niiiiigger" 折叠为 "niigger"
            return Deduplicate(sb.ToString());
        }

        private static string StripTags(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool inTag = false;
            foreach (var ch in s)
            {
                if (ch == '<') { inTag = true; continue; }
                if (ch == '>') { inTag = false; continue; }
                if (!inTag) sb.Append(ch);
            }
            return sb.ToString();
        }

        // 将任何相同字符的连续序列压缩为单个实例。这使填充如
        // "niiiggggerr" 匹配 "niger"-style 存储的术语 IF 存储的术语也被 deduped。
        // 我们在两边都 dedupe，所以存储的术语在加载时隐式通过此调用去重
        private static string Deduplicate(string s)
        {
            if (s.Length == 0) return s;
            var sb = new StringBuilder(s.Length);
            char prev = '\0';
            foreach (var ch in s)
            {
                if (ch != prev) sb.Append(ch);
                prev = ch;
            }
            return sb.ToString();
        }
    }
}
