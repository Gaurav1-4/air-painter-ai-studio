using UnityEngine;
using AirPainter.Rendering;
using AirPainter.Managers;

namespace AirPainter.Drawing
{
    /// <summary>
    /// Command object representing a drawn stroke.
    /// Used by the CommandHistory to Undo/Redo strokes.
    /// </summary>
    public class DrawStrokeCommand : ICommand
    {
        private Stroke strokeData;
        private StrokeRenderer strokeRenderer;
        private StrokePool strokePool;

        public string Description => $"Draw Stroke ({strokeData.Points.Count} points)";

        public DrawStrokeCommand(Stroke stroke, StrokeRenderer renderer, StrokePool pool)
        {
            this.strokeData = stroke;
            this.strokeRenderer = renderer;
            this.strokePool = pool;
        }

        public void Execute()
        {
            // If it's a redo, the renderer might have been returned to the pool
            if (strokeRenderer == null || !strokeRenderer.gameObject.activeInHierarchy)
            {
                strokeRenderer = strokePool.GetRenderer();
                
                // Note: In a real architecture, we need to pass the material.
                // Assuming the renderer knows its material or we look it up from the BrushSystem.
                // For this example, we assume UpdateMesh handles the geometry if Initialize was already called.
            }
            
            strokeRenderer.gameObject.SetActive(true);
            strokeRenderer.UpdateMesh();
        }

        public void Undo()
        {
            if (strokeRenderer != null)
            {
                // Disable the renderer, effectively hiding it
                strokeRenderer.gameObject.SetActive(false);
            }
        }
    }
}
