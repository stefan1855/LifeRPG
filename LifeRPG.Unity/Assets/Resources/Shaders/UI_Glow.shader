Shader "LifeRPG/UI/Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // ── 发光参数 ──
        _GlowColor ("Glow Color", Color) = (0.35, 0.95, 0.2, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.2
        _GlowWidth ("Glow Width (px)", Range(0, 32)) = 6
        _PulseSpeed ("Pulse Speed", Range(0, 20)) = 2.5
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.35

        // ── UI 标准参数（与 UI/Default 一致）──
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _GlowColor;
            half _GlowIntensity;
            half _GlowWidth;
            half _PulseSpeed;
            half _PulseAmount;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 主纹理颜色（UI 预乘 alpha 约定）
                half4 texColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);
                half4 color = texColor * IN.color;

                // ── 脉动强度（正弦，0.5~1.5 区间 * _PulseAmount 缩放）──
                half pulse = 1.0;
                if (_PulseSpeed > 0.001)
                    pulse = 1.0 + _PulseAmount * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed));

                // ── 外发光：对 alpha 做 8 方向偏移采样，取外围辉光量 ──
                half2 px = _MainTex_TexelSize.xy * _GlowWidth;

                half g = 0.0;
                g += tex2D(_MainTex, IN.texcoord + half2( px.x, 0.0)).a;
                g += tex2D(_MainTex, IN.texcoord - half2( px.x, 0.0)).a;
                g += tex2D(_MainTex, IN.texcoord + half2(0.0,  px.y)).a;
                g += tex2D(_MainTex, IN.texcoord - half2(0.0,  px.y)).a;
                g += tex2D(_MainTex, IN.texcoord + half2( px.x,  px.y)).a;
                g += tex2D(_MainTex, IN.texcoord + half2( px.x, -px.y)).a;
                g += tex2D(_MainTex, IN.texcoord + half2(-px.x,  px.y)).a;
                g += tex2D(_MainTex, IN.texcoord + half2(-px.x, -px.y)).a;
                g *= 0.125; // /8

                // 只取外围（减去自身 alpha），避免把中心也加亮
                half glowMask = saturate(g - texColor.a);
                half glowContrib = glowMask * _GlowIntensity * pulse;

                half3 rgb = color.rgb + _GlowColor.rgb * glowContrib;
                half a = saturate(color.a + glowContrib);

                #ifdef UNITY_UI_CLIP_RECT
                a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(a - 0.001);
                #endif

                return half4(rgb, a);
            }
        ENDCG
        }
    }
}
