using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KejarSetoran.Graph;

namespace KejarSetoran.Visual
{
    public static class SpriteFactory
    {
        private static Sprite circleSprite;
        private static Sprite squareSprite;
        private static Sprite ringSprite;
        private static Font cachedFont;
        private static readonly Dictionary<string, Sprite> roundedCache = new Dictionary<string, Sprite>();

        public static Font GetFont()
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return cachedFont;
        }

        public static Sprite RoundedRect(int radius)
        {
            string key = "rr_" + radius;
            if (roundedCache.TryGetValue(key, out var s)) return s;
            s = MakeRoundedRect(radius * 3, radius * 3, radius, Color.white);
            roundedCache[key] = s;
            return s;
        }

        public static Sprite SoftShadow(int radius)
        {
            string key = "shadow_" + radius;
            if (roundedCache.TryGetValue(key, out var s)) return s;
            s = MakeShadow(radius * 3, radius * 3, radius);
            roundedCache[key] = s;
            return s;
        }

        public static Sprite MakeRoundedRect(int width, int height, int radius, Color color)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int cx = -1, cy = -1;
                    bool inCorner = false;
                    if (x < radius && y < radius) { cx = radius; cy = radius; inCorner = true; }
                    else if (x >= width - radius && y < radius) { cx = width - radius - 1; cy = radius; inCorner = true; }
                    else if (x < radius && y >= height - radius) { cx = radius; cy = height - radius - 1; inCorner = true; }
                    else if (x >= width - radius && y >= height - radius) { cx = width - radius - 1; cy = height - radius - 1; inCorner = true; }

                    if (inCorner)
                    {
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        if (d > radius) tex.SetPixel(x, y, clear);
                        else if (d > radius - 1f)
                        {
                            float a = radius - d;
                            tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
                        }
                        else tex.SetPixel(x, y, color);
                    }
                    else tex.SetPixel(x, y, color);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        public static Sprite MakeShadow(int width, int height, int radius)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(radius - x, x - (width - radius - 1)));
                    float dy = Mathf.Max(0, Mathf.Max(radius - y, y - (height - radius - 1)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - dist / radius) * 0.5f;
                    tex.SetPixel(x, y, new Color(0, 0, 0, alpha));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        public static Sprite VerticalGradient(int height, Color top, Color bottom)
        {
            int width = 8;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                Color c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < width; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite Circle()
        {
            if (circleSprite == null) circleSprite = MakeCircle(64, 30, Color.white);
            return circleSprite;
        }

        public static Sprite Square()
        {
            if (squareSprite == null) squareSprite = MakeSquare(64, Color.white);
            return squareSprite;
        }

        public static Sprite Ring()
        {
            if (ringSprite == null) ringSprite = MakeRing(96, 46, 38, Color.white);
            return ringSprite;
        }

        public static Sprite MakeCircle(int size, int radius, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var clear = new Color(0, 0, 0, 0);
            var cx = size / 2f;
            var cy = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    tex.SetPixel(x, y, d <= radius ? color : clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        public static Sprite MakeRing(int size, int outerR, int innerR, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var clear = new Color(0, 0, 0, 0);
            var cx = size / 2f;
            var cy = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    tex.SetPixel(x, y, (d <= outerR && d >= innerR) ? color : clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        public static Sprite MakeSquare(int size, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, color);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        public static MapNode CreateNodeGO(string label, Vector3 pos, Transform parent)
        {
            var go = new GameObject("Node_" + label);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var body = go.AddComponent<SpriteRenderer>();
            body.sprite = Circle();
            body.color = new Color(0.18f, 0.22f, 0.30f, 1f);
            body.sortingOrder = 5;
            go.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            var ringGO = new GameObject("Ring");
            ringGO.transform.SetParent(go.transform, false);
            ringGO.transform.localPosition = Vector3.zero;
            ringGO.transform.localScale = Vector3.one;
            var ringSr = ringGO.AddComponent<SpriteRenderer>();
            ringSr.sprite = Ring();
            ringSr.color = new Color(1f, 1f, 1f, 0.25f);
            ringSr.sortingOrder = 4;

            // Label - world space TextMesh
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            labelGO.transform.localPosition = new Vector3(0f, 0f, 0f);
            labelGO.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
            var tm = labelGO.AddComponent<TextMesh>();
            tm.font = GetFont();
            tm.text = label;
            tm.characterSize = 0.12f;
            tm.fontSize = 64;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.white;
            tm.fontStyle = FontStyle.Bold;
            var mr = labelGO.GetComponent<MeshRenderer>();
            mr.material = tm.font.material;
            mr.sortingOrder = 6;

            var node = go.AddComponent<MapNode>();
            return node;
        }

        public static GameObject CreatePlayer(Transform parent)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Circle();
            sr.color = new Color(0.20f, 0.75f, 1f, 1f);
            sr.sortingOrder = 20;
            go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            var dotGO = new GameObject("Dot");
            dotGO.transform.SetParent(go.transform, false);
            dotGO.transform.localPosition = new Vector3(0.18f, 0.18f, 0f);
            dotGO.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            var dot = dotGO.AddComponent<SpriteRenderer>();
            dot.sprite = Circle();
            dot.color = Color.white;
            dot.sortingOrder = 21;

            return go;
        }

        public static GameObject CreateCustomerMarker(Transform parent, Color c)
        {
            var go = new GameObject("CustomerMarker");
            go.transform.SetParent(parent, false);

            // Pin body
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Circle();
            sr.color = c;
            sr.sortingOrder = 15;
            go.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

            var dotGO = new GameObject("Highlight");
            dotGO.transform.SetParent(go.transform, false);
            dotGO.transform.localPosition = new Vector3(0f, 0f, 0f);
            dotGO.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
            var dot = dotGO.AddComponent<SpriteRenderer>();
            dot.sprite = Circle();
            dot.color = Color.white;
            dot.sortingOrder = 16;

            return go;
        }
    }
}
