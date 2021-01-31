using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ToonSketch.Water.Demo
{
    public class DemoCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 0f, 1f);
        [Range(0f, 1f)]
        public float startZoom = 0.5f;
        public float zoomMin = 0.8f;
        public float zoomMax = 1.5f;
        public float lookMax = 35f;
        public float cameraSpeed = 5f;
        public float lookSpeed = 10f;
        public float orbitSpeed = 15f;
        public bool autoOrbit = true;

        [HideInInspector]
        public float zoom = 0f;
        [HideInInspector]
        public float orbit = 0f;
        [HideInInspector]
        public float look = 0f;

        private Vector3 targetPosition = Vector3.zero;

        private void Start()
        {
            // Set our initial target position and snap to it, also set zoom/orbit/look to defaults
            SetZoom(startZoom);
            SetOrbit(0f);
            SetLook(0f);
            UpdateTargetPosition();
            SnapPosition();
        }

        public void SetZoom(float zoom)
        {
            this.zoom = Mathf.Clamp01(zoom);
        }

        public void SetOrbit(float orbit)
        {
            this.orbit = orbit;
        }

        public void SetLook(float look)
        {
            this.look = Mathf.Clamp(look, -lookMax, lookMax);
        }

        private bool UpdateTargetPosition()
        {
            // If we don't have a target then return false
            if (target == null)
                return false;

            // Otherwise our base target position is our target's position
            targetPosition = target.position;
            // Then we need to work out our offset and zoom
            targetPosition += Quaternion.Euler(look, orbit, 0f) * new Vector3(offset.x, Mathf.Lerp(0f, offset.y, zoom), offset.z * (Mathf.Lerp(zoomMin, zoomMax, zoom)));

            // Since we updated target position then return true
            return true;
        }

        private void SnapPosition()
        {
            // Set our position to the target instantly and update lookat target if we have one
            transform.position = targetPosition;
            if (target != null)
                transform.LookAt(target);
        }

        private void LateUpdate()
        {
            // Grab our inputs and set values
            float zoomInput = Input.GetAxis("Mouse ScrollWheel");
            Vector2 moveInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            // Update look
            if (autoOrbit)
                SetLook(0f);
            else
                SetLook(look + (moveInput.y * lookSpeed));
            // Update orbit
            if (autoOrbit)
                SetOrbit(orbit + (Time.deltaTime * orbitSpeed));
            else
                SetOrbit(orbit + (moveInput.x * orbitSpeed));
            // Update zoom
            SetZoom(zoom - zoomInput);
            // Update our target position and handle moving the camera if we need to
            if (UpdateTargetPosition())
            {
                // Update our position based on target
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * cameraSpeed);
                if (target != null)
                    transform.LookAt(target);
            }
        }
    }
}