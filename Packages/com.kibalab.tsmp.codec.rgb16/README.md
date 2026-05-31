# TSMP Codec RGB16

RGB16 TSMP codec, Udon handler, shaders, materials, and prefab.

## Requirements

- `com.kibalab.tsmp.core` 0.0.1 or newer
- VRChat Worlds SDK 3.9.0 or newer

## Usage

Import this package with TSMP Core. `TSMPSetup` discovers `Runtime/TSMPCodecCatalog.asset` in the editor and adds the RGB16 codec prefab to the codec list automatically.

RGB16 supports fixed and variable channel-bit decode paths for denser payload frames than Luma4.
