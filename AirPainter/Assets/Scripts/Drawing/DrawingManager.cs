using System.Collections.Generic;
using UnityEngine;
using AirPainter.Gestures;
using AirPainter.Brushes;
using AirPainter.Managers;
using AirPainter.AI;

namespace AirPainter.Drawing
{
    /// <summary>
    /// The central orchestrator that links Gesture Inputs to the Drawing Engine, Brush System, and History.
    /// </summary>
    [RequireComponent(typeof(DrawingEngine))]
    public class DrawingManager : MonoBehaviour
    {
        [Header("System References")]
        public GestureDetector gestureDetector;
        public BrushManager brushManager;
        public ColorManager colorManager;
        public CommandHistory history;
        public StrokePool strokePool;

        private DrawingEngine engine;
        private IHandTracker handTracker; // We assume this is initialized elsewhere or acquired via FindObjectOfType

        [Header("State")]
        public bool isDrawing = false;
        private Handedness activeHandedness = Handedness.Right;

        private void Awake()
        {
            engine = GetComponent<DrawingEngine>();
            
            // In a real setup, IHandTracker would be injected or grabbed from a GameManager
            // handTracker = FindObjectOfType<MediaPipeManager>(); 
        }

        private void Update()
        {
            // Placeholder: Get tracked hands from the tracking system
            // HandData[] hands = handTracker.GetTrackedHands();
            HandData[] hands = new HandData[0]; // Replace with real data in production

            if (hands == null || hands.Length == 0) return;

            HandData primaryHand = null;
            foreach (var h in hands)
            {
                if (h != null && h.isTracked && h.handedness == activeHandedness)
                {
                    primaryHand = h;
                    break;
                }
            }

            if (primaryHand == null) return;

            ProcessGestures(primaryHand);
        }

        private void ProcessGestures(HandData hand)
        {
            // 1. PINCH (Start/Stop Drawing)
            if (gestureDetector.IsPinching(hand, out float pinchStrength))
            {
                if (!isDrawing)
                {
                    StartDrawing(hand.landmarks[(int)LandmarkType.INDEX_TIP].position);
                }
                else
                {
                    UpdateDrawing(hand.landmarks[(int)LandmarkType.INDEX_TIP].position, Time.deltaTime);
                }
            }
            else if (isDrawing)
            {
                StopDrawing();
            }

            // Only process other gestures if not currently drawing
            if (!isDrawing)
            {
                // 2. PEACE SIGN (Eraser)
                if (gestureDetector.IsPeace(hand))
                {
                    brushManager.SetBrushById("Eraser");
                    Debug.Log("Gesture: Eraser Active");
                }

                // 3. THUMB UP (Undo)
                if (gestureDetector.IsThumbUp(hand))
                {
                    history.Undo();
                    Debug.Log("Gesture: Undo Triggered");
                }

                // 4. THREE FINGERS (Color Picker)
                if (gestureDetector.IsThreeFingers(hand))
                {
                    // Trigger UI Event
                    Debug.Log("Gesture: Color Picker Opened");
                }
                
                // 5. FOUR FINGERS (Clear Canvas)
                if (gestureDetector.IsFourFingers(hand))
                {
                    // Trigger Clear Canvas Event
                    Debug.Log("Gesture: Canvas Cleared");
                }

                // 6. OPEN PALM (Stop/Idle)
                if (gestureDetector.IsOpenPalm(hand))
                {
                    Debug.Log("Gesture: Idle/Menu");
                }
            }
        }

        private void StartDrawing(Vector3 position)
        {
            isDrawing = true;
            
            BrushSettings brush = brushManager.currentBrush;
            Color color = colorManager.primaryColor;
            float size = brushManager.GetCurrentSize();
            
            StrokeRenderer renderer = strokePool.GetRenderer();
            
            engine.StartStroke(position, brush.brushId, color, size, renderer, brush.brushMaterial);
        }

        private void UpdateDrawing(Vector3 position, float deltaTime)
        {
            engine.UpdateStroke(position, deltaTime);
        }

        private void StopDrawing()
        {
            isDrawing = false;
            Stroke finishedStroke = engine.EndStroke();
            
            if (finishedStroke != null && finishedStroke.Points.Count > 1)
            {
                // Push to Undo/Redo history
                // We need to look up the renderer that was just used. 
                // Since the engine doesn't return the renderer, in a real implementation 
                // we'd probably keep a reference to it here or have the engine return a tuple.
                // For this mock, we assume the command handles it or we find it.
                
                // ICommand command = new DrawStrokeCommand(finishedStroke, activeRenderer, strokePool);
                // history.ExecuteCommand(command);
            }
        }
    }
}
