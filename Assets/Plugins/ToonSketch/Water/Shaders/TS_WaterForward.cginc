#ifndef TOONSKETCH_WATERFORWARD_INCLUDED
#define TOONSKETCH_WATERFORWARD_INCLUDED

#include "../../Shared/Shaders/TS_Forward.cginc"

#include "TS_WaterMain.cginc"
#include "TS_WaterLighting.cginc"

TS_WaterVertexOutputForwardBase TS_VertWaterForwardBase(TS_VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    TS_WaterVertexOutputForwardBase o;
    UNITY_INITIALIZE_OUTPUT(TS_WaterVertexOutputForwardBase, o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	TS_ApplyVertexWaves(v);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    #if UNITY_REQUIRE_FRAG_WORLDPOS
        #if UNITY_PACK_WORLDPOS_WITH_TANGENT
            o.tangentToWorldAndPackedData[0].w = posWorld.x;
            o.tangentToWorldAndPackedData[1].w = posWorld.y;
            o.tangentToWorldAndPackedData[2].w = posWorld.z;
        #else
            o.posWorld = posWorld.xyz;
        #endif
    #endif
    o.pos = UnityObjectToClipPos(v.vertex);

	o.tex = TexCoords((VertexInput)v);
    o.eyeVec.xyz = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
    float3 normalWorld = UnityObjectToWorldNormal(v.normal);
    #ifdef _TANGENT_TO_WORLD
        float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
        float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
        o.tangentToWorldAndPackedData[0].xyz = tangentToWorld[0];
        o.tangentToWorldAndPackedData[1].xyz = tangentToWorld[1];
        o.tangentToWorldAndPackedData[2].xyz = tangentToWorld[2];
    #else
        o.tangentToWorldAndPackedData[0].xyz = 0;
        o.tangentToWorldAndPackedData[1].xyz = 0;
        o.tangentToWorldAndPackedData[2].xyz = normalWorld;
    #endif
    UNITY_TRANSFER_LIGHTING(o, v.uv1);
    o.ambientOrLightmapUV = VertexGIForward((VertexInput)v, posWorld, normalWorld);
    #ifdef _PARALLAXMAP
        TANGENT_SPACE_ROTATION;
        half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
        o.tangentToWorldAndPackedData[0].w = viewDirForParallax.x;
        o.tangentToWorldAndPackedData[1].w = viewDirForParallax.y;
        o.tangentToWorldAndPackedData[2].w = viewDirForParallax.z;
    #endif
    UNITY_TRANSFER_FOG_COMBINED_WITH_EYE_VEC(o, o.pos);

	o.color = v.color;
	o.screenPos = ComputeScreenPos(o.pos);
	o.viewNormal = COMPUTE_VIEW_NORMAL;

	return o;
}

TS_WaterVertexOutputForwardAdd TS_VertWaterForwardAdd(TS_VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    TS_WaterVertexOutputForwardAdd o;
    UNITY_INITIALIZE_OUTPUT(TS_WaterVertexOutputForwardAdd, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	TS_ApplyVertexWaves(v);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    o.pos = UnityObjectToClipPos(v.vertex);

	o.tex = TexCoords((VertexInput)v);
    o.eyeVec.xyz = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
    o.posWorld = posWorld.xyz;
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
    #ifdef _TANGENT_TO_WORLD
        float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
        float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
        o.tangentToWorldAndLightDir[0].xyz = tangentToWorld[0];
        o.tangentToWorldAndLightDir[1].xyz = tangentToWorld[1];
        o.tangentToWorldAndLightDir[2].xyz = tangentToWorld[2];
    #else
        o.tangentToWorldAndLightDir[0].xyz = 0;
        o.tangentToWorldAndLightDir[1].xyz = 0;
        o.tangentToWorldAndLightDir[2].xyz = normalWorld;
    #endif
    UNITY_TRANSFER_LIGHTING(o, v.uv1);

	float3 lightDir = _WorldSpaceLightPos0.xyz - posWorld.xyz * _WorldSpaceLightPos0.w;
    #ifndef USING_DIRECTIONAL_LIGHT
        lightDir = NormalizePerVertexNormal(lightDir);
    #endif
    o.tangentToWorldAndLightDir[0].w = lightDir.x;
    o.tangentToWorldAndLightDir[1].w = lightDir.y;
    o.tangentToWorldAndLightDir[2].w = lightDir.z;
    #ifdef _PARALLAXMAP
        TANGENT_SPACE_ROTATION;
        o.viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
    #endif
    UNITY_TRANSFER_FOG_COMBINED_WITH_EYE_VEC(o, o.pos);

	o.color = v.color;
	o.screenPos = ComputeScreenPos(o.pos);
	o.viewNormal = COMPUTE_VIEW_NORMAL;

	return o;
}

fixed4 TS_FragWaterForwardBase(TS_WaterVertexOutputForwardBase i) : SV_Target
{
	UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	FragmentCommonData s = TS_WaterFragmentSetup(i.tex, i.eyeVec.xyz, IN_VIEWDIR4PARALLAX(i), i.tangentToWorldAndPackedData, IN_WORLDPOS(i),
		i.color, i.viewNormal, i.screenPos);

	UnityLight light = MainLight();
	UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);

	UnityGI gi = FragmentGI(s, 1, i.ambientOrLightmapUV, 1, light, false);

	TS_LightingData l = TS_WaterCalculateLighting(s, gi.light, gi.indirect, atten, i.tex.xy);

#ifdef TS_VERTEXFORWARDBASE_SHADINGPASS
	half4 color = TS_VERTEXFORWARDBASE_SHADINGPASS(s, l);
#else
	half4 color = TS_WaterShading(s, l);
#endif

	UNITY_EXTRACT_FOG_FROM_EYE_VEC(i);
	UNITY_APPLY_FOG(_unity_fogCoord, color.rgb);

	return OutputForward(color, s.alpha);
}

fixed4 TS_FragWaterForwardAdd(TS_WaterVertexOutputForwardAdd i) : SV_Target
{
	UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	FragmentCommonData s = TS_WaterFragmentSetup(i.tex, i.eyeVec.xyz, IN_VIEWDIR4PARALLAX_FWDADD(i), i.tangentToWorldAndLightDir, IN_WORLDPOS_FWDADD(i),
		i.color, i.viewNormal, i.screenPos);

	UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld)

	TS_LightingData l = TS_WaterCalculateLighting(s, AdditiveLight(IN_LIGHTDIR_FWDADD(i), 1), ZeroIndirect(), atten, i.tex.xy);

#ifdef TS_VERTEXFORWARDADD_SHADINGPASS
	half4 color = TS_VERTEXFORWARDADD_SHADINGPASS(s, l);
#else
	half4 color = TS_WaterShading(s, l);
#endif

	UNITY_EXTRACT_FOG_FROM_EYE_VEC(i);
	UNITY_APPLY_FOG_COLOR(_unity_fogCoord, color.rgb, half4(0, 0, 0, 0));

	return OutputForward(color, s.alpha);
}

#endif