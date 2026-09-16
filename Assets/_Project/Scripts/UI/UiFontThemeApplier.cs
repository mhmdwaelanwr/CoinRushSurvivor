using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRushSurvivor.UI
{
    [DisallowMultipleComponent]
    public sealed class UiFontThemeApplier : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private string titleFontResourcePath = "UI/Fonts/CoinRushTitle";
        [SerializeField] private string bodyFontResourcePath = "UI/Fonts/CoinRushBody";

        [Header("Colors")]
        [SerializeField] private Color titleColor = new Color(0.09f, 0.16f, 0.28f);
        [SerializeField] private Color bodyColor = new Color(0.14f, 0.20f, 0.28f);
        [SerializeField] private Color accentColor = new Color(0.95f, 0.72f, 0.10f);
        [SerializeField] private Color panelColor = new Color(1f, 0.97f, 0.91f, 0.92f);

        [Header("Targets")]
        [SerializeField] private Text[] titleTexts = Array.Empty<Text>();
        [SerializeField] private Text[] bodyTexts = Array.Empty<Text>();
        [SerializeField] private Text[] accentTexts = Array.Empty<Text>();
        [SerializeField] private Graphic[] accentGraphics = Array.Empty<Graphic>();
        [SerializeField] private Graphic[] panelGraphics = Array.Empty<Graphic>();

        private void Awake()
        {
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            var titleFont = string.IsNullOrWhiteSpace(titleFontResourcePath)
                ? null
                : Resources.Load<Font>(titleFontResourcePath);
            var bodyFont = string.IsNullOrWhiteSpace(bodyFontResourcePath)
                ? null
                : Resources.Load<Font>(bodyFontResourcePath);

            ApplyTextGroup(titleTexts, titleFont, titleColor);
            ApplyTextGroup(bodyTexts, bodyFont != null ? bodyFont : titleFont, bodyColor);
            ApplyTextGroup(accentTexts, bodyFont != null ? bodyFont : titleFont, accentColor);
            ApplyGraphicGroup(accentGraphics, accentColor);
            ApplyGraphicGroup(panelGraphics, panelColor);
        }

        private static void ApplyTextGroup(Text[] texts, Font font, Color color)
        {
            for (var i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null)
                {
                    continue;
                }

                if (font != null)
                {
                    texts[i].font = font;
                }

                texts[i].color = color;
            }
        }

        private static void ApplyGraphicGroup(Graphic[] graphics, Color color)
        {
            for (var i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null)
                {
                    continue;
                }

                graphics[i].color = color;
            }
        }
    }
}
