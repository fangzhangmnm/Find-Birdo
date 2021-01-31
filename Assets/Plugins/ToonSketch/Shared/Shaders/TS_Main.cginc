#ifndef TOONSKETCH_MAIN_INCLUDED
#define TOONSKETCH_MAIN_INCLUDED

#include "UnityStandardConfig.cginc"
#include "UnityStandardCore.cginc"

#include "TS_Input.cginc"

half3 TS_GetRamp(float2 uv)
{
#ifdef _TS_RAMPTEX_ON
	return saturate(tex2D(_RampTex, uv));
#else
	half ramp = smoothstep(_RampThreshold - _RampCutoff * 0.5, _RampThreshold + _RampCutoff * 0.5, uv);
	return half3(ramp, ramp, ramp);
#endif
}

half4 TS_GetAlbedo(float2 uv)
{
	return tex2D(_MainTex, uv) * _Color;
}

half4 TS_GetSpecular(float2 uv, half alpha)
{
#ifdef _TS_SPECULAR_ON
	half4 specular;
	if (_SmoothnessType == 0)
	{
		specular.rgb = tex2D(_SpecularTex, uv) * _SpecularColor;
		specular.a = alpha;
	}
	else if (_SmoothnessType == 1)
		specular = tex2D(_SpecularTex, uv) * _SpecularColor;
	else
		specular = half4(0, 0, 0, 0);
	specular.a *= _Smoothness;
	return specular;
#else
	return half4(0, 0, 0, 0);
#endif
}

inline FragmentCommonData TS_SpecularSetup(float2 uv)
{
	half4 albedo = TS_GetAlbedo(uv);
	half3 diffColor = albedo.rgb;

#ifdef _TS_SPECULAR_ON
	half4 specular = TS_GetSpecular(uv, albedo.a);
	half3 specColor;
	if (_SpecularType == 0)
		specColor = specular.rgb;
	else if (_SpecularType == 1)
		specColor = specular.rgb * diffColor;
	half smoothness = specular.a;
#else
	half3 specColor = half3(0, 0, 0);
	half smoothness = 0;
#endif

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

inline FragmentCommonData TS_FragmentSetup(inout float4 i_tex, float3 i_eyeVec, half3 i_viewDirForParallax, float4 tangentToWorld[3], float3 i_posWorld)
{
	i_tex = Parallax(i_tex, i_viewDirForParallax);

	half alpha = Alpha(i_tex.xy);
#if defined(_ALPHATEST_ON)
	clip(alpha - _Cutoff);
#endif

	FragmentCommonData o = TS_SpecularSetup(i_tex.xy);
	o.normalWorld = PerPixelWorldNormal(i_tex, tangentToWorld);
	o.eyeVec = NormalizePerPixelNormal(i_eyeVec);
	o.posWorld = i_posWorld;

	o.diffColor = PreMultiplyAlpha(o.diffColor, alpha, o.oneMinusReflectivity, /*out*/ o.alpha);
	return o;
}

#endif