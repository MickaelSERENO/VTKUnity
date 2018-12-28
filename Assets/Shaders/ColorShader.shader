Shader "Sereno/DefaultColor"
{
	Properties
	{
		 _PlanePosition ("Position of the Clipping Plane",  Vector) = (0, 0, 0)
		 _PlaneNormal   ("Normal of the Clipping Plane",    Vector) = (1, 0, 0)
		 _SpherePosition("Position of the Clipping Sphere", Vector) = (0, 0, 0)
		 _SphereRadius  ("Radius of the Clipping Sphere",   Float)  = 1.0
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 256

		Pass
		{
			CGPROGRAM
			#pragma vertex   vert
			#pragma fragment frag
			#pragma multi_compile TEXCOORD0_ON TEXCOORD1_ON TEXCOORD2_ON 
			#pragma shader_feature SPHERE_ON
			#pragma shader_feature PLANE_ON

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				#if   defined(TEXCOORD0_ON)
                float4 color  : TEXCOORD0;
				#endif
				#if defined(TEXCOORD1_ON)
				float4 color  : TEXCOORD1;
				#endif
				#if defined(TEXCOORD2_ON)
				float4 color  : TEXCOORD2;
				#endif
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
                float4 color    : COLOR;
				float3 position : TEXCOORD1;
			};

			float3 _PlaneNormal;
			float3 _PlanePosition;

			float3 _SpherePosition;
			float  _SphereRadius;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color  = v.color;

				float4 pos = mul(unity_ObjectToWorld, v.vertex);
				o.position = pos.xyz / pos.w;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
#ifdef PLANE_ON
				if(dot((i.position - _PlanePosition), _PlaneNormal) < 0.0)
					discard;
#endif
#ifdef SPHERE_ON
				if(length(i.position - _SpherePosition) > _SphereRadius)
					discard;
#endif
				return i.color;
			}
			
			ENDCG
		}
	}
}
