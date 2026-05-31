[한국어](README.md) | **English** | [日本語](README.ja.md)

# TSMP Codec RGB16

RGB16 records TSMP symbols through RGB channels at higher density than Luma4. It can carry more payload, but it depends more on the color fidelity of the capture and broadcast path.

## Characteristics

- RGB-based 16-bit TSMP symbols
- Higher data density than Luma4
- RGB16 and variable channel-bit decode paths
- Best for stream paths that preserve sharp color values
- Automatically discovered in the `TSMPSetup` Codec tab

## Requirements

- TSMP Core: https://github.com/kibalab/TSMP-Core
- `com.kibalab.tsmp.core` 0.0.1 or newer
- VRChat Worlds SDK 3.9.0 or newer

## Installation

Add the VPM repository in VRChat Creator Companion.

```text
https://vpm.kiba.red/
```

Then install `TSMP Core` and `TSMP Codec RGB16`.

## Usage

1. Add `Packages/com.kibalab.tsmp.core/Samples/TSMPController.prefab` from the Core package to your scene.
2. Open the Codec tab in `TSMPSetup` and click `Refresh Codecs`.
3. Select `RGB16`.
4. Click `Apply Setup`.

## Release Status

This package is currently beta and uses `v0.0.x-beta.x` tags.

## License

MIT License. Copyright (c) 2026 KIBA_Labs.
