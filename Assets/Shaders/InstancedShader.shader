Shader "Unlit/InstanceShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
	SubShader{

		Pass {

			Tags {"LightMode" = "ForwardBase"}

			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma target 4.5

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			#include "AutoLight.cginc"

			sampler2D _MainTex;

			struct TransformData
			{
				float3 translate;
				float3 scale;
				float3 rotation;
				int length;
				int enable;
			};

		#if SHADER_TARGET >= 45
			StructuredBuffer<TransformData> transformDataBuffer;
			StructuredBuffer<fixed4> colorBuffer;
		#endif

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv_MainTex : TEXCOORD0;
				float4 color : TEXCOORD3;
				uint instanceID : SV_InstanceID;
			};

			void rotate2D(inout float2 v, float r)
			{
				float s, c;
				sincos(r, s, c);
				
				v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
			}

			v2f vert(appdata_full v, uint instanceID : SV_InstanceID)
			{
				TransformData transform = transformDataBuffer[instanceID];

				float3 localPosition = v.vertex.xyz * transform.scale;
				rotate2D(localPosition.xy, transform.rotation.x + _Time.y);

				float3 worldPosition = transform.translate + localPosition;
				float3 worldNormal = v.normal;

				v2f output;
				output.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
				output.uv_MainTex = v.texcoord;
				output.color = colorBuffer[instanceID];
				output.instanceID = instanceID;

				return output;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				if (!transformDataBuffer[i.instanceID].enable)
				{
					discard;
				}

				float girdPerSide = 10.0;
				float count = girdPerSide * girdPerSide;
				float index = ceil(_Time.y + i.instanceID);
				float scale = 1.0 / 10.0;
				float2 uv = i.uv_MainTex;

				uv.x = 1.0 - uv.x + fmod(index, girdPerSide);
				uv.y = 1.0 - uv.y + ((girdPerSide - floor(index / girdPerSide)) - 1.0);

				uv.x *= scale;
				uv.y *= scale;

				fixed4 texel = tex2D(_MainTex, uv);

				fixed4 output = fixed4(texel.rgb, i.color.w);

				return output;
			}

			ENDCG
		}
	}
}
