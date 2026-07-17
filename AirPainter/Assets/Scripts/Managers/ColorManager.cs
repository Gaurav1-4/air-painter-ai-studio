using System;
using System.Collections.Generic;
using UnityEngine;

namespace AirPainter.Managers
{
    [Serializable]
    public class ColorPalette
    {
        public string paletteName;
        public Color[] colors;
    }

    /// <summary>
    /// Manages primary/secondary colors, history, and palettes.
    /// </summary>
    public class ColorManager : MonoBehaviour
    {
        [Header("Current State")]
        public Color primaryColor = Color.black;
        
        [Header("History")]
        public int maxRecentColors = 20;
        public List<Color> recentColors = new List<Color>();
        
        [Header("Palettes")]
        public ColorPalette[] presetPalettes;

        public event Action<Color> OnColorChanged;

        public void SetColor(Color newColor)
        {
            primaryColor = newColor;
            
            // Add to recent if it's not already the most recent
            if (recentColors.Count == 0 || recentColors[0] != newColor)
            {
                recentColors.Insert(0, newColor);
                if (recentColors.Count > maxRecentColors)
                {
                    recentColors.RemoveAt(recentColors.Count - 1);
                }
            }

            OnColorChanged?.Invoke(primaryColor);
        }

        public void SetColorFromHSV(float hue, float saturation, float value)
        {
            Color c = Color.HSVToRGB(Mathf.Clamp01(hue), Mathf.Clamp01(saturation), Mathf.Clamp01(value));
            SetColor(c);
        }
        
        public void SetColorFromHex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color c))
            {
                SetColor(c);
            }
        }
    }
}
