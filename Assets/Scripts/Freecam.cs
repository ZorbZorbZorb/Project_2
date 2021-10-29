using System;
using UnityEngine;

public class Freecam : MonoBehaviour {
    // How fast the camera moves across the world
    public float movementSpeed;
    public float fastMovementSpeed;
    public float freeLookSensitivity;
    // How fast the camera zooms in and out of the world
    public float zoomSensitivity;
    public float fastZoomSensitivity;
    // Are we in free-look?
    private bool looking = false;

    public BoxCollider2D CameraBounds;
    private float xMin, xMax, yMin, yMax;
    private float camWidth, camHeight, halfCamWidth, halfCamHeight;
    private bool camIsOverX, camIsOverY;
    private float camOverX, camOverY;

    // For debugging without spamming the console
    public float cameraLeft;
    public float cameraRight;
    public float cameraUp;
    public float cameraDown;

    private void Start() {
        // Recalculate camera size and overages
        CalculateCameraSize();
        CalculateCameraOverages();
        UpdateCameraPositions();

        // Set camera bounds
        xMin = CameraBounds.bounds.min.x;
        xMax = CameraBounds.bounds.max.x;
        yMin = CameraBounds.bounds.min.y;
        yMax = CameraBounds.bounds.max.y;
        Destroy(CameraBounds.gameObject);
    }

    void Update() {

        // Are we free-looking?
        if ( Input.GetKeyDown(KeyCode.Mouse1) ) {
            StartLooking();
        }
        else if ( Input.GetKeyUp(KeyCode.Mouse1) ) {
            StopLooking();
        }

        // Are we scrolling fast or slow?
        var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Wow. This code sucks ass!
        if ( looking ) {

            var multiplier = freeLookSensitivity * Time.unscaledDeltaTime;
            var deltaX = Mathf.Clamp(multiplier * Input.GetAxis("Mouse X"), -9, 9);
            var deltaY = Mathf.Clamp(multiplier * Input.GetAxis("Mouse Y"), -9, 9);

            UpdateCameraPositions();
            CalculateCameraOverages();
            CorrectCameraOverages();

            float newX = Mathf.Clamp(transform.position.x + deltaX, xMin, xMax);
            float newY = Mathf.Clamp(transform.position.y + deltaY, yMin, yMax);

            transform.position = new Vector3(newX, newY, -10f);

        }

        float axis = Input.GetAxis("Mouse ScrollWheel");
        if ( axis != 0 ) {
            // Zoom the camera
            var zoomSensitivity = fastMode ? fastZoomSensitivity : this.zoomSensitivity;
            var newSize = Camera.main.orthographicSize + ( -axis * zoomSensitivity );
            Camera.main.orthographicSize = Math.Max(Math.Min(newSize, 600f), 200f);

            // Recalculate the camera size and overages because the camera size has changed
            CalculateCameraSize();
            CalculateCameraOverages();
            CorrectCameraOverages();
        }
    }

    private void CorrectCameraOverages() {
        if ( camIsOverX ) {
            transform.position = transform.position + new Vector3(-camOverX, 0f, -10f);
            UpdateCameraPositions();
        }
        if ( camIsOverY ) {
            transform.position = transform.position + new Vector3(0f, -camOverY, -10f);
            UpdateCameraPositions();
        }
    }

    private void CalculateCameraSize() {
        camHeight = 2 * Camera.main.orthographicSize;
        camWidth = camHeight * Camera.main.aspect;
        halfCamWidth = camWidth / 2;
        halfCamHeight = camHeight / 2;
    }
    private void CalculateCameraOverages() {
        bool overLeft = cameraLeft < xMin;
        bool overDown = cameraDown < yMin;
        camIsOverX = overLeft || cameraRight > xMax;
        camIsOverY = overDown || cameraUp > yMax;
        camOverX = overLeft ? cameraLeft - xMin : cameraRight - xMax;
        camOverY = overDown ? cameraDown - yMin : cameraUp - yMax;
    }
    private void UpdateCameraPositions() {
        cameraLeft = transform.position.x - halfCamWidth;
        cameraRight = transform.position.x + halfCamWidth;
        cameraDown = transform.position.y - halfCamHeight;
        cameraUp = transform.position.y + halfCamHeight;
    }

    void OnDisable() {
        StopLooking();
    }

    public void StartLooking() {
        looking = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void StopLooking() {
        looking = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
