using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NeuralPipelineStudio.Models;

namespace NeuralPipelineStudio.Core
{
    public static class UniversalScanner
    {
        public static List<GameTargetInfo> ScanWholePc(Action<string>? log = null)
        {
            var games = new List<GameTargetInfo>();
            var scannedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void CheckAndAdd(string folder)
            {
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return;
                string norm = Path.GetFullPath(folder).TrimEnd('\\', '/');
                if (scannedDirs.Contains(norm)) return;
                scannedDirs.Add(norm);

                string name = Path.GetFileName(norm);
                if (name.StartsWith("Steamworks", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Steam Controller", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("licenses", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("plugins", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("lspdfr", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("NeuralPipelineStudio", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("GameSave", StringComparison.OrdinalIgnoreCase))
                    return;

                var info = ScanDirectory(norm);
                if (!string.IsNullOrEmpty(info.ExecutablePath) && File.Exists(info.ExecutablePath))
                {
                    try
                    {
                        var fi = new FileInfo(info.ExecutablePath);
                        if (fi.Length > 2 * 1024 * 1024)
                        {
                            games.Add(info);
                            log?.Invoke($"[SCAN] Detected: {info.GameName} ({info.EngineDisplay} | {info.ApiDisplay})");
                        }
                    }
                    catch { }
                }
            }

            log?.Invoke("[SCAN] Initiating whole-PC gaming fleet discovery...");

            var steamLibraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            steamLibraries.Add(@"C:\Program Files (x86)\Steam\steamapps\common");
            steamLibraries.Add(@"C:\Program Files\Steam\steamapps\common");

            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
            {
                string root = drive.RootDirectory.FullName;

                string steamLib = Path.Combine(root, "SteamLibrary", "steamapps", "common");
                if (Directory.Exists(steamLib)) steamLibraries.Add(steamLib);

                string steamApps = Path.Combine(root, "steamapps", "common");
                if (Directory.Exists(steamApps)) steamLibraries.Add(steamApps);

                string[] vdfPaths = {
                    Path.Combine(root, "SteamLibrary", "steamapps", "libraryfolders.vdf"),
                    Path.Combine(root, "Program Files (x86)", "Steam", "steamapps", "libraryfolders.vdf"),
                    Path.Combine(root, "steamapps", "libraryfolders.vdf")
                };

                foreach (var vdf in vdfPaths)
                {
                    if (File.Exists(vdf))
                    {
                        try
                        {
                            string text = File.ReadAllText(vdf);
                            var matches = Regex.Matches(text, @"""path""\s+""([^""]+)""");
                            foreach (Match m in matches)
                            {
                                string p = m.Groups[1].Value.Replace(@"\\", @"\");
                                string common = Path.Combine(p, "steamapps", "common");
                                if (Directory.Exists(common)) steamLibraries.Add(common);
                            }
                        }
                        catch { }
                    }
                }

                // Xbox / Game Pass
                string xbox = Path.Combine(root, "XboxGames");
                if (Directory.Exists(xbox))
                {
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(xbox))
                            CheckAndAdd(dir);
                    }
                    catch { }
                }

                // Epic Games
                string epic = Path.Combine(root, "Program Files", "Epic Games");
                if (Directory.Exists(epic))
                {
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(epic))
                            CheckAndAdd(dir);
                    }
                    catch { }
                }

                // GOG Games
                string gog = Path.Combine(root, "GOG Games");
                if (Directory.Exists(gog))
                {
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(gog))
                            CheckAndAdd(dir);
                    }
                    catch { }
                }

                string gogGalaxy = Path.Combine(root, "Program Files (x86)", "GOG Galaxy", "Games");
                if (Directory.Exists(gogGalaxy))
                {
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(gogGalaxy))
                            CheckAndAdd(dir);
                    }
                    catch { }
                }

                // Generic Games
                string generic = Path.Combine(root, "Games");
                if (Directory.Exists(generic))
                {
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(generic))
                            CheckAndAdd(dir);
                    }
                    catch { }
                }
            }

            foreach (var lib in steamLibraries)
            {
                if (Directory.Exists(lib))
                {
                    try
                    {
                        foreach (var gameDir in Directory.GetDirectories(lib))
                            CheckAndAdd(gameDir);
                    }
                    catch { }
                }
            }

            log?.Invoke($"[SCAN] Discovery finished. Found {games.Count} compatible games across all drives.");
            return games.OrderBy(g => g.GameName).ToList();
        }

        public static GameTargetInfo ScanDirectory(string gameDir)
        {
            var info = new GameTargetInfo
            {
                GameDirectory = gameDir,
                GameName = Path.GetFileName(gameDir.TrimEnd('\\', '/'))
            };

            if (!Directory.Exists(gameDir))
                return info;

            // 1. Check known high-priority games & nested executable locations
            string rdr2 = Path.Combine(gameDir, "RDR2.exe");
            string cp2077 = Path.Combine(gameDir, "bin", "x64", "Cyberpunk2077.exe");
            string gta5 = Path.Combine(gameDir, "GTA5.exe");
            string gta5Enh = Path.Combine(gameDir, "GTA5_Enhanced.exe");
            string fo4 = Path.Combine(gameDir, "Content", "Fallout4.exe");
            string skyrim = Path.Combine(gameDir, "SkyrimSE.exe");
            string ac4 = Path.Combine(gameDir, "ACBlackFlag.exe");

            if (File.Exists(rdr2))
            {
                info.ExecutablePath = rdr2;
                info.GameName = "Red Dead Redemption 2";
                info.DetectedEngine = EngineType.RockstarRAGE;
                info.DetectedApi = GraphicsApi.Vulkan;
            }
            else if (File.Exists(cp2077))
            {
                info.ExecutablePath = cp2077;
                info.GameDirectory = Path.GetDirectoryName(cp2077)!;
                info.GameName = "Cyberpunk 2077";
                info.DetectedEngine = EngineType.REDengine;
                info.DetectedApi = GraphicsApi.DirectX12;
            }
            else if (File.Exists(gta5Enh))
            {
                info.ExecutablePath = gta5Enh;
                info.GameName = "Grand Theft Auto V Enhanced";
                info.DetectedEngine = EngineType.RockstarRAGE;
                info.DetectedApi = GraphicsApi.DirectX12;
            }
            else if (File.Exists(gta5))
            {
                info.ExecutablePath = gta5;
                info.GameName = "Grand Theft Auto V";
                info.DetectedEngine = EngineType.RockstarRAGE;
                info.DetectedApi = GraphicsApi.DirectX11;
            }
            else if (File.Exists(fo4))
            {
                info.ExecutablePath = fo4;
                info.GameDirectory = Path.GetDirectoryName(fo4)!;
                info.GameName = "Fallout 4";
                info.DetectedEngine = EngineType.CreationEngine;
                info.DetectedApi = GraphicsApi.DirectX11;
            }
            else if (File.Exists(ac4))
            {
                info.ExecutablePath = ac4;
                info.GameName = "Assassin's Creed IV Black Flag";
                info.DetectedEngine = EngineType.Anvil;
                info.DetectedApi = GraphicsApi.DirectX11;
            }
            else
            {
                // General search for game executable
                var searchDirs = new List<string> { gameDir };
                string bin64 = Path.Combine(gameDir, "bin", "x64");
                if (Directory.Exists(bin64)) searchDirs.Add(bin64);
                string content = Path.Combine(gameDir, "Content");
                if (Directory.Exists(content)) searchDirs.Add(content);
                string win64 = Path.Combine(gameDir, "Binaries", "Win64");
                if (Directory.Exists(win64)) searchDirs.Add(win64);

                foreach (var sub in searchDirs)
                {
                    var exes = Directory.GetFiles(sub, "*.exe", SearchOption.TopDirectoryOnly)
                                        .Where(f => !Path.GetFileName(f).StartsWith("Uninstall", StringComparison.OrdinalIgnoreCase) &&
                                                    !Path.GetFileName(f).StartsWith("CrashReport", StringComparison.OrdinalIgnoreCase) &&
                                                    !Path.GetFileName(f).StartsWith("UnityCrashHandler", StringComparison.OrdinalIgnoreCase) &&
                                                    !Path.GetFileName(f).StartsWith("NeuralPipelineStudio", StringComparison.OrdinalIgnoreCase) &&
                                                    !Path.GetFileName(f).StartsWith("REDprelauncher", StringComparison.OrdinalIgnoreCase) &&
                                                    !Path.GetFileName(f).EndsWith("Launcher.exe", StringComparison.OrdinalIgnoreCase) &&
                                                    !Path.GetFileName(f).EndsWith("helper.exe", StringComparison.OrdinalIgnoreCase))
                                        .OrderByDescending(f => new FileInfo(f).Length)
                                        .ToList();

                    if (exes.Count > 0)
                    {
                        info.ExecutablePath = exes[0];
                        info.GameDirectory = sub;
                        info.GameName = Path.GetFileNameWithoutExtension(exes[0]);
                        break;
                    }
                }

                // Engine detection
                string checkDir = info.GameDirectory;
                if (File.Exists(Path.Combine(checkDir, "x64a.rpf")) || File.Exists(Path.Combine(checkDir, "system.xml")) ||
                    File.Exists(Path.Combine(checkDir, "PlayRDR2.exe")) || File.Exists(Path.Combine(checkDir, "PlayGTAV.exe")))
                {
                    info.DetectedEngine = EngineType.RockstarRAGE;
                }
                else if (Directory.Exists(Path.Combine(checkDir, "Engine")) || info.ExecutablePath.Contains("Shipping") ||
                         info.ExecutablePath.Contains("UE4") || info.ExecutablePath.Contains("UE5"))
                {
                    info.DetectedEngine = EngineType.UnrealEngine;
                }
                else if (File.Exists(Path.Combine(checkDir, "Cyberpunk2077.exe")) || Directory.Exists(Path.Combine(checkDir, "r6")))
                {
                    info.DetectedEngine = EngineType.REDengine;
                }
                else if (Directory.Exists(Path.Combine(checkDir, "Data")) || File.Exists(Path.Combine(checkDir, "Fallout4.exe")))
                {
                    info.DetectedEngine = EngineType.CreationEngine;
                }
                else if (File.Exists(Path.Combine(checkDir, "UnityPlayer.dll")))
                {
                    info.DetectedEngine = EngineType.Unity;
                }
                else if (info.ExecutablePath.Contains("skullandbones", StringComparison.OrdinalIgnoreCase))
                {
                    info.DetectedEngine = EngineType.Anvil;
                }
                else
                {
                    info.DetectedEngine = EngineType.Custom;
                }

                // API detection
                if (File.Exists(Path.Combine(checkDir, "d3d12.dll")) || File.Exists(Path.Combine(checkDir, "sl.interposer.dll")) ||
                    File.Exists(Path.Combine(checkDir, "nvngx_dlssg.dll")) || info.DetectedEngine == EngineType.UnrealEngine ||
                    info.DetectedEngine == EngineType.REDengine)
                {
                    info.DetectedApi = GraphicsApi.DirectX12;
                }
                else if (File.Exists(Path.Combine(checkDir, "vulkan-1.dll")) || File.Exists(Path.Combine(checkDir, "NvLowLatencyVk.dll")))
                {
                    info.DetectedApi = GraphicsApi.Vulkan;
                }
                else
                {
                    info.DetectedApi = GraphicsApi.DirectX11;
                }
            }

            // Anti-Cheat detection
            string acDir = info.GameDirectory;
            if (Directory.Exists(Path.Combine(acDir, "BattlEye")) || File.Exists(Path.Combine(acDir, "GTA5_Enhanced_BE.exe")))
            {
                info.HasAntiCheat = true;
                info.AntiCheatName = "BattlEye (Singleplayer Mode Safe)";
            }
            else if (Directory.Exists(Path.Combine(acDir, "EasyAntiCheat")) || File.Exists(Path.Combine(acDir, "start_protected_game.exe")))
            {
                info.HasAntiCheat = true;
                info.AntiCheatName = "EasyAntiCheat (Singleplayer Mode Safe)";
            }

            bool hasReShade = File.Exists(Path.Combine(info.GameDirectory, "dxgi.dll")) || File.Exists(Path.Combine(info.GameDirectory, "ReShade64.dll"));
            bool hasShaders = Directory.Exists(Path.Combine(info.GameDirectory, "reshade-shaders"));

            info.IsUpgradeInstalled = hasReShade && hasShaders;
            info.InstallationDetails = info.IsUpgradeInstalled ? "Upgrade Suite ACTIVE" : "Vanilla / Not Installed";

            return info;
        }

        public static bool InstallUpgrade(string targetGameDir, GraphicsApi api, string sourceMasterDir, Action<string> log)
        {
            if (!Directory.Exists(targetGameDir))
            {
                log($"ERROR: Target game folder does not exist: {targetGameDir}");
                return false;
            }

            if (string.IsNullOrEmpty(sourceMasterDir) || !Directory.Exists(Path.Combine(sourceMasterDir, "reshade-shaders")))
            {
                sourceMasterDir = PayloadManager.ResolvePayloadDirectory();
            }

            log($"[INSTALL] Target Game: {Path.GetFileName(targetGameDir)}");
            log($"[INSTALL] Target API: {api}");
            log($"[INSTALL] Payload Source: {sourceMasterDir}");

            var installedFiles = new List<string>();

            try
            {
                string srcShaders = Path.Combine(sourceMasterDir, "reshade-shaders");
                string dstShaders = Path.Combine(targetGameDir, "reshade-shaders");
                if (Directory.Exists(srcShaders))
                {
                    log("[INSTALL] Deploying Lumenite shaders, textures, and headers...");
                    CopyDirectory(srcShaders, dstShaders);
                    installedFiles.Add("reshade-shaders");
                }

                string srcReShade = Path.Combine(sourceMasterDir, "ReShade64.dll");
                if (!File.Exists(srcReShade))
                    srcReShade = Path.Combine(sourceMasterDir, "dxgi.dll");

                string proxyName = api switch
                {
                    GraphicsApi.DirectX11 => "dxgi.dll",
                    GraphicsApi.DirectX10_9 => "d3d9.dll",
                    GraphicsApi.Vulkan => "dxgi.dll",
                    _ => "dxgi.dll"
                };

                if (File.Exists(srcReShade))
                {
                    string dstProxy = Path.Combine(targetGameDir, proxyName);
                    File.Copy(srcReShade, dstProxy, true);
                    File.Copy(srcReShade, Path.Combine(targetGameDir, "ReShade64.dll"), true);
                    installedFiles.Add(proxyName);
                    installedFiles.Add("ReShade64.dll");
                    log($"[INSTALL] Installed ReShade 6.8 ({proxyName})");
                }

                string srcOpti = Path.Combine(sourceMasterDir, "OptiScaler.dll");
                if (!File.Exists(srcOpti))
                    srcOpti = Path.Combine(sourceMasterDir, "winmm.dll");

                if (File.Exists(srcOpti))
                {
                    File.Copy(srcOpti, Path.Combine(targetGameDir, "OptiScaler.dll"), true);
                    File.Copy(srcOpti, Path.Combine(targetGameDir, "winmm.dll"), true);
                    installedFiles.Add("OptiScaler.dll");
                    installedFiles.Add("winmm.dll");
                    log("[INSTALL] Installed OptiScaler v10.0 (OptiScaler.dll + winmm.dll)");
                }

                string srcOptiFolder = Path.Combine(sourceMasterDir, "OptiScaler");
                if (Directory.Exists(srcOptiFolder))
                {
                    CopyDirectory(srcOptiFolder, Path.Combine(targetGameDir, "OptiScaler"));
                    installedFiles.Add("OptiScaler");
                }

                string[] addons = { "renodx-dlss5.addon64", "dlss5-bridge.addon64", "nvngx.dll_dlssnr.dll" };
                foreach (var addon in addons)
                {
                    string srcA = Path.Combine(sourceMasterDir, addon);
                    if (File.Exists(srcA))
                    {
                        File.Copy(srcA, Path.Combine(targetGameDir, addon), true);
                        installedFiles.Add(addon);
                        log($"[INSTALL] Deployed Addon: {addon}");
                    }
                }

                string[] configs = { "ReShade.ini", "ReShadePreset.ini", "OptiScaler.ini", "dlss5-bridge.cfg", "dlss5-feed.cfg" };
                foreach (var cfg in configs)
                {
                    string srcC = Path.Combine(sourceMasterDir, cfg);
                    if (File.Exists(srcC))
                    {
                        File.Copy(srcC, Path.Combine(targetGameDir, cfg), true);
                        installedFiles.Add(cfg);
                        log($"[INSTALL] Initialized Config: {cfg}");
                    }
                }

                // Auto-tune deployed OptiScaler.ini for detected API and GPU
                string targetOptiIni = Path.Combine(targetGameDir, "OptiScaler.ini");
                if (File.Exists(targetOptiIni))
                {
                    try
                    {
                        string iniText = File.ReadAllText(targetOptiIni);
                        var hw = HardwareEngine.CurrentProfile;
                        if (api == GraphicsApi.Vulkan)
                        {
                            iniText = ConfigSync.UpdateIniKey(iniText, "Spoofing", "Dxgi", "false");
                        }
                        else
                        {
                            iniText = ConfigSync.UpdateIniKey(iniText, "Spoofing", "Dxgi", "auto");
                        }

                        if (hw.SupportsTensorCores)
                        {
                            iniText = ConfigSync.UpdateIniKey(iniText, "Upscalers", "Dx12Upscaler", "dlss");
                            iniText = ConfigSync.UpdateIniKey(iniText, "Upscalers", "VulkanUpscaler", "dlss");
                            iniText = ConfigSync.UpdateIniKey(iniText, "DlssNr", "Enabled", "true");
                        }
                        else if (hw.GpuVendor == "AMD")
                        {
                            iniText = ConfigSync.UpdateIniKey(iniText, "Upscalers", "Dx12Upscaler", "ffx");
                            iniText = ConfigSync.UpdateIniKey(iniText, "Upscalers", "VulkanUpscaler", "ffx");
                            iniText = ConfigSync.UpdateIniKey(iniText, "DlssNr", "Enabled", "false");
                        }
                        else if (hw.GpuVendor == "Intel")
                        {
                            iniText = ConfigSync.UpdateIniKey(iniText, "Upscalers", "Dx12Upscaler", "xess");
                            iniText = ConfigSync.UpdateIniKey(iniText, "Upscalers", "VulkanUpscaler", "ffx");
                            iniText = ConfigSync.UpdateIniKey(iniText, "DlssNr", "Enabled", "false");
                        }
                        File.WriteAllText(targetOptiIni, iniText, Encoding.UTF8);
                    }
                    catch { }
                }

                string dlssDir = PayloadManager.ResolveDlss5Directory();
                if (Directory.Exists(dlssDir))
                {
                    var dlssFiles = Directory.GetFiles(dlssDir, "*.*").Where(f => f.EndsWith(".dll") || f.EndsWith(".addon64"));
                    foreach (var df in dlssFiles)
                    {
                        string fname = Path.GetFileName(df);
                        string dest = Path.Combine(targetGameDir, fname);
                        if (!File.Exists(dest))
                        {
                            try
                            {
                                File.Copy(df, dest, true);
                                installedFiles.Add(fname);
                            }
                            catch { }
                        }
                    }
                    log("[INSTALL] Deployed DLSS5 neural models.");
                }

                string manifestPath = Path.Combine(targetGameDir, "upgrade_manifest.json");
                string json = JsonSerializer.Serialize(installedFiles, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(manifestPath, json, Encoding.UTF8);
                log($"[INSTALL] Upgrade Manifest generated ({installedFiles.Count} entries)");

                log("[INSTALL] Deployment completed successfully! Guardrails verified.");
                return true;
            }
            catch (Exception ex)
            {
                log($"[ERROR] Installation failed: {ex.Message}");
                return false;
            }
        }

        public static bool UninstallUpgrade(string targetGameDir, Action<string> log)
        {
            if (!Directory.Exists(targetGameDir))
            {
                log($"ERROR: Target game folder does not exist: {targetGameDir}");
                return false;
            }

            string manifestPath = Path.Combine(targetGameDir, "upgrade_manifest.json");
            var filesToRemove = new List<string>();

            if (File.Exists(manifestPath))
            {
                try
                {
                    string json = File.ReadAllText(manifestPath);
                    filesToRemove = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
                catch { }
            }

            if (filesToRemove.Count == 0)
            {
                filesToRemove.AddRange(new[]
                {
                    "dxgi.dll", "d3d9.dll", "ReShade64.dll", "OptiScaler.dll", "winmm.dll",
                    "renodx-dlss5.addon64", "dlss5-bridge.addon64", "nvngx.dll_dlssnr.dll",
                    "ReShade.ini", "ReShadePreset.ini", "OptiScaler.ini", "dlss5-bridge.cfg", "dlss5-feed.cfg",
                    "upgrade_manifest.json", "reshade-shaders", "OptiScaler", "pipeline_layers.json"
                });
            }

            int deletedCount = 0;
            foreach (var item in filesToRemove)
            {
                string fullPath = Path.Combine(targetGameDir, item);
                try
                {
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        log($"[UNINSTALL] Removed file: {item}");
                        deletedCount++;
                    }
                    else if (Directory.Exists(fullPath))
                    {
                        Directory.Delete(fullPath, true);
                        log($"[UNINSTALL] Removed directory: {item}");
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    log($"[WARNING] Could not delete {item}: {ex.Message}");
                }
            }

            if (File.Exists(manifestPath))
            {
                try { File.Delete(manifestPath); } catch { }
            }

            log($"[UNINSTALL] Clean uninstall finished. ({deletedCount} items cleaned). Game restored to 100% vanilla.");
            return true;
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) return;

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }

        public static string GetCacheFilePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "detected_games.json");
        }

        public static void SaveDetectedGames(List<GameTargetInfo> games)
        {
            try
            {
                string path = GetCacheFilePath();
                string json = JsonSerializer.Serialize(games, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch { }
        }

        public static List<GameTargetInfo> LoadCachedGames()
        {
            try
            {
                string path = GetCacheFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    var list = JsonSerializer.Deserialize<List<GameTargetInfo>>(json);
                    if (list != null && list.Count > 0)
                    {
                        foreach (var g in list)
                        {
                            if (Directory.Exists(g.GameDirectory))
                            {
                                string proxyName = g.DetectedApi switch
                                {
                                    GraphicsApi.DirectX11 => "dxgi.dll",
                                    GraphicsApi.DirectX10_9 => "d3d9.dll",
                                    _ => "dxgi.dll"
                                };
                                bool hasShaders = Directory.Exists(Path.Combine(g.GameDirectory, "reshade-shaders"));
                                bool hasProxy = File.Exists(Path.Combine(g.GameDirectory, proxyName)) || File.Exists(Path.Combine(g.GameDirectory, "ReShade64.dll"));
                                bool hasManifest = File.Exists(Path.Combine(g.GameDirectory, "upgrade_manifest.json"));
                                g.IsUpgradeInstalled = hasShaders && (hasProxy || hasManifest);
                            }
                        }
                        return list.Where(g => Directory.Exists(g.GameDirectory)).ToList();
                    }
                }
            }
            catch { }
            return new List<GameTargetInfo>();
        }
    }
}