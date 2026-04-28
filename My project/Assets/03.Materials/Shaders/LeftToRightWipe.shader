Shader "Custom/LeftToRightWipe"
{
    Properties
    {
        _Color ("Black Screen Color", Color) = (0,0,0,1)
        // 进度：0=全黑, 1=全透
        _Progress ("Progress", Range(0, 1)) = 1.0
        _GridSize ("Grid Size", Float) = 30.0
        _EdgeWidth ("Edge Width", Range(0.1, 2.0)) = 0.8
        
        // 【新增魔法开关】：控制开合的方向！
        // 0 代表“正在合上（从右往左盖过来）”
        // 1 代表“正在拉开（继续向左退出去）”
        [Toggle] _IsOpening ("Is Opening (展开模式)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off

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
            float _GridSize;
            float _EdgeWidth;
            float _IsOpening; // 接收开关状态

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float steppedY = floor(i.uv.y * _GridSize) / _GridSize;
                float noise = random(float2(steppedY, 0.0));

                // 核心逻辑：根据 Progress 算出一道闸门
                float mappedProgress = lerp(-_EdgeWidth, 1.0, _Progress);
                float threshold = mappedProgress + noise * _EdgeWidth;

                float alpha = 0;

                // 【核心视觉分支】：决定幕布从哪边来，往哪边走
                if (_IsOpening > 0.5)
                {
                    // 【拉开模式】：黑幕继续向左退出！
                    // Progress 从 0 变到 1 时，黑幕的右边缘从屏幕右侧慢慢移到屏幕左外侧
                    alpha = step(i.uv.x, 1.0 - threshold);
                }
                else
                {
                    // 【合拢模式】：黑幕从右向左盖过来！
                    // Progress 从 1 变到 0 时，黑幕的左边缘从屏幕右侧移到屏幕左侧
                    alpha = step(i.uv.x, threshold);
                    alpha = 1.0 - alpha; // 反转颜色，使之变黑
                }

                return float4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}