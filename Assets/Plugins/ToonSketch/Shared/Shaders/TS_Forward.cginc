#ifndef TOONSKETCH_FORWARD_INCLUDED
#define TOONSKETCH_FORWARD_INCLUDED

#include "TS_Main.cginc"
#include "TS_Lighting.cginc"

VertexOutputForwardBase TS_VertForwardBase(VertexInput v)
{
#ifdef TS_VERTEXFORWARDBASE_PREPASS
	TS_VERTEXFORWARDBASE_PREPASS(v);
#endif
	VertexOutputForwardBase o = vertForwardBase(v);
#ifdef TS_VERTEXFORWARDBASE_OUTPUT
	return TS_VERTEXFORWARDBASE_OUTPUT(v, o);
#else
	return o;
#endif
}

VertexOutputForwardAdd TS_VertForwardAdd(VertexInput v)
{
#ifdef TS_VERTEXFORWARDADD_PREPASS
	TS_VERTEXFORWARDADD_PREPASS(v);
#endif
	VertexOutputForwardAdd o = vertForwardAdd(v);
#ifdef TS_VERTEXFORWARDADD_OUTPUT
	return TS_VERTEXFORWARDADD_OUTPUT(v, o);
#else
	return o;
#endif
}

fixed4 TS_FragForwardBase(VertexOutputForwardBase i) : SV_Target
{
	UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	FragmentCommonData s = TS_FragmentSetup(i.tex, i.eyeVec.xyz, IN_VIEWDIR4PARALLAX(i), i.tangentToWorldAndPackedData, IN_WORLDPOS(i));

	UnityLight light = MainLight();
	UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);

#ifdef _TS_GLOSSREFLECT_ON
	UnityGI gi = FragmentGI(s, 1, i.ambientOrLightmapUV, 1, light, true);
#else
	UnityGI gi = FragmentGI(s, 1, i.ambientOrLightmapUV, 1, light, false);
#endif

	TS_LightingData l = TS_CalculateLighting(s, gi.light, gi.indirect, atten, i.tex.xy);

#ifdef TS_VERTEXFORWARDBASE_SHADINGPASS
	half4 color = TS_VERTEXFORWARDBASE_SHADINGPASS(s, l);
#else
	half4 color = TS_BasicShading(s, l);
#endif

	UNITY_EXTRACT_FOG_FROM_EYE_VEC(i);
	UNITY_APPLY_FOG(_unity_fogCoord, color.rgb);

	return OutputForward(color, s.alpha);
}

fixed4 TS_FragForwardAdd(VertexOutputForwardAdd i) : SV_Target
{
	UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	FragmentCommonData s = TS_FragmentSetup(i.tex, i.eyeVec.xyz, IN_VIEWDIR4PARALLAX_FWDADD(i), i.tangentToWorldAndLightDir, IN_WORLDPOS_FWDADD(i));

	UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld)

	TS_LightingData l = TS_CalculateLighting(s, AdditiveLight(IN_LIGHTDIR_FWDADD(i), 1), ZeroIndirect(), atten, i.tex.xy);

#ifdef TS_VERTEXFORWARDADD_SHADINGPASS
	half4 color = TS_VERTEXFORWARDADD_SHADINGPASS(s, l);
#else
	half4 color = TS_BasicShading(s, l);
#endif

	UNITY_EXTRACT_FOG_FROM_EYE_VEC(i);
	UNITY_APPLY_FOG_COLOR(_unity_fogCoord, color.rgb, half4(0, 0, 0, 0));

	return OutputForward(color, s.alpha);
}

#endif