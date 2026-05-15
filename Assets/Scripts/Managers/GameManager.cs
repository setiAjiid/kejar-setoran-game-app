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

        [Header("Hint & Penalty")]
        public int hintCostRp = 200;
        public float wrongMovePenaltySeconds = 3f;
        public float hintDurationSeconds = 2f;

        public int Money { get; private set; }
        public int Delivered { get; private set; }
        public float TimeRemaining { get; private set; }

        private PlayerController player;
        private CustomerSpawner spawner;
        private PathVisualizer pathVis;
        private float customerStartTime;

        // Dijkstra path is cached but hidden by default. Player must spend Rp
        // (tap H) to peek for a few seconds. node[0] = player's current/next
        // position, node[last] = current goal (pickup or delivery).
        private List<MapNode> currentOptimalPath;
        private int currentOptimalCost;
        private float hintTimeRemaining;

        public System.Action<MapNode> OnCustomerSpawned;

        public void Bind(PlayerController p, CustomerSpawner s, PathVisualizer v)
        {
            player = p; spawner = s; pathVis = v;
            player.OnArriveAtNode += HandleArrive;
            player.OnDepartTowards += HandleDepart;
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

            if (Input.GetKeyDown(KeyCode.H) && State == GameState.Playing)
            {
                RevealHint();
            }

            if (State != GameState.Playing) return;

            if (hintTimeRemaining > 0f)
            {
                hintTimeRemaining -= Time.deltaTime;
                if (hintTimeRemaining <= 0f) ClearHintReveal();
            }

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
                ComputeAndStoreOptimalPath(player.CurrentNode, c.destinationNode);
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
            ComputeAndStoreOptimalPath(player.CurrentNode, c.pickupNode);
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
            // No-op by design: path trim happens in HandleDepart on correct
            // moves, and wrong moves recompute there too. Nothing to reveal
            // here — the player must press H to peek at Dijkstra.
        }

        private MapNode GetCurrentGoal()
        {
            var c = spawner.Current;
            if (c == null) return null;
            return c.state == CustomerState.Waiting ? c.pickupNode : c.destinationNode;
        }

        private void ComputeAndStoreOptimalPath(MapNode from, MapNode to)
        {
            if (from == null || to == null)
            {
                currentOptimalPath = null;
                currentOptimalCost = 0;
                return;
            }

            var result = DijkstraAlgorithm.FindShortestPath(from, to, GraphManager.Instance.Nodes);
            currentOptimalPath = result.found ? result.path : null;
            currentOptimalCost = result.cost;

            // Hide algorithm output by default. Spatial markers (current/goal)
            // stay visible so the player still knows where to go.
            foreach (var n in GraphManager.Instance.Nodes) n.ResetHighlight();
            from.SetHighlight(new Color(0.3f, 0.95f, 0.4f, 0.95f));
            to.SetHighlight(new Color(0.95f, 0.3f, 0.3f, 0.95f));
            pathVis.ClearPath();
            HUDManager.Instance.UpdatePathInfo("");
        }

        private void RevealHint()
        {
            if (currentOptimalPath == null || currentOptimalPath.Count == 0)
            {
                HUDManager.Instance.FlashStatus("Tidak ada path untuk di-hint");
                return;
            }
            if (Money < hintCostRp)
            {
                HUDManager.Instance.FlashStatus($"Uang kurang untuk hint (Rp {hintCostRp:N0})");
                return;
            }

            Money -= hintCostRp;
            HUDManager.Instance.UpdateMoney(Money);
            HUDManager.Instance.FlashStatus($"Hint! -Rp {hintCostRp:N0}");
            AudioManager.Instance.PlayPickup();

            RenderHintReveal();
            hintTimeRemaining = hintDurationSeconds;
        }

        private void RenderHintReveal()
        {
            if (currentOptimalPath == null || currentOptimalPath.Count == 0) return;

            pathVis.ShowPath(currentOptimalPath);
            foreach (var n in GraphManager.Instance.Nodes) n.ResetHighlight();
            foreach (var pn in currentOptimalPath)
                pn.SetHighlight(new Color(1f, 0.85f, 0.2f, 0.9f));
            currentOptimalPath[0].SetHighlight(new Color(0.3f, 0.95f, 0.4f, 0.95f));
            if (currentOptimalPath.Count > 1)
                currentOptimalPath[currentOptimalPath.Count - 1].SetHighlight(new Color(0.95f, 0.3f, 0.3f, 0.95f));
            HUDManager.Instance.UpdatePathInfo(BuildPathString(currentOptimalPath, currentOptimalCost));
        }

        private void ClearHintReveal()
        {
            pathVis.ClearPath();
            HUDManager.Instance.UpdatePathInfo("");
            foreach (var n in GraphManager.Instance.Nodes) n.ResetHighlight();
            if (currentOptimalPath != null && currentOptimalPath.Count > 0)
                currentOptimalPath[0].SetHighlight(new Color(0.3f, 0.95f, 0.4f, 0.95f));
            var goal = GetCurrentGoal();
            if (goal != null) goal.SetHighlight(new Color(0.95f, 0.3f, 0.3f, 0.95f));
        }

        private void HandleDepart(MapNode from, MapNode to)
        {
            if (State != GameState.Playing) return;

            if (currentOptimalPath == null || currentOptimalPath.Count < 2)
            {
                var goal = GetCurrentGoal();
                if (goal != null) ComputeAndStoreOptimalPath(to, goal);
                return;
            }

            if (currentOptimalPath[1] == to)
            {
                // Correct first edge — consume the source node from the path.
                currentOptimalPath.RemoveAt(0);
                if (hintTimeRemaining > 0f) RenderHintReveal();
            }
            else
            {
                // Wrong edge — penalty + recompute Dijkstra from the new node.
                TimeRemaining -= wrongMovePenaltySeconds;
                HUDManager.Instance.FlashStatus($"Salah jalan! -{wrongMovePenaltySeconds:0}s");
                AudioManager.Instance.PlayTimeout();

                var goal = GetCurrentGoal();
                if (goal != null) ComputeAndStoreOptimalPath(to, goal);
                if (hintTimeRemaining > 0f)
                {
                    hintTimeRemaining = 0f;
                    ClearHintReveal();
                }
            }
        }

        private string BuildPathString(List<MapNode> path, int totalCost)
        {
            if (path == null || path.Count == 0) return "";
            var sb = new StringBuilder();
            sb.Append("Path: ");
            for (int i = 0; i < path.Count; i++)
            {
                sb.Append(path[i].label);
                if (i < path.Count - 1) sb.Append(" -> ");
            }
            sb.Append("  |  Dist: ").Append(totalCost);
            return sb.ToString();
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
