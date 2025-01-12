
using UnityEngine;
using UnityEngine.UI;


namespace Assets.Scripts
{
    public class MiniMap : MonoBehaviour
    {

        public Camera mainCamera; // Az alap kamera
        public float minimapSize = 10f; // A mini térkép kamerájának mérete
        public Vector3 minimapPosition = new Vector3(0, 10, 0); // A mini térkép pozíciója



        private Camera minimapCamera;
        private RenderTexture minimapTexture;
        private Canvas canvas;
        private RawImage minimapRawImage;

        void Start()
        {


            GameObject canvasObj = new GameObject("Canvas");
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();


            GameObject rawImageObj = new GameObject("MiniMap"); 
            rawImageObj.transform.SetParent(canvas.transform);
            minimapRawImage = rawImageObj.AddComponent<RawImage>();


            GameObject minimapCameraObj = new GameObject("MiniMap Camera");
            minimapCamera = minimapCameraObj.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = minimapSize;
            //minimapCamera.cullingMask = minimapLayer;
            minimapCamera.clearFlags = CameraClearFlags.Depth;
            minimapCamera.transform.position = minimapPosition;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Felülnézeti nézet


            // RenderTexture létrehozása
            minimapTexture = new RenderTexture(256, 256, 16);
            minimapCamera.targetTexture = minimapTexture;

            minimapRawImage.texture = minimapTexture;


            // RawImage pozicionálása a jobb alsó sarokba
            RectTransform minimapRect = minimapRawImage.GetComponent<RectTransform>();
            minimapRect.anchorMin = new Vector2(1, 0); // Jobb alsó sarok
            minimapRect.anchorMax = new Vector2(1, 0);
            minimapRect.anchoredPosition = new Vector2(-10, 10); // Finomhangolás
            minimapRect.sizeDelta = new Vector2(256, 256); // A minimap mérete

        }



        void Update()
        {
            
        }


    }
}
