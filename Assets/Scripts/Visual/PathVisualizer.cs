using System.Collections.Generic;
using UnityEngine;
using KejarSetoran.Graph;

namespace KejarSetoran.Visual
{
    public class PathVisualizer : MonoBehaviour
    {
        private readonly List<LineRenderer> edgeLines = new List<LineRenderer>();
        private readonly List<GameObject> weightLabels = new List<GameObject>();
        private LineRenderer pathLine;
        private bool overlayVisible = true;
        private float dashOffset;

        private static readonly Color EdgeColor = new Color(0.6f, 0.6f, 0.7f, 0.5f);
        private static readonly Color PathColor = new Color(1f, 0.85f, 0.15f, 1f);

        public void Init(GraphManager graph)
        {
            foreach (var e in graph.Edges)
            {
                var lineGO = new GameObject("Edge_" + e.a.label + "_" + e.b.label);
                lineGO.transform.SetParent(transform, false);
                var lr = lineGO.AddComponent<LineRenderer>();
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = EdgeColor;
                lr.endColor = EdgeColor;
                lr.startWidth = 0.06f;
                lr.endWidth = 0.06f;
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.SetPosition(0, e.a.transform.position);
                lr.SetPosition(1, e.b.transform.position);
                lr.sortingOrder = 1;
                edgeLines.Add(lr);

                // Weight label at midpoint
                var labelGO = new GameObject("W_" + e.a.label + "_" + e.b.label);
                labelGO.transform.SetParent(transform, false);
                Vector3 mid = (e.a.transform.position + e.b.transform.position) * 0.5f;
                Vector3 perp = Vector3.Cross((e.b.transform.position - e.a.transform.position).normalized, Vector3.forward) * 0.18f;
                labelGO.transform.position = mid + perp;
                labelGO.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
                var tm = labelGO.AddComponent<TextMesh>();
                tm.font = SpriteFactory.GetFont();
                tm.text = e.weight.ToString();
                tm.characterSize = 0.1f;
                tm.fontSize = 48;
                tm.alignment = TextAlignment.Center;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = new Color(0.85f, 0.85f, 0.95f, 0.9f);
                var mr = labelGO.GetComponent<MeshRenderer>();
                mr.material = tm.font.material;
                mr.sortingOrder = 2;
                weightLabels.Add(labelGO);
            }

            // Path line
            var pathGO = new GameObject("PathLine");
            pathGO.transform.SetParent(transform, false);
            pathLine = pathGO.AddComponent<LineRenderer>();
            pathLine.material = new Material(Shader.Find("Sprites/Default"));
            pathLine.startColor = PathColor;
            pathLine.endColor = PathColor;
            pathLine.startWidth = 0.16f;
            pathLine.endWidth = 0.16f;
            pathLine.useWorldSpace = true;
            pathLine.positionCount = 0;
            pathLine.sortingOrder = 3;
        }

        public void ShowPath(List<MapNode> path)
        {
            if (pathLine == null) return;
            if (path == null || path.Count < 2)
            {
                pathLine.positionCount = 0;
                return;
            }
            pathLine.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++)
                pathLine.SetPosition(i, path[i].transform.position);
        }

        public void ClearPath()
        {
            if (pathLine != null) pathLine.positionCount = 0;
        }

        public void ToggleOverlay()
        {
            overlayVisible = !overlayVisible;
            foreach (var lr in edgeLines) lr.enabled = overlayVisible;
            foreach (var l in weightLabels) l.SetActive(overlayVisible);
        }

        private void Update()
        {
            if (pathLine != null && pathLine.positionCount > 0)
            {
                dashOffset += Time.deltaTime * 1.5f;
                float pulse = 0.85f + Mathf.Sin(dashOffset * 4f) * 0.15f;
                Color c = PathColor;
                c.r *= pulse; c.g *= pulse; c.b *= pulse;
                pathLine.startColor = c;
                pathLine.endColor = c;
            }
        }
    }
}
