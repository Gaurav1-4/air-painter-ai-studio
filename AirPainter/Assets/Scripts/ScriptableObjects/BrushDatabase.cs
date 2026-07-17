using System.Collections.Generic;
using UnityEngine;

namespace AirPainter.Brushes
{
    /// <summary>
    /// A central database for all available brushes in the application.
    /// Easily expandable by simply adding new BrushSettings ScriptableObjects in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "BrushDatabase", menuName = "AirPainter/Brush Database")]
    public class BrushDatabase : ScriptableObject
    {
        public List<BrushSettings> brushes = new List<BrushSettings>();

        public BrushSettings GetBrushById(string id)
        {
            return brushes.Find(b => b.brushId == id);
        }

        public BrushSettings GetDefaultBrush()
        {
            if (brushes.Count > 0)
                return brushes[0];
            
            Debug.LogError("BrushDatabase is empty!");
            return null;
        }
    }
}
