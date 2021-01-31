using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ToonSketchBasicShaderGUI : ShaderGUI
{
    private ToonSketch.Shared.EditorParams.Properties basicProperties = new ToonSketch.Shared.EditorParams.Properties();
    private MaterialEditor materialEditor;
    private bool firstTimeApply = true;

    private void Header()
    {
        ToonSketch.Shared.EditorUtils.Header(
            ToonSketch.Shared.EditorParams.module,
            ToonSketch.Shared.EditorParams.version,
            "Toon Shader"
        );
    }

    public void FindProperties(MaterialProperty[] properties)
    {
        basicProperties.styleMode = FindProperty("_Style", properties);
        basicProperties.blendMode = FindProperty("_Blend", properties);
        basicProperties.cullMode = FindProperty("_Cull", properties);
        basicProperties.albedoTexture = FindProperty("_MainTex", properties);
        basicProperties.albedoColor = FindProperty("_Color", properties);
        basicProperties.alphaCutoff = FindProperty("_Cutoff", properties);
        basicProperties.rampTextureEnable = FindProperty("_Ramp", properties);
        basicProperties.rampTexture = FindProperty("_RampTex", properties);
        basicProperties.rampThreshold = FindProperty("_RampThreshold", properties);
        basicProperties.rampCutoff = FindProperty("_RampCutoff", properties);
        basicProperties.bumpEnable = FindProperty("_Bump", properties);
        basicProperties.bumpTexture = FindProperty("_BumpMap", properties);
        basicProperties.bumpStrength = FindProperty("_BumpScale", properties);
        basicProperties.specularEnable = FindProperty("_Specular", properties);
        basicProperties.specularTexture = FindProperty("_SpecularTex", properties);
        basicProperties.specularColor = FindProperty("_SpecularColor", properties);
        basicProperties.specularType = FindProperty("_SpecularType", properties);
        basicProperties.smoothnessType = FindProperty("_SmoothnessType", properties);
        basicProperties.smoothness = FindProperty("_Smoothness", properties);
        basicProperties.glossyReflections = FindProperty("_GlossyReflections", properties);
        basicProperties.specularThreshold = FindProperty("_SpecularThreshold", properties);
        basicProperties.specularCutoff = FindProperty("_SpecularCutoff", properties);
        basicProperties.specularIntensity = FindProperty("_SpecularIntensity", properties);
        basicProperties.rimEnable = FindProperty("_RimLighting", properties);
        basicProperties.rimColor = FindProperty("_RimColor", properties);
        basicProperties.rimType = FindProperty("_RimType", properties);
        basicProperties.rimColoring = FindProperty("_RimColoring", properties);
        basicProperties.rimMin = FindProperty("_RimMin", properties);
        basicProperties.rimMax = FindProperty("_RimMax", properties);
        basicProperties.rimIntensity = FindProperty("_RimIntensity", properties);
        basicProperties.ignoreIndirect = FindProperty("_IgnoreIndirect", properties);
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
            ToonSketch.Shared.EditorUtils.ModePopup(materialEditor, basicProperties);
            ToonSketch.Shared.EditorUtils.MainSettings(materialEditor, basicProperties);
            ToonSketch.Shared.EditorUtils.RampSettings(materialEditor, basicProperties);
            ToonSketch.Shared.EditorUtils.BumpSettings(materialEditor, basicProperties);
            ToonSketch.Shared.EditorUtils.SpecularSettings(materialEditor, basicProperties);
            ToonSketch.Shared.EditorUtils.RimSettings(materialEditor, basicProperties);
            ToonSketch.Shared.EditorUtils.LightSettings(materialEditor, basicProperties);
            ToonSketch.Shared.EditorUtils.AdvancedSettings(materialEditor, basicProperties);
        }
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var obj in basicProperties.blendMode.targets)
                MaterialChanged((Material)obj);
        }
    }

    public static void MaterialChanged(Material material)
    {
        ToonSketch.Shared.EditorUtils.MaterialChanged(material);
    }
}