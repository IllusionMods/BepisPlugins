Shader "" {
    Properties {
        _linewidthG ("linewidthG", Float ) = 0
        //_AlphaMask ("AlphaMask", 2D) = "white" {} //Same as below
        //_alpha_a ("alpha_a", int ) = 1    //by fetching a global (default: 1) we can enforce a default value of 1 when unspecified.
        _alpha_b ("alpha_b", int ) = 1
    }
    SubShader {
        Tags {
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
        }
        Pass {
            Name "Outline"
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma target 3.0

            uniform float _linewidthG;
            uniform sampler2D _AlphaMask; uniform float4 _AlphaMask_ST;
            uniform int _alpha_a;
            uniform int _alpha_b;

            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos( float4(v.vertex.xyz + v.normal*(0.002*_linewidthG),1) );
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                float4 am = tex2D(_AlphaMask, i.uv0);
                
                float aa = step(_alpha_a, am.r);
                float ab = step(_alpha_b, am.g);
                float bm = min(aa, ab);

                //Exactly 1, 0 combo equals 1, else 0
                float alt_mask = step(0.5, _alpha_a) * step(_alpha_b, 0.5);
                bm = bm * (1 - alt_mask) + (alt_mask * am.r);
                clip(bm - 0.5);
                return 0;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
