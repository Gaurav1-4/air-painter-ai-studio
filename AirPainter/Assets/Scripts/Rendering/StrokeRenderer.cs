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
            
            // Create a dedicated mesh instance for this renderer that will be reused
            if (meshFilter.sharedMesh == null)
            {
                Mesh newMesh = new Mesh();
                newMesh.MarkDynamic(); // Optimize for frequent vertex updates
                meshFilter.sharedMesh = newMesh;
            }
        }

        /// <summary>
        /// Initializes the renderer for a new stroke.
        /// </summary>
        public void Initialize(Stroke stroke, Material material)
        {
            CurrentStroke = stroke;
            meshRenderer.material = material;
            
            // Clear the existing mesh geometry but keep the Mesh object
            if (meshFilter.sharedMesh != null)
            {
                meshFilter.sharedMesh.Clear();
            }
        }

        /// <summary>
        /// Rebuilds the mesh. Called every frame while the stroke is active.
        /// </summary>
        public void UpdateMesh()
        {
            if (CurrentStroke == null || CurrentStroke.Points.Count < 2) return;

            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.MarkDynamic();
                meshFilter.sharedMesh = mesh;
            }

            // Zero-allocation mesh update
            MeshBuilder.UpdateStrokeMesh(CurrentStroke.Points, mesh);
        }

        /// <summary>
        /// Clears the renderer data so it can be returned to the pool.
        /// </summary>
        public void Clear()
        {
            CurrentStroke = null;
            if (meshFilter.sharedMesh != null)
            {
                meshFilter.sharedMesh.Clear();
            }
        }
        
        private void OnDestroy()
        {
            // Only destroy the mesh when the GameObject itself is destroyed
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Destroy(meshFilter.sharedMesh);
            }
        }
    }
}
