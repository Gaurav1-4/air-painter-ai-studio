using System.Collections.Generic;
using UnityEngine;
using AirPainter.Rendering;

namespace AirPainter.Managers
{
    /// <summary>
    /// Implements Object Pooling for Stroke GameObjects.
    /// Prevents Garbage Collection spikes from instantiating/destroying GameObjects rapidly.
    /// </summary>
    public class StrokePool : MonoBehaviour
    {
        [Header("Pool Settings")]
        public GameObject strokePrefab; // Prefab with MeshFilter and StrokeRenderer
        public int initialPoolSize = 50;
        
        private Queue<StrokeRenderer> pool = new Queue<StrokeRenderer>();
        private Transform poolContainer;

        private void Awake()
        {
            poolContainer = new GameObject("StrokePoolContainer").transform;
            poolContainer.SetParent(this.transform);
            
            // Pre-warm the pool
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewRenderer();
            }
        }

        private void CreateNewRenderer()
        {
            if (strokePrefab == null)
            {
                Debug.LogError("StrokePool: Stroke Prefab is not assigned!");
                return;
            }

            GameObject obj = Instantiate(strokePrefab, poolContainer);
            obj.name = "PooledStroke";
            obj.SetActive(false);
            
            StrokeRenderer renderer = obj.GetComponent<StrokeRenderer>();
            if (renderer == null)
            {
                renderer = obj.AddComponent<StrokeRenderer>();
            }
            
            pool.Enqueue(renderer);
        }

        /// <summary>
        /// Gets an available StrokeRenderer from the pool.
        /// </summary>
        public StrokeRenderer GetRenderer()
        {
            if (pool.Count == 0)
            {
                Debug.LogWarning("StrokePool: Pool empty, expanding size.");
                CreateNewRenderer();
            }

            StrokeRenderer renderer = pool.Dequeue();
            renderer.gameObject.SetActive(true);
            
            // Move out of the pool container so it renders properly in the scene hierarchy
            renderer.transform.SetParent(null); 
            
            return renderer;
        }

        /// <summary>
        /// Returns a StrokeRenderer back to the pool.
        /// </summary>
        public void ReturnRenderer(StrokeRenderer renderer)
        {
            renderer.Clear(); // Clear mesh data
            renderer.gameObject.SetActive(false);
            renderer.transform.SetParent(poolContainer);
            pool.Enqueue(renderer);
        }
    }
}
