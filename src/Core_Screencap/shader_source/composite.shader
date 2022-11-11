Shader "Unlit/composite"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _Overlay("Overlay", 2D) = "white" {}
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
            uniform sampler2D _Overlay;
            
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
                float4 mt = tex2D(_MainTex, i.uv0);
                float4 o = tex2D(_Overlay, i.uv0);
                mt.rgb *= mt.a;
                o.rgb *= o.a;
                float3 rgb = o.rgb + (mt.rgb * (1 - o.a));
                float a = o.a + mt.rgb * (1.0 - o.a);
                return float4(rgb, a);
            }
            ENDCG
        }
    }
    CustomEditor "ASEMaterialInspector"
}
