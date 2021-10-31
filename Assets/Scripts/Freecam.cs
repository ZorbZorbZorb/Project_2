using UnityEngine;

public class Freecam : MonoBehaviour {
    public static Vector3 Center;
    public static float Depth = -10f;

    public static float MaxZoom = 200f;
    public static float MinZoom = 600f;

    public static bool NoPan = false;
    public static bool NoZoom = false;

    public Camera Camera { get; private set; }
    /// <summary>Is camera panning this update?</summary>
    private bool panning = false;
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

    public BoxCollider2D CameraBounds;
    private float xMin, xMax, yMin, yMax;
    private float halfCamWidth, halfCamHeight;

    // Camera bounds positions
    private float cameraLeft;
    private float cameraRight;
    private float cameraUp;
    private float cameraDown;

    private void Awake() {
        this.Camera = GetComponent<Camera>();

        zoomCurrent = this.Camera.orthographicSize;
        zoomIntent = this.Camera.orthographicSize;
        zooming = false;

        Center = transform.position;

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
        if ( NoPan ) {
            if ( panning ) {
                StopPanning();
            }
        }
        else {
            // Are we free-looking?
            if ( Input.GetKeyDown(KeyCode.Mouse1) ) {
                StartPanning();
            }
            else if ( Input.GetKeyUp(KeyCode.Mouse1) ) {
                StopPanning();
            }

            // Pan the camera
            if ( panning ) {
                PanCamera();
            }
        }

        // Zoom the camera
        if ( !NoZoom ) {
            float axis = Input.GetAxis("Mouse ScrollWheel");
            if ( axis != 0 ) {
                ZoomCamera(axis);
            }
            else if ( zooming ) {
                ApproachZoom();
            }
        }
        // If not allowed to zoom camera, stop zooming now.
        else {
            zoomIntent = zoomCurrent;
            zooming = false;
        }
    }

    private void PanCamera() {
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
    private void ZoomCamera(float axis) {
        zoomIntent = Mathf.Clamp(zoomIntent + (-axis * ZoomSens), MaxZoom, MinZoom);
        if ( zoomIntent != zoomCurrent ) {
            zooming = true;
            ApproachZoom();
        }
    }
    private void ApproachZoom() {
        // Approach the intended zoom amount
        float difference = ( zoomIntent - zoomCurrent );
        float zoomAmount = (difference * Time.fixedDeltaTime) + 0.1f * Mathf.Sign(difference);
        zoomCurrent = Mathf.Clamp(Camera.main.orthographicSize + zoomAmount, MaxZoom, MinZoom);
        Camera.main.orthographicSize = zoomCurrent;

        // Stop zooming if finished approaching
        if ( Mathf.Abs( zoomCurrent - zoomIntent ) < 0.2f ) {
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
    void OnDisable() {
        StopPanning();
    }
    private void StartPanning() {
        panning = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void StopPanning() {
        panning = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    /// <summary>
    /// Unpans the camera, returning it to the position it was when the freecam component was created.
    /// <para>Uses the current main camera</para>
    /// </summary>
    public static void UnpanCamera() {
        Camera.main.transform.position = Center;
    }
    /// <summary>
    /// Unpans the camera, returning it to the position it was when the freecam component was created.
    /// </summary>
    public static void UnpanCamera(Camera camera) {
        camera.transform.position = Center;
    }
    /// <summary>
    /// Unpans the camera, returning it to the position it was when the freecam component was created.
    /// </summary>
    public static void UnpanCamera(Freecam freecam) {
        freecam.Camera.transform.position = Center;
    }
    /// <summary>
    /// Unzooms the camera to MinZoom
    /// <para>Uses the current main camera. Must to call GetComponent. Please consider keeping a reference to your 
    /// freecamera and using <see cref="UnZoomCamera(Freecam)"/> instead.</para>
    /// </summary>
    public static void UnzoomCamera() {
        Camera camera = Camera.main;
        camera.orthographicSize = MinZoom;
        // There's little way around this one, sorry.
        StopZoom(camera.GetComponent<Freecam>());
    }
    /// <summary>
    /// Unzooms the camera to MinZoom
    /// <para>Must to call GetComponent. Please consider keeping a reference to your 
    /// freecamera and using <see cref="UnZoomCamera(Freecam)"/> instead.</para>
    /// </summary>
    public static void UnzoomCamera(Camera camera) {
        camera.orthographicSize = MinZoom;
        // There's little way around this one, sorry.
        StopZoom(camera.GetComponent<Freecam>());
    }
    /// <summary>
    /// Unzooms the camera to MinZoom
    /// </summary>
    public static void UnZoomCamera(Freecam camera) {
        camera.Camera.orthographicSize = MinZoom;
        StopZoom(camera);
    }

    private static void StopZoom(Freecam freecam) {
        freecam.zoomIntent = MinZoom;
        freecam.zooming = false;
        freecam.zoomCurrent = MinZoom;
    }
}
