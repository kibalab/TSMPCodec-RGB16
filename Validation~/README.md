# SDK-Optional Codec Validation

This harness checks the actual package against released Core 0.2.0 and Luma4 0.0.3. It is excluded from the Unity package by the Validation~ directory.

## Setup

Use separate Unity 2022.3.22f1 projects for ordinary Unity and VRChat. Reference the Core, Luma4 and codec package directories with local file dependencies. Do not copy or modify an alternative codec implementation inside the validation projects. The ordinary Unity project must not contain VRCSDK, UdonSharp or Udon scripting defines.

The VRChat project must already have Worlds SDK 3.9.0 or newer configured. This validation used 3.10.4-beta.2 with its bundled UdonSharp. A Core repository checkout at tag v0.2.0 provides the existing native Transform/Humanoid/variable/RPC loopback harness.

## Run

From this repository, run these steps sequentially after Unity import has settled:

```powershell
$unity = 'C:/Program Files/Unity/Hub/Editor/2022.3.22f1/Editor/Unity.exe'
$core = 'F:/Unity/TSMP/TSMP-Core-release'
$native = 'F:/Unity/TSMP/Validation-Codecs-NoSDK'
$vrc = 'F:/Unity/TSMP/Validation-Codecs-VRC'
$results = 'F:/Unity/TSMP/Validation-Results/s07'
& './Validation~/Run-Validation.ps1' -UnityEditor $unity -CoreRepository $core -Project $native -Results $results -Step Play
& './Validation~/Run-Validation.ps1' -UnityEditor $unity -CoreRepository $core -Project $native -Results $results -Step Build
& './Validation~/Run-Validation.ps1' -UnityEditor $unity -CoreRepository $core -Project $native -Results $results -Step Player
& './Validation~/Run-Validation.ps1' -UnityEditor $unity -CoreRepository $core -Project $vrc -Results $results -Step Udon
```

The runner copies only test scripts into Assets/Validation. It sets TSMP_VALIDATION_CODEC from this repository's package manifest. Play and Build require the native project; Udon requires the separate Worlds project. The runner does not install an SDK or alter production scenes.

Play checks automatic codec discovery, an SDK-neutral prefab, shader compilation, native raster output and exact GPU decoding of 1,027 bytes, then runs the existing Core texture loopback. RGB16 covers 4/4/4 and 5/6/4, each with refinement off/on. Color256 covers direct, robust and robust-refine modes. RGB20 covers its fixed mode.

Build creates a Windows x64 Development Mono Player with stripping disabled. Player executes the built Transform, animated humanoid rig, Unicode variable and RPC texture loopback. It uses a real graphics device, without -nographics.

Udon compiles all installed UdonSharp client programs, checks Core's automatic backing-component preparation and runs the codec's real bytecode in the Editor Udon VM. Both full-resolution and block-symbol pixel output are compared against native encoding for every tested mode. Exact GPU payload decoding is checked separately. This is not an uploaded VRChat client test.

## Scope

The package change removes the UPM SDK dependency, adds a Worlds version define, removes prefab-template backings and fixes package shader include paths. Runtime codec algorithms, wire format, codec IDs and existing script/material/prefab GUIDs remain unchanged.

Program field metadata is regenerated against Core 0.2.0. Validation-project-specific serialized program references must not replace the repository's existing program references. SDK-free import warnings for unused Udon program assets are distinct from missing components in the prepared Controller; the latter is asserted absent.

GPU timing during a concurrent cold Unity import is not a useful performance measurement. Run Player on its own after import, and keep failed as well as successful logs. IL2CPP, stripping, Quest, lossy video transport and an uploaded VRChat world are outside this validation.
