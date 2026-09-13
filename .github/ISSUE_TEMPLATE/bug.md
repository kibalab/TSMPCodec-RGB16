---
name: "Bug report"
about: "Report incorrect behavior with reproduction steps or source evidence."
labels: "bug, needs triage"
---

Reports in English, Japanese or Korean are welcome. Search existing issues for the same cause before opening another. Replace the instructions below with your findings.

## Actual behavior

Identify the affected component, the conditions that trigger the problem and what happens.

## Expected behavior

Describe how the component should behave under the same conditions.

## Conditions and reproduction steps

Include the minimum scene configuration and settings. For a source-review finding that has not been reproduced, say so and describe the conditions under which the code would fail.

1. Objects, components and settings:
2. Action performed:
3. Observed result:

## Environment

Enter "not applicable" where appropriate and "not installed" if the SDK is absent.

- TSMP Core / codec version or commit:
- Unity:
- VRChat SDK / UdonSharp:
- Tested environment (Editor edit mode / Play Mode / Standalone Player / Udon VM / VRChat client / source review only):
- OS:
- GPU / graphics API (for rendering issues):
- Player backend / stripping settings (for Player issues):

## Evidence and verification status

Include links to code at a fixed commit, the first error, logs, screenshots or a minimal reproduction. Separate checks that were actually run from checks that have not been run. C# compilation does not verify UdonSharp compilation or runtime behavior.

Do not include credentials, access tokens, private stream URLs or personal data from logs.

## Completion criteria

- [ ] Describe the observable behavior and regression tests required to consider this fixed.

## Related issues

Use `#123` within this repository or `kibalab/repository#123` across repositories. Enter "None" if there are no related issues.
