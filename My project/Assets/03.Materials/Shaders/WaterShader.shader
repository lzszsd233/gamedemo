Shader "Custom/WaterReflection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} 
        _Color ("Water Tint (水面偏色)", Color) = (0.5, 0.6, 0.9, 0.9) 
        
        _WaveSpeed ("Wave Speed (水波速度)", Float) = 1.5
        _WaveFreq ("Wave Frequency (水波密集度)", Float) = 40.0
        _WaveAmp ("Wave Strength (水波幅度)", Float) = 0.005 
        
        // 【全新修改】：梯形透视，越大下面越往中间收缩
        _Perspective ("Perspective (3D透视度)", Range(0, 1)) = 0.2
        // 【全新修改】：安全的变暗系数
        _Darken ("Darken Depth (水深变暗)", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _WaveSpeed;
            float _WaveFreq;
            float _WaveAmp;
            
            float _Perspective;
            float _Darken;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // 【核心修正】：因为 Quad 被 Y=-1 翻转了，此时顶部的 uv.y 是 0，底部是 1
                // 所以 uv.y 本身就代表了“水深”！
                float depth = uv.y; 

                // 【修复拉丝】：真正的 3D 梯形透视算法
                // 让画面底部向中间微微收缩，而不是强行推出屏幕
                uv.x = (uv.x - 0.5) * (1.0 + depth * _Perspective) + 0.5;

                // 水波扭曲 (越深的地方，扭曲幅度稍微变大一点点)
                uv.x += sin(uv.y * _WaveFreq + _Time.y * _WaveSpeed) * (_WaveAmp * (1.0 + depth));

                // 采样图片
                fixed4 col = tex2D(_MainTex, uv);
                
                // 【修复死黑】：使用 saturate 函数，保证数值永远在 0 到 1 之间，绝对不会变负数！
                col.rgb *= _Color.rgb;
                col.rgb *= saturate(1.0 - depth * _Darken);
                
                col.a *= _Color.a;

                return col;
            }
            ENDCG
        }
    }
}