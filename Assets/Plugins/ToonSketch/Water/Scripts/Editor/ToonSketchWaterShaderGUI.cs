using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ToonSketchWaterShaderGUI : ShaderGUI
{
    private ToonSketch.Shared.EditorParams.Properties basicProperties = new ToonSketch.Shared.EditorParams.Properties();
    private ToonSketch.Water.EditorParams.Properties waterProperties = new ToonSketch.Water.EditorParams.Properties();
    private MaterialEditor materialEditor;
    private bool firstTimeApply = true;

    private void Header()
    {
        ToonSketch.Shared.EditorUtils.Header(
            ToonSketch.Water.EditorParams.module,
            ToonSketch.Water.EditorParams.version,
            "Water Shader"
        );
    }

    public void FindProperties(MaterialProperty[] properties)
    {
        basicProperties.styleMode = FindProperty("_Style", properties);
        basicProperties.cullMode = FindProperty("_Cull", properties);
        basicProperties.albedoTexture = FindProperty("_MainTex", properties);
        basicProperties.rampTextureEnable = FindProperty("_Ramp", properties);
        basicProperties.rampTexture = FindProperty("_RampTex", properties);
        basicProperties.rampThreshold = FindProperty("_RampThreshold", properties);
        basicProperties.rampCutoff = FindProperty("_RampCutoff", properties);
        basicProperties.ignoreIndirect = FindProperty("_IgnoreIndirect", properties);
        waterProperties.surfaceTiling = FindProperty("_SurfaceTiling", properties);
        waterProperties.foamTiling = FindProperty("_FoamTiling", properties);
        waterProperties.surfaceUseWorld = FindProperty("_SurfaceUseWorld", properties);
        waterProperties.waterColorShallow = FindProperty("_WaterColorShallow", properties);
        waterProperties.waterColorDeep = FindProperty("_WaterColorDeep", properties);
        waterProperties.waterColorFoam = FindProperty("_WaterColorFoam", properties);
        waterProperties.waterDepthCutoff = FindProperty("_WaterDepthCutoff", properties);
        waterProperties.flowVertexData = FindProperty("_VertexData", properties);
        waterProperties.flowDirection = FindProperty("_FlowDirection", properties);
        waterProperties.flowWaveFactor = FindProperty("_FlowWaveFactor", properties);
        waterProperties.waveAmount = FindProperty("_WaveAmount", properties);
        waterProperties.waveSpeed = FindProperty("_WaveSpeed", properties);
        waterProperties.waveStrength = FindProperty("_WaveStrength", properties);
        waterProperties.waveUseWorld = FindProperty("_WaveUseWorld", properties);
        waterProperties.surfaceDistort = FindProperty("_SurfaceDistort", properties);
        waterProperties.surfaceSpeed = FindProperty("_SurfaceSpeed", properties);
        waterProperties.surfaceStrength = FindProperty("_SurfaceStrength", properties);
        waterProperties.foamDistance = FindProperty("_FoamDistance", properties);
        waterProperties.foamFade = FindProperty("_FoamFade", properties);
        waterProperties.foamDepthMin = FindProperty("_FoamMinDepth", properties);
        waterProperties.foamDepthMax = FindProperty("_FoamMaxDepth", properties);
        waterProperties.foamDistort = FindProperty("_FoamDistort", properties);
        waterProperties.foamSpeed = FindProperty("_FoamSpeed", properties);
        waterProperties.foamSoftness = FindProperty("_FoamSoftness", properties);
        waterProperties.foamSoftNoise = FindProperty("_FoamSoftNoise", properties);
        waterProperties.foamHardEdge = FindProperty("_FoamHardEdge", properties);
        waterProperties.foamNoiseAmount = FindProperty("_FoamAmount", properties);
        waterProperties.foamStrength = FindProperty("_FoamStrength", properties);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        FindProperties(properties);
        this.materialEditor = materialEditor;
        Material material = materialEditor.target as Material;
        if (firstTimeApply)
        {
            MaterialChanged(material);
            firstTimeApply = false;
        }
        ShaderPropertiesGUI(material);
    }

    public void ShaderPropertiesGUI(Material material)
    {
        Header();
        EditorGUIUtility.labelWidth = 0f;
        EditorGUI.BeginChangeCheck();
        {
            ToonSketch.Water.EditorUtils.ModePopup(materialEditor, basicProperties);
            ToonSketch.Water.EditorUtils.MainSettings(materialEditor, basicProperties, waterProperties);
            ToonSketch.Water.EditorUtils.ColorSettings(materialEditor, waterProperties);
            ToonSketch.Shared.EditorUtils.RampSettings(materialEditor, basicProperties);
            ToonSketch.Shared.EditorUtils.LightSettings(materialEditor, basicProperties);
            ToonSketch.Water.EditorUtils.FlowSettings(materialEditor, waterProperties);
            ToonSketch.Water.EditorUtils.WaveSettings(materialEditor, waterProperties);
            ToonSketch.Water.EditorUtils.SurfaceSettings(materialEditor, waterProperties);
            ToonSketch.Water.EditorUtils.FoamSettings(materialEditor, waterProperties);
            ToonSketch.Shared.EditorUtils.AdvancedSettings(materialEditor, basicProperties);
        }
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var obj in basicProperties.cullMode.targets)
                MaterialChanged((Material)obj);
        }
    }

    public static void MaterialChanged(Material material)
    {
        ToonSketch.Water.EditorUtils.MaterialChanged(material);
    }
}