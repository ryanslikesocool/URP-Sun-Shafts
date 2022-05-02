// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

Shader "RenderFeature/SunShafts" {
	Properties {
        [HideInInspector] _MainTex ("Base", 2D) = "" {}
		[HideInInspector] _ColorBuffer ("Color", 2D) = "" {}
		[HideInInspector] _Skybox ("Skybox", 2D) = "" {}
	}

	HLSLINCLUDE

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
		//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		//#include "SunShaftFunctions.hlsl"

		struct Attributes {
			float4 positionOS : POSITION;
			float2 texcoord : TEXCOORD0;
		};

		struct Varyings {
			float4 positionCS : SV_POSITION;
			float2 uv : TEXCOORD0;
			float4 screenPos : TEXCOORD1;
		};

		struct Varyings_radial {
			float4 positionCS : SV_POSITION;
			float2 uv : TEXCOORD0;
			float2 blurVector : TEXCOORD1;
		};

        TEXTURE2D(_MainTex);
        TEXTURE2D(_ColorBuffer);
        TEXTURE2D(_Skybox);

        SAMPLER(sampler_MainTex);
        SAMPLER(sampler_ColorBuffer);
        SAMPLER(sampler_Skybox);

        uniform half4 _SunThreshold;
        uniform half4 _SunColor;
        uniform half4 _BlurRadius4;
        uniform half4 _SunPosition;
        uniform half4 _MainTex_TexelSize;

        #define SAMPLES_FLOAT 6.0f
        #define SAMPLES_INT 6

		Varyings vert(Attributes IN) {
            Varyings OUT;

            OUT.positionCS = TransformObjectToHClip(IN.positionOS);
            OUT.uv = IN.texcoord.xy;

		    return OUT;
		}

		Varyings_radial vert_radial(Attributes IN) {
            Varyings_radial OUT;

            OUT.positionCS = TransformObjectToHClip(IN.positionOS);

            OUT.uv =  IN.texcoord.xy;
            OUT.blurVector = (_SunPosition.xy - IN.texcoord.xy) * _BlurRadius4.xy;

            return OUT;
		}

		float4 fragScreen(Varyings IN) : SV_Target {
            float4 colorA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
            float4 colorB = SAMPLE_TEXTURE2D(_ColorBuffer, sampler_ColorBuffer, IN.uv);
            float4 depthMask = saturate(colorB * _SunColor);
            return 1.0 - (1.0 - colorA) * (1.0 - depthMask);
		}

		float4 fragAdd(Varyings IN) : SV_Target {
            float4 colorA = SAMPLE_TEXTURE2D (_MainTex, sampler_MainTex, IN.uv);
            float4 colorB = SAMPLE_TEXTURE2D (_ColorBuffer, sampler_ColorBuffer, IN.uv);
            float4 depthMask = saturate (colorB * _SunColor);
            return colorA + depthMask;
		}

		float4 frag_radial(Varyings_radial IN) : SV_Target {
            float4 color = float4(0, 0, 0, 0);
            for (int j = 0; j < SAMPLES_INT; j++) {
                float4 tmpColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                color += tmpColor;
                IN.uv.xy += IN.blurVector;
            }
            return color / SAMPLES_FLOAT;
		}

        float TransformColor(float4 skyboxValue) {
            // threshold and convert to greyscale
            return dot(max(skyboxValue.rgb - _SunThreshold.rgb, float3(0, 0, 0)), float3(1, 1, 1));
        }

		float4 frag_depth(Varyings IN) : SV_Target {
			float depthSample = SampleSceneDepth(IN.uv);

            float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

            depthSample = Linear01Depth(depthSample, _ZBufferParams);

            // consider maximum radius
            float2 vec = _SunPosition.xy - IN.uv.xy;
            float dist = saturate(_SunPosition.w - length(vec.xy));

            float4 outColor = 0;

            // consider shafts blockers
            if (depthSample > 0.99f) {
                outColor = TransformColor(tex) * dist;
            }

            return outColor;
		}

		float4 frag_nodepth(Varyings IN) : SV_Target {
            float4 sky = SAMPLE_TEXTURE2D(_Skybox, sampler_Skybox, IN.uv);
            float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

            // consider maximum radius
            float2 vec = _SunPosition.xy - IN.uv.xy;
            float dist = saturate(_SunPosition.w - length(vec));

            float4 outColor = 0;

            // find unoccluded sky pixels
            // consider pixel values that differ significantly between framebuffer and sky-only buffer as occluded
            if (Luminance(abs(sky.rgb - tex.rgb)) < 0.2f) {
                outColor = TransformColor(sky) * dist;
            }

            return outColor;
		}

	ENDHLSL

	Subshader {
		Tags {
            "RenderPipeline"="UniversalPipeline"
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
        }

		ZTest Always
		Cull Off
		ZWrite Off

		Pass { // 0
			Name "Fragment Screen"

			HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing

			#pragma vertex vert
			#pragma fragment fragScreen

			ENDHLSL
		}

		Pass { // 1
			Name "Fragment Radial"

			HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing

			#pragma vertex vert_radial
			#pragma fragment frag_radial

			ENDHLSL
		}

		Pass { // 2
			Name "Fragment Depth"

			HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing

			#pragma vertex vert
			#pragma fragment frag_depth

			ENDHLSL
		}

		Pass { // 3
			Name "Fragment No Depth"

			HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing

			#pragma vertex vert
			#pragma fragment frag_nodepth

			ENDHLSL
		}

		Pass { // 4
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