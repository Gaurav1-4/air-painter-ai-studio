using UnityEngine;

namespace AirPainter.Brushes
{
    public enum BrushCategory
    {
        Basic,
        Artistic,
        Special,
        Eraser
    }

    /// <summary>
    /// Defines a brush using Unity's ScriptableObject system for easy modularity.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBrush", menuName = "AirPainter/Brush Settings")]
    public class BrushSettings : ScriptableObject
    {
        [Header("Identity")]
        public string brushId;
        public string brushName;
        public Sprite icon;
        public BrushCategory category;
        public bool isPremium = false;

        [Header("Material & Rendering")]
        [Tooltip("The material that will be assigned to the StrokeRenderer's MeshRenderer")]
        public Material brushMaterial;
        
        [Header("Dynamics")]
        public float baseSize = 0.5f;
        public float minSize = 0.1f;
        public float maxSize = 2.0f;
        
        [Tooltip("How pressure affects the width of the stroke")]
        public AnimationCurve pressureSizeCurve = AnimationCurve.Linear(0, 0.5f, 1, 1f);
        
        [Tooltip("How pressure affects the opacity (alpha) of the stroke")]
        public AnimationCurve pressureOpacityCurve = AnimationCurve.Linear(0, 0.2f, 1, 1f);

        [Header("Behavior")]
        [Tooltip("Multiplier for the smoothing algorithm. Higher = smoother but less accurate.")]
        public float smoothingMultiplier = 1.0f;
    }
}
