using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KejarSetoran.Graph;
using KejarSetoran.Gameplay;
using KejarSetoran.Visual;

namespace KejarSetoran.Managers
{
    public enum GameState { MainMenu, Playing, Paused, GameOver }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.MainMenu;

        [Header("Tuning")]
        public int dailyTarget = 50000;
        public float dailyDurationSeconds = 300f; // 5 minutes
        public int basePerUnit = 1000;
        public int divisor = 5;
        public int customerTimeoutPenalty = 500;

        public int Money { get; private set; }
        public int Delivered { get; private set; }
        public float TimeRemaining { get; private set; }

        private PlayerController player;
        private CustomerSpawner spawner;
        private PathVisualizer pathVis;
        private float customerStartTime;

        public System.Action<MapNode> OnCustomerSpawned;

        public void Bind(PlayerController p, CustomerSpawner s, PathVisualizer v)
        {
            player = p; spawner = s; pathVis = v;
            player.OnArriveAtNode += HandleArrive;
            HUDManager.Instance.UpdateTarget(dailyTarget);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartGame()
        {
            Money = 0;
            Delivered = 0;
            TimeRemaining = dailyDurationSeconds;
            State = GameState.Playing;
            Time.timeScale = 1f;

            // Reset player to a starting node (F - center)
            var startNode = GraphManager.Instance.GetNodeByLabel("F") ?? GraphManager.Instance.Nodes[0];
            player.Init(startNode, this);

            spawner.Clear();
            pathVis.ClearPath();
            foreach (var n in GraphManager.Instance.Nodes) n.ResetHighlight();

            HUDManager.Instance.ShowGameHud();
            HUDManager.Instance.UpdateMoney(Money);
            HUDManager.Instance.UpdateTarget(dailyTarget);
            HUDManager.Instance.UpdateTimer(TimeRemaining, dailyDurationSeconds);
            HUDManager.Instance.UpdateCustomerInfo("Spawning customer...");
            HUDManager.Instance.UpdatePathInfo("");

            SpawnNewCustomer();
        }

        public void GoToMainMenu()
        {
            State = GameState.MainMenu;
            Time.timeScale = 1f;
            spawner.Clear();
            pathVis.ClearPath();
            foreach (var n in GraphManager.Instance.Nodes) n.ResetHighlight();
            HUDManager.Instance.ShowMainMenu();
        }

        private void Update()
        {
            if (State == GameState.MainMenu) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == GameState.Playing) Pause();
                else if (State == GameState.Paused) Resume();
            }

            if (Input.GetKeyDown(KeyCode.Tab) && State == GameState.Playing)
            {
                pathVis.ToggleOverlay();
            }

            if (State != GameState.Playing) return;

            TimeRemaining -= Time.deltaTime;
            HUDManager.Instance.UpdateTimer(Mathf.Max(0, TimeRemaining), dailyDurationSeconds);

            // Customer info refresh
            var c = spawner.Current;
            if (c != null)
            {
                if (c.state == CustomerState.Waiting)
                {
                    float left = c.PickupTimeLeft();
                    HUDManager.Instance.UpdateCustomerInfo($"Pickup: {c.pickupNode.label}  ->  Drop: {c.destinationNode.label}\nPickup timeout: {left:0.0}s");
                }
                else if (c.state == CustomerState.OnBoard)
                {
                    float elapsed = Time.time - customerStartTime;
                    float left = Mathf.Max(0f, c.deliveryTimeBudget - elapsed);
                    HUDManager.Instance.UpdateCustomerInfo($"ON BOARD  ->  Drop at: {c.destinationNode.label}\nBonus window: {left:0.0}s / {c.deliveryTimeBudget:0.0}s");
                }
            }

            // Pickup / dropoff
            if (Input.GetKeyDown(KeyCode.Space) && !player.IsMoving)
            {
                TryInteract();
            }

            if (TimeRemaining <= 0f)
            {
                EndGame(Money >= dailyTarget);
            }
            else if (Money < 0)
            {
                EndGame(false);
            }
        }

        public void Pause()
        {
            State = GameState.Paused;
            Time.timeScale = 0f;
            HUDManager.Instance.SetPaused(true);
        }
        public void Resume()
        {
            State = GameState.Playing;
            Time.timeScale = 1f;
            HUDManager.Instance.SetPaused(false);
        }

        private void TryInteract()
        {
            var c = spawner.Current;
            if (c == null) return;
            if (c.state == CustomerState.Waiting && player.CurrentNode == c.pickupNode)
            {
                c.MarkOnBoard();
                customerStartTime = Time.time;
                AudioManager.Instance.PlayPickup();
                HUDManager.Instance.FlashStatus("Penumpang diangkut!");
                RecomputePath(player.CurrentNode, c.destinationNode);
            }
            else if (c.state == CustomerState.OnBoard && player.CurrentNode == c.destinationNode)
            {
                int payout = CalculatePayout(c);
                Money += payout;
                Delivered++;
                AudioManager.Instance.PlayDropoff();
                HUDManager.Instance.UpdateMoney(Money);
                HUDManager.Instance.FlashStatus($"+Rp {payout:N0}");
                c.MarkDelivered();
                Destroy(c.gameObject);

                if (Money >= dailyTarget)
                {
                    EndGame(true);
                    return;
                }
                SpawnNewCustomer();
            }
        }

        private int CalculatePayout(Customer c)
        {
            int baseFare = Mathf.CeilToInt(basePerUnit * ((float)c.fareDistance / divisor));
            float elapsed = Time.time - customerStartTime;
            float ratio = elapsed / c.deliveryTimeBudget;
            float mult = 1f;
            if (ratio < 0.5f) mult = 1.2f; // time bonus
            else if (ratio > 1f + (5f / c.deliveryTimeBudget)) mult = 0.5f; // late penalty (5s grace)
            return Mathf.Max(0, Mathf.CeilToInt(baseFare * mult));
        }

        private void SpawnNewCustomer()
        {
            var c = spawner.SpawnRandom(player.CurrentNode);
            OnCustomerSpawned?.Invoke(c.pickupNode);
            RecomputePath(player.CurrentNode, c.pickupNode);
        }

        public void OnCustomerTimedOut(Customer c)
        {
            Money -= customerTimeoutPenalty;
            HUDManager.Instance.UpdateMoney(Money);
            HUDManager.Instance.FlashStatus($"Penumpang kabur! -Rp {customerTimeoutPenalty:N0}");
            AudioManager.Instance.PlayTimeout();
            Destroy(c.gameObject);

            if (Money < 0) { EndGame(false); return; }
            SpawnNewCustomer();
        }

        private void HandleArrive(MapNode n)
        {
            var c = spawner.Current;
            if (c == null) return;
            MapNode goal = c.state == CustomerState.Waiting ? c.pickupNode : c.destinationNode;
            if (goal == null) return;
            RecomputePath(n, goal);
        }

        private void RecomputePath(MapNode from, MapNode to)
        {
            var result = DijkstraAlgorithm.FindShortestPath(from, to, GraphManager.Instance.Nodes);
            pathVis.ShowPath(result.path);
            foreach (var n in GraphManager.Instance.Nodes) n.ResetHighlight();
            if (result.path != null)
            {
                foreach (var pn in result.path) pn.SetHighlight(new Color(1f, 0.85f, 0.2f, 0.9f));
            }
            from.SetHighlight(new Color(0.3f, 0.95f, 0.4f, 0.95f));
            to.SetHighlight(new Color(0.95f, 0.3f, 0.3f, 0.95f));

            var sb = new StringBuilder();
            sb.Append("Path: ");
            for (int i = 0; i < result.path.Count; i++)
            {
                sb.Append(result.path[i].label);
                if (i < result.path.Count - 1) sb.Append(" -> ");
            }
            sb.Append("  |  Dist: ").Append(result.cost);
            HUDManager.Instance.UpdatePathInfo(sb.ToString());
        }

        private void EndGame(bool won)
        {
            State = GameState.GameOver;
            spawner.Clear();
            pathVis.ClearPath();
            foreach (var n in GraphManager.Instance.Nodes) n.ResetHighlight();
            if (won) AudioManager.Instance.PlayWin(); else AudioManager.Instance.PlayGameOver();
            HUDManager.Instance.ShowGameOver(won, Money, dailyTarget, Delivered);
        }
    }
}
