using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NeuralPipelineStudio.Models;

namespace NeuralPipelineStudio.Core
{
    public enum HardwareTier
    {
        Enthusiast, // 16GB - 24GB+ (RTX 4090, 4080, RX 7900 XTX)
        HighEnd,    // 10GB - 12GB (RTX 4070, 3080, RX 6800)
        Mainstream, // 8GB (RTX 4060, 3070, RX 6700, RX 7600)
        Entry,      // 4GB - 6GB (RTX 3050, GTX 1660, RX 6600)
        NonRtx      // AMD, Intel Arc, GTX without Tensor Cores
    }

    public class HardwareProfile
    {
        public string GpuName { get; set; } = "NVIDIA GeForce RTX 4070";
        public string GpuVendor { get; set; } = "NVIDIA";
        public string DriverVersion { get; set; } = "616.56";
        public int TotalVramMB { get; set; } = 12282;
        public int FreeVramMB { get; set; } = 9200;
        public int GpuClockMHz { get; set; } = 2490;
        public string CpuName { get; set; } = "11th Gen Intel(R) Core(TM) i9-11900KF @ 3.50GHz";
        public int CpuCores { get; set; } = 16;
        public int SystemRamGB { get; set; } = 32;
        public string OsVersion { get; set; } = "Windows 11 Pro 64-bit";
        public HardwareTier Tier { get; set; } = HardwareTier.HighEnd;

        public string TierDisplay => Tier switch
        {
            HardwareTier.Enthusiast => "Tier 1: Enthusiast (16GB-24GB) - Ultra Matrix",
            HardwareTier.HighEnd => "Tier 2: High-End (10GB-12GB) - Sweet Spot 82.5%",
            HardwareTier.Mainstream => "Tier 3: Mainstream (8GB) - Balanced Buffer Mode",
            HardwareTier.Entry => "Tier 4: Entry (4GB-6GB) - High Efficiency Mode",
            _ => "Tier 5: Universal Fallback (AMD / Intel / Non-RTX)"
        };

        public bool SupportsFrameGen => GpuName.Contains("RTX 40") || GpuName.Contains("RTX 50");
        public bool SupportsRayReconstruction => GpuName.Contains("RTX");
        public bool SupportsTensorCores => GpuName.Contains("RTX");

        public string SummaryHeader => $"{GpuName} | Driver: {DriverVersion} | VRAM: {TotalVramMB:N0} MB | CPU: {CpuName}";
    }

    public class DlssModelDetail
    {
        public string FileName { get; set; } = string.Empty;
        public string Generation { get; set; } = "DLSS 5";
        public string ProductVersion { get; set; } = "Unknown";
        public string FileDescription { get; set; } = string.Empty;
        public long SizeBytes { get; set; } = 0;
        public string SizeDisplay => $"{(SizeBytes / (1024.0 * 1024.0)):F2} MB";
        public bool IsInstalledInGame { get; set; } = false;
        public string StatusDisplay => IsInstalledInGame ? "🟢 ACTIVE IN GAME" : "📦 IN REPOSITORY";
    }

    public static class HardwareEngine
    {
        public static HardwareProfile CurrentProfile { get; private set; } = new();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        static HardwareEngine()
        {
            CurrentProfile = DetectHardware();
        }

        public static HardwareProfile DetectHardware()
        {
            var profile = new HardwareProfile();

            // 1. Detect GPU and Driver via nvidia-smi
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=driver_version,name,memory.total,memory.free,clocks.current.graphics --format=csv,noheader,nounits",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(1500);

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        var parts = output.Trim().Split(',');
                        if (parts.Length >= 5)
                        {
                            profile.DriverVersion = parts[0].Trim();
                            profile.GpuName = parts[1].Trim();
                            profile.GpuVendor = "NVIDIA";

                            if (int.TryParse(parts[2].Trim(), out int total))
                                profile.TotalVramMB = total;
                            if (int.TryParse(parts[3].Trim(), out int free))
                                profile.FreeVramMB = free;
                            if (int.TryParse(parts[4].Trim(), out int clock))
                                profile.GpuClockMHz = clock;
                        }
                    }
                }
            }
            catch
            {
                DetectGpuFallback(profile);
            }

            // 2. Detect CPU via Registry
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                if (key != null)
                {
                    var val = key.GetValue("ProcessorNameString");
                    if (val != null)
                        profile.CpuName = val.ToString()?.Trim() ?? profile.CpuName;
                }
            }
            catch { }

            profile.CpuCores = Environment.ProcessorCount;

            // 3. Detect System RAM via Win32 GlobalMemoryStatusEx
            try
            {
                var mem = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(mem))
                {
                    profile.SystemRamGB = (int)Math.Round(mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0));
                }
            }
            catch
            {
                profile.SystemRamGB = 32;
            }

            // 4. Determine OS version
            profile.OsVersion = Environment.OSVersion.VersionString + (Environment.Is64BitOperatingSystem ? " (64-bit)" : " (32-bit)");

            // 5. Determine Hardware Tier
            if (!profile.GpuName.Contains("RTX", StringComparison.OrdinalIgnoreCase))
            {
                profile.Tier = HardwareTier.NonRtx;
            }
            else if (profile.TotalVramMB >= 15000)
            {
                profile.Tier = HardwareTier.Enthusiast;
            }
            else if (profile.TotalVramMB >= 10000)
            {
                profile.Tier = HardwareTier.HighEnd;
            }
            else if (profile.TotalVramMB >= 7500)
            {
                profile.Tier = HardwareTier.Mainstream;
            }
            else
            {
                profile.Tier = HardwareTier.Entry;
            }

            CurrentProfile = profile;
            return profile;
        }

        private static void DetectGpuFallback(HardwareProfile profile)
        {
            try
            {
                using var videoKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000");
                if (videoKey != null)
                {
                    string desc = videoKey.GetValue("DriverDesc")?.ToString() ?? "";
                    string ver = videoKey.GetValue("DriverVersion")?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(desc))
                    {
                        profile.GpuName = desc;
                        profile.DriverVersion = ver;
                        if (desc.Contains("AMD", StringComparison.OrdinalIgnoreCase) || desc.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                            profile.GpuVendor = "AMD";
                        else if (desc.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                            profile.GpuVendor = "Intel";
                    }

                    var memVal = videoKey.GetValue("HardwareInformation.qwMemorySize");
                    if (memVal is long ramBytes && ramBytes > 0)
                    {
                        profile.TotalVramMB = (int)(ramBytes / (1024 * 1024));
                    }
                }
            }
            catch { }
        }

        public static NeuralPipelineSettings GenerateOptimalSettings(HardwareProfile profile)
        {
            var s = new NeuralPipelineSettings();

            switch (profile.Tier)
            {
                case HardwareTier.Enthusiast: // 16GB - 24GB (RTX 4090, 4080)
                    s.DownscaleRatio = 0.75f;
                    s.PreChainCycles = 15;
                    s.PostChainCycles = 15;
                    s.NeuralPass1Iterations = 45;
                    s.NeuralPass1Intensity = 2.4f;
                    s.SuperResolutionRatio = 10.0f; // 1000%
                    s.DlssNrPasses = 20;
                    s.DlssNrTransferStrength = 2.0f;
                    s.DlssNrWhitePointTrim = 4.0f;
                    s.DlssNrLocalStructure = 2.0f;
                    s.DlssNrSkinStructure = 2.0f;
                    s.DlssNrIntensity = 4.0f;
                    s.RtaoEnabled = true;
                    s.LsaoEnabled = true;
                    s.SssrEnabled = true;
                    s.BloomEnabled = true;
                    s.TraaEnabled = true;
                    s.MotionBlurEnabled = true;
                    s.MotionBlurSamples = 18;
                    s.MotionBlurStrength = 0.45f;
                    s.VramAutoBalance = true;
                    s.VramTargetPercent = 82.5f;
                    break;

                case HardwareTier.HighEnd: // 10GB - 12GB (RTX 4070, 3080) - Target Locked
                    s.DownscaleRatio = 0.70f;
                    s.PreChainCycles = 10;
                    s.PostChainCycles = 10;
                    s.NeuralPass1Iterations = 30;
                    s.NeuralPass1Intensity = 2.0f;
                    s.SuperResolutionRatio = 9.0f; // 900%
                    s.DlssNrPasses = 15;
                    s.DlssNrTransferStrength = 2.0f;
                    s.DlssNrWhitePointTrim = 4.0f;
                    s.DlssNrLocalStructure = 2.0f;
                    s.DlssNrSkinStructure = 2.0f;
                    s.DlssNrIntensity = 4.0f;
                    s.RtaoEnabled = true;
                    s.LsaoEnabled = true;
                    s.SssrEnabled = true;
                    s.BloomEnabled = true;
                    s.TraaEnabled = true;
                    s.MotionBlurEnabled = true;
                    s.MotionBlurSamples = 14;
                    s.MotionBlurStrength = 0.40f;
                    s.VramAutoBalance = true;
                    s.VramTargetPercent = 82.5f;
                    break;

                case HardwareTier.Mainstream: // 8GB (RTX 4060, 3070, RX 6700)
                    s.DownscaleRatio = 0.65f;
                    s.PreChainCycles = 6;
                    s.PostChainCycles = 6;
                    s.NeuralPass1Iterations = 18;
                    s.NeuralPass1Intensity = 1.6f;
                    s.SuperResolutionRatio = 6.0f; // 600%
                    s.DlssNrPasses = 10;
                    s.DlssNrTransferStrength = 0.85f;
                    s.DlssNrWhitePointTrim = 0.8f;
                    s.DlssNrLocalStructure = 0.9f;
                    s.DlssNrSkinStructure = 0.9f;
                    s.RtaoEnabled = true;
                    s.LsaoEnabled = false; // Bypass to stay in 80-85% of 8GB
                    s.SssrEnabled = true;
                    s.BloomEnabled = true;
                    s.TraaEnabled = true;
                    s.MotionBlurEnabled = false;
                    s.VramAutoBalance = true;
                    s.VramTargetPercent = 82.0f;
                    break;

                case HardwareTier.Entry: // 4GB - 6GB (RTX 3050, GTX 1660)
                    s.DownscaleRatio = 0.50f;
                    s.PreChainCycles = 3;
                    s.PostChainCycles = 3;
                    s.NeuralPass1Iterations = 10;
                    s.NeuralPass1Intensity = 1.2f;
                    s.SuperResolutionRatio = 4.0f; // 400%
                    s.DlssNrPasses = 6;
                    s.DlssNrTransferStrength = 0.70f;
                    s.DlssNrWhitePointTrim = 0.5f;
                    s.RtaoEnabled = false;
                    s.LsaoEnabled = false;
                    s.SssrEnabled = false;
                    s.BloomEnabled = true;
                    s.TraaEnabled = true;
                    s.MotionBlurEnabled = false;
                    s.VramAutoBalance = true;
                    s.VramTargetPercent = 80.0f;
                    break;

                case HardwareTier.NonRtx: // AMD / Intel Arc / GTX
                    s.DownscaleRatio = 0.65f;
                    s.PreChainCycles = 5;
                    s.PostChainCycles = 5;
                    s.NeuralPass1Iterations = 15;
                    s.NeuralPass1Intensity = 1.5f;
                    s.SuperResolutionRatio = 6.0f;
                    s.DlssNrPasses = 8;
                    s.DlssNrTransferStrength = 0.80f;
                    s.RtaoEnabled = true;
                    s.LsaoEnabled = false;
                    s.SssrEnabled = true;
                    s.BloomEnabled = true;
                    s.TraaEnabled = true;
                    s.VramAutoBalance = true;
                    s.VramTargetPercent = 81.0f;
                    break;
            }

            s.UpscaleRatioOverrideValue = (1.0f / s.DownscaleRatio).ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);
            return s;
        }

        public static List<DlssModelDetail> InspectModels(string repoFolder, string gameFolder = "")
        {
            var results = new List<DlssModelDetail>();
            if (!Directory.Exists(repoFolder)) return results;

            var files = Directory.GetFiles(repoFolder, "*.*", SearchOption.TopDirectoryOnly)
                                 .Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".addon64", StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                string name = Path.GetFileName(file);
                var fi = new FileInfo(file);
                var vi = FileVersionInfo.GetVersionInfo(file);

                string gen = "DLSS 5.0 (Synthetic Master)";
                string l = name.ToLowerInvariant();
                if (l.Contains("renodx")) gen = "DLSS 5.0 (HDR Tensor)";
                else if (l.Contains("dlssnr")) gen = "DLSS 4.5 (Neural Denoiser)";
                else if (l.Contains("dlssd")) gen = "DLSS 3.5 (Ray Reconstruction)";
                else if (l.Contains("dlssg")) gen = "DLSS 3.0 (Frame Generation)";
                else if (l.Contains("nvngx_dlss")) gen = "DLSS 4.0/5.0 (Super Resolution)";
                else if (l.Contains("deepdvc")) gen = "DLSS 4.0 (Dynamic Vibrance)";
                else if (l.Contains("interposer")) gen = "Streamline Cross-API Bridge";

                bool installed = false;
                if (!string.IsNullOrEmpty(gameFolder) && Directory.Exists(gameFolder))
                {
                    installed = File.Exists(Path.Combine(gameFolder, name));
                }

                results.Add(new DlssModelDetail
                {
                    FileName = name,
                    Generation = gen,
                    ProductVersion = string.IsNullOrEmpty(vi.ProductVersion) ? vi.FileVersion ?? "1.0.0" : vi.ProductVersion,
                    FileDescription = string.IsNullOrEmpty(vi.FileDescription) ? "NVIDIA Neural Module" : vi.FileDescription,
                    SizeBytes = fi.Length,
                    IsInstalledInGame = installed
                });
            }

            return results;
        }
    }
}