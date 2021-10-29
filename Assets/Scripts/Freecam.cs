using System;
using UnityEngine;

public class Freecam : MonoBehaviour {
    public static Vector3 Center;
    public static float Depth = -10f;

    public static float MaxZoom = 200f;
    public static float MinZoom = 600f;

    public static bool NoPan = false;
    public static bool NoZoom = false;

    // How fast the camera moves across the world
    public float movementSpeed;
    public float fastMovementSpeed;
    public float freeLookSensitivity;
    // How fast the camera zooms in and out of the world
    public float zoomSens;
    public float fastZoomSens;
    // Are we in free-look?
    private bool panning = false;

    public BoxCollider2D CameraBounds;
    private float xMin, xMax, yMin, yMax;
    private float halfCamWidth, halfCamHeight;

    // For debugging without spamming the console
    public float cameraLeft;
    public float cameraRight;
    public float cameraUp;
    public float cameraDown;

    private void Awake() {
        Center = transform.position;

        // Recalculate camera size and overages
        CalculateCameraSize();
        UpdateCameraPositions();

        // Set camera bounds
        xMin = CameraBounds.bounds.min.x;
        xMax = CameraBounds.bounds.max.x;
        yMin = CameraBounds.bounds.min.y;
        yMax = CameraBounds.bounds.max.y;
        Destroy(CameraBounds.gameObject);
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
        }
    }

    private void PanCamera() {
        // How much to pan?
        var multiplier = freeLookSensitivity * Time.unscaledDeltaTime;
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
        var zoomSensitivity = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? fastZoomSens : zoomSens;
        var newSize = Camera.main.orthographicSize + ( -axis * zoomSens );
        Camera.main.orthographicSize = Mathf.Clamp(newSize, MaxZoom, MinZoom);

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
            newX = newX + -( overLeft ? cameraLeft - xMin : cameraRight - xMax );
        }
        if ( overDown || cameraUp > yMax ) {
            newY = newY + -( overDown ? cameraDown - yMin : cameraUp - yMax );
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
    public void StartPanning() {
        panning = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    public void StopPanning() {
        panning = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    public static void UnpanCamera() {
        Camera.main.transform.position = Center;
    }
    public static void UnzoomCamera() {
        Camera.main.orthographicSize = MinZoom;
    }
}
