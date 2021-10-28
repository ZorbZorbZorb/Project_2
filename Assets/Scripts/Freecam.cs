using System;
using UnityEngine;

// Script taken in part from from https://gist.github.com/ashleydavis/f025c03a9221bc840a2b
public class Freecam : MonoBehaviour {
    // How fast the camera moves across the world
    public float movementSpeed;
    public float fastMovementSpeed;
    // How fast the camera zooms in and out of the world
    public float zoomSensitivity;
    public float fastZoomSensitivity;
    // Camera reference for zooming, set on awake
    private Camera camera;
    private void Awake() {
        camera = gameObject.GetComponent<Camera>();
    }

    void Update() {
        var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var movementSpeed = fastMode ? this.fastMovementSpeed : this.movementSpeed;

        if ( Input.GetKey(KeyCode.A) ) {
            transform.position = transform.position + ( ( -transform.right * Time.unscaledDeltaTime ) * movementSpeed );
        }

        if ( Input.GetKey(KeyCode.D) ) {
            transform.position = transform.position + ( ( transform.right * Time.unscaledDeltaTime ) * movementSpeed );
        }

        if ( Input.GetKey(KeyCode.W) ) {
            transform.position = transform.position + ( ( Vector3.up * Time.unscaledDeltaTime ) * movementSpeed );
        }

        if ( Input.GetKey(KeyCode.S) ) {
            transform.position = transform.position + ( ( -Vector3.up * Time.unscaledDeltaTime ) * movementSpeed );
        }

        float axis = Input.GetAxis("Mouse ScrollWheel");
        if ( axis != 0 ) {
            var zoomSensitivity = fastMode ? this.fastZoomSensitivity : this.zoomSensitivity;
            var newSize = camera.orthographicSize + ( -axis * zoomSensitivity );
            camera.orthographicSize = Math.Max(Math.Min(newSize, 600f), 200f);
        }

    }
}
