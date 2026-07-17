/**
 * 1€ Filter (One Euro Filter) implementation in TypeScript.
 * An adaptive low-pass filter for noisy signals.
 * Decreases jitter at low speeds, and reduces lag at high speeds.
 */

function smoothingFactor(t_e: number, cutoff: number): number {
    const r = 2 * Math.PI * cutoff * t_e;
    return r / (r + 1);
}

function exponentialSmoothing(a: number, x: number, x_prev: number): number {
    return a * x + (1 - a) * x_prev;
}

export class OneEuroFilter {
    private minCutoff: number;
    private beta: number;
    private dCutoff: number;

    private xPrev: number | null = null;
    private dxPrev: number = 0;
    private tPrev: number | null = null;

    /**
     * @param minCutoff Minimum cutoff frequency (Hz) - controls jitter at low speeds (e.g. 1.0)
     * @param beta Speed coefficient - controls lag at high speeds (e.g. 0.007)
     * @param dCutoff Cutoff for derivative (Hz) - usually 1.0
     */
    constructor(minCutoff: number = 1.0, beta: number = 0.007, dCutoff: number = 1.0) {
        this.minCutoff = minCutoff;
        this.beta = beta;
        this.dCutoff = dCutoff;
    }

    public filter(x: number, timestamp: number): number {
        if (this.tPrev === null || this.xPrev === null) {
            this.tPrev = timestamp;
            this.xPrev = x;
            this.dxPrev = 0;
            return x;
        }

        const t_e = (timestamp - this.tPrev) / 1000.0; // Time elapsed in seconds

        if (t_e <= 0) return this.xPrev;

        // Calculate velocity (dx)
        let dx = (x - this.xPrev) / t_e;

        // Smooth velocity
        const a_d = smoothingFactor(t_e, this.dCutoff);
        dx = exponentialSmoothing(a_d, dx, this.dxPrev);

        // Adaptive cutoff based on speed
        const cutoff = this.minCutoff + this.beta * Math.abs(dx);

        // Filter the coordinate
        const a_e = smoothingFactor(t_e, cutoff);
        const xFiltered = exponentialSmoothing(a_e, x, this.xPrev);

        // Update state
        this.tPrev = timestamp;
        this.xPrev = xFiltered;
        this.dxPrev = dx;

        return xFiltered;
    }
    
    public reset() {
        this.tPrev = null;
        this.xPrev = null;
        this.dxPrev = 0;
    }
}

export class PointFilter2D {
    private filterX: OneEuroFilter;
    private filterY: OneEuroFilter;
    
    private lastOutputX: number | null = null;
    private lastOutputY: number | null = null;
    private deadzone: number;

    constructor(minCutoff = 1.0, beta = 0.005, deadzone = 2.0) {
        this.filterX = new OneEuroFilter(minCutoff, beta);
        this.filterY = new OneEuroFilter(minCutoff, beta);
        this.deadzone = deadzone;
    }

    public filter(x: number, y: number, timestamp: number) {
        const rawFx = this.filterX.filter(x, timestamp);
        const rawFy = this.filterY.filter(y, timestamp);
        
        if (this.lastOutputX === null || this.lastOutputY === null) {
            this.lastOutputX = rawFx;
            this.lastOutputY = rawFy;
        } else {
            // Apply dead-zone to eliminate micro-jitter
            const dx = rawFx - this.lastOutputX;
            const dy = rawFy - this.lastOutputY;
            const dist = Math.sqrt(dx*dx + dy*dy);
            
            if (dist > this.deadzone) {
                // Only move if we breached the deadzone. 
                // We interpolate slightly to avoid "snapping" out of the deadzone
                this.lastOutputX += dx * 0.5;
                this.lastOutputY += dy * 0.5;
            }
        }

        return {
            x: this.lastOutputX,
            y: this.lastOutputY
        };
    }
    
    public reset() {
        this.filterX.reset();
        this.filterY.reset();
        this.lastOutputX = null;
        this.lastOutputY = null;
    }
}
