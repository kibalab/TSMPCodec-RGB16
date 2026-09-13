# RGB16 Calibration Baseline

These frozen byte-decode shaders are from main at `c53589e`, before G01.
They are comparison inputs, not package assets or alternative runtime shaders.

Use the [Core GPU calibration harness](https://github.com/kibalab/TSMP-Core/tree/main/Validation~/GpuCalibration)
with local Core and all four codec source checkouts. It checks the adaptive
policy against these originals using real GPU readback and records the
preparation pass in GPU timings.

The updated codec requires Core's new `PrepareDecode` API. Update both
source checkouts together; release the Core API and adjust the minimum
Core dependency before publishing the codec. No release is created here.
