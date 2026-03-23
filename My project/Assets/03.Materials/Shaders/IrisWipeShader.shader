Shader "Custom/IrisWipe"
{
    Properties
    {
        _Color ("Black Screen Color", Color) = (0,0,0,1)
        _Radius ("Iris Radius", Range(0, 2)) = 1.5
        _CenterX ("Center X", Range(0, 1)) = 0.5
        _CenterY ("Center Y", Range(0, 1)) = 0.5
    }
    SubShader
    {
        // 专门为 UI 设计的渲染标签
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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

            float4 _Color;
            float _Radius;
            float _CenterX;
            float _CenterY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 获取当前屏幕的宽高比（防止画出来的圆变成椭圆）
                float aspect = _ScreenParams.x / _ScreenParams.y;
                
                // 2. 修正 UV 坐标系
                float2 uv = i.uv;
                uv.x *= aspect; 
                
                float2 center = float2(_CenterX * aspect, _CenterY);
                
                // 3. 计算当前像素距离圆心的距离
                float dist = distance(uv, center);
                
                // 4. 【核心魔法】：step 函数。
                // 如果距离 dist 大于我们设定的半径 _Radius，就返回 1 (纯黑)
                // 如果距离小于半径，就返回 0 (完全透明)
                // 这种非 0 即 1 的计算，会产生完美的、不带模糊的“锯齿状像素边缘”！
                float alpha = step(_Radius, dist);
                
                return float4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}