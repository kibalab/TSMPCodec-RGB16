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
- `com.kibalab.tsmp.core` 1.0.0 or newer
- Unity 2022.3
- VRChat Worlds SDK 3.9.0 or newer only for VRChat; not required in ordinary Unity

## Installation

Add the VPM repository in VRChat Creator Companion.

```text
https://vpm.kiba.red/
```

Then install `TSMP Core` and `TSMP Codec RGB16`.

For ordinary Unity, install Core 1.0.0, the default Luma4 codec and this codec through Unity Package Manager. For a local checkout, use Add package from disk on each package.json; VRCSDK is not required. UPM uses an exact Core 1.0.0 dependency; VPM accepts Core 1.0.0 or newer.

## Usage

1. Add `Packages/com.kibalab.tsmp.core/Samples/TSMPController.prefab` from the Core package to your scene.
2. Open the Codec tab in `TSMPSetup` and select `RGB16` from the automatically discovered codecs.
3. Setup prepares the codec and its materials automatically in both ordinary Unity and VRChat. No conversion menu is needed.

## Release Status

RGB16 2.0.0 is a stable release consolidating all beta changes since 1.0.0. Install Core 1.0.0 first. VCC does not require prerelease packages for these versions.

## License

MIT License. Copyright (c) 2026 KIBA_Labs.

## Preparation API compatibility

This release requires Core 1.0.0 and its codec preparation/output APIs. Update Core before installing this codec. Missing preparation materials can select the legacy shader path, but cannot compensate for an incompatible Core API.

UPM uses Core 1.0.0; VPM accepts Core >=1.0.0. Local/disk or Git installs must supply Core directly in the project's dependencies; package metadata does not tell UPM to fetch Core from GitHub. VRChat Worlds SDK remains a VPM-only dependency.
