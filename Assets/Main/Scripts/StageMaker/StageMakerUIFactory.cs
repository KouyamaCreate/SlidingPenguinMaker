using UnityEngine;
using UnityEngine.UI;

namespace StageMaker
{
    /// <summary>
    /// 実行時に Unity UGUI のオブジェクトを生成するためのヘルパー。
    /// シーンファイルを最小限に保つため、すべてのUIをコードで構築している。
    /// </summary>
    public static class StageMakerUIFactory
    {
        public static Font defaultFont;

        public static Font GetFont()
        {
            if (defaultFont != null) { return defaultFont; }
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return defaultFont;
        }

        public static GameObject CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        public static RectTransform AddRect(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent.transform, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            rt.localScale = Vector3.one;
            return rt;
        }

        public static Image AddImage(GameObject target, Color color)
        {
            var img = target.GetComponent<Image>();
            if (img == null) { img = target.AddComponent<Image>(); }
            img.color = color;
            img.raycastTarget = true;
            return img;
        }

        public static (GameObject go, Button button, Text label) CreateButton(GameObject parent, string name, string text, Color bg, Color textColor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent.transform, false);
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;

            var img = go.AddComponent<Image>();
            img.color = bg;

            var btn = go.AddComponent<Button>();

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(go.transform, false);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            labelRt.localScale = Vector3.one;
            var labelText = labelGo.AddComponent<Text>();
            labelText.font = GetFont();
            labelText.text = text;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = textColor;
            labelText.fontSize = 22;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;

            return (go, btn, labelText);
        }

        public static Text CreateText(GameObject parent, string name, string text, int fontSize, Color color, TextAnchor align, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent.transform, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            var t = go.AddComponent<Text>();
            t.font = GetFont();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }
    }
}
