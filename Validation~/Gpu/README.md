# GPU Decode Regression

Run this suite in a dedicated Unity 2022.3 project that references this checkout and the actual Core and Luma4 packages. Close that project before invoking Unity. The runner installs its test script under `Assets/GpuShaderValidation` and replaces the current test scene. Do not use a working scene project.

```powershell
./Validation~/Gpu/Run-Validation.ps1 -UnityPath 'C:/Program Files/Unity/Hub/Editor/2022.3.22f1/Editor/Unity.exe' -ProjectPath F:/Unity/TSMP/Validation-Codecs-NoSDK -ResultsDirectory F:/Unity/TSMP/Validation-Results/gpu -Mode G02
```

The test requires a graphics device and uses D3D11, not `-nographics`. It compiles the real package shaders and compares complete RGBA8 GPU outputs with frozen baseline shaders. Baselines are text files outside Unity import and are materialized only inside the test project.

## Refinement Candidate Cache (G02)

`Baselines` contains the two pre-optimization shaders from commit `aa1d4ce`. The fixture uses the native codec writer at 1280 x 720 with 8-pixel blocks, a linear source texture, and a linear 4096 x 1 RGBA8 output texture. It covers:

- Fixed 4/4/4 and variable 5/6/4 and 8/8/4 channel bits.
- Refinement radii 1, 2 and 3; sample sizes 1, 4 and 8.
- Payload prefixes of 0, 1, 2, 3, 4, 5, 7, 16, 31, 56, 257 and 1027 bytes.
- Exact raster, deterministic channel distortion with noise, and flat calibration/tie input.
- Complete byte-for-byte baseline equivalence and zeroed bytes beyond the payload.

The eight-bit channel fixture is an equivalence test, not a lossless round-trip assertion: the existing encoder maps 256 levels into only 209 integer color values (24 through 232). Its non-injective quantization is deliberately unchanged.

The timing measurement wraps one GPU blit in each command-buffer GPU marker. It alternates baseline/candidate order, warms up for 20 frames, and records 45 frames with one sample block per marker. Readbacks are outside the timing region. Medians are GPU microseconds, not whole-frame CPU or VRChat timings.

### Verified Result

Unity 2022.3.22f1, NVIDIA GeForce RTX 4090, Direct3D11: all 972 output cases passed. The 99 existing eight-bit-channel round-trip failures produced exactly the same output before and after the change.

Representative 56-byte, radius-2 results:

| Decoder | Sample size | Before (us) | After (us) |
| --- | --- | ---: | ---: |
| Fixed 4/4/4 Refine | 1 | 118.784 | 69.632 |
| Fixed 4/4/4 Refine | 4 | 829.440 | 345.856 |
| Variable 5/6/4 Refine | 1 | 257.024 | 149.504 |
| Variable 5/6/4 Refine | 4 | 1730.560 | 835.584 |

All measured 4/56/1027-byte cases improved in this run. GPU clocks were not locked; another editor was open. These measurements do not establish the same gain on other GPUs, APIs, or actual compressed video streams.
