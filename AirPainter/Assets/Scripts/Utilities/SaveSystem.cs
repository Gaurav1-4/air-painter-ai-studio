using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AirPainter.Drawing;

namespace AirPainter.Utilities
{
    [Serializable]
    public class DrawingSaveData
    {
        public string drawingId;
        public string timestamp;
        public List<Stroke> strokes;
        public string appVersion;
    }

    /// <summary>
    /// Handles saving and loading stroke data to JSON, and exporting canvas to PNG.
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>
        /// Saves all strokes to a JSON file.
        /// </summary>
        public static string SaveDrawingData(List<Stroke> strokes, string drawingId = null)
        {
            if (string.IsNullOrEmpty(drawingId))
            {
                drawingId = Guid.NewGuid().ToString();
            }

            DrawingSaveData data = new DrawingSaveData
            {
                drawingId = drawingId,
                timestamp = DateTime.UtcNow.ToString("O"),
                strokes = strokes,
                appVersion = Application.version
            };

            string json = JsonUtility.ToJson(data, true); // pretty print for debugging
            
            string dirPath = Path.Combine(Application.persistentDataPath, "Drawings");
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            string filePath = Path.Combine(dirPath, $"{drawingId}.json");
            File.WriteAllText(filePath, json);
            
            Debug.Log($"Drawing saved to: {filePath}");
            return filePath;
        }

        /// <summary>
        /// Loads stroke data from a JSON file.
        /// </summary>
        public static DrawingSaveData LoadDrawingData(string drawingId)
        {
            string filePath = Path.Combine(Application.persistentDataPath, "Drawings", $"{drawingId}.json");
            
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Drawing file not found: {filePath}");
                return null;
            }

            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<DrawingSaveData>(json);
        }

        /// <summary>
        /// Captures the main camera view (or a specific render texture) and saves to PNG.
        /// </summary>
        public static string ExportToPNG(Camera camera, int width, int height, bool transparentBackground = false)
        {
            RenderTexture rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            
            Color clearColor = transparentBackground ? Color.clear : Color.white;
            CameraClearFlags oldFlags = camera.clearFlags;
            Color oldBg = camera.backgroundColor;
            
            if (transparentBackground)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = clearColor;
            }
            
            Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);
            camera.Render();
            
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShot.Apply();
            
            // Restore camera
            camera.targetTexture = null;
            RenderTexture.active = null; 
            UnityEngine.Object.Destroy(rt);
            
            camera.clearFlags = oldFlags;
            camera.backgroundColor = oldBg;

            byte[] bytes = screenShot.EncodeToPNG();
            string fileName = $"Export_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            
            File.WriteAllBytes(filePath, bytes);
            
            Debug.Log($"PNG Exported to: {filePath}");
            return filePath;
        }
    }
}
