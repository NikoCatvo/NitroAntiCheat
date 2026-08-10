using System;
using System.IO;
using BepInEx;
using UnityEngine;

namespace WellsAntiCheat
{
    public class WellsGui : MonoBehaviour
    {
        public WellsGui(IntPtr ptr) : base(ptr) { }

        public const string Title = "Nitro Anti Cheat - Well";
        public static KeyCode ToggleKey = KeyCode.F8;
        public static bool RainbowGui = true;

        private bool _open = false;
        private Rect _window = new Rect(20, 20, 350, 720);
        private Vector2 _scroll = Vector2.zero;
        private const int WindowId = 0x4E495452;

        private float _banTimer;
        private bool _loggedSelf;

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                _open = !_open;
                if (!_open) SaveConfig();
            }

            LogSelfOnce();
            EnforceBans();
            MuteManager.CheckMajorityVotes();
        }

        public static void SaveConfig()
        {
            WellsPlugin.SaveAllConfig();
        }

        private void LogSelfOnce()
        {
            if (_loggedSelf) return;
            var me = PlayerControl.LocalPlayer;
            if (me == null || me.Data == null) return;
            _loggedSelf = true;
            WellsPlugin.Log.LogInfo(Strings.LogYourIdentifiers(me.Data.FriendCode ?? "", me.Data.PlayerName ?? ""));
        }

        private void EnforceBans()
        {
            if (!BannedPlayers.Enabled) return;
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            _banTimer += Time.deltaTime;
            if (_banTimer < 2f) return;
            _banTimer = 0f;

            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                if (p == null || Anticheat.IsExempt(p)) continue;
                if (BannedPlayers.IsBanned(p))
                {
                    Anticheat.Flag(p, Strings.ViolationBannedList(Anticheat.Name(p)), false);
                    AmongUsClient.Instance.KickPlayer(p.OwnerId, true);
                }
            }
        }

        private static Color Accent()
            => RainbowGui ? Color.HSVToRGB(Mathf.Repeat(Time.time * 0.15f, 1f), 0.65f, 1f) : Color.white;

        private void OnGUI()
        {
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            DrawNotifications();

            if (!_open) return;

            GUI.backgroundColor = Accent();
            _window = GUI.Window(WindowId, _window, (GUI.WindowFunction)DrawWindow, Strings.WindowTitle);
            GUI.backgroundColor = Color.white;
        }

        private void Header(string text)
        {
            GUI.contentColor = Accent();
            GUILayout.Label(text);
            GUI.contentColor = Color.white;
        }

        private void DrawWindow(int id)
        {
            bool amHost = AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;

            _scroll = GUILayout.BeginScrollView(_scroll);

            GUI.contentColor = amHost ? Color.green : new Color(1f, 0.6f, 0.1f);
            GUILayout.Label(amHost ? Strings.StatusHost : Strings.StatusNotHost);
            GUI.contentColor = Color.white;

            GUI.enabled = amHost;

            Header(Strings.SectionLobby);
            Anticheat.ModdedLobby = GUILayout.Toggle(Anticheat.ModdedLobby, Strings.ToggleModdedLobby);

            Header(Strings.SectionDetection);
            Anticheat.Enabled = GUILayout.Toggle(Anticheat.Enabled, Strings.ToggleMasterSwitch);
            CheatClients.Enabled = GUILayout.Toggle(CheatClients.Enabled, Strings.ToggleCheatClients);
            NameFilter.Enabled = GUILayout.Toggle(NameFilter.Enabled, Strings.ToggleNameFilter);
            if (GUILayout.Button(Strings.ButtonReloadBlocklist)) NameFilter.Load();

            Header(Strings.SectionCrash);
            Anticheat.CrashProtection = GUILayout.Toggle(Anticheat.CrashProtection, Strings.ToggleCrashProtection);
            Anticheat.CheckMalformed = GUILayout.Toggle(Anticheat.CheckMalformed, Strings.ToggleCheckMalformed);
            Anticheat.CheckFlood = GUILayout.Toggle(Anticheat.CheckFlood, Strings.ToggleCheckFlood);
            Anticheat.DetectUnknownRpc = GUILayout.Toggle(Anticheat.DetectUnknownRpc, Strings.ToggleDetectUnknownRpc);

            Header(Strings.SectionStateChecks);
            StateChecks.Enabled = GUILayout.Toggle(StateChecks.Enabled, Strings.ToggleStateChecks);
            StateChecks.CheckCosmetics = GUILayout.Toggle(StateChecks.CheckCosmetics, Strings.ToggleCheckCosmetics);
            StateChecks.CheckLobbyRpcs = GUILayout.Toggle(StateChecks.CheckLobbyRpcs, Strings.ToggleCheckLobbyRpcs);

            Header(Strings.SectionChat);
            var chat = Anticheat.RpcHandlers.TryGetValue(RpcCalls.SendChat, out var sc) ? sc : null;
            if (chat != null) chat.Enabled = GUILayout.Toggle(chat.Enabled, Strings.ToggleChatSpam);
            WellsAntiCheat.Rpc.VoteKickGuard.Enabled = GUILayout.Toggle(WellsAntiCheat.Rpc.VoteKickGuard.Enabled, Strings.ToggleVoteKickGuard);

            Header(Strings.SectionMeeting);
            MeetingTimer.Enabled = GUILayout.Toggle(MeetingTimer.Enabled, Strings.ToggleBlockEarlyMeetings);
            MeetingTimer.EmergencyOnly = GUILayout.Toggle(MeetingTimer.EmergencyOnly, Strings.ToggleEmergencyOnly);
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format(Strings.LabelGrace, MeetingTimer.GraceSeconds), GUILayout.Width(80));
            MeetingTimer.GraceSeconds = Mathf.Round(GUILayout.HorizontalSlider(MeetingTimer.GraceSeconds, 0f, 30f));
            GUILayout.EndHorizontal();

            Header(Strings.SectionExploitProtect);
            Protections.BlockVentKickExploit = GUILayout.Toggle(Protections.BlockVentKickExploit, Strings.ToggleBlockVentKick);
            Protections.BlockServerTeleports = GUILayout.Toggle(Protections.BlockServerTeleports, Strings.ToggleBlockTeleports);
            Protections.BlockVotingOverload = GUILayout.Toggle(Protections.BlockVotingOverload, Strings.ToggleBlockVotingOverload);
            Protections.BlockLargeMessages = GUILayout.Toggle(Protections.BlockLargeMessages, Strings.ToggleBlockLargeMessages);

            Header(Strings.SectionAutoMute);
            MuteManager.MuteOnMajorityVote = GUILayout.Toggle(MuteManager.MuteOnMajorityVote, Strings.ToggleMuteMajorityVote);
            MuteManager.MuteOnChatConsensus = GUILayout.Toggle(MuteManager.MuteOnChatConsensus, Strings.ToggleMuteChatConsensus);

            Header(Strings.SectionOnViolation);
            Anticheat.SendNotification = GUILayout.Toggle(Anticheat.SendNotification, Strings.ToggleSendNotification);
            Anticheat.DiscardRpc = GUILayout.Toggle(Anticheat.DiscardRpc, Strings.ToggleDiscardRpc);
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format(Strings.LabelPunish, Strings.GetPunishmentName(Anticheat.Punishment)), GUILayout.Width(110));
            Anticheat.Punishment = (Anticheat.Punishments)Mathf.RoundToInt(
                GUILayout.HorizontalSlider((float)Anticheat.Punishment, 0, 2));
            GUILayout.EndHorizontal();

            Header(Strings.SectionRpcChecks);
            foreach (var kvp in Anticheat.RpcHandlers)
            {
                var label = string.IsNullOrEmpty(kvp.Value.DisplayName) ? kvp.Value.Name : kvp.Value.DisplayName;
                kvp.Value.Enabled = GUILayout.Toggle(kvp.Value.Enabled, $" {label}");
            }

            Header(Strings.SectionAppearance);
            RainbowGui = GUILayout.Toggle(RainbowGui, Strings.ToggleRainbowGui);

            bool prevEnabled = GUI.enabled;
            GUI.enabled = true;
            Header(Strings.SectionBanPlayer);
            BannedPlayers.Enabled = GUILayout.Toggle(BannedPlayers.Enabled, Strings.ToggleAutoKickBanned);
            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.Data == null || p.AmOwner) continue;
                GUILayout.BeginHorizontal();
                GUILayout.Label(Anticheat.Name(p), GUILayout.Width(150));
                if (GUILayout.Button(Strings.ButtonBan, GUILayout.Width(70)))
                {
                    var code = p.Data.FriendCode ?? "";
                    BannedPlayers.Add(code.Length > 0 ? code : p.Data.PlayerName);
                }
                GUILayout.EndHorizontal();
            }

            Header(Strings.SectionBannedEntries);
            foreach (var entry in BannedPlayers.EntryList())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(entry, GUILayout.Width(150));
                if (GUILayout.Button(Strings.ButtonUnban, GUILayout.Width(70)))
                    BannedPlayers.Remove(entry);
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button(Strings.ButtonReloadFile)) BannedPlayers.Load();
            GUI.enabled = prevEnabled;

            Header(Strings.SectionHostTools);
            GUILayout.Label(string.Format(Strings.LabelMap, (MapNames)HostTools.SelectedMap));
            HostTools.SelectedMap = (byte)Mathf.Round(
                GUILayout.HorizontalSlider(HostTools.SelectedMap, 0, HostTools.MaxMapId));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Strings.ButtonSpawnMap)) HostTools.SpawnMap();
            if (GUILayout.Button(Strings.ButtonDespawnMap)) HostTools.DespawnMap();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Strings.ButtonSpawnLobby)) HostTools.SpawnLobby();
            if (GUILayout.Button(Strings.ButtonDespawnLobby)) HostTools.DespawnLobby();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Strings.ButtonForceCrewWin)) HostTools.ForceCrewVictory();
            if (GUILayout.Button(Strings.ButtonForceImpostorWin)) HostTools.ForceImpostorVictory();
            GUILayout.EndHorizontal();

            GUI.enabled = true;
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void DrawNotifications()
        {
            float y = 10f;
            foreach (var msg in Notifier.Recent())
            {
                GUI.Box(new Rect(Screen.width - 420, y, 410, 40), msg);
                y += 44f;
            }
        }
    }
}
