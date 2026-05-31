Shader "Hidden/TSMP/Decode RGB16 Bytes"
{
    Properties
    {
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
            #include "../../../com.kibalab.tsmp.core/Runtime/Codecs/Common/Shaders/cgincs/TSMPDecodeCommon.cginc"

            float _Rgb16CalibrationStartBlock;

            float SampleChannelCalibration(int level, int offset, int channel)
            {
                float3 c = SampleBlockByIndex(_Rgb16CalibrationStartBlock + offset + clamp(level, 0, 15));
                return channel == 0 ? c.r : channel == 1 ? c.g : c.b;
            }

            int ClassifyChannel444(float value, int offset, int channel)
            {
                float low = SampleChannelCalibration(0, offset, channel);
                float high = SampleChannelCalibration(15, offset, channel);
                float range = high - low;

                if (abs(range) > 0.00001)
                {
                    int estimated = (int)round(saturate((value - low) / range) * 15.0);
                    int bestIndex = estimated;
                    float bestDistance = 999.0;

                    [loop]
                    for (int i = 0; i < 7; i++)
                    {
                        int index = clamp(estimated - 3 + i, 0, 15);
                        float candidate = SampleChannelCalibration(index, offset, channel);
                        float distance = abs(value - candidate);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestIndex = index;
                        }
                    }

                    return bestIndex;
                }

                int fallbackBest = 0;
                float fallbackDistance = 999.0;
                [loop]
                for (int i = 0; i < 16; i++)
                {
                    float candidate = SampleChannelCalibration(i, offset, channel);
                    float distance = abs(value - candidate);
                    if (distance < fallbackDistance)
                    {
                        fallbackDistance = distance;
                        fallbackBest = i;
                    }
                }

                return fallbackBest;
            }

            int DecodeRgb16Symbol444(int symbolIndex)
            {
                float3 rgb = SampleBlockByIndex(PayloadBlockIndex(symbolIndex));
                int r = ClassifyChannel444(rgb.r, 0, 0);
                int g = ClassifyChannel444(rgb.g, 16, 1);
                int b = ClassifyChannel444(rgb.b, 32, 2);
                return r | (g << 4) | (b << 8);
            }

            int SelectCachedSymbol(int symbolOffset, int s0, int s1, int s2, int s3)
            {
                if (symbolOffset == 0) return s0;
                if (symbolOffset == 1) return s1;
                if (symbolOffset == 2) return s2;
                return s3;
            }

            int DecodeByteFromRgb12Symbols(int bitOffset, int s0, int s1, int s2, int s3)
            {
                int symbolOffset = FloorDivNonNegative(bitOffset, 12.0);
                int bitShift = bitOffset - symbolOffset * 12;
                int symbol = SelectCachedSymbol(symbolOffset, s0, s1, s2, s3);

                if (bitShift + 8 <= 12)
                    return (symbol >> bitShift) & 0xFF;

                int nextSymbol = SelectCachedSymbol(symbolOffset + 1, s0, s1, s2, s3);
                int bitsFromFirst = 12 - bitShift;
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

                int baseBit = baseByte * 8;
                int symbolIndex = FloorDivNonNegative(baseBit, 12.0);
                int firstBitOffset = baseBit - symbolIndex * 12;

                int s0 = DecodeRgb16Symbol444(symbolIndex);
                int s1 = DecodeRgb16Symbol444(symbolIndex + 1);
                int s2 = DecodeRgb16Symbol444(symbolIndex + 2);
                int s3 = firstBitOffset == 8 ? DecodeRgb16Symbol444(symbolIndex + 3) : 0;

                int b0 = baseByte + 0 < (int)_ByteCount ? DecodeByteFromRgb12Symbols(firstBitOffset + 0, s0, s1, s2, s3) : 0;
                int b1 = baseByte + 1 < (int)_ByteCount ? DecodeByteFromRgb12Symbols(firstBitOffset + 8, s0, s1, s2, s3) : 0;
                int b2 = baseByte + 2 < (int)_ByteCount ? DecodeByteFromRgb12Symbols(firstBitOffset + 16, s0, s1, s2, s3) : 0;
                int b3 = baseByte + 3 < (int)_ByteCount ? DecodeByteFromRgb12Symbols(firstBitOffset + 24, s0, s1, s2, s3) : 0;

                return float4(b0 / 255.0, b1 / 255.0, b2 / 255.0, b3 / 255.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
