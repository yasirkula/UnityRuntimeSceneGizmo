// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/RuntimeSceneGizmo"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_AlphaVal ("Alpha", Float) = 1.0
	}

	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		LOD 200
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		#pragma surface surf Lambert keepalpha
		#pragma multi_compile __ HIGHLIGHT_ON

		struct Input
		{
			float4 color : COLOR;
		};

		fixed _AlphaVal;
		#if HIGHLIGHT_ON
		fixed4 _Color;
		#endif

		void surf( Input IN, inout SurfaceOutput o )
		{
			#if HIGHLIGHT_ON
			o.Albedo = _Color.rgb;
			#else
			o.Albedo = IN.color.rgb;
			#endif
			o.Alpha = _AlphaVal;
		}
		ENDCG
	}
}
