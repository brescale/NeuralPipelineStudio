# Universal Multi-API Game Deployment Guide

## Supported Graphics APIs
Neural Pipeline Studio includes a universal game scanner and injector compatible across modern and legacy graphics runtimes:
- **DirectX 12**: Uses `dxgi.dll` / `d3d12.dll` interposer with native Streamline / DLSS integration.
- **DirectX 11**: Uses `dxgi.dll` proxy hook.
- **DirectX 10 / 9**: Uses `d3d9.dll` wrapper.
- **Vulkan**: Uses `dxgi.dll` bridge and Vulkan layer interposer.

## Supported Game Engines
- **Rockstar RAGE Engine**: Red Dead Redemption 2, Grand Theft Auto V, Max Payne 3.
- **Unreal Engine 4 & 5**: Cyberpunk 2077 (REDengine), The Witcher 3, Hogwarts Legacy, Black Myth: Wukong.
- **Creation Engine**: Skyrim Special Edition, Fallout 4, Starfield.
- **Unity**: Custom indie and commercial titles.

## Anti-Cheat Safety Protocol
The scanner detects anti-cheat binaries:
- `BattlEye` (`GTA5_Enhanced_BE.exe`, `BattlEye/` directory).
- `EasyAntiCheat` (`EasyAntiCheat/` directory, `start_protected_game.exe`).

When anti-cheat is detected, the studio tags the installation for **Singleplayer / Offline Mode Safe**, ensuring online ban protection.

## Manifest-Based 100% Clean Uninstall
Every installation generates an `upgrade_manifest.json` recording every file copied or modified. Executing **Clean Uninstall** uses this manifest to delete all injected DLLs and shaders, restoring the game to its pure vanilla state.
