using UnityEngine;
using UnityEngine.SceneManagement;
using KejarSetoran.Graph;
using KejarSetoran.Gameplay;
using KejarSetoran.Managers;
using KejarSetoran.Visual;

namespace KejarSetoran.Bootstrap
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            // Avoid double bootstrap (e.g. if scene reloads)
            if (Object.FindFirstObjectByType<GameManager>() != null) return;

            // === Camera ===
            var cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                cam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.14f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // === Root holder ===
            var rootGO = new GameObject("KejarSetoran_Root");

            // Graph
            var graphGO = new GameObject("GraphManager");
            graphGO.transform.SetParent(rootGO.transform, false);
            var graph = graphGO.AddComponent<GraphManager>();

            var nodesParent = new GameObject("Nodes");
            nodesParent.transform.SetParent(rootGO.transform, false);
            graph.BuildDefaultMap(nodesParent.transform);

            // Path visualizer
            var pvGO = new GameObject("PathVisualizer");
            pvGO.transform.SetParent(rootGO.transform, false);
            var pv = pvGO.AddComponent<PathVisualizer>();
            pv.Init(graph);

            // Player
            var startNode = graph.GetNodeByLabel("F") ?? graph.Nodes[0];
            var playerGO = SpriteFactory.CreatePlayer(rootGO.transform);
            var player = playerGO.AddComponent<PlayerController>();

            // Customer spawner
            var spawnerGO = new GameObject("CustomerSpawner");
            spawnerGO.transform.SetParent(rootGO.transform, false);
            var spawner = spawnerGO.AddComponent<CustomerSpawner>();

            // Audio
            var audioGO = new GameObject("AudioManager");
            audioGO.transform.SetParent(rootGO.transform, false);
            audioGO.AddComponent<AudioManager>();

            // HUD
            var hudGO = new GameObject("HUDManager");
            hudGO.transform.SetParent(rootGO.transform, false);
            hudGO.AddComponent<HUDManager>();

            // Game manager
            var gmGO = new GameObject("GameManager");
            gmGO.transform.SetParent(rootGO.transform, false);
            var gm = gmGO.AddComponent<GameManager>();

            // Wire-up: order matters — HUDManager Awake builds canvas before bind
            player.Init(startNode, gm);
            spawner.Init(gm);
            gm.Bind(player, spawner, pv);
        }
    }
}
