using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ToonSketch.Water.Demo
{
    public class DemoSystem : MonoBehaviour
    {
        public DemoCamera mainCam;
        public Light directionalLight;
        public DemoOrbit[] spotLights;
        public Light pointLight;
        public bool hideGUI = false;

        private bool directionalLightOn;
        private bool spotLightsOn;
        private bool spotLightsOrbit;
        private bool pointLightOn;

        private void Awake()
        {
            SetOrbitCam();
            SetDirectionalLight(true);
            SetSpotLights(true);
            SetSpotLightsOrbit(true);
            SetPointLight(false);
        }

        private void SetOrbitCam()
        {
            mainCam.gameObject.SetActive(true);
            mainCam.orbitSpeed = 20f;
            mainCam.autoOrbit = true;
        }

        private void SetStaticCam()
        {
            mainCam.gameObject.SetActive(true);
            mainCam.orbitSpeed = 0f;
            mainCam.autoOrbit = true;
        }

        private void SetFreeCam()
        {
            mainCam.gameObject.SetActive(true);
            mainCam.orbitSpeed = 20f;
            mainCam.autoOrbit = false;
        }

        private void SetDirectionalLight(bool value)
        {
            directionalLightOn = value;
            directionalLight.gameObject.SetActive(directionalLightOn);
        }

        private void SetSpotLights(bool value)
        {
            spotLightsOn = value;
            foreach (DemoOrbit light in spotLights)
                light.gameObject.SetActive(spotLightsOn);
        }

        private void SetSpotLightsOrbit(bool value)
        {
            spotLightsOrbit = value;
            foreach (DemoOrbit light in spotLights)
                light.orbiting = spotLightsOrbit;
        }

        private void SetPointLight(bool value)
        {
            pointLightOn = value;
            pointLight.gameObject.SetActive(pointLightOn);
        }

        private void OnGUI()
        {
            if (hideGUI)
                return;
            int width = 200;
            int x = 10;
            int y = 10;
            // Cameras
            GUI.Box(new Rect(x, y, width, 100), "Camera");
            x += 10;
            y += 30;
            if (GUI.Button(new Rect(x, y, width - 20, 20), "Orbit Cam"))
            {
                SetOrbitCam();
            }
            y += 20;
            if (GUI.Button(new Rect(x, y, width - 20, 20), "Static Cam"))
            {
                SetStaticCam();
            }
            y += 20;
            if (GUI.Button(new Rect(x, y, width - 20, 20), "Free Cam"))
            {
                SetFreeCam();
            }
            x -= 10;
            y += 40;
            // Lights
            GUI.Box(new Rect(x, y, width, 120), "Lights");
            x += 10;
            y += 30;
            if (GUI.Button(new Rect(x, y, width - 20, 20), "Toggle Directional Light"))
            {
                SetDirectionalLight(!directionalLightOn);
            }
            y += 20;
            if (GUI.Button(new Rect(x, y, width - 20, 20), "Toggle Spotlights"))
            {
                SetSpotLights(!spotLightsOn);
            }
            y += 20;
            if (GUI.Button(new Rect(x, y, width - 20, 20), "Toggle Spotlight Orbit"))
            {
                SetSpotLightsOrbit(!spotLightsOrbit);
            }
            y += 20;
            if (GUI.Button(new Rect(x, y, width - 20, 20), "Toggle Point Light"))
            {
                SetPointLight(!pointLightOn);
            }
        }
    }
}