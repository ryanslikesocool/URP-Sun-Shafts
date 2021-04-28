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
		#include "HLSLSupport.cginc"
		#include "SunShaftFunctions.hlsl"

		struct Attributes
		{
			half4 positionOS : POSITION;
			half2 texcoord : TEXCOORD0;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};
		
		struct Varyings
		{
			half4 positionCS : SV_POSITION;
			half2 uv : TEXCOORD0;
			half4 screenPos : TEXCOORD1;
			half3 sunScreenPosition : TEXCOORD2;
			UNITY_VERTEX_OUTPUT_STEREO
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};
		
		struct Varyings_radial
		{
			half4 positionCS : SV_POSITION;
			half2 uv : TEXCOORD0;
			half2 blurVector : TEXCOORD1;
			half3 sunScreenPosition : TEXCOORD2;
			UNITY_VERTEX_OUTPUT_STEREO
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};
		
		CBUFFER_START(UnityPerMaterial)
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_ColorTexture);
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_SunShaftColorBuffer);
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_Skybox);

			uniform half4 _ColorTexture_TexelSize;
		CBUFFER_END

		half _Opacity;
		half4 _SunColor;
		uniform half _DepthThreshold;
		uniform half4 _SunThreshold;
		uniform half4 _BlurRadius;
		uniform half4 _SunPosition;

		Varyings vert(Attributes IN)
		{
			Varyings OUT;

			UNITY_SETUP_INSTANCE_ID(IN);
			UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

			OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
			OUT.screenPos = ComputeScreenPos(OUT.positionCS);
			OUT.sunScreenPosition = worldToScreenPosition(_SunPosition.xyz);
			
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
			half3 sunScreenPosition = worldToScreenPosition(_SunPosition.xyz);
			OUT.sunScreenPosition = sunScreenPosition;
			
			OUT.uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
			OUT.blurVector = (sunScreenPosition.xy - IN.texcoord.xy) * _BlurRadius.xy;	
			
			return OUT; 
		}
			
		half4 fragScreen(Varyings IN) : SV_Target
		{ 
			UNITY_SETUP_INSTANCE_ID(IN);
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

			half4 colorA = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_ColorTexture, IN.uv);
			half4 colorB = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_SunShaftColorBuffer, IN.uv);

			half4 depthMask = saturate(colorB * _SunColor);

			return saturate(1.0f - (1.0f - colorA) * (1.0f - depthMask * _Opacity));	
		}

		half4 fragAdd(Varyings IN) : SV_Target
		{ 
			UNITY_SETUP_INSTANCE_ID(IN);
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
			
			half4 colorA = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_ColorTexture, IN.uv);
			half4 colorB = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_SunShaftColorBuffer, IN.uv);

			half4 depthMask = saturate(colorB * _SunColor);	
			return saturate(colorA + depthMask * _Opacity);	
		}
		
		half4 frag_radial(Varyings_radial IN) : SV_Target 
		{
			UNITY_SETUP_INSTANCE_ID(IN);
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

			half4 color = 0;
			for(int j = 0; j < 6; j++)   
			{	
				half4 tmpColor = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_ColorTexture, IN.uv);
				color += tmpColor;
				IN.uv.xy += IN.blurVector;	
			}
			return saturate(color / 6.0);
		}
		
		half4 frag_depth(Varyings IN) : SV_Target
		{
			UNITY_SETUP_INSTANCE_ID(IN);
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

			half depthSample = SampleSceneDepth(IN.screenPos.xy / IN.screenPos.w);
			
			half4 tex = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_ColorTexture, IN.uv);
			
			depthSample = Linear01Depth(depthSample, _ZBufferParams);
			
			half2 vec = IN.sunScreenPosition.xy - IN.uv.xy;		
			half dist = saturate(_SunPosition.w - length(vec.xy));		
					
			half4 outColor = 0;

			if (depthSample > _DepthThreshold)
			{
				outColor = transformColor(tex, _SunThreshold.rgb) * dist;
			}
			return outColor;
		}
		
		half4 frag_nodepth(Varyings IN) : SV_Target
		{
			UNITY_SETUP_INSTANCE_ID(IN);
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

			half4 sky = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_Skybox, IN.uv);
			half4 tex = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_ColorTexture, IN.uv);
			
			half2 vec = IN.sunScreenPosition.xy - IN.uv.xy;
			half dist = saturate(_SunPosition.w - length(vec));			
			
			half4 outColor = 0;

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

		ZTest Always
		Cull Off
		ZWrite Off

		Pass // 0
		{
			Name "Fragment Screen"

			HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing

			#pragma vertex vert
			#pragma fragment fragScreen
			
			ENDHLSL
		}
		
		Pass // 1
		{
			Name "Fragment Radial"

			HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
			
			#pragma vertex vert_radial
			#pragma fragment frag_radial
			
			ENDHLSL
		}
		
		Pass // 2
		{
			Name "Fragment Depth"

			HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
			
			#pragma vertex vert
			#pragma fragment frag_depth
			
			ENDHLSL
		}
		
		Pass // 3
		{
			Name "Fragment No Depth"

			HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
			
			#pragma vertex vert
			#pragma fragment frag_nodepth
			
			ENDHLSL
		} 
		
		Pass // 4
		{
			Name "Fragment Add"

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