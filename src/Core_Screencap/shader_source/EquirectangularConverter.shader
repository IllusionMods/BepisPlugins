// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Credit: https://github.com/Mapiarz/CubemapToEquirectangular/blob/master/Assets/Shaders/CubemapToEquirectangular.shader

Shader "Hidden/CubemapToEquirectangular" {
  Properties {
		_MainTex ("Cubemap (RGB)", CUBE) = "" {}
	}

	Subshader {
		Pass {
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }      

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				//#pragma fragmentoption ARB_precision_hint_nicest
				#include "UnityCG.cginc"

				#define PI    3.141592653589793
				#define TWOPI 6.283185307179587

				float4x4 _CameraRotationMatrix = (1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1);
				int _is180 = 0;

				struct v2f {
					float4 pos : POSITION;
					float2 uv : TEXCOORD0;
				};
		
				samplerCUBE _MainTex;

				v2f vert( appdata_img v )
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					if (_is180 == 1){
						o.uv = v.texcoord.xy * float2(PI, PI);
					}else{
						o.uv = v.texcoord.xy * float2(TWOPI, PI);
					}
					return o;
				}
		
				fixed4 frag(v2f i) : COLOR 
				{
					float theta = i.uv.y;
					float phi = i.uv.x;
					if (_is180 == 1){
						phi = i.uv.x+PI/2;
					}
					float3 unit = float3(0,0,0);
					unit.x = sin(phi) * sin(theta) * -1;
					unit.y = cos(theta) * -1;
					unit.z = cos(phi) * sin(theta) * -1;
					
					// Rotate the unit vector by the camera rotation matrix so it aligns with the camera direction
					unit = mul(_CameraRotationMatrix, float4(unit.x, unit.y, unit.z, 0)).xyz;

					return texCUBE(_MainTex, unit);
				}
			ENDCG
		}
	}
	Fallback Off
}