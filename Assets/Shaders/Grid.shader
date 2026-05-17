Shader "Custom/GlowingGrid2D"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (0, 1, 1, 1)
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1)
        _GridSpacing ("Grid Spacing", Range(0.01, 1)) = 0.1
        _LineWidth ("Line Width", Range(0.001, 0.2)) = 0.025
        _FlowSpeed ("Flow Speed", Range(0, 5)) = 1
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 0.5
        _ParticleSize ("Particle Size", Range(0.1, 5)) = 1.2
        _ParticleDensity ("Particle Density", Range(10, 500)) = 250
        _HorizontalSpeed ("Horizontal Speed", Range(0, 2)) = 0.08
        _DensityGradient ("Density Gradient", Range(0, 1)) = 0.7
        _LineConcentration ("Line Concentration", Range(1, 20)) = 12
    }
    
    // Built-in Render Pipeline
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
        }
        
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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            float4 _GridColor;
            float4 _BackgroundColor;
            float _GridSpacing;
            float _LineWidth;
            float _FlowSpeed;
            float _GlowIntensity;
            float _ParticleSize;
            float _ParticleDensity;
            float _HorizontalSpeed;
            float _DensityGradient;
            float _LineConcentration;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float random(float2 st)
            {
                return frac(sin(dot(st, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            float DrawParticles(float2 uv, float2 direction, float time, float yPosition, float particleSpeed)
            {
                float densityMultiplier = lerp(1.0, _DensityGradient, yPosition);
                float adjustedDensity = _ParticleDensity * densityMultiplier;
                
                float2 gridCell = floor(uv * adjustedDensity);
                float2 cellUV = frac(uv * adjustedDensity);
                
                float particleStrength = 0.0;
                
                for(int x = -1; x <= 1; x++)
                {
                    for(int y = -1; y <= 1; y++)
                    {
                        float2 neighbor = gridCell + float2(x, y);
                        float2 neighborCellUV = cellUV - float2(x, y);
                        
                        float rand = random(neighbor);
                        
                        // Y座標に基づく出現確率を緩和
                        float appearanceThreshold = lerp(0.2, 0.4, yPosition);
                        if(rand < appearanceThreshold) continue;
                        
                        float2 particlePos = float2(0.5, 0.5);
                        
                        // 移動方向に沿ったアニメーション
                        particlePos += direction * frac(time * particleSpeed + rand) * 0.8 - direction * 0.4;
                        
                        // 線に垂直な方向にのみ小さなばらつきを加える
                        // 移動方向にはばらつきを加えない！
                        if(abs(direction.x) > 0.5) // 横方向の線（X方向に移動）
                        {
                            // Y方向（線に垂直）のばらつきのみ
                            particlePos.y += (random(neighbor + float2(0.0, 1.0)) - 0.5) * 0.05 / _LineConcentration;
                        }
                        else // 縦方向の線（Y方向に移動）
                        {
                            // X方向（線に垂直）のばらつきのみ
                            particlePos.x += (random(neighbor + float2(1.0, 0.0)) - 0.5) * 0.05 / _LineConcentration;
                        }
                        
                        float dist = length(neighborCellUV - particlePos);
                        float particle = 1.0 - smoothstep(0.0, 0.015 * _ParticleSize, dist);
                        particle *= rand * 0.3 + 0.7;
                        
                        particleStrength += particle;
                    }
                }
                
                return saturate(particleStrength);
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float time = _Time.y * _FlowSpeed;
                float2 gridUV = i.uv / _GridSpacing;
                float yPos = i.uv.y;
                
                float horizontalParticles = 0.0;
                
                // 最も近い横線を計算
                float nearestHorizontalLine = round(gridUV.y);
                float distToHorizontalLine = abs(gridUV.y - nearestHorizontalLine);
                
                // 横線の範囲内にいる場合のみ粒子を描画
                if(distToHorizontalLine < _LineWidth / _GridSpacing)
                {
                    float2 lineUV = float2(i.uv.x, nearestHorizontalLine);
                    horizontalParticles = DrawParticles(lineUV, float2(1, 0), time, yPos, _HorizontalSpeed);
                }
                
                // 粒子を合成
                float particles = saturate(horizontalParticles);
                
                // 控えめなグロー効果
                float glow = particles * _GlowIntensity;
                
                // 最終カラーを計算
                fixed4 finalColor = lerp(_BackgroundColor, _GridColor, particles);
                finalColor.rgb += _GridColor.rgb * glow * 0.3;
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Sprites/Default"
}