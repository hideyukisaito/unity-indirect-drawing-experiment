Shader "Unlit/InstanceShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
	SubShader{

		Pass {

			Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
			LOD 100

			AlphaTest Greater 0.0001
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off
			ZWrite Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma target 4.5

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;

			struct TransformData
			{
				float3 translate;
				float3 scale;
				float3 rotation;
				float2 uvOffset;
				float2 uvScale;
				int enable;
			};

		#if SHADER_TARGET >= 45
			StructuredBuffer<TransformData> transformDataBuffer;
			StructuredBuffer<fixed4> colorBuffer;
		#endif

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 color : TEXCOORD3;
				uint instanceID : SV_InstanceID;
			};

			void rotate2D(inout float2 v, float r)
			{
				float s, c;
				sincos(r, s, c);
				
				v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
			}

			float median(float r, float g, float b)
			{
				return max(min(r, g), min(max(r, g), b));
			}

			v2f vert (appdata v, uint instanceID : SV_InstanceID)
			{
				TransformData transform = transformDataBuffer[instanceID];

				float3 localPosition = v.vertex.xyz * transform.scale;

				float3 worldPosition = transform.translate + localPosition;

				v2f output;
				output.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
				output.uv = TRANSFORM_TEX(v.uv, _MainTex);
				output.instanceID = instanceID;

				return output;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				TransformData transform = transformDataBuffer[i.instanceID];

				if (!transform.enable)
				{
					discard;
				}

				float2 uv = i.uv;
				uv *= transform.uvScale;
				uv += transform.uvOffset;

				float3 texel = tex2D(_MainTex, uv).rgb;
				float sigDist = median(texel.r, texel.g, texel.b) - 0.5;
				float alpha = clamp(sigDist / fwidth(sigDist) + 0.5, 0.0, 1.0);
				
				float4 output = float4(1.0, 1.0, 1.0, alpha);

				return output;
			}

			ENDCG
		}
	}
}
