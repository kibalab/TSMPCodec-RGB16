# RGB16 0.0.3-beta.2 Validation

Date: 2026-09-12. Unity 2022.3.22f1, Core 0.2.0, Luma4 0.0.3, Worlds SDK 3.10.4-beta.2 with bundled UdonSharp, RTX 4090 / Direct3D11.

Evidence directory: F:/Unity/TSMP/Validation-Results/s07.

| Check | Result | Log / result prefix |
| --- | --- | --- |
| SDK-free fresh import, Setup, all four shader modes, native GPU loopback | Pass | 20260912-193448-RGB16-Play |
| Windows x64 Development Mono build, stripping disabled | Succeeded, zero errors, eight shader warnings | 20260912-193719-RGB16-Build |
| Actual Player, nine frames of Transform/Humanoid/Unicode/RPC loopback | Pass | 20260912-194206-RGB16-Player |
| Full Udon client compile, automatic backing, all four modes in real Udon VM, block/full-resolution output parity and GPU decoding | Pass | 20260912-194456-RGB16-Udon |

Player: F:/Unity/TSMP/Validation-Codecs-NoSDK/Build/RGB16/CodecValidation.exe. The adjacent .build-report.txt records the build result. Default and 5/6/4 channel allocation were each tested with refinement disabled/enabled; all 1,027 payload bytes matched the GPU output.

Initial harness runs were corrected for a missing test import, SDK sample scripts requiring another Unity import, edit-time codec discovery and VM cleanup initialization. A Player run during the SDK project's cold import timed out on GPU readback; a subsequent isolated run passed. Those earlier logs remain in the evidence directory.

Shader warnings concern existing sample/classifier initialization analysis and signed integer divisions; codec algorithms were not changed. No SDK world upload, IL2CPP, Quest or lossy transport test was performed. The Udon VM test is not a claim of an uploaded VRChat client test.
