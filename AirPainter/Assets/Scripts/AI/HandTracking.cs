using System;
using UnityEngine;

namespace AirPainter.AI
{
    public enum Handedness
    {
        Left,
        Right
    }

    public enum LandmarkType
    {
        WRIST = 0,
        THUMB_CMC = 1,
        THUMB_MCP = 2,
        THUMB_IP = 3,
        THUMB_TIP = 4,
        INDEX_MCP = 5,
        INDEX_PIP = 6,
        INDEX_DIP = 7,
        INDEX_TIP = 8,
        MIDDLE_MCP = 9,
        MIDDLE_PIP = 10,
        MIDDLE_DIP = 11,
        MIDDLE_TIP = 12,
        RING_MCP = 13,
        RING_PIP = 14,
        RING_DIP = 15,
        RING_TIP = 16,
        PINKY_MCP = 17,
        PINKY_PIP = 18,
        PINKY_DIP = 19,
        PINKY_TIP = 20
    }

    [Serializable]
    public struct HandLandmark
    {
        public Vector3 position;     // Normalized (0-1) x, y, z
        public float visibility;     // Confidence 0-1
        public LandmarkType type;
    }

    [Serializable]
    public class HandData
    {
        public HandLandmark[] landmarks = new HandLandmark[21];
        public Handedness handedness;
        public float confidence;
        public bool isTracked;
    }

    /// <summary>
    /// Interface for integrating hand tracking providers like MediaPipe.
    /// </summary>
    public interface IHandTracker
    {
        void Initialize(int maxHands = 2, float minDetectionConfidence = 0.7f);
        HandData[] GetTrackedHands();
        void ProcessFrame(Texture2D frame);
        void Dispose();
    }
}
