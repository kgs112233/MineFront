Shader "Unlit/TileGridOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color   ("Tint", Color)        = (1,1,1,1)

        _LineColor    ("Line Color", Color) = (0,0,0,1)          // 테두리 색
        _LineThickness("Thickness", Range(0.001, 0.2)) = 0.03    // 테두리 두께
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;
            fixed4    _LineColor;
            float     _LineThickness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;      // ★ 타일별 색 정보
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                fixed4 color : COLOR;       // ★ 프래그먼트로 전달
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;          // ★ 그대로 넘김
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 기본 스프라이트 색 + 글로벌 Tint + 타일별 색
                fixed4 col = tex2D(_MainTex, i.uv) * _Color * i.color;

                // UV 기준으로 테두리 영역 계산 (0~1)
                float u = i.uv.x;
                float v = i.uv.y;

                float distToEdge =
                    min(min(u, 1.0 - u),
                        min(v, 1.0 - v));

                // 가장자리 근처면 테두리 색으로
                if (distToEdge < _LineThickness)
                {
                    return _LineColor;
                }

                // 나머지는 타일 색 (흰/회색/빨강 등)
                return col;
            }
            ENDCG
        }
    }
}
