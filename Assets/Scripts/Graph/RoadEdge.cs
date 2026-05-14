namespace KejarSetoran.Graph
{
    public class RoadEdge
    {
        public readonly MapNode a;
        public readonly MapNode b;
        public readonly int weight;

        public RoadEdge(MapNode a, MapNode b, int weight)
        {
            this.a = a;
            this.b = b;
            this.weight = weight;
        }

        public MapNode Other(MapNode from)
        {
            return from == a ? b : a;
        }
    }
}
