Shader "Hidden/TSMP/Decode RGB16 Variable Refine Bytes"
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
        _RBits ("R Bits", Float) = 4
        _GBits ("G Bits", Float) = 4
        _BBits ("B Bits", Float) = 4
        _RefineRadius ("Refine Radius", Float) = 1
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
            float _RBits;
            float _GBits;
            float _BBits;
            float _RefineRadius;

            float ChannelValue(float3 c, int channel)
            {
                return channel == 0 ? c.r : channel == 1 ? c.g : c.b;
            }

            float SampleChannelCalibration(int level, int offset, int channel, int count)
            {
                int index = clamp(level, 0, count - 1);
                return ChannelValue(SampleBlockByIndex(_Rgb16CalibrationStartBlock + offset + index), channel);
            }

            int ClassifyChannel(float value, int count, int offset, int channel)
            {
                float low = SampleChannelCalibration(0, offset, channel, count);
                float high = SampleChannelCalibration(count - 1, offset, channel, count);
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
                    float candidate = SampleChannelCalibration(index, offset, channel, count);
                    float distance = abs(value - candidate);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = index;
                    }
                }

                return bestIndex;
            }

            int RefineRgb16Local(float3 rgb, int r0, int g0, int b0, int rCount, int gCount, int bCount, int gOffset, int bOffset)
            {
                float3 p = RgbToYCoCg(rgb);
                int radius = clamp((int)_RefineRadius, 1, 3);
                int bestR = r0;
                int bestG = g0;
                int bestB = b0;
                float bestDistance = 999999.0;

                [loop]
                for (int db = -3; db <= 3; db++)
                {
                    if (db < -radius || db > radius) continue;

                    [loop]
                    for (int dg = -3; dg <= 3; dg++)
                    {
                        if (dg < -radius || dg > radius) continue;

                        [loop]
                        for (int dr = -3; dr <= 3; dr++)
                        {
                            if (dr < -radius || dr > radius) continue;

                            int r = clamp(r0 + dr, 0, rCount - 1);
                            int g = clamp(g0 + dg, 0, gCount - 1);
                            int b = clamp(b0 + db, 0, bCount - 1);
                            float3 c = RgbToYCoCg(float3(
                                SampleChannelCalibration(r, 0, 0, rCount),
                                SampleChannelCalibration(g, gOffset, 1, gCount),
                                SampleChannelCalibration(b, bOffset, 2, bCount)));
                            float3 d = p - c;
                            float distance = d.x * d.x * 2.0 + d.y * d.y * 0.85 + d.z * d.z * 0.85;
                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                bestR = r;
                                bestG = g;
                                bestB = b;
                            }
                        }
                    }
                }

                return bestR | (bestG << (int)_RBits) | (bestB << ((int)_RBits + (int)_GBits));
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
                return RefineRgb16Local(rgb, r, g, b, rCount, gCount, bCount, gOffset, bOffset);
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

            #include "../../../com.kibalab.tsmp.core/Runtime/Codecs/Common/Shaders/cgincs/TSMPDecodeByteOutput.cginc"
            ENDCG
        }
    }

    Fallback Off
}
