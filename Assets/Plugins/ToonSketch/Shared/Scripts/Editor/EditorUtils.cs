using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ToonSketch.Shared
{
    public class EditorUtils
    {
        public enum Module
        {
            Core,
            Water,
            Terrain
        }

        public static bool ModuleInstalled(Module module)
        {
            string name = GetModuleNamespace(module);
            if (string.IsNullOrEmpty(name))
                return false;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.Namespace == name)
                        return true;
                }
            }
            return false;
        }

        private static string GetModuleNamespace(Module module)
        {
            switch (module)
            {
                case Module.Core:
                    return "ToonSketch.Core";
                case Module.Water:
                    return "ToonSketch.Water";
                case Module.Terrain:
                    return "ToonSketch.Terrain";
            }
            return null;
        }

        public static string GetTitle(string module)
        {
            return string.Format("{0}:", module);
        }

        public static string GetByline(string version)
        {
            return string.Format("© Ikonoclast [v{0}]", version);
        }

        public static void Title(string module, string text = "")
        {
            GUIStyle logoStyle = new GUIStyle(EditorStyles.inspectorDefaultMargins)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUIStyle titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            Texture2D logoTex = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/ToonSketch/Shared/Scripts/Editor/Images/toonsketch-logo.png", typeof(Texture2D));
            if (logoTex != null)
                GUILayout.Box(logoTex, logoStyle, GUILayout.MinHeight(100f), GUILayout.MaxHeight(100f));
            if (!string.IsNullOrEmpty(text))
                text = " " + text;
            GUILayout.Label(string.Format("<size=14>{0}</size>", GetTitle(module) + text), titleStyle);
        }

        public static void Header(string module, string version, string text = "")
        {
            Title(module, text);
            Seperator(GetByline(version));
            EditorGUILayout.Space();
        }

        public static void Section(string text = "")
        {
            EditorGUILayout.Space();
            Seperator(text);
            EditorGUILayout.Space();
        }

        public static void Seperator(string text = "")
        {
            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            if (!string.IsNullOrEmpty(text))
                text = "<size=10>" + text + "</size>";
            GUILayout.Label(text, style);
        }

        public static void ModePopup(MaterialEditor materialEditor, EditorParams.Properties properties)
        {
            EditorGUI.showMixedValue = properties.blendMode.hasMixedValue;
            var mode = (EditorParams.BlendMode)properties.blendMode.floatValue;
            var cull = (EditorParams.CullMode)properties.cullMode.floatValue;
            var style = (EditorParams.StyleMode)properties.styleMode.floatValue;
            EditorGUI.BeginChangeCheck();
            {
                mode = (EditorParams.BlendMode)EditorGUILayout.EnumPopup(EditorParams.Styles.blendModeText, mode);
                cull = (EditorParams.CullMode)EditorGUILayout.EnumPopup(EditorParams.Styles.cullModeText, cull);
                style = (EditorParams.StyleMode)EditorGUILayout.EnumPopup(EditorParams.Styles.styleModeText, style);
            }
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo("Rendering Mode");
                properties.blendMode.floatValue = (float)mode;
                properties.cullMode.floatValue = (float)cull;
                properties.styleMode.floatValue = (float)style;
                foreach (var obj in properties.blendMode.targets)
                    MaterialChanged((Material)obj);
            }
            EditorGUI.showMixedValue = false;
        }

        public static void MainSettings(MaterialEditor materialEditor, EditorParams.Properties properties)
        {
            Section("Main Settings");
            materialEditor.TexturePropertySingleLine(EditorParams.Styles.albedoText, properties.albedoTexture, properties.albedoColor);
            if (properties.blendMode.floatValue == (int)EditorParams.BlendMode.Cutout)
                materialEditor.ShaderProperty(properties.alphaCutoff, EditorParams.Styles.alphaCutoffText, MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);
            materialEditor.TextureScaleOffsetProperty(properties.albedoTexture);
        }

        public static void RampSettings(MaterialEditor materialEditor, EditorParams.Properties properties)
        {
            Section("Ramp Settings");
            materialEditor.ShaderProperty(properties.rampTextureEnable, EditorParams.Styles.rampTextureEnableText);
            EditorGUILayout.Space();
            if (properties.rampTextureEnable != null && properties.rampTextureEnable.floatValue == 1)
            {
                materialEditor.TexturePropertySingleLine(EditorParams.Styles.rampTextureText, properties.rampTexture);
            }
            else
            {
                materialEditor.ShaderProperty(properties.rampThreshold, EditorParams.Styles.rampThresholdText);
                materialEditor.ShaderProperty(properties.rampCutoff, EditorParams.Styles.rampCutoffText);
            }
        }

        public static void BumpSettings(MaterialEditor materialEditor, EditorParams.Properties properties)
        {
            Section("Bump Map Settings");
            materialEditor.ShaderProperty(properties.bumpEnable, EditorParams.Styles.bumpEnableText);
            EditorGUILayout.Space();
            if (properties.bumpEnable != null && properties.bumpEnable.floatValue == 1)
                materialEditor.TexturePropertySingleLine(EditorParams.Styles.bumpTextureText, properties.bumpTexture,
                    properties.bumpTexture.textureValue != null ? properties.bumpStrength : null);
        }

        public static void SpecularSettings(MaterialEditor materialEditor, EditorParams.Properties properties)
        {
            Section("Specular Settings");
            materialEditor.ShaderProperty(properties.specularEnable, EditorParams.Styles.specularEnableText);
            EditorGUILayout.Space();
            if (properties.specularEnable != null && properties.specularEnable.floatValue == 1)
            {
                materialEditor.TexturePropertySingleLine(EditorParams.Styles.specularColorText, properties.specularTexture, properties.specularColor);
                materialEditor.ShaderProperty(properties.specularType, EditorParams.Styles.specularTypeText);
                materialEditor.ShaderProperty(properties.smoothnessType, EditorParams.Styles.smoothnessTypeText);
                materialEditor.ShaderProperty(properties.smoothness, EditorParams.Styles.smoothnessText);
                materialEditor.ShaderProperty(properties.specularThreshold, EditorParams.Styles.specularThresholdText);
                materialEditor.ShaderProperty(properties.specularCutoff, EditorParams.Styles.specularCutoffText);
                materialEditor.ShaderProperty(properties.specularIntensity, EditorParams.Styles.specularIntensityText);
                materialEditor.ShaderProperty(properties.glossyReflections, EditorParams.Styles.glossyReflectionsText);
            }
        }

        public static void RimSettings(MaterialEditor materialEditor, EditorParams.Properties properties)
        {
            Section("Rim Lighting Settings");
            materialEditor.ShaderProperty(properties.rimEnable, EditorParams.Styles.rimEnableText);
            EditorGUILayout.Space();
            if (properties.rimEnable != null && properties.rimEnable.floatValue == 1)
            {
                properties.rimColor.colorValue = EditorGUILayout.ColorField(EditorParams.Styles.rimColorText, properties.rimColor.colorValue);
                materialEditor.ShaderProperty(properties.rimType, EditorParams.Styles.rimTypeText);
                materialEditor.ShaderProperty(properties.rimColoring, EditorParams.Styles.rimColoringText);
                float minVal = properties.rimMin.floatValue;
                float maxVal = properties.rimMax.floatValue;
                EditorGUI.BeginChangeCheck();
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(EditorParams.Styles.rimCutoffText);
                    minVal = EditorGUILayout.FloatField(minVal, GUILayout.MinWidth(50));
                    EditorGUILayout.MinMaxSlider(ref minVal, ref maxVal, 0f, 1f, GUILayout.MinWidth(100));
                    maxVal = EditorGUILayout.FloatField(maxVal, GUILayout.MinWidth(50));
                    EditorGUILayout.EndHorizontal();
                }
                if (EditorGUI.EndChangeCheck())
                {
                    materialEditor.RegisterPropertyChangeUndo("Rim Cutoff");
                    properties.rimMin.floatValue = minVal;
                    properties.rimMax.floatValue = maxVal;
                }
                materialEditor.ShaderProperty(properties.rimIntensity, EditorParams.Styles.rimIntensityText);
            }
        }

        public static void LightSettings(MaterialEditor materialEditor, EditorParams.Properties properties)
        {
            Section("Other Lighting Settings");
            materialEditor.ShaderProperty(properties.ignoreIndirect, EditorParams.Styles.ignoreIndirectText);
        }

        public static void OutlineSettings(MaterialEditor materialEditor, EditorParams.Properties properties)
        {
            Section("Outline Settings");
            properties.outlineColor.colorValue = EditorGUILayout.ColorField(EditorParams.Styles.outlineColorText, properties.outlineColor.colorValue);
            materialEditor.ShaderProperty(properties.outlineWidth, EditorParams.Styles.outlineWidthText);
            materialEditor.ShaderProperty(properties.outlineSaturation, EditorParams.Styles.outlineSaturationText);
            materialEditor.ShaderProperty(properties.outlineBrightness, EditorParams.Styles.outlineBrightnessText);
            materialEditor.ShaderProperty(properties.outlineAngle, EditorParams.Styles.outlineAngleText);
        }

        public static void AdvancedSettings(MaterialEditor materialEditor, EditorParams.Properties properties)
        {
            Section("Advanced Settings");
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        }

        public static void MaterialChanged(Material material)
        {
            SetupMaterialWithBlendMode(material, material.GetInt("_Blend"));
        }

        public static void SetupMaterialWithBlendMode(Material material, int mode)
        {
            switch (mode)
            {
                case 0: // Opaque
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = -1;
                    break;
                case 1: // Cutout
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    break;
                case 2: // Fade
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
                case 3: // Transparent
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
            }
        }
    }
}