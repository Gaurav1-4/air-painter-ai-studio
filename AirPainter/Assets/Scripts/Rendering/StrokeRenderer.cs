using UnityEngine;
using AirPainter.Drawing;
using AirPainter.MeshGeneration;

namespace AirPainter.Rendering
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class StrokeRenderer : MonoBehaviour
    {
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        
        public Stroke CurrentStroke { get; private set; }

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// Initializes the renderer for a new stroke.
        /// </summary>
        public void Initialize(Stroke stroke, Material material)
        {
            CurrentStroke = stroke;
            meshRenderer.material = material;
            
            // Clean up old mesh if being reused from pool
            if (meshFilter.sharedMesh != null)
            {
                Destroy(meshFilter.sharedMesh);
            }
        }

        /// <summary>
        /// Rebuilds the mesh. Called every frame while the stroke is active.
        /// </summary>
        public void UpdateMesh()
        {
            if (CurrentStroke == null || CurrentStroke.Points.Count < 2) return;

            Mesh newMesh = MeshBuilder.GenerateStrokeMesh(CurrentStroke.Points);
            
            if (meshFilter.sharedMesh != null)
            {
                Destroy(meshFilter.sharedMesh); // Prevent memory leak on redraw
            }
            
            meshFilter.sharedMesh = newMesh;
        }

        /// <summary>
        /// Clears the renderer data so it can be returned to the pool.
        /// </summary>
        public void Clear()
        {
            CurrentStroke = null;
            if (meshFilter.sharedMesh != null)
            {
                Destroy(meshFilter.sharedMesh);
                meshFilter.sharedMesh = null;
            }
        }
        
        private void OnDestroy()
        {
            Clear();
        }
    }
}
