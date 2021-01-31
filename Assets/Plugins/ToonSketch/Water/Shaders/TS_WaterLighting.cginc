#ifndef TOONSKETCH_WATERLIGHTING_INCLUDED
#define TOONSKETCH_WATERLIGHTING_INCLUDED

TS_LightingData TS_WaterCalculateLighting(inout FragmentCommonData s, UnityLight light, UnityIndirect indirect, half atten, half2 uv)
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
	l.ambient = s.diffColor * l.indirect.diffuse * TS_GetRamp(l.ambientDiff);
#endif
	// Diffuse
	l.diffuse = DotClamped(l.normal, l.light.dir);
	if (_Style == 0)
		l.diffuse = l.diffuse * 0.5 + 0.5;
	// Color
	l.color = l.ambient + s.diffColor * l.light.color * TS_GetRamp(l.diffuse);
	// Alpha/Attenuation
	l.attenuation = atten;
	l.alpha = s.alpha;
	// Output
	return l;
}

half4 TS_WaterShading(FragmentCommonData s, TS_LightingData l)
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