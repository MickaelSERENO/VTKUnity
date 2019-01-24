Shader "Sereno/LABLab"
{
    Properties
    {
        _Min ("Cold LAB", Vector) = (63.07001, 13.38205, -40.73627)
		_Max ("Warm LAB", Vector) = (48.51714, 71.04501, 6.335425)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
		    Lighting Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
				float3 localPos : TEXCOORD0;
            };

			half3 _Min;
			half3 _Max;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex   = UnityObjectToClipPos(v.vertex);
				o.localPos = v.vertex;

                return o;
            }

			//Inverse function to convert components from LAB to XYZ
			float invF(float v)
			{
				return (v > 6.0/29.0) ? v*v*v : 0.128418*(v - 4.0 / 29.0);
			}

            fixed4 frag (v2f i) : SV_Target
            {
				//Get the LAB Color by interpolation
				half3 white    = half3(100, 0, 0);
				half3 labColor = half3(0, 0, 0);
				if (i.localPos.y < 0.0)
					labColor = (-i.localPos.y)*_Min + (1.0+i.localPos.y)*white;
				else
					labColor = (1.0 - i.localPos.y)*white + i.localPos.y*_Max;

				//Convert to XYZ
				half3 XYZRef = half3(0.9505, 1.0, 1.0890);
				half3 XYZ = half3(XYZRef.x * invF((labColor.x + 16.0) / 116.0 + labColor.y / 500.0),
							      XYZRef.y * invF((labColor.x + 16.0) / 116.0),
							      XYZRef.z * invF((labColor.x + 16.0) / 116.0 - labColor.z / 200.0));

				//Convert to RGB
				fixed4 rgb = fixed4(min(1.0f,  3.2405f*XYZ.x - 1.5371f*XYZ.y - 0.4985f*XYZ.z),
					                min(1.0f, -0.9692f*XYZ.x + 1.8760f*XYZ.y + 0.0415f*XYZ.z),
					                min(1.0f,  0.0556f*XYZ.x - 0.2040f*XYZ.y + 1.0572f*XYZ.z), 1.0);

				return rgb;
            }
            ENDCG
        }
    }
}
