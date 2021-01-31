Shader "ToonSketch/Water"
{
    Properties
    {
		[Enum(Soft,0,Hard,1)] _Style("Style", Float) = 0
		[Enum(Off,0,Front,1,Back,2)] _Cull("Cull Mode", Float) = 2

		[NoScaleOffset] _MainTex("Albedo", 2D) = "white" {}

		_SurfaceTiling("Surface Tiling", Float) = 1
		_FoamTiling("Foam Tiling", Float) = 1
		
		[Toggle(_SURFACE_USEWORLD)] _SurfaceUseWorld("Surface Uses World Space?", Float) = 0

		[Toggle(_TS_RAMPTEX_ON)] _Ramp("Ramp Texture?", Float) = 0
		[NoScaleOffset] _RampTex("Ramp Texture", 2D) = "white" {}
		_RampThreshold("Ramp Threshold", Range(0, 1)) = 0.5
		_RampCutoff("Ramp Cutoff", Range(0, 1)) = 0.1

		[Toggle(_TS_IGNOREINDIRECT_ON)] _IgnoreIndirect("Ignore Indirect Lighting?", Float) = 0

		_WaterColorShallow("Water Color (Shallow)", Color) = (0.3, 0.8, 0.9, 0.7)
		_WaterColorDeep("Water Color (Deep)", Color) = (0.1, 0.4, 1, 0.7)
		_WaterColorFoam("Water Color (Foam)", Color) = (1, 1, 1, 1)
		_WaterDepthCutoff("Water Depth Cutoff", Range(0, 20)) = 1
		
		[Toggle(_USEVERTEXFLOW)] _VertexData("Use Vertex Color Flow?", Float) = 0

		_FlowDirection("Flow Direction", Vector) = (0, 0, 0, 0)
		_FlowWaveFactor("Flow Wave Factor", Range(0, 1)) = 1

		[Toggle(_WAVE_USEWORLD)] _WaveUseWorld("Wave Uses World Space?", Float) = 0

		_WaveAmount("Wave Amount", Range(0, 1)) = 0.5
		_WaveSpeed("Wave Speed", Range(0, 1)) = 0.5
		_WaveStrength("Wave Strength", Range(0, 1)) = 0.5

		_SurfaceDistort("Surface Distortion", Range(0, 1)) = 0.1
		_SurfaceSpeed("Surface Speed", Range(0, 1)) = 0.5
		_SurfaceStrength("Surface Strength", Range(0, 1)) = 0.2
		
		_FoamDistance("Foam Distance", Range(0, 50)) = 50
		_FoamFade("Foam Fade", Range(0, 1)) = 1
		_FoamMaxDepth("Foam Maximum Depth", Range(0, 1)) = 0.8
		_FoamMinDepth("Foam Minimum Depth", Range(0, 1)) = 0.2
		_FoamDistort("Foam Distortion", Range(0, 1)) = 0.2
		_FoamSpeed("Foam Speed", Range(0, 1)) = 1
		_FoamSoftness("Foam Softness", Range(0, 1)) = 1
		_FoamSoftNoise("Foam Soft Noise", Range(0, 1)) = 0.5
		_FoamHardEdge("Foam Hard Edge", Range(0, 1)) = 0.5
		_FoamAmount("Foam Amount", Range(0, 1)) = 1
		_FoamStrength("Foam Strength", Range(0, 1)) = 1
		
		[HideInInspector] _ZWrite("__zw", Float) = 1
    }
    SubShader
    {
		Tags { "RenderType"="Transparent" "IgnoreProjector"="True" "PerformanceChecks"="False" "ForceNoShadowCasting"="True" }
		LOD 200

        Pass
        {
			Name "FORWARD"
			Tags { "LightMode" = "ForwardBase" }
			Blend SrcAlpha OneMinusSrcAlpha
			Cull [_Cull]
			ZWrite [_ZWrite]
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0

			#pragma shader_feature _ALPHABLEND_ON

			#pragma shader_feature _ _SURFACE_USEWORLD
			#pragma shader_feature _ _WAVE_USEWORLD
			#pragma shader_feature _ _USEVERTEXFLOW

			#pragma shader_feature _ _TS_RAMPTEX_ON
			#pragma shader_feature _ _TS_IGNOREINDIRECT_ON

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma vertex TS_VertWaterForwardBase
			#pragma fragment TS_FragWaterForwardBase

			#include "TS_WaterForward.cginc"
            ENDCG
        }
		Pass
        {
            Name "FORWARD_DELTA"
            Tags { "LightMode" = "ForwardAdd" }
			Blend SrcAlpha One
			Cull [_Cull]
            Fog { Color (0,0,0,0) }
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma target 3.0

			#pragma shader_feature _ALPHABLEND_ON

			#pragma shader_feature _ _SURFACE_USEWORLD
			#pragma shader_feature _ _WAVE_USEWORLD
			#pragma shader_feature _ _USEVERTEXFLOW

			#pragma shader_feature _ _TS_RAMPTEX_ON

            #pragma multi_compile_fwdadd
            #pragma multi_compile_fog
			#pragma multi_compile_instancing

            #pragma vertex TS_VertWaterForwardAdd
            #pragma fragment TS_FragWaterForwardAdd

			#include "TS_WaterForward.cginc"
            ENDCG
        }
	}
    FallBack "Diffuse"
	CustomEditor "ToonSketchWaterShaderGUI"
}