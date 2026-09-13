# Changelog

## 0.0.3-beta.3 (Unreleased)

- Require Core 0.3.0-beta.2 in UPM and >=0.3.0-beta.2 in VPM because the codec now calls the preparation API. Core 0.2.0 and 0.3.0-beta.1 do not provide that API.
- Use Float32 calibration preparation for multisample direct decoding, including variable channel bits; retain the original single-sample and Refine paths.
- Include the preparation shader/material on the codec prefab. Missing preparation resources retain ordinary decoding with a compatible Core.
- Preserve codec IDs, packet layout and existing script/material/prefab GUIDs. Keep VRChat SDK requirements in VPM only.
- Release this candidate only after the matching Core is published and the packaged minimum-version combination is validated.

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
