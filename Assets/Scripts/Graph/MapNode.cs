using System.Collections.Generic;
using UnityEngine;

namespace KejarSetoran.Graph
{
    public class MapNode : MonoBehaviour
    {
        public int id;
        public string label;
        public readonly List<RoadEdge> edges = new List<RoadEdge>();

        private SpriteRenderer body;
        private SpriteRenderer ring;
        private Color baseColor;

        public Vector2 Position2D => transform.position;

        public void Init(int id, string label, Vector3 pos, SpriteRenderer body, SpriteRenderer ring)
        {
            this.id = id;
            this.label = label;
            transform.position = pos;
            this.body = body;
            this.ring = ring;
            baseColor = body.color;
        }

        public void SetHighlight(Color color)
        {
            if (ring != null) ring.color = color;
        }

        public void ResetHighlight()
        {
            if (ring != null) ring.color = new Color(1f, 1f, 1f, 0.25f);
        }
    }
}
