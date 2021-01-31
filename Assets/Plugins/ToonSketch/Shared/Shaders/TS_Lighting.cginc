#ifndef TOONSKETCH_LIGHTING_INCLUDED
#define TOONSKETCH_LIGHTING_INCLUDED

inline TS_LightingData TS_InitLightingData(FragmentCommonData s, UnityLight light, UnityIndirect indirect, half atten, half2 uv)
{
	TS_LightingData l;
	UNITY_INITIALIZE_OUTPUT(TS_LightingData, l);
	l.light = light;
	l.indirect = indirect;
	l.uv = uv;
	l.normal = Unity_SafeNormalize(s.normalWorld);
#ifdef _TS_BUMPMAP_ON
	float3 bump = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
	l.normal = BlendNormals(l.normal, bump);
#endif
	return l;
}

TS_LightingData TS_CalculateLighting(inout FragmentCommonData s, UnityLight light, UnityIndirect indirect, half atten, half2 uv)
{
	TS_LightingData l = TS_InitLightingData(s, light, indirect, atten, uv);
	// Surface
	half perceptualRoughness = SmoothnessToPerceptualRoughness(s.smoothness);
	half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
#ifdef UNITY_COLORSPACE_GAMMA
	half surfaceReduction = 0.28;
#else
	half surfaceReduction = (0.6 - 0.08 * perceptualRoughness);
#endif
	surfaceReduction = 1.0 - roughness * perceptualRoughness * surfaceReduction;
	half grazingTerm = saturate(s.smoothness + (1.0 - s.oneMinusReflectivity));
	float3 halfDir = Unity_SafeNormalize(float3(l.light.dir) + -s.eyeVec);
	float nh = DotClamped(l.normal, halfDir);
	half nv = abs(DotClamped(l.normal, -s.eyeVec));
	float lh = DotClamped(l.light.dir, halfDir);
	// Ambient
#ifdef _TS_IGNOREINDIRECT_ON
	l.ambient = 0;
#else
	l.ambientDiff = Luminance(l.indirect.diffuse);
	if (_Style == 0)
		l.ambientDiff = l.ambientDiff * 0.5 + 0.5;
#ifdef _TS_SPECULAR_ON
	l.ambientSpec = SpecularStrength(l.indirect.specular);
	l.ambientSpec = surfaceReduction * l.ambientSpec * FresnelLerpFast(1, grazingTerm, nv);
	l.ambientSpec = smoothstep(_SpecularThreshold - _SpecularCutoff * 0.5, _SpecularThreshold + _SpecularCutoff * 0.5, l.ambientSpec);
#else
	l.ambientSpec = 0;
#endif
	l.ambient = (s.diffColor + s.specColor * l.indirect.specular * l.ambientSpec) * l.indirect.diffuse * TS_GetRamp(l.ambientDiff);
#endif
	// Diffuse
	l.diffuse = DotClamped(l.normal, l.light.dir);
	if (_Style == 0)
		l.diffuse = l.diffuse * 0.5 + 0.5;
	// Specular
#ifdef _TS_SPECULAR_ON
    half specularPower = PerceptualRoughnessToSpecPower(perceptualRoughness);
    half invV = lh * lh * s.smoothness + perceptualRoughness * perceptualRoughness;
    half invF = lh;
	half specular = ((specularPower + 1) * pow (nh, specularPower)) / (8 * invV * invF + 1e-4h);
#ifdef UNITY_COLORSPACE_GAMMA
    specular = sqrt(max(1e-4f, specular));
#endif
#ifdef SHADER_API_MOBILE
	specular = clamp(specular, 0.0, 100.0);
#endif
	l.specular = smoothstep(_SpecularThreshold - _SpecularCutoff * 0.5, _SpecularThreshold + _SpecularCutoff * 0.5, specular);
	l.specular *= _SpecularIntensity;
#else
	l.specular = 0;
#endif
	// Color
	l.color = l.ambient + (s.diffColor + s.specColor * l.specular) * l.light.color * TS_GetRamp(l.diffuse);
	// Rim
#ifdef _TS_RIMLIGHT_ON
	l.rim = smoothstep(_RimMin, _RimMax, 1.0 - DotClamped(-s.eyeVec, l.normal));
	l.rim *= _RimIntensity;
#ifdef _TS_RIMCOLORING_ON
	half3 rim = l.color * _RimColor * l.rim;
#else
	half3 rim = _RimColor * l.rim;
#endif
	if (_RimType == 0)
		l.color += rim;
	else if (_RimType == 1)
		l.color = lerp(l.color, l.color * rim, l.rim);
#else
	l.rim = 0;
#endif
	// Alpha/Attenuation
	l.attenuation = atten;
	l.alpha = s.alpha;
	// Output
	return l;
}

half4 TS_BasicShading(FragmentCommonData s, TS_LightingData l)
{
	// Color
	half4 color;
	color.rgb = l.color;
	color.a = l.alpha;
	// Shading
	color.rgb *= TS_GetRamp(l.attenuation);
	// Alpha/Attenuation
#ifndef UNITY_PASS_FORWARDBASE
	color *= l.attenuation;
#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
	color *= s.alpha;
#endif
#endif
	// Output
	return color;
}

#endif