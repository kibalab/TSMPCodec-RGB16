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

The timing measurement wraps 32 blits in a uniquely named GPU marker and waits for GPU completion after each baseline/candidate batch. It alternates order, warms up for 10 frames, and records 15 frames. GPU medians are divided by 32; readbacks are outside the GPU markers. The additional wall-clock measurement includes submission, completion and readback overhead, amortized over the batch. Neither metric is a whole-frame VRChat measurement.

This supersedes the initial single-draw timing method, which sometimes attributed work to the wrong marker or returned zero. Performance claims below use only completion-separated batches.

### Verified Result

Unity 2022.3.22f1, NVIDIA GeForce RTX 4090, Direct3D11: all 972 output cases passed. The 99 existing eight-bit-channel round-trip failures produced exactly the same output before and after the change.

Representative 56-byte, radius-2 results, isolated at commit `978752f` before G03:

| Decoder | Sample size | Before (us) | After (us) |
| --- | --- | ---: | ---: |
| Fixed 4/4/4 Refine | 1 | 47.200 | 15.808 |
| Fixed 4/4/4 Refine | 4 | 188.800 | 62.496 |
| Variable 5/6/4 Refine | 1 | 82.912 | 27.712 |
| Variable 5/6/4 Refine | 4 | 397.496 | 122.848 |

All measured 4/56/1027-byte cases improved in this run. GPU clocks were not locked; another editor was open. These measurements do not establish the same gain on other GPUs, APIs, or actual compressed video streams.

For isolated historical comparisons, materialize the shader files from the candidate commit in a test-project asset folder with unique shader names and set `TSMP_GPU_CANDIDATE_ASSET_PATH` to that folder. Otherwise the runner uses the current checkout, so a G02 comparison also includes any later G03 changes.

## Variable Symbol Reuse (G03)

Use `-Mode G03` to compare against `SymbolBaselines`, captured after G02 at `d928e8f`. Both Variable and Variable Refine are tested at 1/1/1, 1/1/4, 2/3/3, 4/4/4, 5/6/4, 6/6/4 and 8/8/4. All 3024 complete output comparisons passed on the same Unity/GPU/API.

For totals of at least eight bits, the new fragment packs up to five decoded symbols into one RGBA pixel. Smaller totals retain the original byte decoder. Its existing inability to gather more than two symbols into a byte, and the existing eight-bit-channel quantization collisions, remain outside this performance change. The suite reports 252 such existing round-trip failures but requires exact baseline equivalence in every case.

Representative 5/6/4, 56-byte results:

| Decoder | Sample size | Before (us) | After (us) |
| --- | --- | ---: | ---: |
| Variable | 1 | 5.984 | 2.944 |
| Variable | 4 | 108.544 | 54.008 |
| Variable Refine | 1 | 27.712 | 14.208 |
| Variable Refine | 4 | 123.360 | 61.184 |

All measured 4/56/1027-byte cases improved with the final direct-packing implementation. Dynamic-loop and last-symbol-cache prototypes were rejected; only the measured final implementation is included.
