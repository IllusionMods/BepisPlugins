// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:0,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:False,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:False,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:0,bdst:1,dpts:2,wrdp:True,dith:0,atcv:False,rfrpo:False,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:False,qofs:0,qpre:2,rntp:3,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:5323,x:33502,y:32647,varname:node_5323,prsc:2|custl-4460-RGB,clip-2101-OUT,olwid-1263-OUT;n:type:ShaderForge.SFN_Color,id:4460,x:32539,y:32751,ptovrint:False,ptlb:TargetColour,ptin:_TargetColour,varname:node_4460,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0.5,c2:0.5,c3:1,c4:1;n:type:ShaderForge.SFN_Tex2d,id:8099,x:32969,y:33076,varname:node_8099,prsc:2,tex:eb2f1f300ab869a4690382c474a38909,ntxv:0,isnm:False|TEX-5318-TEX;n:type:ShaderForge.SFN_Tex2dAsset,id:5318,x:32524,y:33086,ptovrint:False,ptlb:MainTex,ptin:_MainTex,varname:node_5318,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:eb2f1f300ab869a4690382c474a38909,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Vector1,id:2836,x:32742,y:32890,varname:node_2836,prsc:2,v1:0.002;n:type:ShaderForge.SFN_Multiply,id:1263,x:32958,y:32890,varname:node_1263,prsc:2|A-2836-OUT,B-4607-OUT;n:type:ShaderForge.SFN_ValueProperty,id:4607,x:32558,y:32975,ptovrint:False,ptlb:linewidthG,ptin:_linewidthG,varname:node_4607,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:20;n:type:ShaderForge.SFN_Tex2d,id:6280,x:32145,y:33251,ptovrint:False,ptlb:AlphaMask,ptin:_AlphaMask,varname:node_6280,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:eb2f1f300ab869a4690382c474a38909,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Multiply,id:2101,x:33313,y:33110,varname:node_2101,prsc:2|A-8099-A,B-9552-OUT;n:type:ShaderForge.SFN_ValueProperty,id:3657,x:32145,y:33492,ptovrint:False,ptlb:alpha_a,ptin:_alpha_a,varname:_linewidthG_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_ValueProperty,id:7962,x:32536,y:33479,ptovrint:False,ptlb:alpha_b,ptin:_alpha_b,varname:_alpha_a_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_Multiply,id:4498,x:32398,y:33332,varname:node_4498,prsc:2|A-6280-G,B-1748-OUT;n:type:ShaderForge.SFN_Add,id:6986,x:32669,y:33264,varname:node_6986,prsc:2|A-6280-R,B-4498-OUT;n:type:ShaderForge.SFN_Clamp01,id:9552,x:33132,y:33266,varname:node_9552,prsc:2|IN-1140-OUT;n:type:ShaderForge.SFN_OneMinus,id:5949,x:32729,y:33469,varname:node_5949,prsc:2|IN-7962-OUT;n:type:ShaderForge.SFN_Add,id:1140,x:32954,y:33266,varname:node_1140,prsc:2|A-6986-OUT,B-5949-OUT;n:type:ShaderForge.SFN_OneMinus,id:1748,x:32353,y:33492,varname:node_1748,prsc:2|IN-3657-OUT;proporder:4460-5318-4607-6280-3657-7962;pass:END;sub:END;*/

Shader "" {
    Properties {
        _TargetColour ("TargetColour", Color) = (0.5,0.5,1,1)
        _MainTex ("MainTex", 2D) = "white" {}
        _linewidthG ("linewidthG", Float ) = 20
        _AlphaMask ("AlphaMask", 2D) = "white" {}
        _alpha_a ("alpha_a", Float ) = 1
        _alpha_b ("alpha_b", Float ) = 1
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
        }
        Pass {
            Name "Outline"
            Tags {
            }
            Cull Front
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma only_renderers d3d9 d3d11 glcore gles 
            #pragma target 3.0
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform float _linewidthG;
            uniform sampler2D _AlphaMask; uniform float4 _AlphaMask_ST;
            uniform float _alpha_a;
            uniform float _alpha_b;
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
                float4 node_8099 = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float4 _AlphaMask_var = tex2D(_AlphaMask,TRANSFORM_TEX(i.uv0, _AlphaMask));
                clip((node_8099.a*saturate(((_AlphaMask_var.r+(_AlphaMask_var.g*(1.0 - _alpha_a)))+(1.0 - _alpha_b)))) - 0.5);
                return fixed4(float3(0,0,0),0);
            }
            ENDCG
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma only_renderers d3d9 d3d11 glcore gles 
            #pragma target 3.0
            uniform float4 _TargetColour;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform sampler2D _AlphaMask; uniform float4 _AlphaMask_ST;
            uniform float _alpha_a;
            uniform float _alpha_b;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos( v.vertex );
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                float4 node_8099 = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float4 _AlphaMask_var = tex2D(_AlphaMask,TRANSFORM_TEX(i.uv0, _AlphaMask));
                clip((node_8099.a*saturate(((_AlphaMask_var.r+(_AlphaMask_var.g*(1.0 - _alpha_a)))+(1.0 - _alpha_b)))) - 0.5);
////// Lighting:
                float3 finalColor = _TargetColour.rgb;
                return fixed4(finalColor,1);
            }
            ENDCG
        }
        Pass {
            Name "ShadowCaster"
            Tags {
                "LightMode"="ShadowCaster"
            }
            Offset 1, 1
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_SHADOWCASTER
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma only_renderers d3d9 d3d11 glcore gles 
            #pragma target 3.0
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform sampler2D _AlphaMask; uniform float4 _AlphaMask_ST;
            uniform float _alpha_a;
            uniform float _alpha_b;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                V2F_SHADOW_CASTER;
                float2 uv0 : TEXCOORD1;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos( v.vertex );
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                float4 node_8099 = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float4 _AlphaMask_var = tex2D(_AlphaMask,TRANSFORM_TEX(i.uv0, _AlphaMask));
                clip((node_8099.a*saturate(((_AlphaMask_var.r+(_AlphaMask_var.g*(1.0 - _alpha_a)))+(1.0 - _alpha_b)))) - 0.5);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
