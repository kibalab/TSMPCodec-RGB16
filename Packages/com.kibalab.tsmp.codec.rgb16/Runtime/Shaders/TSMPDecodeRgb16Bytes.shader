Shader "Hidden/TSMP/Decode RGB16 Bytes"
{
    Properties
    {
        [HideInInspector] _TSMPHeaderTex ("Decoded Header", 2D) = "black" {}
        [HideInInspector] _TSMPHeaderPixels ("Header Pixels", Float) = 0
        [HideInInspector] _CalibrationLut ("Calibration LUT", 2D) = "black" {}
        [HideInInspector] _UseBilinear4 ("Bilinear 4x4 sampling", Float) = 0
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
            #pragma multi_compile_local _ TSMP_CALIBRATION_LUT
            #define TSMP_ENABLE_RGB_BOX4
            #include "Packages/com.kibalab.tsmp.core/Runtime/Codecs/Common/Shaders/cgincs/TSMPDecodeCommon.cginc"

#if defined(TSMP_CALIBRATION_LUT)
            Texture2D<float4> _CalibrationLut;
#endif

            float _Rgb16CalibrationStartBlock;

            float SampleChannelCalibration(int level, int offset, int channel)
            {
#if defined(TSMP_CALIBRATION_LUT)
                float3 c = _CalibrationLut.Load(int3(offset + clamp(level, 0, 15), 0, 0)).rgb;
#else
                float3 c = SampleBlockByIndex(_Rgb16CalibrationStartBlock + offset + clamp(level, 0, 15));
#endif
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

            int DecodeRgb16Symbol(uint symbolIndex)
            {
                float3 rgb = SampleBlockByIndex((uint)_StartBlock + symbolIndex);
                int r = ClassifyChannel444(rgb.r, 0, 0);
                int g = ClassifyChannel444(rgb.g, 16, 1);
                int b = ClassifyChannel444(rgb.b, 32, 2);
                return r | (g << 4) | (b << 8);
            }

            #define RGB16_SYMBOL_BITS 12u
            #include "Rgb16ByteOutput.cginc"
            ENDCG
        }
    }

    Fallback Off
}
