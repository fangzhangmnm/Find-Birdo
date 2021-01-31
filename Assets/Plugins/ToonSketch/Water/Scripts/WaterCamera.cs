using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ToonSketch.Water
{
    [DisallowMultipleComponent, ExecuteInEditMode, ImageEffectAllowedInSceneView]
    [AddComponentMenu("ToonSketch/Water Camera", -1)]
    [RequireComponent(typeof(Camera))]
    public class WaterCamera : MonoBehaviour
    {
        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
        }
    }
}