using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Bootstrap.Editor.Builder
{
    /// <summary>
    /// Small helpers for building uGUI hierarchies from code in the editor builder: one canvas unit equals one CSS
    /// pixel of the prototype (reference resolution 390×844).
    /// </summary>
    internal static class UiFactory
    {
        public static TMP_FontAsset Body;
        public static TMP_FontAsset Display;

        public static readonly Color Ink = Hex("#3B2A1B");
        public static readonly Color Ink2 = Hex("#7A6049");
        public static readonly Color Cream = Hex("#FFF8EA");
        public static readonly Color Walnut = Hex("#5E3C20");
        public static readonly Color Berry = Hex("#C63D4E");
        public static readonly Color Leaf = Hex("#5F9A46");
        public static readonly Color Gold = Hex("#EDB53F");
        public static readonly Color OakD = Hex("#96622F");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        public static Sprite Kit(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Game/Services/UI/Art/Kit/{name}.png");

        public static Sprite Icon(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Game/Services/UI/Art/Icons/{name}.png");

        public static Sprite Art(string path) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Game/Features/Gameplay/Art/{path}.png");

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform At(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Top(this RectTransform rt, float y, Vector2 size) =>
            rt.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -y), size);

        public static RectTransform Center(this RectTransform rt, Vector2 pos, Vector2 size) =>
            rt.At(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

        public static Image Img(string name, Transform parent, Sprite sprite, Color? color = null, bool raycast = false)
        {
            var rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color ?? Color.white;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            return img;
        }

        public static Image Tiled(string name, Transform parent, Sprite sprite)
        {
            var img = Img(name, parent, sprite);
            img.type = Image.Type.Tiled;
            return img;
        }

        public static Image Fit(string name, Transform parent, Sprite sprite)
        {
            var img = Img(name, parent, sprite);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool display = false)
        {
            var rt = Node(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = display ? Display : Body;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        public static Button Button(string name, Transform parent, Sprite sprite, Color? color = null)
        {
            var img = Img(name, parent, sprite, color, true);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
            b.colors = colors;
            return b;
        }

        public static VerticalLayoutGroup Vertical(RectTransform rt, float spacing, RectOffset padding = null,
            TextAnchor align = TextAnchor.UpperCenter)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding ?? new RectOffset();
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup Horizontal(RectTransform rt, float spacing, TextAnchor align = TextAnchor.MiddleCenter,
            bool expand = false)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            // Always control widths so LayoutElement.preferredWidth applies; expand only stretches the children.
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = expand;
            h.childForceExpandHeight = false;
            return h;
        }

        public static LayoutElement Layout(Component c, float height = -1, float width = -1, float flexW = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (height >= 0) le.preferredHeight = height;
            if (width >= 0) le.preferredWidth = width;
            if (flexW >= 0) le.flexibleWidth = flexW;
            return le;
        }

        public static ContentSizeFitter FitHeight(Component c)
        {
            var f = c.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return f;
        }

        /// <summary>
        /// Assigns serialized fields (private [SerializeField] included) by name; arrays take an object[].
        /// </summary>
        public static void Wire(Object target, params (string Field, object Value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in values)
            {
                var p = so.FindProperty(field);
                if (p == null)
                {
                    Debug.LogError($"[Builder] {target.GetType().Name} has no field {field}");
                    continue;
                }

                if (value is IList<Object> list)
                {
                    p.arraySize = list.Count;
                    for (var i = 0; i < list.Count; i++)
                    {
                        p.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
                    }
                }
                else if (value is Object o || value == null)
                {
                    p.objectReferenceValue = (Object)value;
                }
                else if (value is float f)
                {
                    p.floatValue = f;
                }
                else if (value is Color c)
                {
                    p.colorValue = c;
                }
                else if (value is bool b)
                {
                    p.boolValue = b;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
