[한국어](README.ko.md) | **English** | [日本語](README.md)

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
- `com.kibalab.tsmp.core` 0.3.0-beta.2 or newer
- Unity 2022.3
- VRChat Worlds SDK 3.9.0 or newer only for VRChat; not required in ordinary Unity

## Installation

Add the VPM repository in VRChat Creator Companion.

```text
https://vpm.kiba.red/
```

Then install `TSMP Core` and `TSMP Codec RGB16`.

For ordinary Unity, install Core 0.3.0-beta.2, the default Luma4 codec and this codec through Unity Package Manager. For a local checkout, use Add package from disk on each package.json; VRCSDK is not required. UPM uses an exact Core 0.3.0-beta.2 dependency; VPM accepts Core 0.3.0-beta.2 or newer.

## Usage

1. Add `Packages/com.kibalab.tsmp.core/Samples/TSMPController.prefab` from the Core package to your scene.
2. Open the Codec tab in `TSMPSetup` and select `RGB16` from the automatically discovered codecs.
3. Setup prepares the codec and its materials automatically in both ordinary Unity and VRChat. No conversion menu is needed.

## Release Status

This package is currently beta and uses `v0.0.x-beta.x` tags.

## License

MIT License. Copyright (c) 2026 KIBA_Labs.

## Preparation API compatibility

This release requires the preparation API introduced in Core 0.3.0-beta.2. Core 0.2.0 and 0.3.0-beta.1 lack `PrepareDecode` and cannot compile this codec, even when the calibration material is unassigned. Update Core before installing this codec. A missing preparation material only selects the original shader path after compilation.

UPM uses a version string, while VPM uses a version range. Local/disk or Git installs must supply a compatible Core directly in the project's dependencies; package metadata does not tell UPM to fetch Core from GitHub. For VPM betas, enable pre-release packages and select the matching versions after publication.
