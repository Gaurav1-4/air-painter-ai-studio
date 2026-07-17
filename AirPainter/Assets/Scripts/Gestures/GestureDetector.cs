using System;
using System.Collections.Generic;
using UnityEngine;
using AirPainter.AI;

namespace AirPainter.Gestures
{
    public enum GestureType
    {
        NONE,
        DRAWING,
        COLOR_MENU,
        ERASING,
        BRUSH_SIZE,
        UNDO,
        REDO,
        COLOR_WHEEL,
        SAVE,
        ROTATE,
        ZOOM
    }

    public enum FingerType { THUMB, INDEX, MIDDLE, RING, PINKY }
    
    public enum SwipeDirection { NONE, LEFT, RIGHT, UP, DOWN }

    public class GestureDetector : MonoBehaviour
    {
        [Header("Settings")]
        public float pinchThreshold = 0.05f;
        public float swipeSpeedThreshold = 2.0f;
        public float debounceDuration = 0.15f;
        public int circleMinPoints = 20;

        public bool IsFingerExtended(HandData hand, FingerType finger)
        {
            if (hand == null || !hand.isTracked) return false;

            switch (finger)
            {
                case FingerType.INDEX:
                    return IsStraight(hand, LandmarkType.INDEX_MCP, LandmarkType.INDEX_PIP, LandmarkType.INDEX_TIP);
                case FingerType.MIDDLE:
                    return IsStraight(hand, LandmarkType.MIDDLE_MCP, LandmarkType.MIDDLE_PIP, LandmarkType.MIDDLE_TIP);
                case FingerType.RING:
                    return IsStraight(hand, LandmarkType.RING_MCP, LandmarkType.RING_PIP, LandmarkType.RING_TIP);
                case FingerType.PINKY:
                    return IsStraight(hand, LandmarkType.PINKY_MCP, LandmarkType.PINKY_PIP, LandmarkType.PINKY_TIP);
                case FingerType.THUMB:
                    return IsStraight(hand, LandmarkType.THUMB_CMC, LandmarkType.THUMB_MCP, LandmarkType.THUMB_TIP);
                default:
                    return false;
            }
        }

        private bool IsStraight(HandData hand, LandmarkType root, LandmarkType mid, LandmarkType tip)
        {
            Vector3 rootToTip = hand.landmarks[(int)tip].position - hand.landmarks[(int)root].position;
            Vector3 rootToMid = hand.landmarks[(int)mid].position - hand.landmarks[(int)root].position;
            float angle = Vector3.Angle(rootToTip, rootToMid);
            return angle < 30f; // Finger is nearly straight
        }

        public bool IsPinching(HandData hand, out float pinchStrength)
        {
            pinchStrength = 0f;
            if (hand == null || !hand.isTracked) return false;

            float distance = Vector3.Distance(
                hand.landmarks[(int)LandmarkType.THUMB_TIP].position,
                hand.landmarks[(int)LandmarkType.INDEX_TIP].position
            );
            
            const float PINCH_MAX = 0.15f;
            
            pinchStrength = 1f - Mathf.InverseLerp(pinchThreshold, PINCH_MAX, distance);
            return distance < pinchThreshold;
        }

        public bool IsOpenPalm(HandData hand)
        {
            if (hand == null || !hand.isTracked) return false;

            bool allExtended = IsFingerExtended(hand, FingerType.THUMB)
                            && IsFingerExtended(hand, FingerType.INDEX)
                            && IsFingerExtended(hand, FingerType.MIDDLE)
                            && IsFingerExtended(hand, FingerType.RING)
                            && IsFingerExtended(hand, FingerType.PINKY);
                            
            float spread = Vector3.Distance(
                hand.landmarks[(int)LandmarkType.INDEX_TIP].position,
                hand.landmarks[(int)LandmarkType.PINKY_TIP].position
            );

            return allExtended && spread > 0.3f;
        }

        public bool IsFist(HandData hand)
        {
            if (hand == null || !hand.isTracked) return false;

            return !IsFingerExtended(hand, FingerType.THUMB)
                && !IsFingerExtended(hand, FingerType.INDEX)
                && !IsFingerExtended(hand, FingerType.MIDDLE)
                && !IsFingerExtended(hand, FingerType.RING)
                && !IsFingerExtended(hand, FingerType.PINKY);
        }

        public bool IsPeace(HandData hand)
        {
            if (hand == null || !hand.isTracked) return false;
            return IsFingerExtended(hand, FingerType.INDEX) &&
                   IsFingerExtended(hand, FingerType.MIDDLE) &&
                   !IsFingerExtended(hand, FingerType.RING) &&
                   !IsFingerExtended(hand, FingerType.PINKY);
        }

        public bool IsThumbUp(HandData hand)
        {
            if (hand == null || !hand.isTracked) return false;
            // Basic Thumb Up: Thumb extended, others closed
            return IsFingerExtended(hand, FingerType.THUMB) &&
                   !IsFingerExtended(hand, FingerType.INDEX) &&
                   !IsFingerExtended(hand, FingerType.MIDDLE) &&
                   !IsFingerExtended(hand, FingerType.RING) &&
                   !IsFingerExtended(hand, FingerType.PINKY);
        }

        public bool IsThreeFingers(HandData hand)
        {
            if (hand == null || !hand.isTracked) return false;
            return IsFingerExtended(hand, FingerType.INDEX) &&
                   IsFingerExtended(hand, FingerType.MIDDLE) &&
                   IsFingerExtended(hand, FingerType.RING) &&
                   !IsFingerExtended(hand, FingerType.PINKY);
        }

        public bool IsFourFingers(HandData hand)
        {
            if (hand == null || !hand.isTracked) return false;
            return IsFingerExtended(hand, FingerType.INDEX) &&
                   IsFingerExtended(hand, FingerType.MIDDLE) &&
                   IsFingerExtended(hand, FingerType.RING) &&
                   IsFingerExtended(hand, FingerType.PINKY); // Thumb can be closed or open, but all 4 fingers open
        }

        public SwipeDirection DetectSwipe(HandData hand, HandData previousHand, float deltaTime)
        {
            if (hand == null || previousHand == null || deltaTime <= 0) return SwipeDirection.NONE;

            Vector3 velocity = (hand.landmarks[(int)LandmarkType.WRIST].position - 
                                previousHand.landmarks[(int)LandmarkType.WRIST].position) / deltaTime;
            
            if (velocity.magnitude > swipeSpeedThreshold)
            {
                if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
                {
                    return velocity.x > 0 ? SwipeDirection.RIGHT : SwipeDirection.LEFT;
                }
                else
                {
                    return velocity.y > 0 ? SwipeDirection.UP : SwipeDirection.DOWN;
                }
            }
            return SwipeDirection.NONE;
        }

        public bool DetectCircleGesture(List<Vector2> recentPositions)
        {
            if (recentPositions == null || recentPositions.Count < circleMinPoints) return false;
            
            Vector2 centroid = Vector2.zero;
            foreach (var p in recentPositions) centroid += p;
            centroid /= recentPositions.Count;
            
            float avgRadius = 0;
            foreach (var p in recentPositions) avgRadius += Vector2.Distance(p, centroid);
            avgRadius /= recentPositions.Count;
            
            float variance = 0;
            foreach (var p in recentPositions)
            {
                float d = Vector2.Distance(p, centroid) - avgRadius;
                variance += d * d;
            }
            variance /= recentPositions.Count;
            
            float closure = Vector2.Distance(recentPositions[0], recentPositions[^1]);
            
            return variance < 0.002f && closure < avgRadius * 0.5f;
        }
    }
}
