Shader "" {
    Properties {
        _linewidthG ("linewidthG", Float ) = 0
        _MainTex ("MainTex", 2D) = "white" {}
        //_AlphaMask ("AlphaMask", 2D) = "white" {} //Same as below
        //_alpha_a ("alpha_a", int ) = 1    //by fetching a global (default: 1) we can enforce a default value of 1 when unspecified.
        _alpha_b ("alpha_b", int ) = 1
        _DetailMask ("Detail Mask", 2D) = "black" {}
        _LineWidthS ("LineWidthS", Float) = 1
    }
    SubShader {
        Tags {
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
            "SHADOWSUPPORT"="true"
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
            uniform float _LineWidthS;
            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _AlphaMask; float4 _AlphaMask_ST;
            sampler2D _DetailMask; float4 _DetailMask_ST;
            uniform int _alpha_a;
            uniform int _alpha_b;

            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
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
                o.something = u_xlat0;

                float dist = distance(_WorldSpaceCameraPos, u_xlat0);
                u_xlat0.x = dist * 0.0999999866 + 0.300000012;
                u_xlat0.x = clamp(_linewidthG * 0.00499999989f * (dist * 0.0999999866 + 0.300000012), 0.0, 1.0);
                float2 detailMask = (1-tex2Dlod(_DetailMask, float4(TRANSFORM_TEX(v.uv,_DetailMask).xy,0,0)).z);
                
                u_xlat0.x *= detailMask;
                u_xlat0.x *= _LineWidthS;

                float3 recipObjScale = float3( length(unity_WorldToObject[0].xyz), length(unity_WorldToObject[1].xyz), length(unity_WorldToObject[2].xyz) );
                float3 objScale = 1.0/recipObjScale;
                u_xlat0.xyz = (u_xlat0.xxx / objScale) * v.normal + v.vertex;

                u_xlat0 = mul( unity_ObjectToWorld, u_xlat0);
                o.pos = UnityWorldToClipPos(u_xlat0);
                return o;
            }

            float4 frag(VertexOutput i) : COLOR {
                float4 am = tex2D(_AlphaMask, TRANSFORM_TEX(i.uv0,_AlphaMask));
                
                float2 aa = max(1 - float2(_alpha_a, _alpha_b), am.xy);
                float bm = min(aa.x, aa.y);

                float4 mt = tex2D(_MainTex, TRANSFORM_TEX(i.uv0, _MainTex));
                bm *= mt.a;

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
