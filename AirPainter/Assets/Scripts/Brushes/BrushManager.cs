using System;
using UnityEngine;

namespace AirPainter.Brushes
{
    /// <summary>
    /// Manages the currently selected brush and brush size overrides.
    /// </summary>
    public class BrushManager : MonoBehaviour
    {
        [Header("Configuration")]
        public BrushDatabase database;
        
        [Header("State")]
        public BrushSettings currentBrush;
        private float customSizeOverride = -1f;

        public event Action<BrushSettings> OnBrushChanged;
        public event Action<float> OnBrushSizeChanged;

        private void Start()
        {
            if (database != null)
            {
                SetBrush(database.GetDefaultBrush());
            }
        }

        public void SetBrush(BrushSettings newBrush)
        {
            if (newBrush == null) return;

            currentBrush = newBrush;
            
            // Reset override on brush change
            customSizeOverride = -1f; 

            OnBrushChanged?.Invoke(currentBrush);
            OnBrushSizeChanged?.Invoke(GetCurrentSize());
        }

        public void SetBrushById(string id)
        {
            if (database != null)
            {
                SetBrush(database.GetBrushById(id));
            }
        }

        public void SetCustomSize(float size)
        {
            if (currentBrush == null) return;
            
            customSizeOverride = Mathf.Clamp(size, currentBrush.minSize, currentBrush.maxSize);
            OnBrushSizeChanged?.Invoke(customSizeOverride);
        }

        public float GetCurrentSize()
        {
            if (customSizeOverride > 0f) return customSizeOverride;
            if (currentBrush != null) return currentBrush.baseSize;
            return 1.0f;
        }
    }
}
