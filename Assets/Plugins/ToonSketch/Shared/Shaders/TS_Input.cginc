#ifndef TOONSKETCH_INPUT_INCLUDED
#define TOONSKETCH_INPUT_INCLUDED

int			_Style;

#ifdef _TS_RAMPTEX_ON
sampler2D   _RampTex;
#else
half		_RampThreshold;
half		_RampCutoff;
#endif

#ifdef _TS_SPECULAR_ON
sampler2D   _SpecularTex;
fixed4		_SpecularColor;
int			_SpecularType;
int			_SmoothnessType;
half		_Smoothness;
half		_SpecularThreshold;
half		_SpecularCutoff;
half		_SpecularIntensity;
#endif

#ifdef _TS_RIMLIGHT_ON
fixed4		_RimColor;
half		_RimMin;
half		_RimMax;
int			_RimType;
half		_RimIntensity;
#endif

struct TS_LightingData
{
	UnityLight light;
	UnityIndirect indirect;

	float2 uv;
	float3 normal;

	half3 ambient;
	half3 color;

	half ambientDiff;
	half ambientSpec;
	half diffuse;
	half specular;
	half rim;

	half attenuation;
	half alpha;
};

#endif