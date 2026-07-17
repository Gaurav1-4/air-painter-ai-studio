/**
 * Gesture Recognition Engine (Industry Standard 3D Vector Logic)
 */

export enum GestureType {
    NONE,
    POINTING,       // Draw (Index finger up)
    FIST,           // Change Color (All fingers closed)
    PEACE,          // Eraser (Index and Middle up)
    THUMB_UP,       // Undo
    THUMB_DOWN,     // Redo
    FOUR_FINGERS,   // Clear Canvas
    PINCH           // Kept for backward compatibility
}

export class GestureRecognizer {
    
    // Calculates the true 3D Euclidean distance between two landmarks
    private distance3D(p1: any, p2: any): number {
        const dx = p1.x - p2.x;
        const dy = p1.y - p2.y;
        const dz = (p1.z || 0) - (p2.z || 0); // Handle cases where z might be missing
        return Math.sqrt(dx * dx + dy * dy + dz * dz);
    }

    // A finger is considered extended if the tip is further from the wrist (0) than the PIP joint
    // This makes the detection completely rotation-invariant in 3D space
    private isFingerExtended(tip: any, pip: any, wrist: any): boolean {
        const tipDist = this.distance3D(tip, wrist);
        const pipDist = this.distance3D(pip, wrist);
        // The tip should be significantly further from the wrist than the PIP joint
        return tipDist > pipDist * 1.1; 
    }

    public detectGesture(landmarks: any[]): GestureType {
        if (!landmarks || landmarks.length < 21) return GestureType.NONE;

        const wrist = landmarks[0];
        
        const thumbTip = landmarks[4];
        const thumbIp = landmarks[3];
        const thumbMcp = landmarks[2];
        
        const indexTip = landmarks[8];
        const indexPip = landmarks[6];
        const indexMcp = landmarks[5];
        
        const middleTip = landmarks[12];
        const middlePip = landmarks[10];
        const middleMcp = landmarks[9];
        
        const ringTip = landmarks[16];
        const ringPip = landmarks[14];
        const ringMcp = landmarks[13];
        
        const pinkyTip = landmarks[20];
        const pinkyPip = landmarks[18];
        const pinkyMcp = landmarks[17];

        const indexUp = this.isFingerExtended(indexTip, indexPip, wrist);
        const middleUp = this.isFingerExtended(middleTip, middlePip, wrist);
        const ringUp = this.isFingerExtended(ringTip, ringPip, wrist);
        const pinkyUp = this.isFingerExtended(pinkyTip, pinkyPip, wrist);
        
        // Thumb logic: thumb is usually extended sideways, so comparing against wrist is trickier.
        // Instead, we check if the thumb tip is further from the pinky MCP than the thumb MCP is.
        const thumbDistToPinky = this.distance3D(thumbTip, pinkyMcp);
        const thumbMcpDistToPinky = this.distance3D(thumbMcp, pinkyMcp);
        const thumbIsExtendedOutward = thumbDistToPinky > thumbMcpDistToPinky * 1.2;

        // For Thumb Up/Down, we still need directional context relative to the camera
        const thumbUpY = thumbTip.y < thumbIp.y - 0.04; 
        const thumbDownY = thumbTip.y > thumbIp.y + 0.04;

        // 1. POINTING (Only Index Finger Up - used for Drawing)
        if (indexUp && !middleUp && !ringUp && !pinkyUp) {
            return GestureType.POINTING;
        }

        // 2. PEACE SIGN (Index and Middle extended, Ring and Pinky closed)
        if (indexUp && middleUp && !ringUp && !pinkyUp) {
            return GestureType.PEACE;
        }

        // 3. FOUR FINGERS (Index, Middle, Ring, Pinky extended)
        if (indexUp && middleUp && ringUp && pinkyUp) {
            return GestureType.FOUR_FINGERS;
        }

        // 4. THUMB UP (Only thumb extended upwards)
        if (!indexUp && !middleUp && !ringUp && !pinkyUp && thumbUpY) {
            return GestureType.THUMB_UP;
        }

        // 5. THUMB DOWN
        if (!indexUp && !middleUp && !ringUp && !pinkyUp && thumbDownY) {
            return GestureType.THUMB_DOWN;
        }

        // 6. FIST (All 4 fingers closed) 
        if (!indexUp && !middleUp && !ringUp && !pinkyUp) {
            return GestureType.FIST;
        }

        // 7. PINCH
        const pinchDistance = this.distance3D(indexTip, thumbTip);
        if (pinchDistance < 0.05) { // Adjusted distance for 3D
            return GestureType.PINCH;
        }
        
        return GestureType.NONE;
    }
}
