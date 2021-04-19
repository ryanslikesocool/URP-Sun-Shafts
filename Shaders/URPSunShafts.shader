Shader "RenderFeatures/URPSunShafts"
{	
	Properties
	{
		[HideInInspector] _ColorTexture ("Color Texture", 2D) = "" {}
	}
	
	HLSLINCLUDE

	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
	#include "SunShaftFunctions.hlsl"

	struct Attributes
	{
		float4 positionOS : POSITION;
		float2 texcoord : TEXCOORD0;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};
	
	struct Varyings
	{
		float4 positionCS : SV_POSITION;
		float2 uv : TEXCOORD0;
		float4 screenPos : TEXCOORD1;
		UNITY_VERTEX_OUTPUT_STEREO
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};
	
	struct Varyings_radial
	{
		float4 positionCS : SV_POSITION;
		float2 uv : TEXCOORD0;
		float2 blurVector : TEXCOORD1;
		UNITY_VERTEX_OUTPUT_STEREO
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};
	
	SAMPLER(sampler_ColorTexture);

	CBUFFER_START(UnityPerMaterial)
		TEXTURE2D(_ColorTexture);
		TEXTURE2D(_SunShaftColorBuffer);
		TEXTURE2D(_Skybox);

		uniform float4 _ColorTexture_TexelSize;
	CBUFFER_END

	uniform float _Opacity;
	uniform float _DepthThreshold;
	uniform float4 _SunColor;
	uniform float4 _SunThreshold;
	uniform float4 _SunPosition;
	uniform float4 _BlurRadius;
			
	Varyings vert(Attributes IN)
	{
		Varyings OUT;

		UNITY_SETUP_INSTANCE_ID(IN);
		UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

		OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
		OUT.screenPos = ComputeScreenPos(OUT.positionCS);
		
		OUT.uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
		#if UNITY_UV_STARTS_AT_TOP
		if (_ColorTexture_TexelSize.y < 0)
		{
			OUT.uv.y = 1 - OUT.uv.y;
		}
		#endif			
		
		return OUT;
	}

	Varyings_radial vert_radial(Attributes IN)
	{
		Varyings_radial OUT;

		UNITY_SETUP_INSTANCE_ID(IN);
		UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

		OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);

		OUT.uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
		OUT.blurVector = (_SunPosition.xy - IN.texcoord.xy) * _BlurRadius.xy;	
		
		return OUT; 
	}
		
	float4 fragScreen(Varyings IN) : SV_Target
	{ 
		UNITY_SETUP_INSTANCE_ID(IN);
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

		float4 colorA = SAMPLE_TEXTURE2D(_ColorTexture, sampler_ColorTexture, IN.uv);
		float4 colorB = SAMPLE_TEXTURE2D(_SunShaftColorBuffer, sampler_ColorTexture, IN.uv);

		float4 depthMask = saturate(colorB * _SunColor);

		/*if (distance(IN.uv, _SunPosition.xy) < 0.025) {
			depthMask = float4(1, 0, 1, 1);
		}
		if (distance(IN.uv.x, 0.5) < 0.0025) {
			depthMask = float4(0, 1, 1, 1);
		}
		if (distance(IN.uv.y, 0.5) < 0.0025) {
			depthMask = float4(0, 1, 1, 1);
		}*/
		return saturate(1.0f - (1.0f - colorA) * (1.0f - depthMask * _Opacity));	
	}

	float4 fragAdd(Varyings IN) : SV_Target
	{ 
		UNITY_SETUP_INSTANCE_ID(IN);
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
		
		float4 colorA = SAMPLE_TEXTURE2D(_ColorTexture, sampler_ColorTexture, IN.uv);
		float4 colorB = SAMPLE_TEXTURE2D(_SunShaftColorBuffer, sampler_ColorTexture, IN.uv);

		float4 depthMask = saturate(colorB * _SunColor);	
		return saturate(colorA + depthMask * _Opacity);	
	}
	
	float4 frag_radial(Varyings_radial IN) : SV_Target 
	{
		UNITY_SETUP_INSTANCE_ID(IN);
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

		float4 color = 0;
		for(int j = 0; j < 6; j++)   
		{	
			float4 tmpColor = SAMPLE_TEXTURE2D(_ColorTexture, sampler_ColorTexture, IN.uv);
			color += tmpColor;
			IN.uv.xy += IN.blurVector;	
		}
		return saturate(color / 6.0);
	}
	
	float4 frag_depth(Varyings IN) : SV_Target
	{
		UNITY_SETUP_INSTANCE_ID(IN);
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

		float depthSample = SampleSceneDepth(IN.screenPos.xy / IN.screenPos.w);
		
		float4 tex = SAMPLE_TEXTURE2D(_ColorTexture, sampler_ColorTexture, IN.uv);
		
		depthSample = Linear01Depth(depthSample, _ZBufferParams);
		 
		float2 vec = _SunPosition.xy - IN.uv.xy;		
		float dist = saturate(_SunPosition.w - length(vec.xy));		
				
		float4 outColor = 0;

		if (depthSample > _DepthThreshold)
		{
			outColor = transformColor(tex, _SunThreshold.rgb) * dist;
		}
		return outColor;
	}
	
	float4 frag_nodepth(Varyings IN) : SV_Target
	{
		UNITY_SETUP_INSTANCE_ID(IN);
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

		float4 sky = SAMPLE_TEXTURE2D(_Skybox, sampler_ColorTexture, IN.uv);
		float4 tex = SAMPLE_TEXTURE2D(_ColorTexture, sampler_ColorTexture, IN.uv);
		
		float2 vec = _SunPosition.xy - IN.uv.xy;
		float dist = saturate(_SunPosition.w - length(vec));			
		
		float4 outColor = 0;

		if (Luminance(abs(sky.rgb - tex.rgb)) > _DepthThreshold)
		{
			outColor = transformColor(tex, _SunThreshold.xyz) * dist;
		}

		return outColor;
	}	

	ENDHLSL
	
	Subshader
	{
		Tags
        {
            "RenderPipeline"="UniversalPipeline"
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
        }

		Pass // 0
		{
			ZTest Always
			Cull Off
			ZWrite Off

			HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing

			#pragma vertex vert
			#pragma fragment fragScreen
			
			ENDHLSL
		}
		
		Pass // 1
		{
			ZTest Always
			Cull Off
			ZWrite Off

			HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
			
			#pragma vertex vert_radial
			#pragma fragment frag_radial
			
			ENDHLSL
		}
		
		Pass // 2
		{
			ZTest Always
			Cull Off
			ZWrite Off

			HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
			
			#pragma vertex vert
			#pragma fragment frag_depth
			
			ENDHLSL
		}
		
		Pass // 3
		{
			ZTest Always
			Cull Off
			ZWrite Off

			HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
			
			#pragma vertex vert
			#pragma fragment frag_nodepth
			
			ENDHLSL
		} 
		
		Pass // 4
		{
			ZTest Always
			Cull Off
			ZWrite Off

			HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
			
			#pragma vertex vert
			#pragma fragment fragAdd
			
			ENDHLSL
		} 
	}

	Fallback off
}