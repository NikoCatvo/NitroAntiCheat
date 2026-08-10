using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using InnerNet;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace WellsAntiCheat
{
    // 仅主机工具。这里的所有内容在做任何事情前都检查 AmHost，因此 GUI 可以
    // 禁用这些按钮，但即使是意外调用也是安全的
    internal static class HostTools
    {
        // 地图 id：0 Skeld, 1 MiraHQ, 2 Polus, 3 Dleks, 4 Airship, 5 Fungle
        public static byte SelectedMap = 0;
        public const byte MaxMapId = 5;

        private static bool AmHost => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;

        public static void DespawnMap()
        {
            if (!AmHost) return;
            if (ShipStatus.Instance != null)
            {
                ShipStatus.Instance.Despawn();
                Notifier.Show(Strings.NotifyCurrentMapDespawned);
            }
            else Notifier.Show(Strings.NotifyNoMapSpawned);
        }

        public static void SpawnMap()
        {
            if (!AmHost) return;
            AmongUsClient.Instance.StartCoroutine(SpawnMapRoutine(SelectedMap).WrapToIl2Cpp());
        }

        private static IEnumerator SpawnMapRoutine(byte mapId)
        {
            WellsPlugin.Log.LogInfo($"正在生成地图 id {mapId}");
            AsyncOperationHandle<GameObject> handle =
                AmongUsClient.Instance.ShipPrefabs[mapId].InstantiateAsync(null, false);
            yield return handle;

            ShipStatus ship = handle.Result.GetComponent<ShipStatus>();
            AmongUsClient.Instance.Spawn(ship, -2, SpawnFlags.None);
            Notifier.Show(string.Format(Strings.NotifyMapSpawned, mapId));
        }

        public static void DespawnLobby()
        {
            if (!AmHost) return;
            if (LobbyBehaviour.Instance != null)
            {
                LobbyBehaviour.Instance.Despawn();
                Notifier.Show(Strings.NotifyLobbyDespawned);
            }
            else Notifier.Show(Strings.NotifyLobbyAlreadyDespawned);
        }

        public static void SpawnLobby()
        {
            if (!AmHost) return;
            LobbyBehaviour.Instance = Object.Instantiate(GameStartManager.Instance.LobbyPrefab);
            AmongUsClient.Instance.Spawn(LobbyBehaviour.Instance, -2, SpawnFlags.None);
            Notifier.Show(Strings.NotifyLobbySpawned);
        }

        public static void ForceCrewVictory()
        {
            if (!AmHost || GameManager.Instance == null) return;
            GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByTask, false);
            Notifier.Show(Strings.NotifyCrewVictory);
        }

        public static void ForceImpostorVictory()
        {
            if (!AmHost || GameManager.Instance == null) return;
            GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsByKill, false);
            Notifier.Show(Strings.NotifyImpostorVictory);
        }
    }
}
