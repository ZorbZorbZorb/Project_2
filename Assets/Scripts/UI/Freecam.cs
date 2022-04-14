using UnityEngine;

namespace Assets.Scripts.UI {
    public class Freecam : MonoBehaviour {
        public static Vector3 Center;
        public static float Depth = -10f;

        public static float MaxZoom = 200f;
        public static float MinZoom = 600f;

        public bool Locked = false;

        public Camera Camera { get; private set; }
        private bool mousePanning = false;
        public bool AutoPanning = false;
        public Vector3 PanIntent;
        /// <summary>Camera pan speed</summary>
        public float FreeLookSensitivity;
        /// <summary>Amount to zoom the camera when scrolling</summary>
        public float ZoomSens;
        /// <summary>Smooth scrolling speed</summary>
        public float ZoomLerp;
        /// <summary>What is the zoom the camera is lerping towords?</summary>
        public float zoomIntent;
        public float zoomCurrent;
        public bool zooming;

        public float Zoom => Camera.orthographicSize;
        public Vector2 Pan => Camera.transform.position;

        public BoxCollider2D CameraBounds;
        private float xMin, xMax, yMin, yMax;
        private float halfCamWidth, halfCamHeight;

        // Camera bounds positions
        private float cameraLeft;
        private float cameraRight;
        private float cameraUp;
        private float cameraDown;

        private void Awake() {
            Camera = GetComponent<Camera>();

            Center = transform.position;

            zoomCurrent = Camera.orthographicSize;
            zoomIntent = Camera.orthographicSize;
            zooming = false;

            PanIntent = Center;

            // Recalculate camera size and overages
            CalculateCameraSize();
            UpdateCameraPositions();

            // Set camera bounds
            xMin = CameraBounds.bounds.min.x;
            xMax = CameraBounds.bounds.max.x;
            yMin = CameraBounds.bounds.min.y;
            yMax = CameraBounds.bounds.max.y;

            if ( Debug.isDebugBuild ) {
                // Disable raycasts on camera bounds collider so we can still see it in debugging
                gameObject.layer = 2;
            }
            else {
                // Destroy the collider because it is more performant
                Destroy(CameraBounds.gameObject);
            }
        }
        void Update() {
            if ( Locked ) {
                if ( mousePanning ) {
                    StopMousePanning();
                }
            }
            else {
                // Are we free-looking?
                if ( Input.GetKeyDown(KeyCode.Mouse1) ) {
                    // Stop auto-panning
                    AutoPanning = false;  
                    StartMousePanning();
                }
                else if ( Input.GetKeyUp(KeyCode.Mouse1) ) {
                    StopMousePanning();
                }

                // Pan the camera
                if ( mousePanning ) {
                    MousePanCamera();
                }
            }

            // If Auto-panning
            if ( AutoPanning ) {
                PanCamera();
            }

            // Capture mouse scrollwheel for zoom
            if ( !Locked ) {
                float axis = Input.GetAxis("Mouse ScrollWheel");
                if ( axis != 0 ) {
                    AutoPanning = false;
                    ZoomCamera(axis);
                }
            }
            // Zoom the camera
            if ( zooming ) {
                ApproachZoom();
            }
        }
        void OnDisable() {
            if ( mousePanning ) {
                StopMousePanning();
            }
        }

        private void MousePanCamera() {
            // How much to pan?
            var multiplier = FreeLookSensitivity * Time.fixedDeltaTime;
            // The clamp will prevent the camera from 'snapping' when alt-tabbing and disorienting the player
            var deltaX = -Mathf.Clamp(multiplier * Input.GetAxis("Mouse X"), -50, 50);
            var deltaY = -Mathf.Clamp(multiplier * Input.GetAxis("Mouse Y"), -50, 50);

            // New x and y to move camera to
            float newX = Mathf.Clamp(transform.position.x + deltaX, xMin, xMax);
            float newY = Mathf.Clamp(transform.position.y + deltaY, yMin, yMax);

            // Move the camera
            UpdateCameraPosition(newX, newY);
        }
        private void PanCamera() {
            float distance = Vector3.Distance(transform.position, PanIntent);
            if ( distance < 0.1f ) {
                AutoPanning = false;
            }
            else {
                float amount = 20f + ( distance * 2f );
                transform.position = Vector3.MoveTowards(transform.position, PanIntent, amount * Time.fixedDeltaTime);
            }
        }
        private void ZoomCamera(float axis) {
            zoomIntent = Mathf.Clamp(zoomIntent + ( -axis * ZoomSens ), MaxZoom, MinZoom);
            if ( zoomIntent != zoomCurrent ) {
                zooming = true;
                ApproachZoom();
            }
        }
        private void ApproachZoom() {
            // Approach the intended zoom amount
            float difference = ( zoomIntent - zoomCurrent );
            float zoomAmount = ( difference * Time.fixedDeltaTime ) + 0.1f * Mathf.Sign(difference);
            zoomCurrent = Mathf.Clamp(Camera.main.orthographicSize + zoomAmount, MaxZoom, MinZoom);
            Camera.main.orthographicSize = zoomCurrent;

            // Stop zooming if finished approaching
            if ( Mathf.Abs(zoomCurrent - zoomIntent) < 0.2f ) {
                zooming = false;
                zoomIntent = zoomCurrent;
            }

            // Recalculate the camera size and overages because the camera size has changed
            CalculateCameraSize();
            UpdateCameraPosition(transform.position.x, transform.position.y);
        }
        private void UpdateCameraPosition(float newX, float newY) {
            cameraLeft = newX - halfCamWidth;
            cameraRight = newX + halfCamWidth;
            cameraDown = newY - halfCamHeight;
            cameraUp = newY + halfCamHeight;

            bool overLeft = cameraLeft < xMin;
            bool overDown = cameraDown < yMin;

            if ( overLeft || cameraRight > xMax ) {
                newX -= ( overLeft ? cameraLeft - xMin : cameraRight - xMax );
            }
            if ( overDown || cameraUp > yMax ) {
                newY -= ( overDown ? cameraDown - yMin : cameraUp - yMax );
            }

            transform.position = new Vector3(newX, newY, Depth);
        }
        private void CalculateCameraSize() {
            halfCamHeight = Camera.main.orthographicSize;
            halfCamWidth = halfCamHeight * Camera.main.aspect;
        }
        private void UpdateCameraPositions() {
            cameraLeft = transform.position.x - halfCamWidth;
            cameraRight = transform.position.x + halfCamWidth;
            cameraDown = transform.position.y - halfCamHeight;
            cameraUp = transform.position.y + halfCamHeight;
        }
        private void StartMousePanning() {
            mousePanning = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        private void StopMousePanning() {
            mousePanning = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        public void UnpanCamera() {
            Camera.transform.position = Center;
        }
        /// <summary>
            /// Auto-Pans the camera to a vector3.
            /// <para>Disables mouse panning</para>
            /// </summary>
            /// <param name="pan"></param>
            /// <param name="instant"></param>
        public void PanTo(Vector2 pan, bool instant = false) {
            Vector3 destination = new Vector3(pan.x, pan.y, transform.position.z);

            if (mousePanning) {
                StopMousePanning();
            }

            if ( instant ) {
                AutoPanning = false;
                transform.position = destination;
            }
            else {
                AutoPanning = true;
                PanIntent = destination;
            }
        }
        /// <summary>
        /// Auto-Zooms the camera to a zoom level.
        /// </summary>
        /// <param name="zoom"></param>
        /// <param name="instant"></param>
        public void ZoomTo(float zoom, bool instant = false) {
            if ( instant ) {
                Camera.orthographicSize = MinZoom;
                zoomIntent = MinZoom;
                zooming = false;
                zoomCurrent = MinZoom;
            }
            else {
                zoomIntent = zoom;
                zooming = true;
            }
        }
    }
}