Shader "Hidden/TSMP/Debug Rgb16 Calibration"
{
    Properties
    {
        _MainTex ("TSMP Source", 2D) = "black" {}
        _BlockSize ("Block Size", Float) = 8
        _ActiveWidthBlocks ("Active Width Blocks", Float) = 80
        _SourceWidth ("Source Width", Float) = 640
        _SourceHeight ("Source Height", Float) = 360
        _FlipY ("Flip Y", Float) = 1
        _Rgb16CalibrationStartBlock ("RGB16 Calibration Start Block", Float) = 640
        _GridOpacity ("Grid Opacity", Range(0, 1)) = 0.22
        _LineWidth ("Line Width", Range(0.001, 0.05)) = 0.008
        _RBits ("R Bits", Float) = 4
        _GBits ("G Bits", Float) = 4
        _BBits ("B Bits", Float) = 4
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            float _BlockSize;
            float _ActiveWidthBlocks;
            float _SourceWidth;
            float _SourceHeight;
            float _FlipY;
            float _Rgb16CalibrationStartBlock;
            float _GridOpacity;
            float _LineWidth;
            float _RBits;
            float _GBits;
            float _BBits;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            static const float kGap        = 0.014;
            static const float kHeaderH    = 0.052;
            static const float kGraphLeft  = 0.018;
            static const float kGraphRight = 0.715;
            static const float kSwatchLeft = 0.735;
            static const float kSwatchRight= 0.982;
            static const float kPanelTop   = 0.982;
            static const float kPanelBot   = 0.018;
            static const float3 kBgColor   = float3(0.030, 0.032, 0.038);
            static const float3 kPanelColor= float3(0.060, 0.063, 0.072);
            static const float3 kAxisColor = float3(0.55, 0.57, 0.66);

            float3 SampleRgbAtTopLeftPixel(float2 pixel)
            {
                float rawY = (pixel.y + 0.5) / _SourceHeight;
                float uvY = lerp(rawY, 1.0 - rawY, _FlipY);
                float2 uv = float2((pixel.x + 0.5) / _SourceWidth, uvY);
                return _MainTex.SampleLevel(sampler_MainTex, uv, 0.0).rgb;
            }

            float3 SampleBlockRgb(float blockX, float blockY)
            {
                float sampleSize = _BlockSize >= 8.0 ? 4.0 : 3.0;
                int sampleLimit = (int)sampleSize;
                float startOffset = floor((_BlockSize - sampleSize) * 0.5);
                float2 startPixel = float2(blockX, blockY) * _BlockSize + startOffset.xx;
                float3 sum = 0.0;

                [unroll]
                for (int y = 0; y < 4; y++)
                {
                    [unroll]
                    for (int x = 0; x < 4; x++)
                    {
                        if (x < sampleLimit && y < sampleLimit)
                            sum += SampleRgbAtTopLeftPixel(startPixel + float2(x, y));
                    }
                }

                return sum / (sampleLimit * sampleLimit);
            }

            float3 SampleBlockByIndex(float blockIndex)
            {
                float blockX = fmod(blockIndex, _ActiveWidthBlocks);
                float blockY = floor(blockIndex / _ActiveWidthBlocks);
                return SampleBlockRgb(blockX, blockY);
            }

            float3 ChannelColor(int channel)
            {
                return channel == 0 ? float3(0.96, 0.36, 0.36)
                     : channel == 1 ? float3(0.42, 0.92, 0.46)
                                    : float3(0.40, 0.62, 1.00);
            }

            float SampleCalibration(int channel, int level)
            {
                int rCount = 1 << clamp((int)_RBits, 1, 8);
                int gCount = 1 << clamp((int)_GBits, 1, 8);
                int bCount = 1 << clamp((int)_BBits, 1, 4);
                int offset = channel == 0 ? 0 : channel == 1 ? rCount : rCount + gCount;
                int count  = channel == 0 ? rCount : channel == 1 ? gCount : bCount;
                int clamped = clamp(level, 0, count - 1);
                float3 c = SampleBlockByIndex(_Rgb16CalibrationStartBlock + offset + clamped);
                return channel == 0 ? c.r : channel == 1 ? c.g : c.b;
            }

            float CurveValue(float x, int channel)
            {
                int rCount = 1 << clamp((int)_RBits, 1, 8);
                int gCount = 1 << clamp((int)_GBits, 1, 8);
                int bCount = 1 << clamp((int)_BBits, 1, 4);
                int count = channel == 0 ? rCount : channel == 1 ? gCount : bCount;
                float scaled = saturate(x) * (count - 1);
                int left = (int)floor(scaled);
                int right = min(left + 1, count - 1);
                float t = scaled - left;
                float a = SampleCalibration(channel, left);
                float b = SampleCalibration(channel, right);
                return lerp(a, b, t);
            }

            float Aa(float d, float r)
            {
                float fw = max(fwidth(d), 0.0005);
                return 1.0 - smoothstep(r - fw, r + fw, d);
            }

            bool InRect(float2 uv, float l, float r, float b, float t)
            {
                return uv.x >= l && uv.x <= r && uv.y >= b && uv.y <= t;
            }

            float BorderMask(float2 uv, float l, float r, float b, float t, float thick)
            {
                bool outer = InRect(uv, l - thick, r + thick, b - thick, t + thick);
                bool inner = InRect(uv, l + thick, r - thick, b + thick, t - thick);
                return (outer && !inner) ? 1.0 : 0.0;
            }

            float MajorGridMask(float2 g)
            {
                float dx = min(abs(g.x - 0.25), min(abs(g.x - 0.5), abs(g.x - 0.75)));
                float dy = min(abs(g.y - 0.25), min(abs(g.y - 0.5), abs(g.y - 0.75)));
                float d = min(dx, dy);
                return Aa(d, 0.003);
            }

            float MinorGridMask(float2 g)
            {
                float fx = frac(g.x * 8.0); fx = min(fx, 1.0 - fx) / 8.0;
                float fy = frac(g.y * 8.0); fy = min(fy, 1.0 - fy) / 8.0;
                float d = min(fx, fy);
                return Aa(d, 0.0018);
            }

            float IdealLineMask(float2 g)
            {
                float d = abs(g.y - g.x) * 0.70710678;
                return Aa(d, _LineWidth * 0.55);
            }

            float CurveLineMask(float2 g, int channel)
            {
                float curveY = CurveValue(g.x, channel);
                float dy = abs(g.y - curveY);
                float slopeFw = max(fwidth(curveY), 0.0);
                return Aa(dy, _LineWidth + slopeFw);
            }

            float MilestoneDotMask(float2 g, int channel)
            {
                int rCount = 1 << clamp((int)_RBits, 1, 8);
                int gCount = 1 << clamp((int)_GBits, 1, 8);
                int bCount = 1 << clamp((int)_BBits, 1, 4);
                int count = channel == 0 ? rCount : channel == 1 ? gCount : bCount;
                float best = 999.0;

                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    int level = (int)round(i * (count - 1) / 4.0);
                    float x = level / (float)(count - 1);
                    float y = SampleCalibration(channel, level);
                    best = min(best, length(g - float2(x, y)));
                }
                return Aa(best, _LineWidth * 1.9);
            }

            float3 SwatchPanel(float2 uv)
            {
                int rCount = 1 << clamp((int)_RBits, 1, 8);
                int gCount = 1 << clamp((int)_GBits, 1, 8);
                int bCount = 1 << clamp((int)_BBits, 1, 4);

                const float rowH = 1.0 / 3.0;
                int channel = uv.y > 2.0 * rowH ? 0 : uv.y > rowH ? 1 : 2;
                int count = channel == 0 ? rCount : channel == 1 ? gCount : bCount;

                float rowLocal = fmod(uv.y, rowH) / rowH;
                if (rowLocal < 0.04 || rowLocal > 0.96)
                    return kPanelColor;

                const float chipW = 0.11;
                if (uv.x < chipW)
                {
                    float chipLocalX = uv.x / chipW;
                    if (chipLocalX < 0.10 || chipLocalX > 0.86)
                        return kPanelColor;
                    return ChannelColor(channel);
                }

                float cellArea = (uv.x - chipW) / (1.0 - chipW);
                int level = clamp((int)floor(cellArea * count), 0, count - 1);
                float measured = SampleCalibration(channel, level);
                float3 baseColor = ChannelColor(channel) * measured;

                float majorLocal = frac(cellArea * 4.0);
                if (majorLocal < 0.006 || majorLocal > 0.994)
                    baseColor *= 0.35;

                return baseColor;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float3 color = kBgColor;

                float graphPanelTop = kPanelTop - kHeaderH - kGap;

                if (InRect(uv, kGraphLeft, kGraphRight, graphPanelTop + kGap, kPanelTop))
                {
                    float t = (uv.x - kGraphLeft) / (kGraphRight - kGraphLeft);
                    int chipCh = t < 0.3333 ? 0 : t < 0.6666 ? 1 : 2;

                    float chipLocal = frac(t * 3.0);
                    if (chipLocal < 0.02 || chipLocal > 0.98)
                        color = kBgColor;
                    else
                    {
                        float headerLocalY = (uv.y - (graphPanelTop + kGap)) / (kPanelTop - (graphPanelTop + kGap));
                        if (headerLocalY < 0.18)
                            color = ChannelColor(chipCh);
                        else
                            color = lerp(kPanelColor, ChannelColor(chipCh), 0.22);
                    }
                }

                else if (InRect(uv, kGraphLeft, kGraphRight, kPanelBot, graphPanelTop))
                {
                    color = kPanelColor;

                    float2 g = float2(
                        (uv.x - kGraphLeft) / (kGraphRight - kGraphLeft),
                        (uv.y - kPanelBot)  / (graphPanelTop - kPanelBot)
                    );

                    color += MinorGridMask(g) * _GridOpacity * 0.45;
                    color += MajorGridMask(g) * _GridOpacity * 1.10;

                    color = lerp(color, kAxisColor, IdealLineMask(g) * 0.55);

                    [unroll]
                    for (int ch = 0; ch < 3; ch++)
                    {
                        float curveMask = CurveLineMask(g, ch);
                        float dotMask   = MilestoneDotMask(g, ch);
                        color = lerp(color, ChannelColor(ch), saturate(curveMask + dotMask));
                    }

                    color = lerp(color, kAxisColor,
                        BorderMask(uv, kGraphLeft, kGraphRight, kPanelBot, graphPanelTop, 0.0015) * 0.45);
                }

                else if (InRect(uv, kSwatchLeft, kSwatchRight, kPanelBot, kPanelTop))
                {
                    float2 s = float2(
                        (uv.x - kSwatchLeft) / (kSwatchRight - kSwatchLeft),
                        (uv.y - kPanelBot)   / (kPanelTop - kPanelBot)
                    );
                    color = SwatchPanel(s);

                    color = lerp(color, kAxisColor,
                        BorderMask(uv, kSwatchLeft, kSwatchRight, kPanelBot, kPanelTop, 0.0015) * 0.45);
                }

                return float4(saturate(color), 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
