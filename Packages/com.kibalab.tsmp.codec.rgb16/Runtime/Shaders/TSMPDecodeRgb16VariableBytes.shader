Shader "Hidden/TSMP/Decode RGB16 Variable Bytes"
{
    Properties
    {
        [HideInInspector] _CalibrationLut ("Calibration LUT", 2D) = "black" {}
        _MainTex ("TSMP Source", 2D) = "black" {}
        _BlockSize ("Block Size", Float) = 8
        _SampleSize ("Sample Size", Float) = 0
        _StartBlock ("Start Block", Float) = 0
        _ByteCount ("Byte Count", Float) = 0
        _ActiveWidthBlocks ("Active Width Blocks", Float) = 80
        _SourceWidth ("Source Width", Float) = 640
        _SourceHeight ("Source Height", Float) = 360
        _OutputWidth ("Output Width", Float) = 14
        _OutputHeight ("Output Height", Float) = 1
        _FlipY ("Flip Y", Float) = 1
        _Rgb16CalibrationStartBlock ("RGB16 Calibration Start Block", Float) = 640
        _RBits ("R Bits", Float) = 4
        _GBits ("G Bits", Float) = 4
        _BBits ("B Bits", Float) = 4
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Overlay" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ TSMP_CALIBRATION_LUT
            #include "Packages/com.kibalab.tsmp.core/Runtime/Codecs/Common/Shaders/cgincs/TSMPDecodeCommon.cginc"

#if defined(TSMP_CALIBRATION_LUT)
            Texture2D<float4> _CalibrationLut;
#endif

            float _Rgb16CalibrationStartBlock;
            float _RBits;
            float _GBits;
            float _BBits;

            float3 SampleRgb16Calibration(int index)
            {
#if defined(TSMP_CALIBRATION_LUT)
                return _CalibrationLut.Load(int3(index, 0, 0)).rgb;
#else
                return SampleBlockByIndex(_Rgb16CalibrationStartBlock + index);
#endif
            }

            float ChannelValue(float3 c, int channel)
            {
                return channel == 0 ? c.r : channel == 1 ? c.g : c.b;
            }

            int ClassifyChannel(float value, int count, int offset, int channel)
            {
                float low = ChannelValue(SampleRgb16Calibration(offset), channel);
                float high = ChannelValue(SampleRgb16Calibration(offset + count - 1), channel);
                float range = high - low;
                int estimated = abs(range) > 0.00001 ? (int)round(saturate((value - low) / range) * (count - 1)) : count / 2;
                int radius = count <= 32 ? 5 : 10;
                int bestIndex = estimated;
                float bestDistance = 999.0;

                [loop]
                for (int i = 0; i < 21; i++)
                {
                    if (i > radius * 2) break;
                    int index = clamp(estimated - radius + i, 0, count - 1);
                    float candidate = ChannelValue(SampleRgb16Calibration(offset + index), channel);
                    float distance = abs(value - candidate);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = index;
                    }
                }

                return bestIndex;
            }

            int DecodeRgb16Symbol(int symbolIndex)
            {
                int rBits = (int)_RBits;
                int gBits = (int)_GBits;
                int bBits = (int)_BBits;
                int rCount = 1 << rBits;
                int gCount = 1 << gBits;
                int bCount = 1 << bBits;
                int gOffset = rCount;
                int bOffset = rCount + gCount;

                float3 rgb = SampleBlockByIndex(PayloadBlockIndex(symbolIndex));
                int r = ClassifyChannel(rgb.r, rCount, 0, 0);
                int g = ClassifyChannel(rgb.g, gCount, gOffset, 1);
                int b = ClassifyChannel(rgb.b, bCount, bOffset, 2);
                return r | (g << rBits) | (b << (rBits + gBits));
            }

            int DecodeByte(int byteIndex)
            {
                if (byteIndex < 0 || byteIndex >= (int)_ByteCount)
                    return 0;

                int totalBits = (int)_RBits + (int)_GBits + (int)_BBits;
                int bitIndex = byteIndex * 8;
                int symbolIndex = FloorDivNonNegative(bitIndex, (float)totalBits);
                int bitShift = bitIndex - symbolIndex * totalBits;
                int symbol = DecodeRgb16Symbol(symbolIndex);

                if (bitShift + 8 <= totalBits)
                    return (symbol >> bitShift) & 0xFF;

                int nextSymbol = DecodeRgb16Symbol(symbolIndex + 1);
                int bitsFromFirst = totalBits - bitShift;
                int lowMask = (1 << bitsFromFirst) - 1;
                int highBits = 8 - bitsFromFirst;
                int highMask = (1 << highBits) - 1;
                return ((symbol >> bitShift) & lowMask) | ((nextSymbol & highMask) << bitsFromFirst);
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 pixel = floor(i.uv * float2(_OutputWidth, _OutputHeight));
                pixel = clamp(pixel, 0.0, float2(_OutputWidth - 1.0, _OutputHeight - 1.0));
                int baseByte = ((int)pixel.y * (int)_OutputWidth + (int)pixel.x) * 4;
                if (baseByte >= (int)_ByteCount)
                    return 0.0;

                int totalBits = (int)_RBits + (int)_GBits + (int)_BBits;
                if (totalBits < 8)
                    return float4(DecodeByte(baseByte), DecodeByte(baseByte + 1),
                        DecodeByte(baseByte + 2), DecodeByte(baseByte + 3)) / 255.0;

                int bitIndex = baseByte * 8;
                int symbolIndex = FloorDivNonNegative(bitIndex, (float)totalBits);
                int bitShift = bitIndex - symbolIndex * totalBits;
                int requiredBits = min(4, (int)_ByteCount - baseByte) * 8;
                uint packed = (uint)DecodeRgb16Symbol(symbolIndex) >> bitShift;
                int nextBit = totalBits - bitShift;

                if (nextBit < requiredBits)
                {
                    packed |= (uint)DecodeRgb16Symbol(symbolIndex + 1) << nextBit;
                    nextBit += totalBits;
                }
                if (nextBit < requiredBits)
                {
                    packed |= (uint)DecodeRgb16Symbol(symbolIndex + 2) << nextBit;
                    nextBit += totalBits;
                }
                if (nextBit < requiredBits)
                {
                    packed |= (uint)DecodeRgb16Symbol(symbolIndex + 3) << nextBit;
                    nextBit += totalBits;
                }
                if (nextBit < requiredBits)
                {
                    packed |= (uint)DecodeRgb16Symbol(symbolIndex + 4) << nextBit;
                    nextBit += totalBits;
                }

                if (requiredBits < 32)
                    packed &= (1u << requiredBits) - 1u;

                return float4(packed & 0xFFu, (packed >> 8) & 0xFFu,
                    (packed >> 16) & 0xFFu, (packed >> 24) & 0xFFu) / 255.0;
            }
            ENDCG
        }
    }

    Fallback Off
}
