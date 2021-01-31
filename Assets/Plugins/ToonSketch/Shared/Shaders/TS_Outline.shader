Shader "Hidden/ToonSketch/Outlines"
{
    Properties
    {
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
		_OutlineWidth("Outline Width", Range(0, 20)) = 1
		_OutlineSaturation("Outline Saturation", Range(0, 1)) = 0.5
		_OutlineBrightness("Outline Brightness", Range(0, 1)) = 0.5
		_OutlineAngle("Outline Angle Cutoff", Range(0, 180)) = 90
    }
    SubShader
    {
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 200

		Pass
		{
			Name "OUTLINE"
			Tags { "LightMode" = "ForwardBase" }
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Front
			ZWrite On
			ZTest Less

			CGPROGRAM
			#pragma target 3.0

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON

			#pragma shader_feature _ _TS_BUMPMAP_ON

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma vertex TS_VertOutlines
			#pragma fragment TS_FragOutlines

			#include "../../Shared/Shaders/TS_Outlines.cginc"
			ENDCG
		}
    }
}
