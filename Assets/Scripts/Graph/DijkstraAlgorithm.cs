using System.Collections.Generic;

namespace KejarSetoran.Graph
{
    public struct DijkstraResult
    {
        public List<MapNode> path;
        public int cost;
        public List<MapNode> visitedOrder;
        public bool found;
    }

    public static class DijkstraAlgorithm
    {
        public static DijkstraResult FindShortestPath(MapNode source, MapNode target, IReadOnlyList<MapNode> allNodes)
        {
            var dist = new Dictionary<MapNode, int>(allNodes.Count);
            var prev = new Dictionary<MapNode, MapNode>(allNodes.Count);
            var visitedOrder = new List<MapNode>(allNodes.Count);
            var settled = new HashSet<MapNode>();

            foreach (var n in allNodes)
            {
                dist[n] = int.MaxValue;
                prev[n] = null;
            }
            dist[source] = 0;

            var pq = new SortedSet<(int cost, int id, MapNode node)>(Comparer<(int, int, MapNode)>.Create((x, y) =>
            {
                int c = x.Item1.CompareTo(y.Item1);
                if (c != 0) return c;
                return x.Item2.CompareTo(y.Item2);
            }));
            pq.Add((0, source.id, source));

            while (pq.Count > 0)
            {
                var top = pq.Min;
                pq.Remove(top);
                MapNode u = top.node;
                if (settled.Contains(u)) continue;
                settled.Add(u);
                visitedOrder.Add(u);

                if (u == target) break;

                foreach (var edge in u.edges)
                {
                    MapNode v = edge.Other(u);
                    if (settled.Contains(v)) continue;
                    int alt = dist[u] + edge.weight;
                    if (alt < dist[v])
                    {
                        dist[v] = alt;
                        prev[v] = u;
                        pq.Add((alt, v.id, v));
                    }
                }
            }

            var result = new DijkstraResult
            {
                path = new List<MapNode>(),
                cost = 0,
                visitedOrder = visitedOrder,
                found = false
            };

            if (dist[target] == int.MaxValue) return result;

            result.cost = dist[target];
            result.found = true;
            var stack = new Stack<MapNode>();
            var cur = target;
            while (cur != null)
            {
                stack.Push(cur);
                cur = prev[cur];
            }
            while (stack.Count > 0) result.path.Add(stack.Pop());
            return result;
        }

        public static MapNode NearestNodeBFS(MapNode start, System.Func<MapNode, bool> predicate)
        {
            if (start == null) return null;
            if (predicate(start)) return start;
            var visited = new HashSet<MapNode> { start };
            var queue = new Queue<MapNode>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var u = queue.Dequeue();
                foreach (var e in u.edges)
                {
                    var v = e.Other(u);
                    if (visited.Contains(v)) continue;
                    visited.Add(v);
                    if (predicate(v)) return v;
                    queue.Enqueue(v);
                }
            }
            return null;
        }
    }
}
