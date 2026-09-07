# Neural Pipeline Architecture Specification

## Overview
Neural Pipeline Studio coordinates a high-performance, multi-stage neural rendering pipeline engineered to deliver maximum visual fidelity, sub-pixel reconstruction, and temporal stability in AAA games.

```
[ Game 3D Engine Framebuffer ]
             │
             ▼
[ 10x Pre-Downscale Iterative Ping-Pong Chain ] ─── (0.70x Downscale + Catmull-Rom 600%)
             │
             ▼
[ Stage 1 Pre-Downscale Stack & High-Freq Preservation ]
             │
             ▼
[ Lumenite Optical Flow Motion Kernel ]
             │
             ▼
[ DLSS 5 Guide Feed Interposer ] ─── (Luma / Depth / Motion Vector Validation)
             │
             ▼
[ 1° Neural Pass: RenoDX Tensor Engine ] ─── (30 Iterations, Enhanced Direct Volume)
             │
             ▼
[ DLSS Super Resolution Reconstruction ] ─── (900% Ultra Quality, Preset F)
             │
             ▼
[ 2° Neural Pass: DLSS-NR Reconstruction ] ─── (15 Passes, 100% Synthesized Layer)
             │
             ▼
[ ReShade Post-Stack: RTAO + LSAO + SSSR + Bloom + TRAA + Motion Blur ]
             │
             ▼
[ 10x Post-Neural Output Iterative Ping-Pong Chain ] ─── (Dynamic Contrast & Clarity Boost)
             │
             ▼
[ Final Calibrated Display Swapchain ]
```

## Ping-Pong Texture Recycling
To prevent VRAM explosion during multi-cycle execution (1 to 20 cycles), intermediate passes share an allocated pool of two ping-pong buffers (`TexDownA` and `TexDownB` at downscaled resolution; `TexFullA` and `TexFullB` at display resolution). This guarantees constant memory footprint regardless of whether 5, 10, or 20 iterative passes are executed.

## 100% Synthesized Neural Denoising Layer
DLSS-NR operates in 100% transfer synthesis mode:
- `TransferStrength = 1.000000`
- `WhitePointTrim = 4.000000`
- `LocalStructure = 4.000000`
- `Passes = 15`
This completely eliminates stochastic ray tracing noise, temporal shimmer, and film grain while maintaining razor-sharp geometric silhouettes.
