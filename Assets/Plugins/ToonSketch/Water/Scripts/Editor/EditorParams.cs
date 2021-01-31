using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ToonSketch.Water
{
    public class EditorParams
    {
        public const string module = "Water";
        public const string version = "1.0";

        public static class Styles
        {
            public static GUIContent waterTextureText = new GUIContent("Water Texture", "The texture to use for the water effect");
            public static GUIContent surfaceTilingText = new GUIContent("Surface Tiling", "The tiling amount to use for surface texture");
            public static GUIContent foamTilingText = new GUIContent("Foam Tiling", "The tiling amount to use for foam texture");
            public static GUIContent surfaceUseWorldText = new GUIContent("Use World Space?", "Should surface textures use world space");
            public static GUIContent waterColorShallowText = new GUIContent("Shallow Color", "The color to use for shallow water");
            public static GUIContent waterColorDeepText = new GUIContent("Deep Color", "The color to use for deep water");
            public static GUIContent waterColorFoamText = new GUIContent("Foam Color", "The color to use for water foam");
            public static GUIContent waterDepthCutoffText = new GUIContent("Depth Cutoff", "The depth cutoff for water color");
            public static GUIContent flowVertexDataText = new GUIContent("Use Vertex Color Data?", "Should flow direction be set by vertex color?");
            public static GUIContent flowDirectionXText = new GUIContent("Flow Direction X", "Amount that water effect should flow in X direction");
            public static GUIContent flowDirectionYText = new GUIContent("Flow Direction Y", "Amount that water effect should flow in Y direction");
            public static GUIContent flowWaveFactorText = new GUIContent("Flow Wave Factor", "The amount of effect flow should have on wave distortion");
            public static GUIContent waveAmountText = new GUIContent("Wave Noise Amount", "The amount of noise in wave distortion");
            public static GUIContent waveSpeedText = new GUIContent("Wave Motion Speed", "The speed of wave distortion");
            public static GUIContent waveStrengthText = new GUIContent("Wave Effect Strength", "The overall strength of wave distortion");
            public static GUIContent waveUseWorldText = new GUIContent("Use World Space?", "Should wave distortion use world space");
            public static GUIContent surfaceDistortText = new GUIContent("Surface Distortion Amount", "Amount of distortion to apply to surface texture effects");
            public static GUIContent surfaceSpeedText = new GUIContent("Surface Distortion Speed", "The speed of the surface distortion effect");
            public static GUIContent surfaceStrengthText = new GUIContent("Surface Strength", "The overall strength of surface effect");
            public static GUIContent foamDistanceText = new GUIContent("Foam Distance", "The distance of the foam effect");
            public static GUIContent foamFadeText = new GUIContent("Foam Falloff", "The falloff of the foam effect");
            public static GUIContent foamDepthText = new GUIContent("Foam Depth Cutoff", "The min/max depth cutoff to use for foam effect");
            public static GUIContent foamDistortText = new GUIContent("Foam Distortion Amount", "Amount of distortion to apply to foam texture effects");
            public static GUIContent foamSpeedText = new GUIContent("Foam Distortion Speed", "The speed of the foam distortion effect");
            public static GUIContent foamSoftnessText = new GUIContent("Foam Softness", "The softness of the overall foam effect");
            public static GUIContent foamSoftNoiseText = new GUIContent("Foam Noise", "The amount of noise in the overall foam effect");
            public static GUIContent foamHardEdgeText = new GUIContent("Foam Edge Amount", "The amount of foam at water's edge");
            public static GUIContent foamNoiseAmountText = new GUIContent("Foam Edge Noise", "The amount of noise in the edge foam");
            public static GUIContent foamStrengthText = new GUIContent("Foam Strength", "The overall strength of foam effect");
        }

        public class Properties
        {
            public MaterialProperty surfaceTiling = null;
            public MaterialProperty foamTiling = null;
            public MaterialProperty surfaceUseWorld = null;
            public MaterialProperty waterColorShallow = null;
            public MaterialProperty waterColorDeep = null;
            public MaterialProperty waterColorFoam = null;
            public MaterialProperty waterDepthCutoff = null;
            public MaterialProperty flowVertexData = null;
            public MaterialProperty flowDirection = null;
            public MaterialProperty flowWaveFactor = null;
            public MaterialProperty waveAmount = null;
            public MaterialProperty waveSpeed = null;
            public MaterialProperty waveStrength = null;
            public MaterialProperty waveUseWorld = null;
            public MaterialProperty surfaceDistort = null;
            public MaterialProperty surfaceSpeed = null;
            public MaterialProperty surfaceStrength = null;
            public MaterialProperty foamDistance = null;
            public MaterialProperty foamFade = null;
            public MaterialProperty foamDepthMin = null;
            public MaterialProperty foamDepthMax = null;
            public MaterialProperty foamDistort = null;
            public MaterialProperty foamSpeed = null;
            public MaterialProperty foamSoftness = null;
            public MaterialProperty foamSoftNoise = null;
            public MaterialProperty foamHardEdge = null;
            public MaterialProperty foamNoiseAmount = null;
            public MaterialProperty foamStrength = null;
        }
    }
}