// @ts-nocheck
import './style.css'
import { PointFilter2D } from './filters';
import { MotionPredictor } from './prediction';
import { GestureRecognizer, GestureType } from './gestures';

const videoElement = document.getElementsByClassName('input_video')[0] as HTMLVideoElement;
const canvasElement = document.getElementsByClassName('output_canvas')[0] as HTMLCanvasElement;
const canvasCtx = canvasElement.getContext('2d', { willReadFrequently: true })!;

// State
let currentColor = 'red';
let currentBrushSize = 10;
let isEraser = false;
let isDrawing = false;
let lastTime = 0;

// Systems
const pointFilter = new PointFilter2D(1.0, 0.005); // 1€ Filter
const gestureRecognizer = new GestureRecognizer();

const colorsList = ['red', 'blue', 'green', 'yellow', 'white'];
let colorIndex = 0;

// Bezier Control Points buffer
let pointBuffer: {x: number, y: number}[] = [];

// Undo/Redo State
const MAX_HISTORY = 20;
let undoStack: ImageData[] = [];
let redoStack: ImageData[] = [];
let isGestureCooldown = false;

// Drawing Canvas (Persistent)
const drawingCanvas = document.createElement('canvas');
const drawingCtx = drawingCanvas.getContext('2d', { willReadFrequently: true })!;

const CAM_WIDTH = 1280;
const CAM_HEIGHT = 720;

function resizeCanvas() {
  canvasElement.width = CAM_WIDTH;
  canvasElement.height = CAM_HEIGHT;
  drawingCanvas.width = CAM_WIDTH;
  drawingCanvas.height = CAM_HEIGHT;
  saveState(); // Save initial blank state
}
resizeCanvas();

// UI Hooks
document.getElementById('clear-btn')!.addEventListener('click', clearCanvas);
document.querySelectorAll('.color-btn').forEach(btn => {
  btn.addEventListener('click', (e) => {
    currentColor = (e.target as HTMLElement).getAttribute('data-color') || 'red';
    isEraser = false;
    document.querySelectorAll('.color-btn').forEach(b => b.style.transform = 'scale(1)');
    (e.target as HTMLElement).style.transform = 'scale(1.2)';
  });
});
document.getElementById('size-slider')!.addEventListener('input', (e) => {
  currentBrushSize = parseInt((e.target as HTMLInputElement).value);
});

// History Logic
function saveState() {
  if (undoStack.length >= MAX_HISTORY) undoStack.shift();
  undoStack.push(drawingCtx.getImageData(0, 0, drawingCanvas.width, drawingCanvas.height));
  redoStack = [];
}

function undo() {
  if (undoStack.length > 1) {
    redoStack.push(undoStack.pop()!);
    const previousState = undoStack[undoStack.length - 1];
    drawingCtx.putImageData(previousState, 0, 0);
  }
}

function redo() {
  if (redoStack.length > 0) {
    const nextState = redoStack.pop()!;
    undoStack.push(nextState);
    drawingCtx.putImageData(nextState, 0, 0);
  }
}

function clearCanvas() {
  drawingCtx.clearRect(0, 0, drawingCanvas.width, drawingCanvas.height);
  saveState();
}

function triggerGestureWithCooldown(action: () => void) {
    if (isGestureCooldown) return;
    action();
    isGestureCooldown = true;
    setTimeout(() => { isGestureCooldown = false; }, 1000); // 1 sec cooldown
}

// MediaPipe Callback
function onResults(results: any) {
  const now = performance.now();
  canvasCtx.save();
  canvasCtx.clearRect(0, 0, canvasElement.width, canvasElement.height);
  
  if (results.image) {
    canvasCtx.drawImage(results.image, 0, 0, canvasElement.width, canvasElement.height);
  }
  canvasCtx.drawImage(drawingCanvas, 0, 0);

  if (results.multiHandLandmarks && results.multiHandLandmarks.length > 0) {
    const landmarks = results.multiHandLandmarks[0];
    const gesture = gestureRecognizer.detectGesture(landmarks);

    const indexTip = landmarks[8];
    
    // 1:1 Aspect Ratio Correction (Fixes the "Oval" shape problem)
    let correctedX = indexTip.x;
    let correctedY = indexTip.y;
    if (videoElement.videoWidth > 0 && videoElement.videoHeight > 0) {
        const videoAspect = videoElement.videoWidth / videoElement.videoHeight;
        const canvasAspect = canvasElement.width / canvasElement.height;
        
        if (Math.abs(videoAspect - canvasAspect) > 0.01) {
            // Camera ratio differs from Canvas ratio (e.g. 4:3 webcam on 16:9 canvas)
            // We scale X to prevent horizontal stretching which causes Ovals
            const scaleX = videoAspect / canvasAspect;
            correctedX = (indexTip.x - 0.5) * scaleX + 0.5;
        }
    }

    const rawX = correctedX * canvasElement.width;
    const rawY = correctedY * canvasElement.height;

    // Apply 1€ Filter only (Prediction removed to fix overshoot/offset)
    const filtered = pointFilter.filter(rawX, rawY, now);
    const cursorX = filtered.x;
    const cursorY = filtered.y;

    if (gesture === GestureType.PINCH) {
        if (!isDrawing) {
            isDrawing = true;
            pointBuffer = [{x: cursorX, y: cursorY}];
            
            // Draw a single dot if they just tap
            drawingCtx.beginPath();
            drawingCtx.arc(cursorX, cursorY, currentBrushSize / 2, 0, Math.PI * 2);
            drawingCtx.fillStyle = isEraser ? 'rgba(0,0,0,1)' : currentColor;
            drawingCtx.globalCompositeOperation = isEraser ? 'destination-out' : 'source-over';
            drawingCtx.fill();
        } else {
            pointBuffer.push({x: cursorX, y: cursorY});
            
            // Bezier Interpolation
            if (pointBuffer.length >= 3) {
                drawingCtx.beginPath();
                drawingCtx.moveTo(pointBuffer[0].x, pointBuffer[0].y);
                
                // Calculate middle point for quadratic curve
                const midX = (pointBuffer[0].x + pointBuffer[1].x) / 2;
                const midY = (pointBuffer[0].y + pointBuffer[1].y) / 2;
                
                drawingCtx.quadraticCurveTo(pointBuffer[0].x, pointBuffer[0].y, midX, midY);
                
                drawingCtx.globalCompositeOperation = isEraser ? 'destination-out' : 'source-over';
                drawingCtx.strokeStyle = currentColor;
                drawingCtx.lineWidth = currentBrushSize;
                drawingCtx.lineCap = 'round';
                drawingCtx.lineJoin = 'round';
                drawingCtx.stroke();
                
                // Shift buffer
                pointBuffer[0] = {x: midX, y: midY};
                pointBuffer[1] = pointBuffer[2];
                pointBuffer.pop();
            }
        }
        
        // Visual indicator (Drawing)
        canvasCtx.beginPath();
        canvasCtx.arc(cursorX, cursorY, currentBrushSize / 2 + 5, 0, 2 * Math.PI);
        canvasCtx.fillStyle = isEraser ? 'rgba(255, 255, 255, 0.8)' : 'rgba(0, 255, 0, 0.5)';
        canvasCtx.fill();
    } else {
        if (isDrawing) {
            isDrawing = false;
            saveState(); // Save state when stroke ends
            pointFilter.reset();
        }
        
        // Handle Action Gestures
        if (gesture === GestureType.PEACE) {
            isEraser = true;
            document.getElementById('status-text').innerText = "Mode: Eraser";
        } else if (gesture === GestureType.THUMB_UP) {
            triggerGestureWithCooldown(undo);
            document.getElementById('status-text').innerText = "Action: Undo";
        } else if (gesture === GestureType.THUMB_DOWN) {
            triggerGestureWithCooldown(redo);
            document.getElementById('status-text').innerText = "Action: Redo";
        } else if (gesture === GestureType.FOUR_FINGERS) {
            triggerGestureWithCooldown(clearCanvas);
            document.getElementById('status-text').innerText = "Action: Cleared";
        } else if (gesture === GestureType.THREE_FINGERS) {
            triggerGestureWithCooldown(() => {
                colorIndex = (colorIndex + 1) % colorsList.length;
                currentColor = colorsList[colorIndex];
                isEraser = false;
            });
            document.getElementById('status-text').innerText = `Color: ${currentColor}`;
        } else {
            document.getElementById('status-text').innerText = "Mode: Draw";
            isEraser = false; // Reset to draw mode
        }

        // Visual indicator (Idle/Hover)
        canvasCtx.beginPath();
        canvasCtx.arc(cursorX, cursorY, 10, 0, 2 * Math.PI);
        canvasCtx.fillStyle = 'rgba(255, 0, 0, 0.5)';
        canvasCtx.fill();
    }

    // Draw hand skeleton
    window.drawConnectors(canvasCtx, landmarks, window.HAND_CONNECTIONS, {color: 'rgba(255,255,255,0.2)', lineWidth: 2});
  }
  
  // FPS Counter
  const fps = Math.round(1000 / (now - lastTime));
  lastTime = now;
  document.getElementById('fps-counter').innerText = `FPS: ${fps}`;
  
  canvasCtx.restore();
}

const hands = new window.Hands({locateFile: (file: string) => {
  return `https://cdn.jsdelivr.net/npm/@mediapipe/hands/${file}`;
}});
hands.setOptions({
  maxNumHands: 1,
  modelComplexity: 0, 
  minDetectionConfidence: 0.5, 
  minTrackingConfidence: 0.5
});
hands.onResults(onResults);

const camera = new window.Camera(videoElement, {
  onFrame: async () => {
    await hands.send({image: videoElement});
  },
  width: CAM_WIDTH,
  height: CAM_HEIGHT
});
camera.start();
