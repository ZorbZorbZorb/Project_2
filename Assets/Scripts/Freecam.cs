using System;
using UnityEngine;

// Script taken in part from from https://gist.github.com/ashleydavis/f025c03a9221bc840a2b
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
    // Camera reference for zooming, set on awake
    private Camera cam;
    private void Awake() {
        cam = gameObject.GetComponent<Camera>();
    }

    void Update() {

        // Are we free-looking?
        if ( Input.GetKeyDown(KeyCode.Mouse1) ) {
            StartLooking();
        }
        else if ( Input.GetKeyUp(KeyCode.Mouse1) ) {
            StopLooking();
        }

        // Are we moving fast or slow?
        var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var movementSpeed = fastMode ? this.fastMovementSpeed : this.movementSpeed;

        // Move the camera
        if ( Input.GetKey(KeyCode.A) ) {
            transform.position = transform.position + ( ( -transform.right * Time.unscaledDeltaTime ) * movementSpeed );
        }
        else if ( Input.GetKey(KeyCode.D) ) {
            transform.position = transform.position + ( ( transform.right * Time.unscaledDeltaTime ) * movementSpeed );
        }
        if ( Input.GetKey(KeyCode.W) ) {
            transform.position = transform.position + ( ( Vector3.up * Time.unscaledDeltaTime ) * movementSpeed );
        }
        else if ( Input.GetKey(KeyCode.S) ) {
            transform.position = transform.position + ( ( -Vector3.up * Time.unscaledDeltaTime ) * movementSpeed );
        }

        if ( looking ) {
            transform.position = transform.position + (freeLookSensitivity * Input.GetAxis("Mouse X") * Time.unscaledDeltaTime * -Vector3.right );
            transform.position = transform.position + (freeLookSensitivity * Input.GetAxis("Mouse Y") * Time.unscaledDeltaTime * -Vector3.up );
        }

        float axis = Input.GetAxis("Mouse ScrollWheel");
        if ( axis != 0 ) {
            var zoomSensitivity = fastMode ? this.fastZoomSensitivity : this.zoomSensitivity;
            var newSize = cam.orthographicSize + ( -axis * zoomSensitivity );
            cam.orthographicSize = Math.Max(Math.Min(newSize, 600f), 200f);
        }
    }

    void OnDisable() {
        StopLooking();
    }

    /// <summary>
    /// Enable free looking.
    /// </summary>
    public void StartLooking() {
        looking = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Disable free looking.
    /// </summary>
    public void StopLooking() {
        looking = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
