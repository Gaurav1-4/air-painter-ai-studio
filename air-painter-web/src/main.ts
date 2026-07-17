// @ts-nocheck
import './style.css'
import { GestureRecognizer, GestureType } from './gestures';

const videoElement = document.getElementsByClassName('input_video')[0] as HTMLVideoElement;
const canvasElement = document.getElementsByClassName('output_canvas')[0] as HTMLCanvasElement;
const canvasCtx = canvasElement.getContext('2d')!;

// State
let currentColor = 'red';
let currentBrushSize = 10;
let isEraser = false;
let isDrawing = false;
let lastTime = performance.now();
let cursorX = 0;
let cursorY = 0;

// Systems
const gestureRecognizer = new GestureRecognizer();
const colorsList = ['red', 'blue', 'green', 'yellow', 'white'];
let colorIndex = 0;

// Spline Control Points buffer
let pointBuffer: {x: number, y: number, time: number}[] = [];

// Undo/Redo State
const MAX_HISTORY = 20;
let undoStack: ImageData[] = [];
let redoStack: ImageData[] = [];
let isGestureCooldown = false;

// Drawing grace period to prevent breaking strokes
let framesSinceLastDraw = 0;
const GRACE_FRAMES = 15; // 0.25s grace period

// Drawing Canvas (Persistent)
const drawingCanvas = document.createElement('canvas');
const drawingCtx = drawingCanvas.getContext('2d', { willReadFrequently: true })!;

// Base Resolution
const CAM_WIDTH = 1280;
const CAM_HEIGHT = 720;
const dpr = window.devicePixelRatio || 1; // High-DPI Scaling for crisp rendering

function resizeCanvas() {
  canvasElement.width = CAM_WIDTH * dpr;
  canvasElement.height = CAM_HEIGHT * dpr;
  canvasElement.style.width = `${CAM_WIDTH}px`;
  canvasElement.style.height = `${CAM_HEIGHT}px`;
  
  drawingCanvas.width = CAM_WIDTH * dpr;
  drawingCanvas.height = CAM_HEIGHT * dpr;
  
  canvasCtx.scale(dpr, dpr);
  drawingCtx.scale(dpr, dpr);
  
  saveState(); 
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
  drawingCtx.clearRect(0, 0, CAM_WIDTH, CAM_HEIGHT);
  saveState();
}

function triggerGestureWithCooldown(action: () => void) {
    if (isGestureCooldown) return;
    action();
    isGestureCooldown = true;
    setTimeout(() => { isGestureCooldown = false; }, 1000); 
}

// Catmull-Rom Spline Drawing Logic with Velocity Thickness
function drawCatmullRomSegment(ctx: CanvasRenderingContext2D, p0: any, p1: any, p2: any, p3: any, baseSize: number, color: string, isEraser: boolean) {
    const tension = 1;
    const cp1x = p1.x + (p2.x - p0.x) / (6 * tension);
    const cp1y = p1.y + (p2.y - p0.y) / (6 * tension);
    
    const cp2x = p2.x - (p3.x - p1.x) / (6 * tension);
    const cp2y = p2.y - (p3.y - p1.y) / (6 * tension);

    const dx = p2.x - p1.x;
    const dy = p2.y - p1.y;
    const dist = Math.sqrt(dx*dx + dy*dy);
    const dt = Math.max(1, p2.time - p1.time);
    const velocity = dist / dt; // pixels per ms
    
    // Brush Dynamics: Faster = thinner stroke
    let velocityMultiplier = 1.0;
    if (velocity > 0.5) {
        velocityMultiplier = Math.max(0.2, 1.0 - (velocity - 0.5) * 0.4);
    } else {
        velocityMultiplier = Math.min(1.5, 1.0 + (0.5 - velocity) * 1.0);
    }
    
    ctx.lineWidth = baseSize * velocityMultiplier;
    ctx.strokeStyle = color;
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.globalCompositeOperation = isEraser ? 'destination-out' : 'source-over';
    
    // Premium Glow effect
    ctx.shadowBlur = isEraser ? 0 : 8;
    ctx.shadowColor = color;
    
    ctx.beginPath();
    ctx.moveTo(p1.x, p1.y);
    ctx.bezierCurveTo(cp1x, cp1y, cp2x, cp2y, p2.x, p2.y);
    ctx.stroke();
    
    // Reset shadow for performance
    ctx.shadowBlur = 0;
}

// MediaPipe Callback
function onResults(results: any) {
  const now = performance.now();
  
  canvasCtx.clearRect(0, 0, CAM_WIDTH, CAM_HEIGHT);
  
  if (results.image) {
    canvasCtx.drawImage(results.image, 0, 0, CAM_WIDTH, CAM_HEIGHT);
  }
  
  // Draw the permanent canvas
  // We use standard dimensions here because context is scaled
  canvasCtx.drawImage(drawingCanvas, 0, 0, CAM_WIDTH, CAM_HEIGHT);

  let gesture = GestureType.NONE;

  if (results.multiHandLandmarks && results.multiHandLandmarks.length > 0) {
    const landmarks = results.multiHandLandmarks[0];
    gesture = gestureRecognizer.detectGesture(landmarks);

    const indexTip = landmarks[8];
    
    let correctedX = indexTip.x;
    let correctedY = indexTip.y;
    if (videoElement.videoWidth > 0 && videoElement.videoHeight > 0) {
        const videoAspect = videoElement.videoWidth / videoElement.videoHeight;
        const canvasAspect = CAM_WIDTH / CAM_HEIGHT;
        if (Math.abs(videoAspect - canvasAspect) > 0.01) {
            const scaleX = videoAspect / canvasAspect;
            correctedX = (indexTip.x - 0.5) * scaleX + 0.5;
        }
    }

    const rawX = correctedX * CAM_WIDTH;
    const rawY = correctedY * CAM_HEIGHT;

    // Instant tracking
    cursorX = rawX;
    cursorY = rawY;

    // Draw hand skeleton
    window.drawConnectors(canvasCtx, landmarks, window.HAND_CONNECTIONS, {color: 'rgba(255,255,255,0.2)', lineWidth: 2});
  }

  // Grace Period Logic
  let shouldDraw = false;
  if (gesture === GestureType.POINTING) {
      shouldDraw = true;
      framesSinceLastDraw = 0;
  } else if (isDrawing) {
      framesSinceLastDraw++;
      if (framesSinceLastDraw < GRACE_FRAMES) {
          shouldDraw = true;
      }
  }

  if (shouldDraw) {
      if (!isDrawing) {
          isDrawing = true;
          pointBuffer = [{x: cursorX, y: cursorY, time: now}];
      } else {
          if (gesture === GestureType.POINTING) {
              pointBuffer.push({x: cursorX, y: cursorY, time: now});
          }
          
          // Commit smooth splines to the permanent canvas
          while (pointBuffer.length >= 4) {
              drawCatmullRomSegment(drawingCtx, pointBuffer[0], pointBuffer[1], pointBuffer[2], pointBuffer[3], currentBrushSize, currentColor, isEraser);
              pointBuffer.shift(); 
          }
      }
      
      // PREDICTIVE TAIL: Draw instantly to the temporary canvas for 0 perceived latency
      if (pointBuffer.length > 0) {
          canvasCtx.beginPath();
          canvasCtx.moveTo(pointBuffer[pointBuffer.length - 1].x, pointBuffer[pointBuffer.length - 1].y);
          canvasCtx.lineTo(cursorX, cursorY);
          canvasCtx.strokeStyle = currentColor;
          canvasCtx.lineWidth = currentBrushSize * (isEraser ? 1 : 0.6); // Tapered end
          canvasCtx.lineCap = 'round';
          canvasCtx.globalCompositeOperation = isEraser ? 'destination-out' : 'source-over';
          if (!isEraser) {
              canvasCtx.shadowBlur = 8;
              canvasCtx.shadowColor = currentColor;
          }
          canvasCtx.stroke();
          canvasCtx.shadowBlur = 0; // reset
      }
      
      document.getElementById('status-text')!.innerText = "Mode: Draw (Finger)";
  } else {
      if (isDrawing) {
          // Flush remaining points
          if (pointBuffer.length === 3) {
              drawCatmullRomSegment(drawingCtx, pointBuffer[0], pointBuffer[0], pointBuffer[1], pointBuffer[2], currentBrushSize, currentColor, isEraser);
              drawCatmullRomSegment(drawingCtx, pointBuffer[0], pointBuffer[1], pointBuffer[2], pointBuffer[2], currentBrushSize, currentColor, isEraser);
          } else if (pointBuffer.length === 2) {
             drawingCtx.beginPath();
             drawingCtx.moveTo(pointBuffer[0].x, pointBuffer[0].y);
             drawingCtx.lineTo(pointBuffer[1].x, pointBuffer[1].y);
             drawingCtx.lineWidth = currentBrushSize;
             drawingCtx.strokeStyle = currentColor;
             drawingCtx.lineCap = 'round';
             drawingCtx.stroke();
          }
          isDrawing = false;
          saveState(); 
      }
      
      // Handle Action Gestures
      if (gesture === GestureType.PEACE) {
          isEraser = true;
          document.getElementById('status-text')!.innerText = "Mode: Eraser";
      } else if (gesture === GestureType.THUMB_UP) {
          triggerGestureWithCooldown(undo);
          document.getElementById('status-text')!.innerText = "Action: Undo";
      } else if (gesture === GestureType.THUMB_DOWN) {
          triggerGestureWithCooldown(redo);
          document.getElementById('status-text')!.innerText = "Action: Redo";
      } else if (gesture === GestureType.FOUR_FINGERS) {
          triggerGestureWithCooldown(clearCanvas);
          document.getElementById('status-text')!.innerText = "Action: Cleared";
      } else if (gesture === GestureType.FIST) {
          triggerGestureWithCooldown(() => {
              colorIndex = (colorIndex + 1) % colorsList.length;
              currentColor = colorsList[colorIndex];
              isEraser = false;
              
              document.querySelectorAll('.color-btn').forEach(b => b.style.transform = 'scale(1)');
              const activeBtn = document.querySelector(`.color-btn[data-color="${currentColor}"]`) as HTMLElement;
              if (activeBtn) activeBtn.style.transform = 'scale(1.2)';
          });
          document.getElementById('status-text')!.innerText = `Color: ${currentColor}`;
      } else if (gesture !== GestureType.NONE) {
          document.getElementById('status-text')!.innerText = "Idle";
          isEraser = false; 
      }

      // Hover indicator
      if (gesture !== GestureType.NONE) {
          canvasCtx.beginPath();
          canvasCtx.arc(cursorX, cursorY, 10, 0, 2 * Math.PI);
          canvasCtx.fillStyle = 'rgba(255, 0, 0, 0.5)';
          canvasCtx.fill();
      }
  }
  
  // FPS Counter
  const fps = Math.round(1000 / (now - lastTime));
  lastTime = now;
  document.getElementById('fps-counter')!.innerText = `FPS: ${fps}`;
}

const hands = new window.Hands({locateFile: (file: string) => {
  return `https://cdn.jsdelivr.net/npm/@mediapipe/hands/${file}`;
}});
hands.setOptions({
  maxNumHands: 1,
  modelComplexity: 1, // High Accuracy Industry Standard
  minDetectionConfidence: 0.5, 
  minTrackingConfidence: 0.5
});
hands.onResults(onResults);

// Zero-Latency Hardware Sync Loop
async function processVideoFrame() {
    if (videoElement.readyState >= 2) {
        await hands.send({image: videoElement});
    }
    if ('requestVideoFrameCallback' in videoElement) {
        (videoElement as any).requestVideoFrameCallback(processVideoFrame);
    } else {
        requestAnimationFrame(processVideoFrame);
    }
}

// Initialize Camera
navigator.mediaDevices.getUserMedia({
    video: { facingMode: 'user' }
}).then((stream) => {
    videoElement.srcObject = stream;
    videoElement.play();
    if ('requestVideoFrameCallback' in videoElement) {
        (videoElement as any).requestVideoFrameCallback(processVideoFrame);
    } else {
        requestAnimationFrame(processVideoFrame);
    }
}).catch(err => {
    console.error("Camera failed:", err);
    requestAnimationFrame(fallbackLoop); // Use fallback if camera fails
});

// Fallback Mouse Implementation
let mouseX = 0;
let mouseY = 0;
let isMouseDown = false;

canvasElement.addEventListener('mousedown', (e) => {
  isMouseDown = true;
  mouseX = e.offsetX;
  mouseY = e.offsetY;
  gestureRecognizer.currentGesture = GestureType.POINTING; 
});

canvasElement.addEventListener('mousemove', (e) => {
  mouseX = e.offsetX;
  mouseY = e.offsetY;
});

canvasElement.addEventListener('mouseup', () => {
  isMouseDown = false;
  gestureRecognizer.currentGesture = GestureType.NONE;
});

canvasElement.addEventListener('mouseleave', () => {
  isMouseDown = false;
  gestureRecognizer.currentGesture = GestureType.NONE;
});

function fallbackLoop() {
  const now = performance.now();
  
  canvasCtx.clearRect(0, 0, CAM_WIDTH, CAM_HEIGHT);
  canvasCtx.fillStyle = '#222'; 
  canvasCtx.fillRect(0, 0, CAM_WIDTH, CAM_HEIGHT);
  canvasCtx.drawImage(drawingCanvas, 0, 0, CAM_WIDTH, CAM_HEIGHT);

  if (isMouseDown) {
    cursorX = mouseX;
    cursorY = mouseY;
    
    if (!isDrawing) {
        isDrawing = true;
        pointBuffer = [{x: cursorX, y: cursorY, time: now}];
    } else {
        pointBuffer.push({x: cursorX, y: cursorY, time: now});
        while (pointBuffer.length >= 4) {
            drawCatmullRomSegment(drawingCtx, pointBuffer[0], pointBuffer[1], pointBuffer[2], pointBuffer[3], currentBrushSize, currentColor, isEraser);
            pointBuffer.shift();
        }
    }
    
    // Predictive Tail
    if (pointBuffer.length > 0) {
        canvasCtx.beginPath();
        canvasCtx.moveTo(pointBuffer[pointBuffer.length - 1].x, pointBuffer[pointBuffer.length - 1].y);
        canvasCtx.lineTo(cursorX, cursorY);
        canvasCtx.strokeStyle = currentColor;
        canvasCtx.lineWidth = currentBrushSize * 0.6;
        canvasCtx.lineCap = 'round';
        canvasCtx.shadowBlur = 8;
        canvasCtx.shadowColor = currentColor;
        canvasCtx.stroke();
        canvasCtx.shadowBlur = 0;
    }
  } else {
    if (isDrawing) {
        if (pointBuffer.length === 3) {
            drawCatmullRomSegment(drawingCtx, pointBuffer[0], pointBuffer[0], pointBuffer[1], pointBuffer[2], currentBrushSize, currentColor, isEraser);
        }
        isDrawing = false;
        saveState();
    }
    canvasCtx.beginPath();
    canvasCtx.arc(mouseX, mouseY, 10, 0, 2 * Math.PI);
    canvasCtx.fillStyle = 'rgba(255, 0, 0, 0.5)';
    canvasCtx.fill();
  }

  canvasCtx.font = "20px Arial";
  canvasCtx.fillStyle = "white";
  canvasCtx.fillText("Mouse Fallback Mode (No Camera)", 20, 40);

  requestAnimationFrame(fallbackLoop);
}
