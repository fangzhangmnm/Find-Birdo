using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ToonSketch.Shared
{
    public class EditorParams
    {
        public const string module = "Basic";
        public const string version = "2.0";

        public enum BlendMode
        {
            Opaque,
            Cutout,
            Fade,
            Transparent
        }

        public enum CullMode
        {
            Off,
            Front,
            Back
        }

        public enum StyleMode
        {
            Soft,
            Hard
        }

        public static class Styles
        {
            public static GUIContent blendModeText = new GUIContent("Blend Mode", "Blending mode to use");
            public static GUIContent cullModeText = new GUIContent("Cull Mode", "Culling mode to use");
            public static GUIContent styleModeText = new GUIContent("Diffuse Style", "Diffuse lighting type to use");
            public static GUIContent albedoText = new GUIContent("Albedo", "Albedo (RGB) and Transparency (A)");
            public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
            public static GUIContent rampTextureEnableText = new GUIContent("Ramp Texture?", "Enable custom ramp texture?");
            public static GUIContent rampTextureText = new GUIContent("Ramp", "Ramp (RGB)");
            public static GUIContent rampThresholdText = new GUIContent("Ramp Threshold", "Ramp threshold");
            public static GUIContent rampCutoffText = new GUIContent("Ramp Cutoff", "Ramp cutoff");
            public static GUIContent bumpEnableText = new GUIContent("Bump Mapping?", "Enable bump mapping?");
            public static GUIContent bumpTextureText = new GUIContent("Bump Texture", "Bump Map (Normal)");
            public static GUIContent specularEnableText = new GUIContent("Enable Specular Highlights?", "Enable specular highlights?");
            public static GUIContent specularColorText = new GUIContent("Specular Color", "Specular Color (RGB) and Smoothness (A)");
            public static GUIContent specularTypeText = new GUIContent("Specular Type", "Specular type");
            public static GUIContent smoothnessTypeText = new GUIContent("Smoothness Channel", "Smoothness channel");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Smoothness amount");
            public static GUIContent glossyReflectionsText = new GUIContent("Glossy Reflections?", "Enable glossy reflections?");
            public static GUIContent specularThresholdText = new GUIContent("Specular Threshold", "Specular threshold");
            public static GUIContent specularCutoffText = new GUIContent("Specular Cutoff", "Specular cutoff");
            public static GUIContent specularIntensityText = new GUIContent("Specular Intensity", "Specular intensity");
            public static GUIContent rimEnableText = new GUIContent("Enable Rim Lighting?", "Enable rim lighting?");
            public static GUIContent rimColorText = new GUIContent("Rim Color", "Rim Color (RGB)");
            public static GUIContent rimTypeText = new GUIContent("Rim Type", "Rim type");
            public static GUIContent rimColoringText = new GUIContent("Rim Uses Material Color?", "Rim uses material color?");
            public static GUIContent rimCutoffText = new GUIContent("Rim Cutoff", "Rim cutoff");
            public static GUIContent rimIntensityText = new GUIContent("Rim Intensity", "Rim intensity");
            public static GUIContent ignoreIndirectText = new GUIContent("Ignore Indirect Lighting?", "Ignore indirect lighting?");
            public static GUIContent outlineColorText = new GUIContent("Outline Color", "Outline Color (RGB)");
            public static GUIContent outlineWidthText = new GUIContent("Outline Width", "Outline width");
            public static GUIContent outlineSaturationText = new GUIContent("Outline Saturation", "Outline saturation");
            public static GUIContent outlineBrightnessText = new GUIContent("Outline Brightness", "Outline brightness");
            public static GUIContent outlineAngleText = new GUIContent("Outline Angle Cutoff", "Outline angle cutoff");
        }

        public class Properties
        {
            public MaterialProperty styleMode = null;
            public MaterialProperty blendMode = null;
            public MaterialProperty cullMode = null;
            public MaterialProperty albedoTexture = null;
            public MaterialProperty albedoColor = null;
            public MaterialProperty alphaCutoff = null;
            public MaterialProperty rampTextureEnable = null;
            public MaterialProperty rampTexture = null;
            public MaterialProperty rampThreshold = null;
            public MaterialProperty rampCutoff = null;
            public MaterialProperty bumpEnable = null;
            public MaterialProperty bumpTexture = null;
            public MaterialProperty bumpStrength = null;
            public MaterialProperty specularEnable = null;
            public MaterialProperty specularTexture = null;
            public MaterialProperty specularColor = null;
            public MaterialProperty specularType = null;
            public MaterialProperty smoothnessType = null;
            public MaterialProperty smoothness = null;
            public MaterialProperty glossyReflections = null;
            public MaterialProperty specularThreshold = null;
            public MaterialProperty specularCutoff = null;
            public MaterialProperty specularIntensity = null;
            public MaterialProperty rimEnable = null;
            public MaterialProperty rimColor = null;
            public MaterialProperty rimType = null;
            public MaterialProperty rimColoring = null;
            public MaterialProperty rimMin = null;
            public MaterialProperty rimMax = null;
            public MaterialProperty rimIntensity = null;
            public MaterialProperty ignoreIndirect = null;
            public MaterialProperty outlineColor = null;
            public MaterialProperty outlineWidth = null;
            public MaterialProperty outlineSaturation = null;
            public MaterialProperty outlineBrightness = null;
            public MaterialProperty outlineAngle = null;
        }
    }
}