// By RikkiBallboa
Shader "3d_screencap"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _TextureTwo("TextureTwo", 2D) = "white" {}
        _OverlapOffset("OverlapOffset", float) = 0.2
    }
    
    SubShader
    {
        Pass
        {
            Name "Unlit"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            uniform sampler2D _MainTex;
            uniform sampler2D _TextureTwo;
            float _OverlapOffset;
            
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
                float s = i.uv0.x * (2 - _OverlapOffset * 2);
                float4 mt = tex2D(_MainTex, float2(saturate(s), i.uv0.y));
                float4 t2 = tex2D(_TextureTwo, float2(saturate(s - 1 + _OverlapOffset * 2), i.uv0.y));
                return lerp(mt, t2, floor(i.uv0.x + 0.5));
            }
            ENDCG
        }
    }
}