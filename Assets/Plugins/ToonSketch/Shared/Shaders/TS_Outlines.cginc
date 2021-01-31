#ifndef TOONSKETCH_OUTLINES_INCLUDED
#define TOONSKETCH_OUTLINES_INCLUDED

#include "TS_Main.cginc"

half		_OutlineWidth;
half4		_OutlineColor;
half		_OutlineSaturation;
half		_OutlineBrightness;
half		_OutlineAngle;

VertexOutputForwardBase TS_VertOutlines(VertexInput v)
{
	VertexOutputForwardBase o = vertForwardBase(v);

	half4 worldPos = mul(unity_ObjectToWorld, v.vertex);
	half4 worldNormal = mul(unity_ObjectToWorld, v.normal);
	float3 worldViewDir = worldPos - _WorldSpaceCameraPos;
	half dist = length(worldViewDir);

	half width = _OutlineWidth;
	half minWidth = width * 0.5;
	half maxWidth = width;
	width = clamp(width * dist, minWidth, maxWidth);

	half4 projPos = UnityObjectToClipPos(v.vertex);
	half4 projNormal = normalize(UnityObjectToClipPos(half4(v.normal, 0)));
	half4 projObjDir = normalize(UnityObjectToClipPos(v.vertex.xyz - float4(0, 0, 0, 1)));

	float viewDot = dot(normalize(worldViewDir), normalize(worldNormal));
	half viewAngle = degrees(acos(viewDot));

	half4 scaledDir = projNormal;
	if (viewAngle < _OutlineAngle)
	{
		scaledDir *= (1 - viewDot);
		width *= (1 - viewDot) * 0.5;
	}
	scaledDir = scaledDir * width * 0.00285;
	scaledDir += projNormal * 0.00001;

	o.pos = projPos + scaledDir;

	return o;
}

fixed4 TS_FragOutlines(VertexOutputForwardBase i) : COLOR
{
	UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	FragmentCommonData s = TS_FragmentSetup(i.tex, i.eyeVec.xyz, IN_VIEWDIR4PARALLAX(i), i.tangentToWorldAndPackedData, IN_WORLDPOS(i));

	UnityLight light = MainLight();

#ifdef _TS_GLOSSREFLECT_ON
	UnityGI gi = FragmentGI(s, 1, i.ambientOrLightmapUV, 1, light, true);
#else
	UnityGI gi = FragmentGI(s, 1, i.ambientOrLightmapUV, 1, light, false);
#endif

	half4 albedo = TS_GetAlbedo(i.tex.xy);

	half4 color = albedo;
	half maxVal = max(max(color.r, color.g), color.b);
	maxVal -= (1.0 / 255.0);
	half3 blend = saturate((color.rgb - float3(maxVal, maxVal, maxVal)) * 255.0);
	color.rgb = lerp(color.rgb * _OutlineSaturation, color.rgb, blend);
	color.rgb = half4(color.rgb * _OutlineBrightness * albedo.rgb, albedo.a) * _OutlineColor * (gi.indirect.diffuse + light.color);

	UNITY_EXTRACT_FOG_FROM_EYE_VEC(i);
	UNITY_APPLY_FOG(_unity_fogCoord, color.rgb);

	return OutputForward(color, s.alpha);
}

#endif