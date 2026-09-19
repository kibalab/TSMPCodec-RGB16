# Changelog

## 2.0.0

Stable promotion of all 0.0.3-beta.1 through 0.0.3-beta.4 changes since 1.0.0. No runtime/shader changes from 0.0.3-beta.4.

- Support ordinary Unity through UPM without VRCSDK/UdonSharp; keep Worlds SDK requirements in VPM only.
- Use Core's shared automatic Controller workflow and an SDK-neutral codec prefab. Detect installed Worlds packages through assembly version defines, with no manual define or conversion menu.
- Resolve shared shader includes through Packages/com.kibalab.tsmp.core for installed and local packages.
- Regenerate bundled Udon program/field metadata for the updated Core API without profiler instrumentation.
- Cache refinement channel candidates instead of resampling calibration during candidate combinations.
- Reuse variable-bit decoded symbols across output bytes when symbol totals are at least eight bits. Keep the original path below eight bits and preserve existing quantization limitations.
- Prepare Float32 calibration for multisample direct decoding, including variable channel bits. Retain the original single-sample and Refine paths where the measured optimization did not help.
- Bundle the preparation shader/material and retain ordinary decoding if preparation resources are unavailable with a compatible Core.
- Write decoded header and payload bytes directly to Core's combined readback texture, removing a payload intermediate and packing draw while preserving the legacy path.
- Require Core 1.0.0 in UPM and >=1.0.0 in VPM. Keep Worlds SDK >=3.9.0 VPM-only; ordinary Unity does not require VRCSDK.
- Preserve codec IDs, protocol bytes, quantization behavior and existing asset GUIDs.
- Update Japanese, English and Korean READMEs for stable installation, automatic setup and API compatibility; retain the beta history below.
- Keep the historical 1.0.0 release intact; 2.0.0 adopts the current Core API baseline.
- See [2.0.0 release notes](https://github.com/kibalab/TSMPCodec-RGB16/releases/tag/v2.0.0) for the cumulative changes, validation evidence and remaining limits.

## 0.0.3-beta.4

- Support Core's optional combined decoder output: write the decoded header prefix and payload into the readback texture directly, removing a payload intermediate and packing draw.
- Retain the legacy shader path and preserve codec IDs, quantization behavior, protocol bytes and asset GUIDs.
- Target Core 0.3.0-beta.3 through UPM and >=0.3.0-beta.3 through VPM. SDK dependencies remain VPM-only.
- Verified byte parity, codec switching, payload growth/shrink and fallback in native Player and compiled Udon VM tests. No isolated GPU-time or frame-loss improvement is claimed for this codec.
- See the matching Core release notes for the complete pipeline measurements and validation limits.

## 0.0.3-beta.3

- Require Core 0.3.0-beta.2 in UPM and >=0.3.0-beta.2 in VPM because the codec now calls the preparation API. Core 0.2.0 and 0.3.0-beta.1 do not provide that API.
- Use Float32 calibration preparation for multisample direct decoding, including variable channel bits; retain the original single-sample and Refine paths.
- Cache refinement channel candidates to avoid repeated calibration samples during candidate combinations.
- Reuse decoded variable-bit symbols across output bytes for symbol totals of at least eight bits. Preserve the original path below eight bits and existing quantization limitations.
- Include the preparation shader/material on the codec prefab. Missing preparation resources retain ordinary decoding with a compatible Core.
- Preserve codec IDs, packet layout and existing script/material/prefab GUIDs. Keep VRChat SDK requirements in VPM only.
- Update Core before installing this codec. Enable prerelease packages in VCC to select the matching beta versions.

## 0.0.3-beta.2

- Support ordinary Unity without a VRChat SDK dependency through UPM; retain Worlds SDK requirements for VPM.
- Require Core 0.2.0 and use its shared automatic Controller workflow with an SDK-neutral codec prefab.
- Detect installed Worlds packages with an assembly version define so the Udon encoder path is available without manual scripting defines.
- Resolve shared shader includes through Packages/com.kibalab.tsmp.core for local and installed packages.
- Preserve codec IDs, settings, script/material/prefab GUIDs and the encoded wire format.
- Regenerate bundled Udon bytecode and field metadata against Core 0.2.0 without profiler instrumentation.

## 0.0.3-beta.1

- Beta release metadata for VPM distribution.
- Includes RGB16 codec runtime, shaders, materials, prefab, and sample.
