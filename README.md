# ⚡ Neural Pipeline Studio v2.0

[![Release](https://img.shields.io/badge/release-v2.0.0-00E5FF.svg)](https://github.com/)
[![Target .NET](https://img.shields.io/badge/.NET-8.0_LTS-512BD4.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-win--x64-blue.svg)](https://microsoft.com/)
[![Graphics APIs](https://img.shields.io/badge/APIs-DX12%20|%20DX11%20|%20Vulkan%20|%20DX10--9-00E676.svg)](#supported-apis)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Bilingual](https://img.shields.io/badge/Language-English%20%7C%20Italiano-FFD700.svg)](#localization)

> [!IMPORTANT]
> ### 📦 Looking for the ready-to-run app? (No installation required)
> **[👉 Click here to download NeuralPipelineStudio_v2.0_Portable.zip (570 MB) 👈](https://github.com/brescale/NeuralPipelineStudio/releases/download/v2.0-beta/NeuralPipelineStudio_v2.0_Portable.zip)**  
> *The portable release comes pre-packaged with `NeuralPipelineStudio.exe`, 16 DLSS models, ReShade shaders, runtime proxies, presets, and the embedded .NET 8 runtime. Extract anywhere and double-click `NeuralPipelineStudio.exe` or `Launch Studio.bat`!*  
> *(If you downloaded the source code ZIP, `Launch Studio.bat` will automatically compile with .NET 8 or prompt you to download the portable ZIP above).*

**Neural Pipeline Studio** is a standalone, universal GPU neural rendering workstation and multi-API game injection manager engineered for AAA gaming environments (*Red Dead Redemption 2*, *Cyberpunk 2077*, *Grand Theft Auto V*, *The Witcher 3*, and more).

Featuring a modern **Home-First architecture**, the studio automatically scans your PC storage drives to discover installed titles across Steam, Xbox Game Pass, Epic Games, and GOG, granting dedicated, isolated workstations for every title with real-time ping-pong rescaling, tensor multiplier control, hardware auto-calibration, and multi-API injection.

---

## 📸 Architectural Overview

`
[ Game Engine Framebuffer ]
             │
             ▼
[ 10x Pre-Chain Iterative Ping-Pong ] ─── (0.70x Downscale ⇄ 1.428x Upscale Loop)
             │
             ▼
[ Stage 1 Pre-Downscale Edge Preservation ]
             │
             ▼
[ Lumenite Optical Flow Velocity Buffer ]
             │
             ▼
[ DLSS 5 Guide Feed Interposer ] ─── (Luma, Depth & Motion Vector Agreement)
             │
             ▼
[ 1° Neural Pass: RenoDX Tensor Engine ] ─── (30 Iterations, Native HDR 498 nits)
             │
             ▼
[ DLSS Super Resolution Reconstruction ] ─── (900% Ultra Quality, Preset F/G)
             │
             ▼
[ 2° Neural Pass: DLSS-NR Reconstruction ] ─── (15 Passes, 100% Synthesized Layer)
             │
             ▼
[ ReShade Post-Stack: RTAO + LSAO + SSSR + Bloom + TRAA + Motion Blur ]
             │
             ▼
[ 10x Post-Neural Output Iterative Chain ] ─── (Dynamic Clarity & Micro-Contrast Lock)
             │
             ▼
[ Final Calibrated Display Swapchain ]
`

---

## 🌟 Core Features in v2.0

### 🏠 Universal Fleet Hub & PC Games Scanner
- **Zero Configuration First Launch**: Opens directly to the interactive PC Games Library.
- **Whole-PC Storage Scanner**: Automatically traverses all fixed and external storage drives (C:, D:, E:, etc.) and detects installed libraries from Steam, Xbox Game Pass, Epic Games Store, and GOG.
- **Direct Folder Import**: Manually target any custom directory or non-standard game installation with one click.
- **Contextual Game Cards**: Real-time identification of Graphics API (DirectX 12, Vulkan, DirectX 11), game engine (Rockstar RAGE, Unreal Engine 4/5, REDengine, Creation Engine, Unity), and Anti-Cheat guardrails.

### 🎛 Dedicated Per-Game Studio
- Seamlessly transition from the Home Hub to the dedicated workstation for any specific title.
- Complete isolation: tuning the pipeline for *Red Dead Redemption 2* does not modify settings for *Cyberpunk 2077*.
- Quick action header bar: **🚀 Inject**, **🧹 Vanilla Restore**, **▶ Launch**, **💾 Save**, and **⬅ Back to Home**.

### 🌐 Complete Bilingual Localization (IT / EN)
- Instant language switching via the 🇮🇹 IT / 🇬🇧 EN buttons in the top header bar.
- Fully localized interface, sidebar navigation, parameter inspector, hardware telemetry, status bar, and all **8 Wiki Chapters**.
- Automatic persistence in studio_settings.json with fallback matching your Windows OS locale.

### 📐 Interactive Inspector & Ping-Pong Rescale Engine
- Fine-tune every parameter with synchronized **Sliders** and **Exact Editable Text Boxes**:
  - **Downscale Compression Ratio**: (e.g. 70%, 50%, 80%)
  - **Upscale Expansion Ratio**: (e.g. 142%, 200%, 600%, 900% up to 1500%)
  - **Loop Cycles**: (1x to 50x)
  - **Neural Tensor Iterations**: (1 to 60 passes)
  - **Synthetic Blend Intensity**: (0% to 500%)
- Direct selection of associated ReShade shaders (.fx), neural DLL models (
vngx_dlss.dll, 
vngx.dll_dlssnr.dll), and runtime addons (enodx-dlss5.addon64, OptiScaler.dll).

### 📊 Hardware Auto-Calibration & VRAM Sweet Spot Guardrail
- Live hardware profiling querying GPU model, dedicated VRAM, driver version, CPU, and RAM.
- **1-Click Auto-Calibration**: Intelligently scales loop counts, tensor passes, and DLSS resolution multipliers while locking memory consumption to the **80-85% Sweet Spot** to prevent driver-level out-of-memory crashes.
- Tailored profiles for **Enthusiast (16-24 GB)**, **High-End (10-12 GB)**, **Mainstream (8 GB)**, and **Entry (4-6 GB)** hardware, plus non-RTX AMD/Intel fallback via OptiScaler.

### 📚 Integrated Technical Wiki (8 Chapters)
- Interactive documentation covering pipeline architecture, DLSS generation comparisons (2.x through 5.0), ping-pong mathematics, VRAM telemetry, hardware matrix, and troubleshooting.

---

## 📥 Download & Installation

The production release is distributed as a **100% portable single-package release**:

1. Go to the [Releases](https://github.com/) section on GitHub.
2. Download **NeuralPipelineStudio_v2.0_Portable.zip** (~570 MB).
3. Extract the archive to any location (e.g. Desktop, games folder, or USB drive).
4. Launch **NeuralPipelineStudio.exe**.

> **Note**: No .NET runtime installation or administrative privileges required. All dependencies, runtime proxies, shaders, and 16 DLSS models are self-contained.

---

## 📁 Repository Structure

`
NeuralPipelineStudio/
├── .gitattributes                # Git line-ending normalization rules
├── .gitignore                    # Build and archive exclusions (GitHub safe)
├── LICENSE                       # MIT License
├── README.md                     # Comprehensive documentation & specification
├── RELEASE_NOTES_v2.0.md         # Production release notes
├── Directory.Build.props         # Global solution properties
├── build.ps1                     # One-click automated build & package script
├── docs/                         # Architectural specifications
│   ├── ARCHITECTURE.md           # Pipeline stage topology & buffer recycling
│   ├── VRAM_OPTIMIZATION.md      # Mathematical breakdown of 80-85% sweet spot
│   └── MULTI_API_GUIDE.md        # PE inspection, hooking, and anti-cheat safe list
├── presets/                      # Certified hardware JSON presets
│   ├── 01_SweetSpot_82.5_RTX4070.json
│   ├── 02_Cinematic_Ultra_Quality.json
│   ├── 03_Performance_Balanced.json
│   ├── 04_Denoiser_100pct_Synth.json
│   └── 05_Competitive_Low_Latency.json
└── src/
    └── NeuralPipelineStudio/
        ├── NeuralPipelineStudio.csproj
        ├── App.xaml / App.xaml.cs    # Global dark Fluent theme & styling
        ├── MainWindow.xaml / .cs      # Fluent 2 WPF interface & reactive bindings
        ├── Common/                    # LocalizationManager, HardwareEngine, Logger
        ├── Models/                    # Data contracts, API & pipeline domain models
        └── Core/                      # HLSL generator, VRAM engine, ConfigSync, UniversalScanner, WikiRepository
`

---

## 🛠 Building from Source

### Prerequisites
- Windows 10 / 11 (x64)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build Command
`powershell
# Clone the repository
git clone https://github.com/your-username/NeuralPipelineStudio.git
cd NeuralPipelineStudio

# Build in Release mode
dotnet build src/NeuralPipelineStudio/NeuralPipelineStudio.csproj -c Release

# Or publish standalone self-contained binary
dotnet publish src/NeuralPipelineStudio/NeuralPipelineStudio.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist/
`

---

## 🇮🇹 Guida Rapida in Italiano

**Neural Pipeline Studio** è una suite portabile completa per il rendering neurale avanzato nei videogiochi su PC:

1. **Scansione PC**: Avvia l'app e clicca su **🔍 Scansiona Giochi nel Tuo PC**. L'app rileva tutti i titoli installati su Steam, Game Pass, Epic Games e GOG.
2. **Workbench Dedicato**: Clicca su **🎛 Configura Gioco & Pipeline** su qualsiasi scheda gioco per accedere al suo studio dedicato indipendente.
3. **Lingua**: Clicca su **🇮🇹 IT** o **🇬🇧 EN** in alto a destra per cambiare lingua in qualsiasi momento.
4. **Auto-Calibrazione**: Clicca **⚡ Auto-Calibrate PC** per ottimizzare istantaneamente la pipeline per la tua scheda grafica e bloccare il consumo VRAM all'82.5%.
5. **Iniezione**: Clicca **🚀 Inietta** per installare la suite nel gioco, o **🧹 Vanilla** per rimuoverla completamente in qualsiasi momento.

---

## 🤝 Credits & Acknowledgements

Neural Pipeline Studio integrates and orchestrates the outstanding work of the PC gaming and open-source modding communities. Full details in [CREDITS.md](CREDITS.md):

- **[ReShade](https://reshade.me)** (Crosire & team): Core post-processing runtime and interposer hooking architecture.
- **[RenoDX](https://github.com/clshortfuse/renodx)** (clshortfuse): HDR color science and neural tensor synthesis passes.
- **[OptiScaler](https://github.com/cdozdil/OptiScaler)** (cdozdil): Universal upscaling translation layer bridging DLSS, FSR 3.1, and XeSS 1.3 across all GPUs.
- **NVIDIA Corporation**: DLSS 2/3/3.5/4/5 technology, Streamline framework, and DLSS-NR neural denoisers.
- **PureDark**: Groundbreaking DLSS and Frame Generation integration in AAA games.
- **Marty McFly (Pascal Gilcher)**: Foundational screen-space ray tracing (RTGI) and depth motion research.
- **Lumenite & ReShade FX Authors**: Community shaders (RTAO, SSSR, bloom, motion blur, TRAA).

---

## 📄 License

Distributed under the MIT License. See [LICENSE](LICENSE) for more details.