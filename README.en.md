[한국어](README.md) | **English** | [日本語](README.ja.md)

# TSMP Codec RGB16

RGB16 codec package for TSMP.

This package is used with TSMP Core and provides the RGB16 codec handler, decode shaders, materials, prefab, and codec catalog asset discovered by the Codec tab in `TSMPSetup`.

## Installation

Add the VPM repository in VRChat Creator Companion.

```text
https://vpm.kiba.red/
```

Then install `TSMP Codec RGB16`.

## Requirements

- `com.kibalab.tsmp.core` 0.0.1 or newer
- VRChat Worlds SDK 3.9.0 or newer

## Usage

1. Add the TSMP Core `TSMPController.prefab` or an equivalent TSMP setup to the scene.
2. Open the Codec tab on `TSMPSetup`.
3. Press `Refresh Codecs`.
4. Confirm that `RGB16` appears and select it.
5. Run `Apply Setup`.

RGB16 can carry denser frames than Luma4. Use it when the capture and receive path is stable enough and the scene needs more payload capacity.

## Release

This repository is configured so pushing a version tag creates release artifacts and registers the package with the VPM backend.

The tag must match the `version` in `package.json`.

Example:

```bash
git tag v1.0.0
git push origin v1.0.0
```
