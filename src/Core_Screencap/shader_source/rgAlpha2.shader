Shader "rgAlpha" {
    Properties {
        _green("green", 2D) = "white" {}
        _MainTex("red", 2D) = "white" {}
    }
    SubShader {        
        Pass {
            Name "Unlit"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            uniform sampler2D _green;
            uniform sampler2D _MainTex;

            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };

            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (VertexOutput i) : COLOR
            {
                float4 green = tex2D(_green, i.uv0);
                float4 red = tex2D(_MainTex, i.uv0);
                float3 comp = float3(green.r, red.g, min(red.b, green.b));
                float3 diff = abs(red.rgb - green.rgb);
                float alpha = max(max(diff.x, diff.y), diff.z);
                return float4(alpha, alpha, alpha, 1);
            }
            ENDCG
        }
    }
}