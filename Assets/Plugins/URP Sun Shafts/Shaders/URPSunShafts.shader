Shader "RenderFeatures/URPSunShafts" {	
	Properties {
		_MainTex ("Main Texture", 2D) = "" {}
		_Skybox ("Skybox", 2D) = "" {}
	}
	
	HLSLINCLUDE

	#include "UnityCG.cginc"
	
	struct v2f {
		float4 vertex : SV_POSITION;
		float2 uv : TEXCOORD0;
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv1 : TEXCOORD1;
		#endif
		UNITY_VERTEX_OUTPUT_STEREO
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};
	
	struct v2f_radial {
		float4 vertex : SV_POSITION;
		float2 uv : TEXCOORD0;
		float2 blurVector : TEXCOORD1;
		UNITY_VERTEX_OUTPUT_STEREO
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};
	
	UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
	UNITY_DECLARE_SCREENSPACE_TEXTURE(_SunShaftColorBuffer);
	UNITY_DECLARE_SCREENSPACE_TEXTURE(_Skybox);
	UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

	float _Opacity;
	uniform half4 _SunThreshold;
		
	uniform half4 _SunColor;
	uniform half4 _BlurRadius4;
	uniform half4 _SunPosition;
	uniform half4 _MainTex_TexelSize;	

	uniform half _DepthThreshold;

	#define SAMPLES_FLOAT 6.0f
	#define SAMPLES_INT 6
			
	v2f vert(appdata_img v) {
		v2f o;

		UNITY_SETUP_INSTANCE_ID(v);
		UNITY_INITIALIZE_OUTPUT(v2f, o);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		
		#if UNITY_UV_STARTS_AT_TOP
		o.uv1 = v.texcoord.xy;
		if (_MainTex_TexelSize.y < 0) {
			o.uv1.y = 1 - o.uv1.y;
		}
		#endif				
		
		return o;
	}

	v2f_radial vert_radial(appdata_img v) {
		v2f_radial o;

		UNITY_SETUP_INSTANCE_ID(v);
		UNITY_INITIALIZE_OUTPUT(v2f_radial, o);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		o.vertex = UnityObjectToClipPos(v.vertex);
		
		o.uv.xy =  v.texcoord.xy;
		o.blurVector = (_SunPosition.xy - v.texcoord.xy) * _BlurRadius4.xy;	
		
		return o; 
	}
		
	half4 fragScreen(v2f i) : SV_Target { 
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

		half4 colorA = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv);
		#if UNITY_UV_STARTS_AT_TOP
		half4 colorB = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_SunShaftColorBuffer, i.uv1);
		#else
		half4 colorB = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_SunShaftColorBuffer, i.uv);
		#endif
		half4 depthMask = saturate(colorB * _SunColor);	
		return 1.0f - (1.0f - colorA) * (1.0f - depthMask * _Opacity);	
	}

	half4 fragAdd(v2f i) : SV_Target { 
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
		
		half4 colorA = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv);
		#if UNITY_UV_STARTS_AT_TOP
		half4 colorB = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_SunShaftColorBuffer, i.uv1);
		#else
		half4 colorB = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_SunShaftColorBuffer, i.uv);
		#endif
		half4 depthMask = saturate(colorB * _SunColor);	
		return colorA + depthMask * _Opacity;	
	}
	
	half4 frag_radial(v2f_radial i) : SV_Target 
	{	
		half4 color = half4(0, 0, 0, 0);
		for(int j = 0; j < SAMPLES_INT; j++)   
		{	
			half4 tmpColor = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv);
			color += tmpColor;
			i.uv.xy += i.blurVector; 	
		}
		return color / SAMPLES_FLOAT;
	}	
	
	half TransformColor(half4 skyboxValue) {
		return dot(max(skyboxValue.rgb - _SunThreshold.rgb, half3(0, 0, 0)), half3(1, 1, 1)); // threshold and convert to greyscale
	}
	
	half4 frag_depth(v2f i) : SV_Target {
		#if UNITY_UV_STARTS_AT_TOP
		float depthSample = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv1.xy);
		#else
		float depthSample = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);		
		#endif
		
		half4 tex = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv);
		
		depthSample = Linear01Depth(depthSample);
		 
		// consider maximum radius
		#if UNITY_UV_STARTS_AT_TOP
		half2 vec = _SunPosition.xy - i.uv1.xy;
		#else
		half2 vec = _SunPosition.xy - i.uv.xy;		
		#endif
		half dist = saturate(_SunPosition.w - length(vec.xy));		
		
		half4 outColor = 0;
		
		// consider shafts blockers
		if (depthSample > _DepthThreshold) {
			outColor = TransformColor(tex) * dist;
		}
		
		return outColor;
	}
	
	half4 frag_nodepth(v2f i) : SV_Target {
		#if UNITY_UV_STARTS_AT_TOP
		float4 sky = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_Skybox, i.uv1);
		#else
		float4 sky = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_Skybox, i.uv);		
		#endif
		
		float4 tex = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv);
		
		// consider maximum radius
		#if UNITY_UV_STARTS_AT_TOP
		half2 vec = _SunPosition.xy - i.uv1.xy;
		#else
		half2 vec = _SunPosition.xy - i.uv.xy;		
		#endif
		half dist = saturate(_SunPosition.w - length (vec));			
		
		half4 outColor = 0;

		// find unoccluded sky pixels
		// consider pixel values that differ significantly between framebuffer and sky-only buffer as occluded
		if (Luminance(abs(sky.rgb - tex.rgb)) > _DepthThreshold) {
			outColor = TransformColor(tex) * dist;
		}
		
		return outColor;
	}	

	ENDHLSL
	
	Subshader {
		Pass { //0
			ZTest Always Cull Off ZWrite Off

			HLSLPROGRAM
			#pragma target 3.0

			#pragma vertex vert
			#pragma fragment fragScreen
			
			ENDHLSL
		}
		
		Pass { //1
			ZTest Always Cull Off ZWrite Off

			HLSLPROGRAM
			#pragma target 3.0
			
			#pragma vertex vert_radial
			#pragma fragment frag_radial
			
			ENDHLSL
		}
		
		Pass { //2
			ZTest Always Cull Off ZWrite Off

			HLSLPROGRAM
			#pragma target 3.0
			
			#pragma vertex vert
			#pragma fragment frag_depth
			
			ENDHLSL
		}
		
		Pass { //3
			ZTest Always Cull Off ZWrite Off

			HLSLPROGRAM
			#pragma target 3.0
			
			#pragma vertex vert
			#pragma fragment frag_nodepth
			
			ENDHLSL
		} 
		
		Pass { //4
			ZTest Always Cull Off ZWrite Off

			HLSLPROGRAM
			#pragma target 3.0
			
			#pragma vertex vert
			#pragma fragment fragAdd
			
			ENDHLSL
		} 
	}

	Fallback off
}