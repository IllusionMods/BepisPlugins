Shader "" {
    Properties {
		_green("green", 2D) = "white" {}
		_red("red", 2D) = "white" {}
    }
    SubShader {
        Tags {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        
        Pass {
            Name "Outline"
            Cull Off

		Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma target 3.0

			uniform sampler2D _green;
			uniform float4 _green_ST;
			uniform sampler2D _red;
			uniform float4 _red_ST;

            struct VertexInput {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 something : TEXCOORD1;
            };

            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.uv;

                float4 u_xlat0 = mul( unity_ObjectToWorld, v.vertex );
                o.pos = UnityWorldToClipPos(u_xlat0);
                return o;
            }

            float4 frag(VertexOutput i) : COLOR {

				float2 uv_green = i.uv0 * _green_ST.xy + _green_ST.zw;
				float4 tex2DNode39 = tex2D( _green, uv_green );
				float2 uv_red = i.uv0 * _red_ST.xy + _red_ST.zw;
				float4 tex2DNode38 = tex2D( _red, uv_red );
				float3 appendResult52 = (float3(tex2DNode39.r , tex2DNode38.g , min( tex2DNode38.b , tex2DNode39.b )));
				float3 break47 = abs( ( (tex2DNode38).rgb - (tex2DNode39).rgb ) );
                return float4(appendResult52, ( 1.0 - max( max( break47.x , break47.y ) , break47.z ) ));
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}