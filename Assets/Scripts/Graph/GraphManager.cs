using System.Collections.Generic;
using UnityEngine;
using KejarSetoran.Visual;

namespace KejarSetoran.Graph
{
    public class GraphManager : MonoBehaviour
    {
        public static GraphManager Instance { get; private set; }

        public List<MapNode> Nodes { get; private set; } = new List<MapNode>();
        public List<RoadEdge> Edges { get; private set; } = new List<RoadEdge>();

        private const float ColSpacing = 2.4f;
        private const float RowSpacing = 1.8f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void BuildDefaultMap(Transform parent)
        {
            ClearGraph();

            // Layout grid (col, row): col 0..4, row 0..3 (row 0 = top)
            // A B C D
            // E F G H
            // I J K L
            //   M N O P
            var positions = new Dictionary<string, Vector2>
            {
                {"A", Grid(0, 0)}, {"B", Grid(1, 0)}, {"C", Grid(2, 0)}, {"D", Grid(3, 0)},
                {"E", Grid(0, 1)}, {"F", Grid(1, 1)}, {"G", Grid(2, 1)}, {"H", Grid(3, 1)},
                {"I", Grid(0, 2)}, {"J", Grid(1, 2)}, {"K", Grid(2, 2)}, {"L", Grid(3, 2)},
                {"M", Grid(1, 3)}, {"N", Grid(2, 3)}, {"O", Grid(3, 3)}, {"P", Grid(4, 3)},
            };

            var nodeByLabel = new Dictionary<string, MapNode>();
            int id = 0;
            foreach (var kv in positions)
            {
                var node = SpriteFactory.CreateNodeGO(kv.Key, kv.Value, parent);
                node.Init(id++, kv.Key, kv.Value, node.GetComponent<SpriteRenderer>(), node.transform.GetChild(0).GetComponent<SpriteRenderer>());
                node.ResetHighlight();
                Nodes.Add(node);
                nodeByLabel[kv.Key] = node;
            }

            // Edges from GDD section 5.2
            var edgeData = new (string, string, int)[]
            {
                ("A","B",4), ("B","C",3), ("C","D",5),
                ("A","E",2), ("B","F",6), ("C","G",4), ("D","H",3),
                ("E","F",5), ("F","G",2), ("G","H",7),
                ("E","I",3), ("F","J",4), ("G","K",5), ("H","L",2),
                ("I","J",6), ("J","K",3), ("K","L",4),
                ("J","M",5), ("K","N",3),
                ("M","N",4), ("N","O",6), ("O","P",2),
            };

            foreach (var (a, b, w) in edgeData)
            {
                var edge = new RoadEdge(nodeByLabel[a], nodeByLabel[b], w);
                nodeByLabel[a].edges.Add(edge);
                nodeByLabel[b].edges.Add(edge);
                Edges.Add(edge);
            }
        }

        private Vector2 Grid(int col, int row)
        {
            float x = (col - 2f) * ColSpacing;
            float y = (1.5f - row) * RowSpacing;
            return new Vector2(x, y);
        }

        public MapNode GetNodeByLabel(string label)
        {
            foreach (var n in Nodes) if (n.label == label) return n;
            return null;
        }

        public MapNode RandomNodeExcept(params MapNode[] excluded)
        {
            var pool = new List<MapNode>(Nodes);
            foreach (var x in excluded) if (x != null) pool.Remove(x);
            if (pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }

        public MapNode NearestNeighborInDirection(MapNode from, Vector2 dir)
        {
            MapNode best = null;
            float bestScore = 0.3f; // minimum alignment threshold
            foreach (var e in from.edges)
            {
                var other = e.Other(from);
                Vector2 delta = ((Vector2)other.transform.position - (Vector2)from.transform.position).normalized;
                float dot = Vector2.Dot(delta, dir);
                if (dot > bestScore)
                {
                    bestScore = dot;
                    best = other;
                }
            }
            return best;
        }

        public int GetEdgeWeight(MapNode a, MapNode b)
        {
            foreach (var e in a.edges) if (e.Other(a) == b) return e.weight;
            return 0;
        }

        private void ClearGraph()
        {
            foreach (var n in Nodes) if (n != null) Destroy(n.gameObject);
            Nodes.Clear();
            Edges.Clear();
        }
    }
}
