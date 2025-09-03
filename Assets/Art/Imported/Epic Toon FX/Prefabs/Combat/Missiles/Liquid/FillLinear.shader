Shader "Custom/Fill-Linear" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1) 
		_Center ("Center", Vector) = (0,0,0,0) 
		_FillRate ("FillRate", Range(0, 1.0)) = 1.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100
		
		CGPROGRAM
		#pragma surface surf Unlit

		sampler2D _MainTex;
		float _FillRate;
		float4 _Center;
		float4 _Color;

		struct Input {
			float2 uv_MainTex;
		};

		half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten)
		{
			return half4(s.Albedo/1.35, s.Alpha);
		}

		void surf (Input IN, inout SurfaceOutput o) {
			half2 uv = IN.uv_MainTex.xy;
 			half2 _textCord = uv;
 			float _distanceX = distance(uv.x, _Center.x);
			float _distanceY = distance(uv.y, _Center.y);
 			if (_distanceX <= _FillRate && _distanceY <=_FillRate)
 			{
				half4 c = tex2D(_MainTex, _textCord);
 				o.Albedo = c.rgb ;
 			} 
			else 
			{
 				o.Albedo =  _Color;
			}
 			
			
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
