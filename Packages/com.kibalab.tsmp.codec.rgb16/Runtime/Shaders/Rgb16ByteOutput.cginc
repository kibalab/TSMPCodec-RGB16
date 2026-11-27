float4 frag(v2f i) : SV_Target
{
#if defined(TSMP_COMBINED_BYTE_OUTPUT)
    int baseByte;
    float4 prefix;
    if (TSMPReadOutputPrefix(i.uv, baseByte, prefix))
        return prefix;
#else
    int2 size = int2(_OutputWidth, _OutputHeight);
    int2 pixel = clamp((int2)floor(i.uv * size), int2(0, 0), size - 1);
    int baseByte = (pixel.y * size.x + pixel.x) * 4;
#endif
    if (baseByte >= (int)_ByteCount)
        return 0;

    uint bits = RGB16_SYMBOL_BITS;
    uint firstBit = (uint)baseByte * 8u;
    uint symbolIndex = firstBit / bits;
    int shift = -(int)(firstBit - symbolIndex * bits);
    int required = min(4, (int)_ByteCount - baseByte) * 8;
    uint packed = 0u;
    [loop]
    for (; shift < required; shift += (int)bits)
    {
        uint symbol = (uint)DecodeRgb16Symbol(symbolIndex++);
        packed |= shift < 0 ? symbol >> -shift : symbol << shift;
    }
    if (required < 32)
        packed &= (1u << required) - 1u;
    return float4(packed & 255u, (packed >> 8) & 255u,
                  (packed >> 16) & 255u, packed >> 24) / 255.0;
}
