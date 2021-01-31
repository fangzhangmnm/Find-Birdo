using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ToonSketch.Water.Demo
{
    public class DemoOrbit : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 0f, 1f);
        public float moveSpeed = 5f;
        public float orbitSpeed = 15f;

        [HideInInspector]
        public float orbit = 0f;

        public bool orbiting = false;

        private Vector3 targetPosition = Vector3.zero;

        private void Start()
        {
            SetOrbit(0f);
            UpdateTargetPosition();
            SnapPosition();
        }

        public void SetOrbit(float orbit)
        {
            this.orbit = orbit;
        }

        private bool UpdateTargetPosition()
        {
            // If we don't have a target then return false
            if (target == null)
                return false;

            // Otherwise our base target position is our target's position
            targetPosition = target.position;
            // Then we need to work out our offset
            targetPosition += Quaternion.Euler(0f, orbit, 0f) * offset;

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
            // Update orbit
            if (orbiting)
                SetOrbit(orbit + (Time.deltaTime * orbitSpeed));
            // Update our target position and handle moving the transform if we need to
            if (UpdateTargetPosition())
            {
                // Update our position based on target
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
                if (target != null)
                    transform.LookAt(target);
            }
        }
    }
}