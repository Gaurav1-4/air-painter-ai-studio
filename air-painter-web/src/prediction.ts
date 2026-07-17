/**
 * Velocity-based Motion Predictor
 * Extrapolates future positions to reduce perceived latency.
 */
export class MotionPredictor {
    private lastX: number | null = null;
    private lastY: number | null = null;
    private lastTime: number | null = null;
    
    private predictionFrames: number;

    constructor(predictionFrames: number = 2) {
        // How many frames into the future to predict (assuming 60fps)
        this.predictionFrames = predictionFrames;
    }

    public predict(x: number, y: number, timestamp: number) {
        if (this.lastX === null || this.lastY === null || this.lastTime === null) {
            this.lastX = x;
            this.lastY = y;
            this.lastTime = timestamp;
            return { x, y };
        }

        const dt = (timestamp - this.lastTime) || 16.66; // avoid div by 0
        
        // Velocity (pixels per ms)
        const vx = (x - this.lastX) / dt;
        const vy = (y - this.lastY) / dt;

        // Predict ahead (assuming 16.66ms per frame)
        const timeAhead = this.predictionFrames * 16.66;
        
        const predictedX = x + (vx * timeAhead);
        const predictedY = y + (vy * timeAhead);

        this.lastX = x;
        this.lastY = y;
        this.lastTime = timestamp;

        return { x: predictedX, y: predictedY };
    }
    
    public reset() {
        this.lastX = null;
        this.lastY = null;
        this.lastTime = null;
    }
}
