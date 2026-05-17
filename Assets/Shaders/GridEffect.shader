Shader "Custom/CircuitGridEffect_URP"
{
    Properties
    {
        _MainTex ("Circuit Texture", 2D) = "white" {}
        
        [Header(Colors)]
        _Color1 ("Primary Color (Cyan)", Color) = (0.0, 1.0, 1.0, 1.0)
        _Color2 ("Secondary Color (Pink)", Color) = (1.0, 0.3, 0.8, 1.0)
        _Color3 ("Tertiary Color (Purple)", Color) = (0.5, 0.2, 1.0, 1.0)
        _ColorMixSpeed ("Color Mix Speed", Range(0.1, 5.0)) = 1.0
        
        [Header(Scrolling Animation)]
        _ScrollSpeedX ("Scroll Speed X", Range(-2.0, 2.0)) = 0.0
        _ScrollSpeedY ("Scroll Speed Y", Range(-2.0, 2.0)) = -0.15
        
        [Header(Glow Effect)]
        _GlowIntensity ("Glow Intensity", Range(0.0, 10.0)) = 3.0
        _GlowPower ("Glow Power", Range(0.5, 5.0)) = 1.5
        _PulseSpeed ("Pulse Speed", Range(0.0, 5.0)) = 1.0
        _PulseAmount ("Pulse Amount", Range(0.0, 1.0)) = 0.3
        
        [Header(Fade Settings)]
        _FadeStart ("Fade Start (Y)", Range(0.0, 1.0)) = 0.7
        _FadeEnd ("Fade End (Y)", Range(0.0, 1.0)) = 0.0
        _TopFadeStart ("Top Fade Start", Range(0.0, 1.0)) = 0.9
        _TopFadeEnd ("Top Fade End", Range(0.0, 1.0)) = 1.0
        
        [Header(Horizontal Lines)]
        _HLineIntensity ("Horizontal Line Intensity", Range(0.0, 2.0)) = 0.5
        _HLineCount ("Horizontal Line Count", Range(1.0, 100.0)) = 30.0
        _HLineSpeed ("Horizontal Line Speed", Range(0.0, 5.0)) = 0.5
        
        [Header(Distortion)]
        _DistortionAmount ("Distortion Amount", Range(0.0, 0.1)) = 0.01
        _DistortionSpeed ("Distortion Speed", Range(0.0, 10.0)) = 2.0
        
        [Header(Layer Settings)]
        _Layer2Scale ("Layer 2 Scale", Range(0.5, 3.0)) = 1.5
        _Layer2Opacity ("Layer 2 Opacity", Range(0.0, 1.0)) = 0.3
        _Layer2Offset ("Layer 2 Scroll Offset", Range(0.0, 2.0)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        
        Pass
        {
            Name "CircuitGrid"
            Tags { "LightMode" = "Universal2D" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float fogFactor : TEXCOORD1;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float _ColorMixSpeed;
                float _ScrollSpeedX;
                float _ScrollSpeedY;
                float _GlowIntensity;
                float _GlowPower;
                float _PulseSpeed;
                float _PulseAmount;
                float _FadeStart;
                float _FadeEnd;
                float _TopFadeStart;
                float _TopFadeEnd;
                float _HLineIntensity;
                float _HLineCount;
                float _HLineSpeed;
                float _DistortionAmount;
                float _DistortionSpeed;
                float _Layer2Scale;
                float _Layer2Opacity;
                float _Layer2Offset;
            CBUFFER_END
            
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            float noise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);
                
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y;
                
                // ディストーション
                float2 distortion = float2(
                    sin(IN.uv.y * 20.0 + time * _DistortionSpeed) * _DistortionAmount,
                    cos(IN.uv.x * 15.0 + time * _DistortionSpeed * 0.7) * _DistortionAmount * 0.5
                );
                
                // メインレイヤーUV
                float2 uv1 = IN.uv + distortion;
                uv1.x += time * _ScrollSpeedX;
                uv1.y += time * _ScrollSpeedY;
                
                // セカンドレイヤーUV
                float2 uv2 = IN.uv * _Layer2Scale + distortion * 0.5;
                uv2.x += time * _ScrollSpeedX * _Layer2Offset;
                uv2.y += time * _ScrollSpeedY * _Layer2Offset;
                
                // テクスチャサンプリング
                half4 tex1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv1);
                half4 tex2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv2);
                
                // 輝度計算
                float lum1 = dot(tex1.rgb, float3(0.299, 0.587, 0.114));
                float lum2 = dot(tex2.rgb, float3(0.299, 0.587, 0.114));
                float combinedLum = lum1 + lum2 * _Layer2Opacity;
                
                // カラーミキシング
                float yColorMix = smoothstep(0.0, 1.0, IN.uv.y);
                half3 mixedColor = lerp(_Color1.rgb, lerp(_Color2.rgb, _Color3.rgb, yColorMix * 0.5), yColorMix);
                
                // パルス
                float pulse = 1.0 + sin(time * _PulseSpeed) * _PulseAmount;
                
                // グロー
                float glow = pow(combinedLum, _GlowPower) * _GlowIntensity * pulse;
                
                // 水平ライン
                float hLine = sin((IN.uv.y + time * _HLineSpeed) * _HLineCount * 6.28318);
                hLine = smoothstep(0.8, 1.0, hLine) * _HLineIntensity;
                hLine *= noise(float2(IN.uv.x * 10.0, time * 0.5));
                
                // フェード
                float bottomFade = smoothstep(_FadeEnd, _FadeStart, IN.uv.y);
                float topFade = 1.0 - smoothstep(_TopFadeStart, _TopFadeEnd, IN.uv.y);
                float totalFade = bottomFade * topFade;
                
                // 最終カラー
                half3 finalColor = mixedColor * (glow + hLine);
                float alpha = saturate(combinedLum * glow + hLine * 0.5) * totalFade * IN.color.a;
                
                finalColor = min(finalColor, 10.0);
                
                half4 col = half4(finalColor, alpha);
                col.rgb = MixFog(col.rgb, IN.fogFactor);
                
                return col;
            }
            ENDHLSL
        }
        
        // 3D用のパス
        Pass
        {
            Name "CircuitGrid3D"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float fogFactor : TEXCOORD1;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float _ColorMixSpeed;
                float _ScrollSpeedX;
                float _ScrollSpeedY;
                float _GlowIntensity;
                float _GlowPower;
                float _PulseSpeed;
                float _PulseAmount;
                float _FadeStart;
                float _FadeEnd;
                float _TopFadeStart;
                float _TopFadeEnd;
                float _HLineIntensity;
                float _HLineCount;
                float _HLineSpeed;
                float _DistortionAmount;
                float _DistortionSpeed;
                float _Layer2Scale;
                float _Layer2Opacity;
                float _Layer2Offset;
            CBUFFER_END
            
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            float noise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y;
                
                float2 distortion = float2(
                    sin(IN.uv.y * 20.0 + time * _DistortionSpeed) * _DistortionAmount,
                    cos(IN.uv.x * 15.0 + time * _DistortionSpeed * 0.7) * _DistortionAmount * 0.5
                );
                
                float2 uv1 = IN.uv + distortion;
                uv1.x += time * _ScrollSpeedX;
                uv1.y += time * _ScrollSpeedY;
                
                float2 uv2 = IN.uv * _Layer2Scale + distortion * 0.5;
                uv2.x += time * _ScrollSpeedX * _Layer2Offset;
                uv2.y += time * _ScrollSpeedY * _Layer2Offset;
                
                half4 tex1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv1);
                half4 tex2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv2);
                
                float lum1 = dot(tex1.rgb, float3(0.299, 0.587, 0.114));
                float lum2 = dot(tex2.rgb, float3(0.299, 0.587, 0.114));
                float combinedLum = lum1 + lum2 * _Layer2Opacity;
                
                float yColorMix = smoothstep(0.0, 1.0, IN.uv.y);
                half3 mixedColor = lerp(_Color1.rgb, lerp(_Color2.rgb, _Color3.rgb, yColorMix * 0.5), yColorMix);
                
                float pulse = 1.0 + sin(time * _PulseSpeed) * _PulseAmount;
                float glow = pow(combinedLum, _GlowPower) * _GlowIntensity * pulse;
                
                float hLine = sin((IN.uv.y + time * _HLineSpeed) * _HLineCount * 6.28318);
                hLine = smoothstep(0.8, 1.0, hLine) * _HLineIntensity;
                hLine *= noise(float2(IN.uv.x * 10.0, time * 0.5));
                
                float bottomFade = smoothstep(_FadeEnd, _FadeStart, IN.uv.y);
                float topFade = 1.0 - smoothstep(_TopFadeStart, _TopFadeEnd, IN.uv.y);
                float totalFade = bottomFade * topFade;
                
                half3 finalColor = mixedColor * (glow + hLine);
                float alpha = saturate(combinedLum * glow + hLine * 0.5) * totalFade * IN.color.a;
                
                finalColor = min(finalColor, 10.0);
                
                half4 col = half4(finalColor, alpha);
                col.rgb = MixFog(col.rgb, IN.fogFactor);
                
                return col;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Unlit"
}
