Shader "Hidden/GrassOcclusion"
{
	SubShader
	{
		Pass
		{
			Blend DstColor Zero
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0

			struct GrassInstance
			{
				float2 position;
				float scale;
				float rotation;
			};

			StructuredBuffer<GrassInstance> _Instances;
			StructuredBuffer<float2> _Verts;
			sampler2D _Occlusion;
			float _Bias;
			
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
			
			v2f vert (uint v : SV_VertexID, uint i : SV_InstanceID)
			{
				v2f o;
				o.vertex = 1;
				o.vertex.xy = _Verts[v];

				o.uv = o.vertex.xy * 0.5 + 0.5;
				o.uv.y = 1 - o.uv.y;

				GrassInstance inst = _Instances[i];

				// Scale
				float terrainScale = 0.05;
				float scale = terrainScale;
				scale *= inst.scale;
				o.vertex.xy *= scale;

				// Rotate
				float s, c;
				sincos(inst.rotation, s, c);
				float2x2 m = float2x2(c, -s, s, c);
				o.vertex.xy = mul(m, o.vertex.xy);

				// Translate
				o.vertex.xy -= 1.0;
				float2 offset = inst.position;
				offset.y = 1 - offset.y;
				o.vertex.xy += 2 * offset;

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// return tex2D(_Occlusion, i.uv).a + _Bias;

				float o = tex2D(_Occlusion, i.uv).a;
				float off = 1.0/64.0;
				o += tex2D(_Occlusion, i.uv + float2( off, 0)).a;
				o += tex2D(_Occlusion, i.uv + float2(-off, 0)).a;
				o += tex2D(_Occlusion, i.uv + float2(0, off)).a;
				o += tex2D(_Occlusion, i.uv + float2(0,-off)).a;
				return o/5.0 + _Bias;

				// float2 localPos = uv * 2 - 1;
				// return smoothstep(0, 1, sqrt(localPos.x*localPos.x + localPos.y*localPos.y) + _Bias);
				return 1;
			}
			ENDCG
		}
	}
}
