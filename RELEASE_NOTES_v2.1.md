# ⚡ Neural Pipeline Studio v2.1 Release Notes

**Release Date:** September 2026  
**Build:** v2.1.0 (Production Release)  
**Target Runtime:** .NET 8.0 LTS (win-x64, Self-Contained)

---

## 🌟 What's New in v2.1

### 1. 🎨 Calibrated Photometric Exposure & Dynamic Range Guard
- **1:1 Native Camera Exposure Alignment**: White point trim precisely synchronized to `1.000000`, eliminating dark/crushed scenes and luminance truncation.
- **Unclamped HDR Dynamic Range**: Disabled rigid SDR clamping (`DAClampOutput = false`), allowing the full luminance scale of modern HDR/wide-gamut displays to shine without clipping highlight details.
- **Dynamic Auto-Exposure Hook**: Direct reading and continuous adaptation from the game engine camera exposure (`AutoExposure = true`, `WhitePointFromExposure = true`).

### 2. ⚡ Adaptive Framerate & Dynamic Resolution Scaling (DRS)
- **FPS-Target Adaptive Model Resolution**: Neural model workload and working resolution (`WorkingScale = auto`, `Preset = auto`) dynamically scale to sustain target framerate without static supersampling stutter.
- **Decoupled Forced Overrides**: Disabled artificial ratio locks (`UpscaleRatioOverrideEnabled = false`, `QualityRatioOverrideEnabled = false`), unlocking native DLSS Quality/Balanced/Performance presets and game DRS.

### 3. 🎯 Color Fidelity & Maximum Neural Detail Tuning
- **Standard Color Fidelity**: Locked to reference `1.000000` with neutral profile (`Style = 0`) to eliminate artificial oversaturation and color cast.
- **Maximized Neural Detail Stack**:
  - **Transfer Detail Strength**: `2.000000` (Max)
  - **Neural Intensity**: `4.000000` (Max)
  - **Local Structure Definition**: `2.000000` (Max)
  - **Local Tone Recovery**: `4.000000` (Max)
  - **Skin & Organic Texture Structure**: `2.000000` (Max)
  - **Highlight Guard Ratio**: `8.000000` (Max)
  - **Neural Passes**: `15` iterations
  - **Reconstruction Downscaler**: Lanczos3 filter (`ScalingDownscaler = 4`)

### 4. 🏠 Core Universal Platform (from v2.0)
- **Universal GPU Architecture**: 100% compatible across NVIDIA GeForce (RTX & GTX), AMD Radeon (RX 7000/6000/5000 via OptiScaler FSR 3.1), and Intel Arc (A-Series via XeSS 1.3).
- **Multi-Game PC Fleet Scanner**: 1-click discovery across Steam, Xbox Game Pass, Epic Games Store, and GOG.
- **Bilingual Interface**: English default out-of-the-box with 1-click Italian switcher (`[🇬🇧 EN]` / `[🇮🇹 IT]`).

---

## 📦 Distribution Packages

| Package | Size | Description |
| :--- | :--- | :--- |
| **NeuralPipelineStudio_v2.1_Portable.zip** | ~570 MB | **Recommended for all users.** 100% standalone portable package with embedded .NET 8 runtime, 16 DLSS models, ReShade shaders, and presets. Zero installation required. |
| **Source Code** | ZIP / TAR | Complete C# / WPF source code for building and contributing. |

---

## 🚀 Quickstart Guide

1. Download **NeuralPipelineStudio_v2.1_Portable.zip** from the Release assets below.
2. Extract the archive anywhere (Desktop, games drive, or portable USB drive).
3. Run **NeuralPipelineStudio.exe** (or `Launch Studio.bat`).
4. Click **🔍 Scan PC Games** to detect all installed titles.
5. Click **🎛 Configure Game & Pipeline**, check your settings, and click **🚀 Inject**!
