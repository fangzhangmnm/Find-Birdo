Shader "ToonSketch/Basic"
{
    Properties
    {
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		[Enum(Soft,0,Hard,1)] _Style("Style", Float) = 0
		[Enum(Off,0,Front,1,Back,2)] _Cull("Cull Mode", Float) = 2
		[Enum(Opaque,0,Cutout,1,Fade,2,Transparent,3)] _Blend("Blend Mode", Float) = 0

		[Toggle(_TS_RAMPTEX_ON)] _Ramp("Ramp Texture?", Float) = 0
		[NoScaleOffset] _RampTex("Ramp Texture", 2D) = "white" {}
		_RampThreshold("Ramp Threshold", Range(0, 1)) = 0.5
		_RampCutoff("Ramp Cutoff", Range(0, 1)) = 0.1

		[Toggle(_TS_BUMPMAP_ON)] _Bump("Bump Mapping?", Float) = 0
		[Normal] _BumpMap("Bump Map", 2D) = "bump" {}
		_BumpScale("Bump Strength", Range(0, 1)) = 1

		[Toggle(_TS_SPECULAR_ON)] _Specular("Specular Highlights?", Float) = 0
		_SpecularTex("Specular Texture", 2D) = "white" {}
		_SpecularColor("Specular Color", Color) = (1, 1, 1)
		[Enum(Additive,0,Multiply,1)] _SpecularType("Specular Type", Float) = 0
		[Enum(Albedo Alpha,0,Specular Alpha,1)] _SmoothnessType("Smoothness Type", Float) = 0
		_Smoothness("Smoothness", Range(0, 1)) = 0.5
		[Toggle(_TS_GLOSSREFLECT_ON)] _GlossyReflections("Glossy Reflections?", Float) = 1.0
		_SpecularThreshold("Specular Threshold", Range(0, 1)) = 0.5
		_SpecularCutoff("Specular Cutoff", Range(0, 1)) = 0.1
		_SpecularIntensity("Specular Intensity", Range(0, 10)) = 1

		[Toggle(_TS_RIMLIGHT_ON)] _RimLighting("Rim Lighting?", Float) = 0
		_RimColor("Rim Color", Color) = (1, 1, 1, 1)
		[Enum(Additive,0,Multiply,1)] _RimType("Rim Type", Float) = 0
		[Toggle(_TS_RIMCOLORING_ON)] _RimColoring("Rim Use Color?", Float) = 1
		_RimMin("Rim Min", Range(0, 1)) = 0.6
		_RimMax("Rim Max", Range(0, 1)) = 0.8
		_RimIntensity("Rim Intensity", Range(0, 10)) = 1

		[Toggle(_TS_IGNOREINDIRECT_ON)] _IgnoreIndirect("Ignore Indirect Lighting?", Float) = 0

		[HideInInspector] _SrcBlend("__src", Float) = 1
		[HideInInspector] _DstBlend("__dst", Float) = 0
		[HideInInspector] _ZWrite("__zw", Float) = 1
    }
	CGINCLUDE
		#define UNITY_SETUP_BRDF_INPUT SpecularSetup
	ENDCG
    SubShader
    {
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 200

        Pass
        {
			Name "FORWARD"
			Tags { "LightMode" = "ForwardBase" }
			Blend [_SrcBlend] [_DstBlend]
			Cull [_Cull]
			ZWrite [_ZWrite]
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON

			#pragma shader_feature _ _TS_RAMPTEX_ON
			#pragma shader_feature _ _TS_BUMPMAP_ON
			#pragma shader_feature _ _TS_SPECULAR_ON
			#pragma shader_feature _ _TS_GLOSSREFLECT_ON
			#pragma shader_feature _ _TS_RIMLIGHT_ON
			#pragma shader_feature _ _TS_RIMCOLORING_ON
			#pragma shader_feature _ _TS_IGNOREINDIRECT_ON

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma vertex TS_VertForwardBase
			#pragma fragment TS_FragForwardBase

			#include "../../Shared/Shaders/TS_Forward.cginc"
            ENDCG
        }
		Pass
        {
            Name "FORWARD_DELTA"
            Tags { "LightMode" = "ForwardAdd" }
            Blend [_SrcBlend] One
			Cull [_Cull]
            Fog { Color (0,0,0,0) }
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma target 3.0

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON

			#pragma shader_feature _ _TS_RAMPTEX_ON
			#pragma shader_feature _ _TS_BUMPMAP_ON
			#pragma shader_feature _ _TS_SPECULAR_ON
			#pragma shader_feature _ _TS_GLOSSREFLECT_ON
			#pragma shader_feature _ _TS_RIMLIGHT_ON
			#pragma shader_feature _ _TS_RIMCOLORING_ON

            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
			#pragma multi_compile_instancing

            #pragma vertex TS_VertForwardAdd
            #pragma fragment TS_FragForwardAdd

			#include "../../Shared/Shaders/TS_Forward.cginc"
            ENDCG
        }
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			Cull [_Cull]
			ZWrite On
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _ _TS_BUMPMAP_ON

			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"
			ENDCG
		}
    }
	Fallback "Diffuse"
	CustomEditor "ToonSketchBasicShaderGUI"
}
