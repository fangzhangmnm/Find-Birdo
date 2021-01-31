#ifndef TOONSKETCH_WATERMAIN_INCLUDED
#define TOONSKETCH_WATERMAIN_INCLUDED

#include "TS_WaterInput.cginc"
#include "../../Shared/Shaders/Noise/Noise.cginc"

float3 GetWaterFlow(float4 color)
{
#ifdef _USEVERTEXFLOW
	return color.xyz;
#else
	return float3(_FlowDirection, _FlowWaveFactor);
#endif
}

float CalculateWaveHeight(float2 pos, float3 flow)
{
	float noise = snoise(pos * (5 * _WaveAmount) + (_Time.y * flow.xy * flow.z));
	float time = _Time.y * (5 * _WaveSpeed);
	float wave = ((sin(noise + time) + sin(noise * 2.3 + time * 1.5) + sin(noise * 3.3 + time * 0.4)) / 3) * 0.5 + 0.5;
	return saturate(wave * _WaveStrength);
}

void TS_ApplyVertexWaves(inout TS_VertexInput v)
{
	float3 flow = GetWaterFlow(v.color);
#ifdef _WAVE_USEWORLD
	float2 pos = mul(unity_ObjectToWorld, v.vertex).xz;
#else
	float2 pos = v.uv0;
#endif
	v.vertex.y += (CalculateWaveHeight(pos, flow) - CalculateWaveHeight(0, flow));
}

half2 GetTextureUV(float2 uv, float3 flow, float speed, float distortion)
{
	uv += _Time.y * flow.xy;
	float noise = snoise(uv);
	float time = _Time.y * (5 * speed);
	float wave = ((sin(noise + time) + sin(noise * 2.3 + time * 1.5) + sin(noise * 3.3 + time * 0.4)) / 3) * 0.5 + 0.5;
	return uv + (wave * distortion);
}

float4 BlendColors(float4 a, float4 b)
{
	float3 color = (a.rgb * a.a) + (b.rgb * (1 - a.a));
	float alpha = a.a + b.a * (1 - a.a);
	return float4(color, alpha);
}

half4 GetWaterColor(float2 uv, half4 color, float3 viewNormal, float3 posWorld, float4 screenPos)
{
	float epsilon = 0.00001;
	float depth;
	float3 normals;

	DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, screenPos.xyz / screenPos.w), depth, normals);

	depth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(screenPos));
	depth = LinearEyeDepth(depth) - screenPos.w;

	float waterDepth = saturate(depth / max(_WaterDepthCutoff, epsilon));
	half4 waterColor = lerp(_WaterColorShallow, _WaterColorDeep, waterDepth);
	
	float3 flow = GetWaterFlow(color);
	
	float2 uv0, uv1;

	#ifdef _SURFACE_USEWORLD
		uv0 = posWorld.xz * _SurfaceTiling;
		uv1 = posWorld.xz * _FoamTiling;
	#else
		uv0 = uv * _SurfaceTiling;
		uv1 = uv * _FoamTiling;
	#endif

	half4 surfaceTex = tex2D(_MainTex, GetTextureUV(uv0, flow, _SurfaceSpeed, _SurfaceDistort));
	half4 foamTex = tex2D(_MainTex, GetTextureUV(uv1, flow, _FoamSpeed, _FoamDistort));

	float foamNoise = saturate(snoise(uv1 * (5 * _FoamAmount) + (_Time.y * _FoamSpeed)) * 0.5 + 0.5);
	float foamDistance = lerp(_FoamMaxDepth, _FoamMinDepth, saturate(dot(normals, viewNormal)));

	float foamDepth;
	foamDepth = saturate(depth / max(foamDistance * _FoamHardEdge, epsilon));
	float hardFoam = smoothstep(foamDepth - epsilon, foamDepth + epsilon, foamNoise);

	foamDepth = saturate(depth / max(foamDistance, epsilon));
	float softFoam = smoothstep(foamDepth - _FoamSoftness, foamDepth + _FoamSoftness, lerp(1, foamNoise, _FoamSoftNoise));
	foamDepth = saturate(depth / max(foamDistance * _FoamDistance, epsilon));
	softFoam = lerp(0, softFoam, smoothstep(_FoamFade, 0, foamDepth));
	
	half4 output = waterColor;
	output.rgb -= _SurfaceStrength * surfaceTex;
	output.rgb += softFoam * (_FoamStrength * _WaterColorFoam * foamTex);
	output = BlendColors(_WaterColorFoam * hardFoam, saturate(output));
	return output;
}

inline FragmentCommonData TS_WaterSpecularSetup(half4 color)
{
	half3 diffColor = color.rgb;
	half3 specColor = half3(0, 0, 0);
	half smoothness = 0;

	half3 specularOut;
	half oneMinusReflectivity;
	diffColor = DiffuseAndSpecularFromMetallic(diffColor, 0, specularOut, oneMinusReflectivity);
	specColor *= specularOut;

	FragmentCommonData o;
	UNITY_INITIALIZE_OUTPUT(FragmentCommonData, o);
	o.diffColor = diffColor;
	o.specColor = specColor;
	o.oneMinusReflectivity = oneMinusReflectivity;
	o.smoothness = smoothness;
	return o;
}

inline FragmentCommonData TS_WaterFragmentSetup(inout float4 i_tex, float3 i_eyeVec, half3 i_viewDirForParallax, float4 tangentToWorld[3], float3 i_posWorld,
	half4 i_color, float3 i_viewNormal, float4 i_screenPos)
{
	i_tex = Parallax(i_tex, i_viewDirForParallax);

	half4 color = GetWaterColor(i_tex.xy, i_color, i_viewNormal, i_posWorld, i_screenPos);

	half alpha = color.a;
#if defined(_ALPHATEST_ON)
	clip(alpha - _Cutoff);
#endif

	FragmentCommonData o = TS_WaterSpecularSetup(color);

	o.normalWorld = PerPixelWorldNormal(i_tex, tangentToWorld);
	o.eyeVec = NormalizePerPixelNormal(i_eyeVec);
	o.posWorld = i_posWorld;

	o.diffColor = PreMultiplyAlpha(o.diffColor, alpha, o.oneMinusReflectivity, o.alpha);
	return o;
}

#endif