#ifndef TOONSKETCH_WATERINPUT_INCLUDED
#define TOONSKETCH_WATERINPUT_INCLUDED

sampler2D_float _CameraDepthTexture;
sampler2D_float _CameraDepthNormalsTexture;

float _SurfaceTiling, _FoamTiling;
float4 _WaterColorShallow, _WaterColorDeep, _WaterColorFoam;
float _WaterDepthCutoff;
float _SurfaceDistort, _SurfaceSpeed, _SurfaceStrength;
float _WaveAmount, _WaveSpeed, _WaveStrength;
float2 _FlowDirection;
float _FlowWaveFactor;
float _FoamAmount, _FoamDistort, _FoamSpeed, _FoamStrength;
float _FoamMaxDepth, _FoamMinDepth, _FoamDistance, _FoamHardEdge, _FoamSoftNoise, _FoamSoftness, _FoamFade;

struct TS_VertexInput
{
    float4 vertex		: POSITION;
	half3 normal		: NORMAL;
    float2 uv0			: TEXCOORD0;
    float2 uv1			: TEXCOORD1;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
    float2 uv2			: TEXCOORD2;
#endif
#ifdef _TANGENT_TO_WORLD
    half4 tangent		: TANGENT;
#endif
	half4 color			: COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct TS_WaterVertexOutputForwardBase
{
	UNITY_POSITION(pos);
	half4 color								: COLOR;
	float3 viewNormal						: NORMAL;
    float4 tex								: TEXCOORD0;
	float4 eyeVec							: TEXCOORD1;
	float4 tangentToWorldAndPackedData[3]	: TEXCOORD2;
    half4 ambientOrLightmapUV				: TEXCOORD5;
	UNITY_LIGHTING_COORDS(6, 7)
#if UNITY_REQUIRE_FRAG_WORLDPOS && !UNITY_PACK_WORLDPOS_WITH_TANGENT
    float3 posWorld							: TEXCOORD8;
#endif
    float4 screenPos						: TEXCOORD9;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct TS_WaterVertexOutputForwardAdd
{
	UNITY_POSITION(pos);
	half4 color							: COLOR;
	float3 viewNormal					: NORMAL;
    float4 tex                          : TEXCOORD0;
    float4 eyeVec                       : TEXCOORD1;
    float4 tangentToWorldAndLightDir[3]	: TEXCOORD2;
    float3 posWorld                     : TEXCOORD5;
    UNITY_LIGHTING_COORDS(6, 7)
#if defined(_PARALLAXMAP)
    half3 viewDirForParallax            : TEXCOORD8;
#endif
    float4 screenPos					: TEXCOORD9;
    UNITY_VERTEX_OUTPUT_STEREO
};

#endif