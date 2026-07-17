/**
 * Gesture Recognition Engine
 */

export enum GestureType {
    NONE,
    PINCH,          // Draw
    PEACE,          // Eraser
    THUMB_UP,       // Undo
    THUMB_DOWN,     // Redo
    FOUR_FINGERS,   // Clear Canvas
    OPEN_PALM       // Stop Drawing / Idle
}

export class GestureRecognizer {
    
    // Check if a finger is extended by comparing the tip Y to the PIP joint Y
    // (Note: works best when hand is upright relative to camera)
    private isFingerExtended(tip: any, pip: any): boolean {
        return tip.y < pip.y;
    }

    public detectGesture(landmarks: any[]): GestureType {
        const thumbTip = landmarks[4];
        const thumbIp = landmarks[3];
        const thumbMcp = landmarks[2];
        
        const indexTip = landmarks[8];
        const indexPip = landmarks[6];
        
        const middleTip = landmarks[12];
        const middlePip = landmarks[10];
        
        const ringTip = landmarks[16];
        const ringPip = landmarks[14];
        
        const pinkyTip = landmarks[20];
        const pinkyPip = landmarks[18];

        const indexUp = this.isFingerExtended(indexTip, indexPip);
        const middleUp = this.isFingerExtended(middleTip, middlePip);
        const ringUp = this.isFingerExtended(ringTip, ringPip);
        const pinkyUp = this.isFingerExtended(pinkyTip, pinkyPip);
        
        // Thumb logic is trickier due to rotation, comparing X for thumb
        // Assuming right hand facing camera: thumb tip x is less than mcp x
        const isRightHand = thumbMcp.x > pinkyPip.x; 
        const thumbUpY = thumbTip.y < thumbIp.y - 0.05; 
        const thumbDownY = thumbTip.y > thumbIp.y + 0.05;

        // 1. PINCH (Thumb and Index close together)
        const dx = indexTip.x - thumbTip.x;
        const dy = indexTip.y - thumbTip.y;
        // Basic 2D distance for pinch
        const pinchDistance = Math.sqrt(dx*dx + dy*dy);
        if (pinchDistance < 0.08) {
            return GestureType.PINCH;
        }

        // 2. PEACE SIGN (Index and Middle extended, Ring and Pinky closed)
        if (indexUp && middleUp && !ringUp && !pinkyUp) {
            return GestureType.PEACE;
        }

        // 3. FOUR FINGERS (Index, Middle, Ring, Pinky extended, thumb doesn't matter as much)
        if (indexUp && middleUp && ringUp && pinkyUp) {
            // Distinguish between open palm and four fingers by checking thumb
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
        
        // Default
        return GestureType.NONE;
    }
}
