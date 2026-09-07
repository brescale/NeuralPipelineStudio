# ⚡ Neural Pipeline Studio v2.0 Release Notes

**Release Date:** September 2026  
**Build:** v2.0.0 (Production Release)  
**Target Runtime:** .NET 8.0 LTS (win-x64, Self-Contained)

---

## 🌟 What's New in v2.0

### 1. 🏠 Universal Fleet Hub & Home-First Architecture
- **Multi-Drive Storage Scanner**: 1-click scanning across all local and external drives (C:, D:, E:, etc.), automatically detecting installed titles across **Steam**, **Xbox Game Pass**, **Epic Games Store**, and **GOG**.
- **Contextual Game Cards**: Real-time identification of Graphics API (DirectX 12, Vulkan, DirectX 11), game engine (Rockstar RAGE, Unreal Engine 4/5, REDengine, Creation Engine, Unity), and Anti-Cheat guardrails.
- **Dedicated Per-Game Workbench**: Seamlessly drill down into any game to tune its specific rendering sequence, DLSS models, and presets without cross-game contamination.

### 2. 🌐 Complete Bilingual Localization (🇬🇧 English & 🇮🇹 Italian)
- **1-Click Header Switcher**: Switch between English and Italian instantly with the header toggle buttons (🇮🇹 IT / 🇬🇧 EN).
- **Full App Coverage**: All views, sidebar items, telemetry cards, inspector parameters, status messages, and all **8 Wiki Chapters** are fully localized.
- **Persistent Preferences**: Language selection is saved to studio_settings.json and automatically matches your OS culture on first launch.

### 3. 🎛 Interactive Parameter Inspector & Rescaling Engine
- **Dual Input Controls**: Every numerical parameter features synchronized sliders and exact-value text boxes.
- **Ping-Pong Rescaling Loop**: Fine-tune Downscale ratios (e.g. 0.70x / 70%), Upscale expansion (e.g. 1.428x / 143% or up to 1500% Super Resolution), and loop counts (1x - 50x) with zero VRAM buffer overhead.
- **Direct Shader, DLL & Addon Mappings**: Explicitly link each rendering pass with its associated ReShade shader (`.fx`), DLSS model (`nvngx_dlss.dll`, `nvngx.dll_dlssnr.dll`), and runtime addon (`renodx-dlss5.addon64`, `OptiScaler.dll`).

### 4. 📊 Hardware Auto-Calibration & VRAM Sweet Spot Guardrail
- **Auto-Calibrate for This PC**: 1-click calculation tailoring pipeline cycles, tensor passes, and DLSS resolution multipliers directly to your GPU's exact VRAM and architecture.
- **Hardware Tier Profiles**:
  - **Enthusiast (16-24 GB VRAM)**: RTX 4090 / 4080 (15x cycles, 1000% Super Resolution)
  - **High-End (10-12 GB VRAM)**: RTX 4070 / 3080 (10x cycles, 900% Super Resolution)
  - **Mainstream (8 GB VRAM)**: RTX 4060 / 3070 / RX 6700 (6x cycles, 600% Super Resolution)
  - **Entry (4-6 GB VRAM)**: RTX 3050 / GTX 1660 (3x cycles, 400% Super Resolution)
  - **Non-RTX**: AMD Radeon & Intel Arc universal OptiScaler / XeSS bridge
- **VRAM Cap Locking**: Enforces an 80-85% memory ceiling to prevent DirectX/Vulkan out-of-memory driver crashes.

### 5. 📚 Built-in Technical Wiki (8 Chapters)
- Comprehensive technical documentation covering pipeline architecture, DLSS generations comparison (DLSS 2 through 5.0), ping-pong mathematics, VRAM budgeting, hardware tier tuning, and multi-game troubleshooting.

---

## 📦 Distribution Packages

| Package | Size | Description |
| :--- | :--- | :--- |
| **NeuralPipelineStudio_v2.0_Portable.zip** | ~570 MB | **Recommended for all users.** 100% standalone portable package with embedded .NET 8 runtime, 16 DLSS models, ReShade shaders, and presets. Zero installation required. |
| **Source Code** | ZIP / TAR | Complete C# / WPF source code for building and contributing. |

---

## 🚀 Quickstart Guide

1. Download **NeuralPipelineStudio_v2.0_Portable.zip** from the Release assets below.
2. Extract the archive anywhere (Desktop, games drive, or portable drive).
3. Run **NeuralPipelineStudio.exe**.
4. Click **🔍 Scan PC Games** on the home screen to detect all installed games, or click **📁 Add Folder Manually...** to select any specific game folder.
5. Click **🎛 Configure Game & Pipeline** on your game card, verify your parameters, and click **🚀 Inject**!

---

## 🇮🇹 Note di Rilascio in Italiano

- **Flotta Giochi Universale**: Scansione automatica di tutti i dischi per rilevare giochi Steam, Xbox Game Pass, Epic e GOG.
- **Workbench Dedicato**: Ogni gioco dispone di un ambiente di calibrazione dedicato e indipendente.
- **Bilingue Completo**: Tasto rapido in alto per passare istantaneamente tra Italiano ed Inglese.
- **Inspector Interattivo**: Regola percentuali di scaling (downscale/upscale), cicli di loop e iterazioni con slider e caselle di testo numeriche.
- **Auto-Calibrazione Hardware**: 1 click per bloccare il carico VRAM all'82.5% ideale su qualsiasi PC e scheda video.
- **100% Portabile**: Estrai e avvia senza alcuna installazione.