using UnityEngine;

namespace K13A.TSMP
{
    [AddComponentMenu("TSMP/Codecs/RGB16 Codec")]
    public sealed class TSMPCodecRGB16 : TSMPCodec
    {
        private const int SymbolModeRgb16 = 5;

        [Range(1, 8)] public int rBits = 4;
        [Range(1, 8)] public int gBits = 4;
        [Range(1, 4)] public int bBits = 4;
        public bool localRefine = true;
        [Range(1, 3)] public int refineRadius = 2;
        public Material direct444ByteDecodeMaterial;
        public Material refine444ByteDecodeMaterial;
        public Material variableByteDecodeMaterial;
        public Material variableRefineByteDecodeMaterial;
        public Material[] debugMaterials;
#if UDONSHARP
        private byte[] _rEncoderLevels;
        private byte[] _gEncoderLevels;
        private byte[] _bEncoderLevels;
        private Color32[] _rgb444EncoderColors;
        private bool _rgb444EncoderColorsValid;
        private int _cachedEncoderRBits = -1;
        private int _cachedEncoderGBits = -1;
        private int _cachedEncoderBBits = -1;
#endif

#if !COMPILER_UDONSHARP
        public override int SymbolMode => SymbolModeRgb16;
        public override int GetPayloadStartRow(int width, int blockSize)
        {
            int activeWidthBlocks = Mathf.Max(1, FrameCapacity.GetActiveWidthBlocks(width, blockSize));
            return Luma4Raster.PayloadStartRow + ((GetCalibrationSymbolCount() + activeWidthBlocks - 1) / activeWidthBlocks);
        }

        public override int GetPayloadCapacityBytes(int width, int height, int blockSize)
        {
            int activeWidthBlocks = FrameCapacity.GetActiveWidthBlocks(width, blockSize);
            int activeHeightBlocks = FrameCapacity.GetActiveHeightBlocks(height, blockSize);
            int payloadRows = Mathf.Max(0, activeHeightBlocks - GetPayloadStartRow(width, blockSize) - Luma4Raster.ReservedEndRows);
            return activeWidthBlocks * payloadRows * GetTotalBits() / 8;
        }

        public override int GetPayloadBlocksForBytes(int byteCount) => (byteCount * 8 + GetTotalBits() - 1) / GetTotalBits();
        private int GetTotalBits() => Mathf.Clamp(rBits, 1, 8) + Mathf.Clamp(gBits, 1, 8) + Mathf.Clamp(bBits, 1, 4);

        public override bool TryWriteFrame(Texture2D texture, int blockSize, byte[] headerBytes, byte[] payloadBytes, out string error)
        {
            if (!ValidateFrame(texture, blockSize, headerBytes, payloadBytes, out error))
                return false;

            Color32[] pixels = FrameRaster.CreateClearedPixels(texture.width, texture.height);
            Luma4Raster.WriteBaseRegions(pixels, texture.width, texture.height, blockSize, headerBytes);
            WriteCalibration(pixels, texture.width, texture.height, blockSize);
            WritePayload(pixels, texture.width, texture.height, blockSize, payloadBytes, payloadBytes.Length);
            FrameRaster.WriteEndMarker(pixels, texture.width, texture.height, blockSize);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return true;
        }

        public override byte[] GetCodecOptionBytes()
        {
            return new[]
            {
                (byte)Mathf.Clamp(rBits, 1, 8),
                (byte)Mathf.Clamp(gBits, 1, 8),
                (byte)Mathf.Clamp(bBits, 1, 4),
                (byte)(localRefine ? 1 : 0),
                (byte)Mathf.Clamp(refineRadius, 1, 3)
            };
        }

        public override int DecodeMaterialCount => 4;
        public override int DebugMaterialCount => debugMaterials != null ? debugMaterials.Length : 0;
        public override Material GetDebugMaterial(int index) => debugMaterials != null && index >= 0 && index < debugMaterials.Length ? debugMaterials[index] : null;

        public override Material GetDecodeMaterial(int index)
        {
            if (index == 0) return direct444ByteDecodeMaterial;
            if (index == 1) return refine444ByteDecodeMaterial;
            if (index == 2) return variableByteDecodeMaterial;
            if (index == 3) return variableRefineByteDecodeMaterial;
            return null;
        }

        public override void ConfigureMaterials(CodecMaterialContext context)
        {
            base.ConfigureMaterials(context);
            ConfigureRgb16Material(direct444ByteDecodeMaterial, context);
            ConfigureRgb16Material(refine444ByteDecodeMaterial, context);
            ConfigureRgb16Material(variableByteDecodeMaterial, context);
            ConfigureRgb16Material(variableRefineByteDecodeMaterial, context);
            ConfigureMaterialGroup(debugMaterials, context, false);
            if (debugMaterials != null)
            {
                for (int i = 0; i < debugMaterials.Length; i++)
                    ConfigureRgb16Material(debugMaterials[i], context);
            }
        }

        private void ConfigureRgb16Material(Material material, CodecMaterialContext context)
        {
            if (material == null)
                return;

            SetFloatIfPresent(material, "_Rgb16CalibrationStartBlock", Luma4Raster.PayloadStartRow * context.FrameLayout.ActiveWidthBlocks);
            if (material.HasProperty("_RBits")) material.SetFloat("_RBits", Mathf.Clamp(rBits, 1, 8));
            if (material.HasProperty("_GBits")) material.SetFloat("_GBits", Mathf.Clamp(gBits, 1, 8));
            if (material.HasProperty("_BBits")) material.SetFloat("_BBits", Mathf.Clamp(bBits, 1, 4));
            if (material.HasProperty("_RefineRadius")) material.SetFloat("_RefineRadius", Mathf.Clamp(refineRadius, 1, 3));
        }

        private bool ValidateFrame(Texture2D texture, int blockSize, byte[] headerBytes, byte[] payloadBytes, out string error)
        {
            return ValidateRasterFrame(texture, blockSize, headerBytes, payloadBytes, out error);
        }

        private void WriteCalibration(Color32[] pixels, int width, int height, int blockSize)
        {
            int blockCursor = Luma4Raster.PayloadStartRow * FrameCapacity.GetActiveWidthBlocks(width, blockSize);
            int r = Mathf.Clamp(rBits, 1, 8);
            int g = Mathf.Clamp(gBits, 1, 8);
            int b = Mathf.Clamp(bBits, 1, 4);

            for (int i = 0; i < 1 << r; i++, blockCursor++)
                FrameRaster.WriteColorBlockAtIndex(pixels, width, height, blockSize, blockCursor, RgbRedCalibrationColor(i, r));
            for (int i = 0; i < 1 << g; i++, blockCursor++)
                FrameRaster.WriteColorBlockAtIndex(pixels, width, height, blockSize, blockCursor, RgbGreenCalibrationColor(i, g));
            for (int i = 0; i < 1 << b; i++, blockCursor++)
                FrameRaster.WriteColorBlockAtIndex(pixels, width, height, blockSize, blockCursor, RgbBlueCalibrationColor(i, b));
        }

        private void WritePayload(Color32[] pixels, int width, int height, int blockSize, byte[] payloadBytes, int payloadByteCount)
        {
            int activeWidthBlocks = FrameCapacity.GetActiveWidthBlocks(width, blockSize);
            int payloadStartBlock = GetPayloadStartRow(width, blockSize) * activeWidthBlocks;
            int totalBits = GetTotalBits();
            int symbolCount = (payloadByteCount * 8 + totalBits - 1) / totalBits;
            int maxBlocks = GetPayloadBlocksForBytes(GetPayloadCapacityBytes(width, height, blockSize));
            int r = Mathf.Clamp(rBits, 1, 8);
            int g = Mathf.Clamp(gBits, 1, 8);
            int b = Mathf.Clamp(bBits, 1, 4);

            for (int i = 0; i < symbolCount && i < maxBlocks; i++)
            {
                uint symbol = FrameRaster.ReadBits(payloadBytes, payloadByteCount, i * totalBits, totalBits);
                FrameRaster.WriteColorBlockAtIndex(pixels, width, height, blockSize, payloadStartBlock + i, SymbolToRgbColor(symbol, r, g, b));
            }
        }
#endif

        public override void ApplyDecodeOptions()
        {
            int optionRBits = ReadCodecOptionByte(0, 4);
            int optionGBits = ReadCodecOptionByte(1, 4);
            int optionBBits = ReadCodecOptionByte(2, 4);
            bool optionLocalRefine = ReadCodecOptionFlag(3, false);
            int optionRefineRadius = ReadCodecOptionByte(4, 1);

            optionRBits = Mathf.Clamp(optionRBits, 1, 8);
            optionGBits = Mathf.Clamp(optionGBits, 1, 8);
            optionBBits = Mathf.Clamp(optionBBits, 1, 4);
            optionRefineRadius = Mathf.Clamp(optionRefineRadius, 1, 3);

            bool default444 = optionRBits == 4 && optionGBits == 4 && optionBBits == 4;
            if (default444)
                selectedDecodeMaterial = optionLocalRefine && refine444ByteDecodeMaterial != null ? refine444ByteDecodeMaterial : direct444ByteDecodeMaterial;
            else
                selectedDecodeMaterial = optionLocalRefine && variableRefineByteDecodeMaterial != null ? variableRefineByteDecodeMaterial : variableByteDecodeMaterial;

            int calSymbols = (1 << optionRBits) + (1 << optionGBits) + (1 << optionBBits);
            int totalBits = optionRBits + optionGBits + optionBBits;
            payloadStartRow = activeWidthBlocks > 0 ? 5 + ((calSymbols + activeWidthBlocks - 1) / activeWidthBlocks) : 5;
            payloadBlockCount = (byteCount * 8 + totalBits - 1) / totalBits;

            if (selectedDecodeMaterial != null)
            {
                selectedDecodeMaterial.SetFloat("_Rgb16CalibrationStartBlock", calibrationStartBlock);
                selectedDecodeMaterial.SetFloat("_RBits", optionRBits);
                selectedDecodeMaterial.SetFloat("_GBits", optionGBits);
                selectedDecodeMaterial.SetFloat("_BBits", optionBBits);
                selectedDecodeMaterial.SetFloat("_RefineRadius", optionRefineRadius);
            }
        }

#if UDONSHARP
        public override int GetEncoderSymbolMode()
        {
            return SymbolModeRgb16;
        }

        public override int GetEncoderPayloadStartRow(int width, int blockSize)
        {
            int activeWidthBlocks = Mathf.Max(1, GetEncoderActiveWidthBlocks(width, blockSize));
            return 5 + ((GetCalibrationSymbolCount() + activeWidthBlocks - 1) / activeWidthBlocks);
        }

        public override int GetEncoderPayloadCapacityBytes(int width, int height, int blockSize)
        {
            int activeWidthBlocks = GetEncoderActiveWidthBlocks(width, blockSize);
            int activeHeightBlocks = GetEncoderActiveHeightBlocks(height, blockSize);
            int payloadRows = Mathf.Max(0, activeHeightBlocks - GetEncoderPayloadStartRow(width, blockSize) - 1);
            return activeWidthBlocks * payloadRows * GetTotalBitsUdon() / 8;
        }

        public override int GetEncoderCodecOptionByteCount()
        {
            return 5;
        }

        public override int GetEncoderCodecOptionByte(int index)
        {
            if (index == 0)
                return Mathf.Clamp(rBits, 1, 8);
            if (index == 1)
                return Mathf.Clamp(gBits, 1, 8);
            if (index == 2)
                return Mathf.Clamp(bBits, 1, 4);
            if (index == 3)
                return localRefine ? 1 : 0;
            if (index == 4)
                return Mathf.Clamp(refineRadius, 1, 3);
            return 0;
        }

        public override bool WriteEncoderPayload(Color32[] pixels, int width, int height, int blockSize, byte[] payloadBytes, int payloadByteCount)
        {
            int activeWidthBlocks = GetEncoderActiveWidthBlocks(width, blockSize);
            int activeHeightBlocks = GetEncoderActiveHeightBlocks(height, blockSize);
            if (pixels == null || payloadBytes == null || activeWidthBlocks <= 0 || activeHeightBlocks <= 0)
                return false;

            int r = Mathf.Clamp(rBits, 1, 8);
            int g = Mathf.Clamp(gBits, 1, 8);
            int b = Mathf.Clamp(bBits, 1, 4);
            int rCount = 1 << r;
            int gCount = 1 << g;
            int bCount = 1 << b;
            int blockCursor = 5 * activeWidthBlocks;
            bool pixelsAreBlocks = encoderPixelsAreBlocks;
            EnsureEncoderLevels(r, g, b);

            if (pixelsAreBlocks)
            {
                if (r == 4 && g == 4 && b == 4)
                    return WriteEncoderPayloadBlocks444(pixels, activeWidthBlocks, activeHeightBlocks, payloadBytes, payloadByteCount, rCount, gCount, bCount, blockCursor);

                return WriteEncoderPayloadBlocks(pixels, activeWidthBlocks, activeHeightBlocks, payloadBytes, payloadByteCount, r, g, b, rCount, gCount, bCount, blockCursor);
            }

            for (int i = 0; i < rCount; i++, blockCursor++)
                WriteEncoderColorBlockFast(pixels, width, height, activeWidthBlocks, activeHeightBlocks, blockSize, blockCursor, new Color32(_rEncoderLevels[i], 128, 128, 255), pixelsAreBlocks);
            for (int i = 0; i < gCount; i++, blockCursor++)
                WriteEncoderColorBlockFast(pixels, width, height, activeWidthBlocks, activeHeightBlocks, blockSize, blockCursor, new Color32(128, _gEncoderLevels[i], 128, 255), pixelsAreBlocks);
            for (int i = 0; i < bCount; i++, blockCursor++)
                WriteEncoderColorBlockFast(pixels, width, height, activeWidthBlocks, activeHeightBlocks, blockSize, blockCursor, new Color32(128, 128, _bEncoderLevels[i], 255), pixelsAreBlocks);

            int payloadStartRow = 5 + ((rCount + gCount + bCount + activeWidthBlocks - 1) / activeWidthBlocks);
            int payloadStartBlock = payloadStartRow * activeWidthBlocks;
            int maxBlocks = activeWidthBlocks * Mathf.Max(0, activeHeightBlocks - payloadStartRow - 1);
            int totalBits = r + g + b;
            int symbolCount = (payloadByteCount * 8 + totalBits - 1) / totalBits;
            int rMask = rCount - 1;
            int gMask = gCount - 1;
            int bMask = bCount - 1;
            uint symbolMask = (1u << totalBits) - 1u;

            for (int i = 0; i < symbolCount && i < maxBlocks; i++)
            {
                int bitOffset = i * totalBits;
                int byteIndex = bitOffset >> 3;
                int bitShift = bitOffset - byteIndex * 8;
                uint bits = 0u;
                if (byteIndex < payloadByteCount)
                    bits = payloadBytes[byteIndex];
                int nextIndex = byteIndex + 1;
                if (nextIndex < payloadByteCount)
                    bits |= (uint)payloadBytes[nextIndex] << 8;
                nextIndex++;
                if (nextIndex < payloadByteCount)
                    bits |= (uint)payloadBytes[nextIndex] << 16;
                nextIndex++;
                if (nextIndex < payloadByteCount)
                    bits |= (uint)payloadBytes[nextIndex] << 24;

                uint symbol = (bits >> bitShift) & symbolMask;
                int rIndex = (int)(symbol & (uint)rMask);
                int gIndex = (int)((symbol >> r) & (uint)gMask);
                int bIndex = (int)((symbol >> (r + g)) & (uint)bMask);
                Color32 color = new Color32(_rEncoderLevels[rIndex], _gEncoderLevels[gIndex], _bEncoderLevels[bIndex], 255);
                WriteEncoderColorBlockFast(pixels, width, height, activeWidthBlocks, activeHeightBlocks, blockSize, payloadStartBlock + i, color, pixelsAreBlocks);
            }

            return true;
        }

        private bool WriteEncoderPayloadBlocks(Color32[] pixels, int activeWidthBlocks, int activeHeightBlocks, byte[] payloadBytes, int payloadByteCount, int r, int g, int b, int rCount, int gCount, int bCount, int blockCursor)
        {
            int blockX = blockCursor % activeWidthBlocks;
            int blockY = blockCursor / activeWidthBlocks;
            int pixelIndex = (activeHeightBlocks - 1 - blockY) * activeWidthBlocks + blockX;
            for (int i = 0; i < rCount; i++, blockCursor++)
            {
                if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                    pixels[pixelIndex] = new Color32(_rEncoderLevels[i], 128, 128, 255);
                blockX++;
                if (blockX < activeWidthBlocks)
                    pixelIndex++;
                else
                {
                    blockX = 0;
                    pixelIndex = pixelIndex - activeWidthBlocks * 2 + 1;
                }
            }

            for (int i = 0; i < gCount; i++, blockCursor++)
            {
                if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                    pixels[pixelIndex] = new Color32(128, _gEncoderLevels[i], 128, 255);
                blockX++;
                if (blockX < activeWidthBlocks)
                    pixelIndex++;
                else
                {
                    blockX = 0;
                    pixelIndex = pixelIndex - activeWidthBlocks * 2 + 1;
                }
            }

            for (int i = 0; i < bCount; i++, blockCursor++)
            {
                if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                    pixels[pixelIndex] = new Color32(128, 128, _bEncoderLevels[i], 255);
                blockX++;
                if (blockX < activeWidthBlocks)
                    pixelIndex++;
                else
                {
                    blockX = 0;
                    pixelIndex = pixelIndex - activeWidthBlocks * 2 + 1;
                }
            }

            int payloadStartRow = 5 + ((rCount + gCount + bCount + activeWidthBlocks - 1) / activeWidthBlocks);
            int maxBlocks = activeWidthBlocks * Mathf.Max(0, activeHeightBlocks - payloadStartRow - 1);
            int totalBits = r + g + b;
            int symbolCount = (payloadByteCount * 8 + totalBits - 1) / totalBits;
            int rMask = rCount - 1;
            int gMask = gCount - 1;
            int bMask = bCount - 1;
            uint symbolMask = (1u << totalBits) - 1u;
            blockX = 0;
            pixelIndex = (activeHeightBlocks - 1 - payloadStartRow) * activeWidthBlocks;

            for (int i = 0; i < symbolCount && i < maxBlocks; i++)
            {
                int bitOffset = i * totalBits;
                int byteIndex = bitOffset >> 3;
                int bitShift = bitOffset - byteIndex * 8;
                uint bits = 0u;
                if (byteIndex < payloadByteCount)
                    bits = payloadBytes[byteIndex];
                int nextIndex = byteIndex + 1;
                if (nextIndex < payloadByteCount)
                    bits |= (uint)payloadBytes[nextIndex] << 8;
                nextIndex++;
                if (nextIndex < payloadByteCount)
                    bits |= (uint)payloadBytes[nextIndex] << 16;
                nextIndex++;
                if (nextIndex < payloadByteCount)
                    bits |= (uint)payloadBytes[nextIndex] << 24;

                uint symbol = (bits >> bitShift) & symbolMask;
                int rIndex = (int)(symbol & (uint)rMask);
                int gIndex = (int)((symbol >> r) & (uint)gMask);
                int bIndex = (int)((symbol >> (r + g)) & (uint)bMask);
                if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                    pixels[pixelIndex] = new Color32(_rEncoderLevels[rIndex], _gEncoderLevels[gIndex], _bEncoderLevels[bIndex], 255);
                blockX++;
                if (blockX < activeWidthBlocks)
                    pixelIndex++;
                else
                {
                    blockX = 0;
                    pixelIndex = pixelIndex - activeWidthBlocks * 2 + 1;
                }
            }

            return true;
        }

        private bool WriteEncoderPayloadBlocks444(Color32[] pixels, int activeWidthBlocks, int activeHeightBlocks, byte[] payloadBytes, int payloadByteCount, int rCount, int gCount, int bCount, int blockCursor)
        {
            EnsureRgb444EncoderColors();

            int blockX = blockCursor % activeWidthBlocks;
            int blockY = blockCursor / activeWidthBlocks;
            int pixelIndex = (activeHeightBlocks - 1 - blockY) * activeWidthBlocks + blockX;

            for (int i = 0; i < rCount; i++, blockCursor++)
            {
                if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                    pixels[pixelIndex] = new Color32(_rEncoderLevels[i], 128, 128, 255);
                blockX++;
                if (blockX < activeWidthBlocks)
                    pixelIndex++;
                else
                {
                    blockX = 0;
                    pixelIndex = pixelIndex - activeWidthBlocks * 2 + 1;
                }
            }

            for (int i = 0; i < gCount; i++, blockCursor++)
            {
                if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                    pixels[pixelIndex] = new Color32(128, _gEncoderLevels[i], 128, 255);
                blockX++;
                if (blockX < activeWidthBlocks)
                    pixelIndex++;
                else
                {
                    blockX = 0;
                    pixelIndex = pixelIndex - activeWidthBlocks * 2 + 1;
                }
            }

            for (int i = 0; i < bCount; i++, blockCursor++)
            {
                if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                    pixels[pixelIndex] = new Color32(128, 128, _bEncoderLevels[i], 255);
                blockX++;
                if (blockX < activeWidthBlocks)
                    pixelIndex++;
                else
                {
                    blockX = 0;
                    pixelIndex = pixelIndex - activeWidthBlocks * 2 + 1;
                }
            }

            int payloadStartRow = 5 + ((rCount + gCount + bCount + activeWidthBlocks - 1) / activeWidthBlocks);
            int maxBlocks = activeWidthBlocks * Mathf.Max(0, activeHeightBlocks - payloadStartRow - 1);
            int symbolCount = (payloadByteCount * 8 + 11) / 12;
            if (symbolCount > maxBlocks)
                symbolCount = maxBlocks;

            blockX = 0;
            pixelIndex = (activeHeightBlocks - 1 - payloadStartRow) * activeWidthBlocks;

            int written = 0;
            int byteIndex = 0;
            while (written + 1 < symbolCount)
            {
                int byte0 = byteIndex < payloadByteCount ? payloadBytes[byteIndex] : 0;
                int byte1Index = byteIndex + 1;
                int byte1 = byte1Index < payloadByteCount ? payloadBytes[byte1Index] : 0;
                int byte2Index = byteIndex + 2;
                int byte2 = byte2Index < payloadByteCount ? payloadBytes[byte2Index] : 0;
                int packed = byte0 | (byte1 << 8) | (byte2 << 16);

                if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                    pixels[pixelIndex] = _rgb444EncoderColors[packed & 0x0FFF];
                blockX++;
                if (blockX < activeWidthBlocks)
                    pixelIndex++;
                else
                {
                    blockX = 0;
                    pixelIndex = pixelIndex - activeWidthBlocks * 2 + 1;
                }

                if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                    pixels[pixelIndex] = _rgb444EncoderColors[(packed >> 12) & 0x0FFF];
                blockX++;
                if (blockX < activeWidthBlocks)
                    pixelIndex++;
                else
                {
                    blockX = 0;
                    pixelIndex = pixelIndex - activeWidthBlocks * 2 + 1;
                }

                byteIndex += 3;
                written += 2;
            }

            if (written < symbolCount)
            {
                int byte0 = byteIndex < payloadByteCount ? payloadBytes[byteIndex] : 0;
                int byte1Index = byteIndex + 1;
                int byte1 = byte1Index < payloadByteCount ? payloadBytes[byte1Index] : 0;
                int symbol = (byte0 | (byte1 << 8)) & 0x0FFF;
                if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                    pixels[pixelIndex] = _rgb444EncoderColors[symbol];
            }

            return true;
        }

        private void EnsureEncoderLevels(int r, int g, int b)
        {
            if (_rEncoderLevels == null || _rEncoderLevels.Length != 256)
                _rEncoderLevels = new byte[256];
            if (_gEncoderLevels == null || _gEncoderLevels.Length != 256)
                _gEncoderLevels = new byte[256];
            if (_bEncoderLevels == null || _bEncoderLevels.Length != 16)
                _bEncoderLevels = new byte[16];

            if (_cachedEncoderRBits != r)
            {
                int max = (1 << r) - 1;
                for (int i = 0; i <= max; i++)
                    _rEncoderLevels[i] = QuantizeColorLevel(i, max);
                _cachedEncoderRBits = r;
                _rgb444EncoderColorsValid = false;
            }

            if (_cachedEncoderGBits != g)
            {
                int max = (1 << g) - 1;
                for (int i = 0; i <= max; i++)
                    _gEncoderLevels[i] = QuantizeColorLevel(i, max);
                _cachedEncoderGBits = g;
                _rgb444EncoderColorsValid = false;
            }

            if (_cachedEncoderBBits != b)
            {
                int max = (1 << b) - 1;
                for (int i = 0; i <= max; i++)
                    _bEncoderLevels[i] = QuantizeColorLevel(i, max);
                _cachedEncoderBBits = b;
                _rgb444EncoderColorsValid = false;
            }
        }

        private void EnsureRgb444EncoderColors()
        {
            if (_rgb444EncoderColors == null || _rgb444EncoderColors.Length != 4096)
            {
                _rgb444EncoderColors = new Color32[4096];
                _rgb444EncoderColorsValid = false;
            }

            if (_cachedEncoderRBits != 4 || _cachedEncoderGBits != 4 || _cachedEncoderBBits != 4)
                EnsureEncoderLevels(4, 4, 4);

            if (_rgb444EncoderColorsValid)
                return;

            for (int i = 0; i < 4096; i++)
            {
                int rIndex = i & 15;
                int gIndex = (i >> 4) & 15;
                int bIndex = (i >> 8) & 15;
                _rgb444EncoderColors[i] = new Color32(_rEncoderLevels[rIndex], _gEncoderLevels[gIndex], _bEncoderLevels[bIndex], 255);
            }

            _rgb444EncoderColorsValid = true;
        }

        private static void WriteEncoderColorBlockFast(Color32[] pixels, int width, int height, int activeWidthBlocks, int activeHeightBlocks, int blockSize, int blockIndex, Color32 color, bool pixelsAreBlocks)
        {
            int blockX = blockIndex % activeWidthBlocks;
            int blockY = blockIndex / activeWidthBlocks;
            if (blockX < 0 || blockX >= activeWidthBlocks || blockY < 0 || blockY >= activeHeightBlocks)
                return;

            if (pixelsAreBlocks)
            {
                int pixelIndex = (activeHeightBlocks - 1 - blockY) * activeWidthBlocks + blockX;
                if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                    pixels[pixelIndex] = color;
                return;
            }

            int startX = blockX * blockSize;
            int topY = blockY * blockSize;
            int endX = startX + blockSize;
            if (endX > width)
                endX = width;

            for (int y = 0; y < blockSize; y++)
            {
                int pixelY = height - 1 - (topY + y);
                if (pixelY < 0 || pixelY >= height)
                    continue;

                int rowOffset = pixelY * width;
                for (int x = startX; x < endX; x++)
                    pixels[rowOffset + x] = color;
            }
        }

        private int GetTotalBitsUdon()
        {
            return Mathf.Clamp(rBits, 1, 8) + Mathf.Clamp(gBits, 1, 8) + Mathf.Clamp(bBits, 1, 4);
        }

#endif

        private int GetCalibrationSymbolCount()
        {
            return (1 << Mathf.Clamp(rBits, 1, 8)) + (1 << Mathf.Clamp(gBits, 1, 8)) + (1 << Mathf.Clamp(bBits, 1, 4));
        }

        private static Color32 SymbolToRgbColor(uint symbol, int rBits, int gBits, int bBits)
        {
            int rMask = (1 << rBits) - 1;
            int gMask = (1 << gBits) - 1;
            int bMask = (1 << bBits) - 1;
            int rIndex = (int)(symbol & (uint)rMask);
            int gIndex = (int)((symbol >> rBits) & (uint)gMask);
            int bIndex = (int)((symbol >> (rBits + gBits)) & (uint)bMask);
            return new Color32(QuantizeColorLevel(rIndex, rMask), QuantizeColorLevel(gIndex, gMask), QuantizeColorLevel(bIndex, bMask), 255);
        }

        private static Color32 RgbRedCalibrationColor(int index, int bits)
        {
            return new Color32(QuantizeColorLevel(index, (1 << bits) - 1), 128, 128, 255);
        }

        private static Color32 RgbGreenCalibrationColor(int index, int bits)
        {
            return new Color32(128, QuantizeColorLevel(index, (1 << bits) - 1), 128, 255);
        }

        private static Color32 RgbBlueCalibrationColor(int index, int bits)
        {
            return new Color32(128, 128, QuantizeColorLevel(index, (1 << bits) - 1), 255);
        }

        private static byte QuantizeColorLevel(int index, int maxIndex)
        {
            if (maxIndex <= 0)
                return 128;

            return (byte)(24 + (index * 208 + maxIndex / 2) / maxIndex);
        }
    }
}
