using System;
using UnityEngine;
using UnityEngine.Android;

namespace AirPainter.Camera
{
    public enum CameraFacing { Front, Back }

    public class CameraState
    {
        public bool IsRunning { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int FPS { get; set; }
    }

    /// <summary>
    /// Manages the device camera feed, handling permissions and delivering frames to the MediaPipe/AR systems.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        [Header("Configuration")]
        public CameraFacing cameraFacing = CameraFacing.Front;
        public Vector2Int targetResolution = new Vector2Int(1280, 720);
        public int targetFPS = 30;
        
        [Header("Processing")]
        public bool flipHorizontally = true;
        public float exposureCompensation = 0f;
        
        public event Action<Texture2D> OnFrameCaptured;
        public event Action<CameraState> OnStateChanged;

        private WebCamTexture webCamTexture;
        private Texture2D outputTexture;
        private CameraState currentState = new CameraState();

        private void Start()
        {
            RequestPermissionsAndStart();
        }

        private void RequestPermissionsAndStart()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                Invoke(nameof(CheckPermissionAndInit), 1.0f);
            }
            else
            {
                InitializeCamera();
            }
#elif UNITY_IOS
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                Application.RequestUserAuthorization(UserAuthorization.WebCam);
                Invoke(nameof(CheckPermissionAndInit), 1.0f);
            }
            else
            {
                InitializeCamera();
            }
#else
            InitializeCamera();
#endif
        }

        private void CheckPermissionAndInit()
        {
            // Simple check again after a delay
#if UNITY_ANDROID
            if (Permission.HasUserAuthorizedPermission(Permission.Camera)) InitializeCamera();
#elif UNITY_IOS
            if (Application.HasUserAuthorization(UserAuthorization.WebCam)) InitializeCamera();
#endif
        }

        private void InitializeCamera()
        {
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogError("No camera device found.");
                return;
            }

            string deviceName = WebCamTexture.devices[0].name;
            foreach (var device in WebCamTexture.devices)
            {
                if (cameraFacing == CameraFacing.Front && device.isFrontFacing)
                {
                    deviceName = device.name;
                    break;
                }
                else if (cameraFacing == CameraFacing.Back && !device.isFrontFacing)
                {
                    deviceName = device.name;
                    break;
                }
            }

            webCamTexture = new WebCamTexture(deviceName, targetResolution.x, targetResolution.y, targetFPS);
            webCamTexture.Play();

            currentState.IsRunning = true;
            currentState.Width = webCamTexture.width;
            currentState.Height = webCamTexture.height;
            currentState.FPS = targetFPS;
            
            outputTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

            OnStateChanged?.Invoke(currentState);
            Debug.Log($"Camera Initialized: {deviceName} at {webCamTexture.width}x{webCamTexture.height}");
        }

        private void Update()
        {
            if (webCamTexture == null || !webCamTexture.didUpdateThisFrame) return;

            ProcessFrame();
        }

        private void ProcessFrame()
        {
            // In a production app with MediaPipe, we'd extract the raw color buffer
            // and pass it to the native C++ library for performance.
            // For now, we invoke a C# event for prototyping.
            
            if (OnFrameCaptured != null)
            {
                outputTexture.SetPixels(webCamTexture.GetPixels());
                outputTexture.Apply();
                OnFrameCaptured.Invoke(outputTexture);
            }
        }

        private void OnDestroy()
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }
        }
    }
}
