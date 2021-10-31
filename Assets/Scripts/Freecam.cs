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
    private float zoomIntent;
    private float zoomCurrent;
    private bool zooming;

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
            else if ( zoomIntent != zoomCurrent ) {
                ApproachZoom();
            }
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
        zoomIntent = Mathf.Clamp(-axis * ZoomSens, MaxZoom, MinZoom);
        if (zoomIntent != zoomCurrent) {
            zooming = true;
            ApproachZoom();
        }

        //var newSize = Camera.main.orthographicSize + ( -axis * ZoomSens );
        //Camera.main.orthographicSize = Mathf.Clamp(newSize, MaxZoom, MinZoom);

        // Recalculate the camera size and overages because the camera size has changed
        //CalculateCameraSize();
        //UpdateCameraPosition(transform.position.x, transform.position.y);
    }
    private void ApproachZoom() {
        // Approach the intended zoom amount
        float difference = ( zoomIntent - zoomCurrent );
        float zoomAmount = difference * Time.fixedDeltaTime;
        float newSize = Mathf.Clamp(Camera.main.orthographicSize + zoomAmount, MaxZoom, MinZoom);
        Camera.main.orthographicSize = newSize;

        // Stop zooming if finished approaching
        if (newSize == MaxZoom || newSize == MinZoom) {
            zooming = false;
            zoomIntent = newSize;
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
    /// <para>Uses the current main camera. Must to call GetComponent. Please use <see cref="UnZoomCamera(Freecam)"/> instead.</para>
    /// </summary>
    public static void UnzoomCamera() {
        Camera camera = Camera.main;
        // There's little way around this one besides a singleton, sorry.
        camera.GetComponent<Freecam>().zoomIntent = MinZoom;
        camera.orthographicSize = MinZoom;
    }
    /// <summary>
    /// Unzooms the camera to MinZoom
    /// <para>Must to call GetComponent. Please use <see cref="UnZoomCamera(Freecam)"/> instead.</para>
    /// </summary>
    public static void UnzoomCamera(Camera camera) {
        // There's little way around this one besides a singleton, sorry.
        camera.GetComponent<Freecam>().zoomIntent = MinZoom;
        camera.orthographicSize = MinZoom;
    }
    /// <summary>
    /// Unzooms the camera to MinZoom
    /// </summary>
    public static void UnZoomCamera(Freecam camera) {
        camera.zoomIntent = MinZoom;
        camera.Camera.orthographicSize = MinZoom;
    }
}
