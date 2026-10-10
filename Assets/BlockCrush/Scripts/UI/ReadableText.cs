using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI
{
    public static class ReadableText
    {
        public static readonly Color Ink = new Color32(36, 18, 61, 255);
        public static readonly Color ButtonInk = new Color32(16, 33, 20, 255);
        public static void OnLight(Text text)
        {
            text.color = Ink;
            DisableShadow(text);
            var outline = text.GetComponent<Outline>();
            if (outline)
                outline.enabled = false;
        }

        public static void OnButton(Text text)
        {
            text.color = ButtonInk;
            Edge(text, Color.white, 1.5f);
        }

        public static void OnMixed(Text text, Color color)
        {
            text.color = color;
            Edge(text, new Color32(20, 12, 36, 255), 3);
        }

        static void Edge(Text text, Color color, float width)
        {
            DisableShadow(text);
            var outline = text.GetComponent<Outline>();
            if (!outline)
                outline = text.gameObject.AddComponent<Outline>();
            outline.enabled = true;
            outline.effectColor = color;
            outline.effectDistance = new Vector2(width, -width);
            outline.useGraphicAlpha = true;
        }

        static void DisableShadow(Text text)
        {
            foreach (var shadow in text.GetComponents<Shadow>())
                if (!(shadow is Outline))
                    shadow.enabled = false;
        }

        public static void RefreshButtonArtwork(Button button)
        {
            var label = button.transform.Find("Label");
            var image = label ? label.GetComponent<Image>() : null;
            if (image)
                image.color = button.interactable ? ButtonInk : Color.white;
        }
    }
}
