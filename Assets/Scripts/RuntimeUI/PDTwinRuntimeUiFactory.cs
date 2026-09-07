using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PDTwin.RuntimeUI
{
    public static class PDTwinRuntimeUiFactory
    {
        private static Sprite roundedSprite;
        private static Sprite circleSprite;

        public static GameObject CreateRect(string name, Transform parent, Color color, bool rounded = false)
        {
            GameObject value = new GameObject(name, typeof(RectTransform), typeof(Image));
            value.transform.SetParent(parent, false);
            Image image = value.GetComponent<Image>();
            image.color = color;
            if (rounded)
            {
                image.sprite = RoundedSprite;
                image.type = Image.Type.Sliced;
            }
            return value;
        }

        public static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string value,
            float size,
            Color color,
            FontStyles style = FontStyles.Normal,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.text = value ?? string.Empty;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Color background,
            Color foreground,
            float fontSize = 17f)
        {
            GameObject root = CreateRect(name, parent, background, true);
            Button button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.5f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TextMeshProUGUI text = CreateText("Label", root.transform, label, fontSize, foreground, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            return button;
        }

        public static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        public static void AnchorTop(RectTransform rect, float height, float left = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        public static void AnchorBottom(RectTransform rect, float height, float left = 0f, float right = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }

        public static void SetCircle(Image image)
        {
            if (image == null)
                return;
            image.sprite = CircleSprite;
            image.type = Image.Type.Simple;
        }

        private static Sprite RoundedSprite => roundedSprite != null ? roundedSprite : roundedSprite = BuildRoundedSprite(64, 15, 18f);
        private static Sprite CircleSprite => circleSprite != null ? circleSprite : circleSprite = BuildRoundedSprite(64, 31, 0f);

        private static Sprite BuildRoundedSprite(int size, int radius, float border)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PDTwin_Runtime_Rounded",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32 clear = new Color32(255, 255, 255, 0);
            Color32 solid = new Color32(255, 255, 255, 255);
            Color32[] pixels = new Color32[size * size];
            float edge = radius - 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = x < radius ? radius - x : x >= size - radius ? x - (size - radius - 1) : 0f;
                    float cy = y < radius ? radius - y : y >= size - radius ? y - (size - radius - 1) : 0f;
                    float distance = Mathf.Sqrt(cx * cx + cy * cy);
                    pixels[y * size + x] = distance <= edge || cx == 0f || cy == 0f ? solid : clear;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }
    }
}
