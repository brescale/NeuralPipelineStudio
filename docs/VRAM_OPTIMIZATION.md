# VRAM Budget Engine & 80-85% Sweet Spot Optimization

## The 80-85% Sweet Spot Principle
Modern GPUs (especially Ada Lovelace architectures like the **NVIDIA GeForce RTX 4070 12GB**) experience performance degradation under two extremes:
1. **Under-utilization (<70%)**: Dedicated VRAM bandwidth and tensor cores sit idle; visual fidelity is left on the table.
2. **Over-saturation (>92%)**: Windows DWM and NVIDIA driver paging engines engage system RAM migration, inducing frame time spikes, micro-stutter, and potential Out-Of-Memory (OOM) application crashes.

The **80% to 85% Sweet Spot** represents the thermodynamic and architectural peak for 12GB GPUs:
- **Total Dedicated VRAM**: 12,282 MiB
- **80% Lower Boundary**: 9,825 MiB
- **85% Upper Boundary**: 10,440 MiB
- **Calibrated Target**: **82.5% (~10,132 MiB)**

## Memory Allocation Breakdown (4K Target)
| Component | Allocated VRAM | Description |
| :--- | :--- | :--- |
| **Base Game Engine & Textures** | ~5,500 MB | Ultra texture mipmaps, geometry, mesh buffers |
| **Internal Render Targets (0.70x)** | ~380 MB | 2688x1512 HDR16F buffers |
| **Pre-Chain Ping-Pong Recycling** | ~180 MB | Dynamic scratch memory pool |
| **1° Neural Pass (RenoDX 30it)** | ~810 MB | Working tensor activations |
| **DLSS Super Resolution (900%)** | ~1,075 MB | Temporal history buffer & sub-pixel cache |
| **2° Neural Pass (DLSS-NR 15p)** | ~1,610 MB | 4-generation temporal synthesis scratchpool |
| **ReShade Post-Stack Shaders** | ~605 MB | RTAO, LSAO, SSSR, Anamorphic Bloom, TRAA, Blur |
| **Post-Chain Output Ping-Pong** | ~180 MB | Output contrast and Catmull-Rom upscale |
| **TOTAL WORKING SET** | **~10,140 MB** | **82.5% VRAM (Target Locked)** |
