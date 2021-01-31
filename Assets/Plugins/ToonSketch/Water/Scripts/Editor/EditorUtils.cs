using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ToonSketch.Water
{
    public class EditorUtils
    {
        public static void ModePopup(MaterialEditor materialEditor, Shared.EditorParams.Properties properties)
        {
            EditorGUI.showMixedValue = properties.cullMode.hasMixedValue;
            var cull = (Shared.EditorParams.CullMode)properties.cullMode.floatValue;
            var style = (Shared.EditorParams.StyleMode)properties.styleMode.floatValue;
            EditorGUI.BeginChangeCheck();
            {
                cull = (Shared.EditorParams.CullMode)EditorGUILayout.EnumPopup(Shared.EditorParams.Styles.cullModeText, cull);
                style = (Shared.EditorParams.StyleMode)EditorGUILayout.EnumPopup(Shared.EditorParams.Styles.styleModeText, style);
            }
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo("Rendering Mode");
                properties.cullMode.floatValue = (float)cull;
                properties.styleMode.floatValue = (float)style;
                foreach (var obj in properties.cullMode.targets)
                    MaterialChanged((Material)obj);
            }
            EditorGUI.showMixedValue = false;
        }

        public static void MainSettings(MaterialEditor materialEditor, Shared.EditorParams.Properties basicProperties, EditorParams.Properties waterProperties)
        {
            Shared.EditorUtils.Section("Main Settings");
            materialEditor.TexturePropertySingleLine(EditorParams.Styles.waterTextureText, basicProperties.albedoTexture);
            materialEditor.ShaderProperty(waterProperties.surfaceTiling, EditorParams.Styles.surfaceTilingText);
            materialEditor.ShaderProperty(waterProperties.foamTiling, EditorParams.Styles.foamTilingText);
            materialEditor.ShaderProperty(waterProperties.surfaceUseWorld, EditorParams.Styles.surfaceUseWorldText);
        }

        public static void ColorSettings(MaterialEditor materialEditor, EditorParams.Properties properties)
        {
            Shared.EditorUtils.Section("Color Settings");
            materialEditor.ShaderProperty(properties.waterColorShallow, EditorParams.Styles.waterColorShallowText);
            materialEditor.ShaderProperty(properties.waterColorDeep, EditorParams.Styles.waterColorDeepText);
            materialEditor.ShaderProperty(properties.waterColorFoam, EditorParams.Styles.waterColorFoamText);
            materialEditor.ShaderProperty(properties.waterDepthCutoff, EditorParams.Styles.waterDepthCutoffText);
        }

        public static void FlowSettings(MaterialEditor materialEditor, EditorParams.Properties properties)
        {
            Shared.EditorUtils.Section("Flow Settings");
            materialEditor.ShaderProperty(properties.flowVertexData, EditorParams.Styles.flowVertexDataText);
            if (properties.flowVertexData == null || properties.flowVertexData.floatValue == 0)
            {
                float xVal = properties.flowDirection.vectorValue.x;
                float yVal = properties.flowDirection.vectorValue.y;
                EditorGUI.BeginChangeCheck();
                {
                    xVal = EditorGUILayout.FloatField(EditorParams.Styles.flowDirectionXText, xVal);
                    yVal = EditorGUILayout.FloatField(EditorParams.Styles.flowDirectionYText, yVal);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    materialEditor.RegisterPropertyChangeUndo("Flow Direction");
                    properties.flowDirection.vectorValue = new Vector4(xVal, yVal, 0f, 0f);
                }
            }
            materialEditor.ShaderProperty(properties.flowWaveFactor, EditorParams.Styles.flowWaveFactorText);
        }

        public static void WaveSettings(MaterialEditor materialEditor, EditorParams.Properties properties)
        {
            Shared.EditorUtils.Section("Wave Settings");
            materialEditor.ShaderProperty(properties.waveAmount, EditorParams.Styles.waveAmountText);
            materialEditor.ShaderProperty(properties.waveSpeed, EditorParams.Styles.waveSpeedText);
            materialEditor.ShaderProperty(properties.waveStrength, EditorParams.Styles.waveStrengthText);
            materialEditor.ShaderProperty(properties.waveUseWorld, EditorParams.Styles.waveUseWorldText);
        }

        public static void SurfaceSettings(MaterialEditor materialEditor, EditorParams.Properties properties)
        {
            Shared.EditorUtils.Section("Surface Settings");
            materialEditor.ShaderProperty(properties.surfaceDistort, EditorParams.Styles.surfaceDistortText);
            materialEditor.ShaderProperty(properties.surfaceSpeed, EditorParams.Styles.surfaceSpeedText);
            EditorGUILayout.Space();
            materialEditor.ShaderProperty(properties.surfaceStrength, EditorParams.Styles.surfaceStrengthText);
        }

        public static void FoamSettings(MaterialEditor materialEditor, EditorParams.Properties properties)
        {
            Shared.EditorUtils.Section("Foam Settings");
            materialEditor.ShaderProperty(properties.foamDistance, EditorParams.Styles.foamDistanceText);
            materialEditor.ShaderProperty(properties.foamFade, EditorParams.Styles.foamFadeText);
            float minDepthVal = properties.foamDepthMin.floatValue;
            float maxDepthVal = properties.foamDepthMax.floatValue;
            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(EditorParams.Styles.foamDepthText);
                minDepthVal = EditorGUILayout.FloatField(minDepthVal, GUILayout.MinWidth(50));
                EditorGUILayout.MinMaxSlider(ref minDepthVal, ref maxDepthVal, 0f, 1f, GUILayout.MinWidth(100));
                maxDepthVal = EditorGUILayout.FloatField(maxDepthVal, GUILayout.MinWidth(50));
                EditorGUILayout.EndHorizontal();
            }
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo("Foam Depth");
                properties.foamDepthMin.floatValue = minDepthVal;
                properties.foamDepthMax.floatValue = maxDepthVal;
            }
            EditorGUILayout.Space();
            materialEditor.ShaderProperty(properties.foamDistort, EditorParams.Styles.foamDistortText);
            materialEditor.ShaderProperty(properties.foamSpeed, EditorParams.Styles.foamSpeedText);
            EditorGUILayout.Space();
            materialEditor.ShaderProperty(properties.foamSoftness, EditorParams.Styles.foamSoftnessText);
            materialEditor.ShaderProperty(properties.foamSoftNoise, EditorParams.Styles.foamSoftNoiseText);
            EditorGUILayout.Space();
            materialEditor.ShaderProperty(properties.foamHardEdge, EditorParams.Styles.foamHardEdgeText);
            materialEditor.ShaderProperty(properties.foamNoiseAmount, EditorParams.Styles.foamNoiseAmountText);
            EditorGUILayout.Space();
            materialEditor.ShaderProperty(properties.foamStrength, EditorParams.Styles.foamStrengthText);
        }

        public static void MaterialChanged(Material material)
        {
            Shared.EditorUtils.SetupMaterialWithBlendMode(material, 2);
        }
    }
}