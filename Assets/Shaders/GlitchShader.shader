
Shader "Custom/GeneratedShader" {
    Properties {
        [HideInInspector] _MainTex("Texture", 2D) = "white" { }
        _Color("Color", Color) = (1, 1, 1, 1)
        _Contrast("Contrast", Range(0, 5)) = 1.0
        _Brightness("Brightness", Range(-2, 2)) = 0.0
            [Toggle] _uBasicEnabled ("basicEnabled", Float) = 1.0 
        [Toggle] _uGlitchEnabled ("glitchEnabled", Float) = 1.0 
        [Toggle] _uHologramEnabled ("hologramEnabled", Float) = 1.0 
        _uTiling ("tiling", Vector) = (1.0000, 1.0000, 0, 0) 
        _uGlitchStrength ("glitchStrength", Range(0, 1)) = 1.0000 
        _uGlitchSpeed ("glitchSpeed", Range(0, 20)) = 18.5000 
        _uGlitchBlockSize ("glitchBlockSize", Range(1, 50)) = 14.0000 
        _uBlendOpacity ("blendOpacity", Range(0, 1)) = 1.0000 
        _uHologramDensity ("hologramDensity", Range(1, 20)) = 2.0000 
        _uHologramSpeed ("hologramSpeed", Range(0, 10)) = 2.0000 
        _uHologramColorStrength ("hologramColorStrength", Range(0, 2)) = 0.5000 
        _uHologramFlicker ("hologramFlicker", Range(0, 1)) = 0.5000 

    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            
#define vec2 float2
#define vec3 float3
#define vec4 float4
#define mat2 float2x2
#define mat3 float3x3
#define mat4 float4x4
#define mix lerp
#define fract frac
#define texture2D tex2D

            // GLSLの mod(x, y) は x - y * floor(x/y)
            float mod_impl(float x, float y) { return x - y * floor(x / y); }
            float2 mod_impl(float2 x, float y) { return x - y * floor(x / y); }
            float3 mod_impl(float3 x, float y) { return x - y * floor(x / y); }
            float4 mod_impl(float4 x, float y) { return x - y * floor(x / y); }
            float2 mod_impl(float2 x, float2 y) { return x - y * floor(x / y); }
            float3 mod_impl(float3 x, float3 y) { return x - y * floor(x / y); }
            float4 mod_impl(float4 x, float4 y) { return x - y * floor(x / y); }
#define mod mod_impl
    

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

                sampler2D _MainTex;
    float _uTime;
    float2 _uResolution;
    float4 _Color;
    float _Contrast;
    float _Brightness;
    // トグル
    float _uBasicEnabled;
    // Phase 16 トグル
    float _uGlitchEnabled;
    // Batch 1 トグル
    // Batch 2 トグル
    float _uHologramEnabled;
    // Batch 3 トグル
    // Batch 4 トグル
    // Batch 5 トグル
    // パラメータ
    float _uDissolveNoiseType;
    // Phase 16 パラメータ
    float _uGlitchStrength;
    float _uGlitchSpeed;
    float _uGlitchBlockSize;
    // Phase H1
    // データモッシュ
    // フラクタルノイズ
    // Batch 1 パラメータ
    // UV回転
    // 極座標変換
    // ノイズ歪み
    // カスタムマスク
    // 円形マスク
    // グラデーションマスク
    // ノイズマスク
    // 距離フェード
    // Batch 2 パラメータ
    // ミラータイル
    // ランダムタイル
    // ハイライト走査
    // ホログラム
    float _uHologramDensity;
    float _uHologramSpeed;
    float _uHologramColorStrength;
    float _uHologramFlicker;
    // Batch 3 パラメータ
    // Batch 4 パラメータ
    // Batch 5 パラメータ
    float _uBlendOpacity;
    float4 _uTiling;


            
    
// === ノイズ関数 ===
    float3 permute(float3 x) { return fmod(((x * 34.0) + 1.0) * x, 289.0); }

    float snoise(float2 v) {
      const float4 C = float4(0.211324865405187, 0.366025403784439,
                          -0.577350269189626, 0.024390243902439);
      float2 i  = floor(v + dot(v, C.yy));
      float2 x0 = v - i + dot(i, C.xx);
      float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
      float4 x12 = x0.xyxy + C.xxzz;
      x12.xy -= i1;
      i = fmod(i, 289.0);
      float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0)) + i.x + float3(0.0, i1.x, 1.0));
      float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
      m = m * m; m = m * m;
      float3 x_ = 2.0 * frac(p * C.www) - 1.0;
      float3 h = abs(x_) - 0.5;
      float3 ox = floor(x_ + 0.5);
      float3 a0 = x_ - ox;
      m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
      float3 g;
      g.x = a0.x * x0.x + h.x * x0.y;
      g.yz = a0.yz * x12.xz + h.yz * x12.yw;
      return 130.0 * dot(m, g);
    }

    float voronoi(float2 p) {
      float2 n = floor(p); float2 f = frac(p);
      float minDist = 1.0;
      for (int j = -1; j <= 1; j++) {
        for (int i = -1; i <= 1; i++) {
          float2 g = float2(float(i), float(j));
          float2 o = float2(frac(sin(dot(n+g, float2(127.1,311.7)))*43758.5453),
                        frac(sin(dot(n+g, float2(269.5,183.3)))*43758.5453));
          float d = dot(g+o-f, g+o-f);
          minDist = min(minDist, d);
        }
      }
      return sqrt(minDist);
    }

    float cellular(float2 p) {
      float2 n = floor(p); float2 f = frac(p);
      float d1 = 1.0, d2 = 1.0;
      for (int j = -1; j <= 1; j++) {
        for (int i = -1; i <= 1; i++) {
          float2 g = float2(float(i), float(j));
          float2 o = float2(frac(sin(dot(n+g, float2(127.1,311.7)))*43758.5453),
                        frac(sin(dot(n+g, float2(269.5,183.3)))*43758.5453));
          float d = dot(g+o-f, g+o-f);
          if (d < d1) { d2 = d1; d1 = d; }
          else if (d < d2) { d2 = d; }
        }
      }
      return d2 - d1;
    }

    float getNoiseValue(float2 p) {
      if (_uDissolveNoiseType < 0.5) { float n = snoise(p); return (n+1.0)*0.5; }
      else if (_uDissolveNoiseType < 1.5) { return voronoi(p); }
      else { return cellular(p); }
    }

    // ランダム関数
    float rand(float2 n) {
      return frac(sin(dot(n, float2(12.9898, 4.1414))) * 43758.5453);
    }

    
// === Custom Shader PlaceHolder ===
    // この関数は実行時にユーザー入力コードで置換されます
    #define CUSTOM_SHADER_AVAILABLE
    //__CUSTOM_START__
    float4 customEffect(float4 color, float2 uv, float time) {
        return color;
    }
    //__CUSTOM_END__


            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Simple pass through for UV
                o.uv = v.uv; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                
                float2 uv = i.uv * _uTiling.xy; // Unity uv
                float time = _Time.y; // Unity組み込みの時間を使用（_uTimeは常に0のため）
                float2 _uResolution = _ScreenParams.xy;
                float aspect = 1.0;
                if (length(ddx(uv)) > 0.0001 && length(ddy(uv)) > 0.0001) {
                    aspect = abs(ddy(uv.y)) / max(abs(ddx(uv.x)), 0.0001);
                } else {
                    aspect = _uResolution.x / max(_uResolution.y, 1.0);
                }
                // Dummy for uniform texture sizes if any
                float2 texSize = float2(1024.0, 1024.0);
                float4 texColor = tex2D(_MainTex, uv);
                float3 color = texColor.rgb * _Color.rgb;
                float3 originalColor = color;
                float alpha = texColor.a * _Color.a;
    
                // --- glitch_uv ---
                
if (_uGlitchEnabled > 0.5) {
        float timeBlock = floor(time * _uGlitchSpeed);
        float trigger = step(0.8, rand(float2(timeBlock, 0.0)));
  if (trigger > 0.5) {
          float blockY = floor(uv.y * _uGlitchBlockSize);
          float shift = (rand(float2(blockY, timeBlock)) - 0.5) * _uGlitchStrength * 0.1;
    uv.x += shift;
          float blockNoise = rand(float2(blockY, timeBlock + 1.0));
    if (blockNoise > 0.95) {
      uv.y += (rand(float2(timeBlock, blockY)) - 0.5) * 0.02 * _uGlitchStrength;
    }
  }
}

                // --- basic ---
                
if (_uBasicEnabled > 0.5) {
  color *= _Color.rgb;
  color = (color - 0.5) * _Contrast + 0.5 + _Brightness;
}

                // --- hologram ---
                
if (_uHologramEnabled > 0.5) {
  float holoLine = sin(uv.y * _uHologramDensity * 100.0 + time * _uHologramSpeed) * 0.5 + 0.5;
  float holoFlicker = 1.0 - _uHologramFlicker * rand(float2(floor(time * 10.0), 0.0)) * 0.3;
  float3 holoRainbow = float3(
    sin(uv.y * 20.0 + time) * 0.5 + 0.5,
    sin(uv.y * 20.0 + time + 2.094) * 0.5 + 0.5,
    sin(uv.y * 20.0 + time + 4.189) * 0.5 + 0.5
  );
  color = lerp(color, color + holoRainbow * _uHologramColorStrength, holoLine * 0.5);
  color *= holoFlicker;
}

                return fixed4(color, saturate(alpha * _uBlendOpacity));
    
            }
            ENDCG
        }
    }
}
    