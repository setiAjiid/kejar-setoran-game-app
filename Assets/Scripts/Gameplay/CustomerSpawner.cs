using UnityEngine;
using KejarSetoran.Graph;
using KejarSetoran.Managers;

namespace KejarSetoran.Gameplay
{
    public class CustomerSpawner : MonoBehaviour
    {
        public Customer Current { get; private set; }
        private GameManager game;

        public void Init(GameManager game)
        {
            this.game = game;
        }

        public Customer SpawnRandom(MapNode playerNode)
        {
            if (Current != null) Destroy(Current.gameObject);

            var pickup = GraphManager.Instance.RandomNodeExcept(playerNode);
            var destination = GraphManager.Instance.RandomNodeExcept(playerNode, pickup);

            var go = new GameObject("Customer");
            go.transform.SetParent(transform, false);
            var c = go.AddComponent<Customer>();

            // Compute Dijkstra-based delivery budget: cost * 4s per unit, min 20s
            var dr = DijkstraAlgorithm.FindShortestPath(pickup, destination, GraphManager.Instance.Nodes);
            float budget = Mathf.Max(20f, dr.cost * 4f);

            c.Init(pickup, destination, budget);
            c.fareDistance = dr.cost;
            Current = c;
            return c;
        }

        public void Clear()
        {
            if (Current != null) Destroy(Current.gameObject);
            Current = null;
        }

        private void Update()
        {
            if (game == null || game.State != GameState.Playing) return;
            if (Current == null) return;
            if (Current.state == CustomerState.Waiting && Current.PickupTimeLeft() <= 0f)
            {
                Current.MarkTimedOut();
                game.OnCustomerTimedOut(Current);
            }
        }
    }
}
