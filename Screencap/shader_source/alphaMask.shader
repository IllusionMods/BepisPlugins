// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge v1.35 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.35;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,lico:0,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:False,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:3,bdst:7,dpts:2,wrdp:False,dith:0,atcv:False,rfrpo:False,rfrpn:Refraction,coma:15,ufog:False,aust:False,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False;n:type:ShaderForge.SFN_Final,id:5323,x:32994,y:32748,varname:node_5323,prsc:2|custl-8099-RGB,alpha-9177-OUT;n:type:ShaderForge.SFN_Tex2d,id:8099,x:32740,y:32825,varname:node_8099,prsc:2,tex:a4259037a0bba7b41884b33f9653b20e,ntxv:0,isnm:False|TEX-5318-TEX;n:type:ShaderForge.SFN_Tex2dAsset,id:5318,x:32534,y:32863,ptovrint:False,ptlb:MainTex,ptin:_MainTex,varname:node_5318,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:a4259037a0bba7b41884b33f9653b20e,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Tex2dAsset,id:9732,x:32340,y:33078,ptovrint:False,ptlb:Mask,ptin:_Mask,varname:_MainTex_copy,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:490b89048087b054e94c9ef00b92145c,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Tex2d,id:6494,x:32518,y:33039,varname:node_6494,prsc:2,tex:490b89048087b054e94c9ef00b92145c,ntxv:0,isnm:False|TEX-9732-TEX;n:type:ShaderForge.SFN_OneMinus,id:9177,x:32754,y:33051,varname:node_9177,prsc:2|IN-6494-R;proporder:5318-9732;pass:END;sub:END;*/

Shader "Shader Forge/alphaMask" {
    Properties {
        _MainTex ("MainTex", 2D) = "white" {}
        _Mask ("Mask", 2D) = "white" {}
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma only_renderers d3d9 d3d11 glcore gles 
            #pragma target 3.0
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform sampler2D _Mask; uniform float4 _Mask_ST;
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
                o.pos = UnityObjectToClipPos(v.vertex );
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
////// Lighting:
                float4 node_8099 = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float3 finalColor = node_8099.rgb;
                float4 node_6494 = tex2D(_Mask,TRANSFORM_TEX(i.uv0, _Mask));
                return fixed4(finalColor,(1.0 - node_6494.r));
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
