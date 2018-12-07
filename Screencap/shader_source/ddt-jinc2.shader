/*
   Hyllian's ddt-jinc2-lobe with anti-ringing Shader
   
   Copyright (C) 2011-2014 Hyllian/Jararaca - sergiogdb@gmail.com

   This program is free software; you can redistribute it and/or
   modify it under the terms of the GNU General Public License
   as published by the Free Software Foundation; either version 2
   of the License, or (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program; if not, write to the Free Software
   Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

*/

Shader "ddt-jinc2"
{
    Properties{
        _MainTex ("Main Texture", 2D) = "white" {}
        _texture_size ("Clarity", Float ) = 128
    }

    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma exclude_renderers gles
            #pragma vertex vert
            #pragma fragment main

            uniform sampler2D _MainTex;
            uniform float _texture_size;

            /*
                This is an approximation of Jinc(x)*Jinc(x*r1/r2) for x < 2.5,
                where r1 and r2 are the first two zeros of jinc function.
                For a jinc 2-lobe best approximation, use A=0.5 and B=0.825.
            */  
			
            #define A 0.5
            #define B 0.1
            const static float     halfpi     = 1.5707963267948966192313216916398;
            const static float         pi     = 3.1415926535897932384626433832795;
            const static float         wa     = A*pi;
            const static float         wb     = B*pi;

            const static float3 dtt = float3(65536,255,1);

            float reduce(float3 color)
            {
                return dot(color, dtt);
            }

            float d(float2 pt1, float2 pt2)
            {
                float2 v = pt2 - pt1;
                return sqrt(dot(v,v));
            }

            float4 min4(float4 a, float4 b, float4 c, float4 d)
            {
                return min(a, min(b, min(c, d)));
            }
            float4 max4(float4 a, float4 b, float4 c, float4 d)
            {
                return max(a, max(b, max(c, d)));
            }
     
            struct out_vertex {
                float4 position : POSITION;
                float2 texCoord : TEXCOORD0;
            };
     
            out_vertex vert ( float4 vertex : POSITION, float2 uv : TEXCOORD0 )
            {
                out_vertex o;
                o.position = UnityObjectToClipPos(vertex);
                o.texCoord = uv;
                return o;
            }
     
            float4 lanczos(float4 x)
            {
                float wawb = wa*wb;
                return (x==float4(0.0, 0.0, 0.0, 0.0)) ?  float4(wawb,wawb, wawb, wawb)  :  sin(x*wa)*sin(x*wb)/(x*x);
            }
     
            float4 main(in out_vertex VAR) : COLOR
            {
                float4x4 weights;

                float2 dx = float2(1.0, 0.0);
                float2 dy = float2(0.0, 1.0);

                float2 pc = VAR.texCoord*_texture_size;

                float2 tc = (floor(pc-float2(0.5,0.5))+float2(0.5,0.5));
     
                float2 pos = frac(pc-float2(0.5,0.5));

                weights[0] = lanczos(float4(d(pc, tc    -dx    -dy), d(pc, tc           -dy), d(pc, tc    +dx    -dy), d(pc, tc+2.0*dx    -dy)));
                weights[1] = lanczos(float4(d(pc, tc    -dx       ), d(pc, tc              ), d(pc, tc    +dx       ), d(pc, tc+2.0*dx       )));
                weights[2] = lanczos(float4(d(pc, tc    -dx    +dy), d(pc, tc           +dy), d(pc, tc    +dx    +dy), d(pc, tc+2.0*dx    +dy)));
                weights[3] = lanczos(float4(d(pc, tc    -dx+2.0*dy), d(pc, tc       +2.0*dy), d(pc, tc    +dx+2.0*dy), d(pc, tc+2.0*dx+2.0*dy)));

                dx /= _texture_size;
                dy /= _texture_size;
                tc /= _texture_size;
     
                fixed4  c00 = tex2D(_MainTex, tc    -dx    -dy);
                fixed4  c10 = tex2D(_MainTex, tc           -dy);
                fixed4  c20 = tex2D(_MainTex, tc    +dx    -dy);
                fixed4  c30 = tex2D(_MainTex, tc+2.0*dx    -dy);
                fixed4  c01 = tex2D(_MainTex, tc    -dx       );
                fixed4  c11 = tex2D(_MainTex, tc              );
                fixed4  c21 = tex2D(_MainTex, tc    +dx       );
                fixed4  c31 = tex2D(_MainTex, tc+2.0*dx       );
                fixed4  c02 = tex2D(_MainTex, tc    -dx    +dy);
                fixed4  c12 = tex2D(_MainTex, tc           +dy);
                fixed4  c22 = tex2D(_MainTex, tc    +dx    +dy);
                fixed4  c32 = tex2D(_MainTex, tc+2.0*dx    +dy);
                fixed4  c03 = tex2D(_MainTex, tc    -dx+2.0*dy);
                fixed4  c13 = tex2D(_MainTex, tc       +2.0*dy);
                fixed4  c23 = tex2D(_MainTex, tc    +dx+2.0*dy);
                fixed4  c33 = tex2D(_MainTex, tc+2.0*dx+2.0*dy);

                float a = reduce(c11);
                float b = reduce(c21);
                float c = reduce(c12);
                float d = reduce(c22);

                float p = abs(pos.x);
                float q = abs(pos.y);

                float4 min_sample = min4(c11, c21, c12, c22);
                float4 max_sample = max4(c11, c21, c12, c22);

                if (abs(a-d) < abs(b-c))
                {
                    if (q <= p) c12 = c11 + c22 - c21;
                    else c21 = c11 + c22 - c12;
                }
                else if (abs(a-d) > abs(b-c))
                {
                    if ((p+q) < 1.0) c22 = c21 + c12 - c11;
                    else c11 = c21 + c12 - c22;
                }

                float4 color =
                    mul(weights[0], float4x4(c00, c10, c20, c30)) +
                    mul(weights[1], float4x4(c01, c11, c21, c31)) +
                    mul(weights[2], float4x4(c02, c12, c22, c32)) +
                    mul(weights[3], float4x4(c03, c13, c23, c33));
                
                color = color/(dot(mul(weights, float4(1, 1, 1, 1)), 1));
                color = clamp(color, min_sample, max_sample);

                return color;
     
            }
            ENDCG
        }
    }
}