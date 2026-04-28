Shader "Custom/PixelDissolveWipe"
{
    Properties
    {
        _Color ("Black Screen Color", Color) = (0,0,0,1)
        _Progress ("Progress (1=透明, 0=全黑)", Range(0, 1)) = 1.0
        _PixelSize ("马赛克大小 (建议 30-50)", Float) = 40.0
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            float4 _Color;
            float _Progress;
            float _PixelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // 一个极简但极好用的伪随机数生成器 (基于坐标返回 0~1 的随机值)
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 【马赛克化坐标】：把屏幕强行划分为 _PixelSize 那么多的大方格！
                float2 pixelatedUV = floor(i.uv * _PixelSize) / _PixelSize;

                // 2. 【生成随机噪点】：为每个马赛克格子生成一个唯一的随机值 (0到1之间)
                float noise = random(pixelatedUV);

                // 3. 【溶解判断】：如果这个格子的随机值 大于 进度条，它就变黑！
                // 当 _Progress 接近 0 时，绝大多数格子的随机值都会比它大，屏幕几乎全黑。
                float alpha = step(_Progress, noise);

                return float4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}