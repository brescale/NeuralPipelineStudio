using System;
using System.Diagnostics;
using NeuralPipelineStudio.Models;

namespace NeuralPipelineStudio.Core
{
    public static class VramEngine
    {
        private static int _cachedTotalVram = 12282;
        private static string _cachedGpuName = "NVIDIA GeForce RTX 4070";

        static VramEngine()
        {
            RefreshHardwareGpuInfo();
        }

        public static void RefreshHardwareGpuInfo()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=name,memory.total,memory.used --format=csv,noheader,nounits",
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
                        if (parts.Length >= 2)
                        {
                            _cachedGpuName = parts[0].Trim();
                            if (int.TryParse(parts[1].Trim(), out int total))
                                _cachedTotalVram = total;
                        }
                    }
                }
            }
            catch
            {
                _cachedGpuName = "NVIDIA GeForce RTX 4070";
                _cachedTotalVram = 12282;
            }
        }

        public static int GetCurrentLiveUsedMemoryMB()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=memory.used --format=csv,noheader,nounits",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(1000);
                    if (int.TryParse(output.Trim(), out int used))
                        return used;
                }
            }
            catch { }

            return 2100;
        }

        public static VramReport CalculateReport(NeuralPipelineSettings settings, System.Collections.Generic.List<PipelineLayer>? layers = null)
        {
            int baseGameEngineMB = 5500;
            int internalResMB = 380;
            int preChainMB = settings.VramAutoBalance ? 180 : (settings.PreChainCycles * 28);
            int neuralPass1MB = 450 + (settings.NeuralPass1Iterations * 12);
            int superResMB = 850 + (int)(settings.SuperResolutionRatio * 25);
            int dlssNrMB = 980 + (settings.DlssNrPasses * 42);

            int postStackMB = 0;
            if (settings.RtaoEnabled) postStackMB += 180;
            if (settings.LsaoEnabled) postStackMB += 90;
            if (settings.SssrEnabled) postStackMB += 160;
            if (settings.BloomEnabled) postStackMB += 60;
            if (settings.TraaEnabled) postStackMB += 75;
            if (settings.MotionBlurEnabled) postStackMB += (40 + settings.MotionBlurSamples * 4);

            int postChainMB = settings.VramAutoBalance ? 180 : (settings.PostChainCycles * 28);
            int frameGenMB = settings.FrameGenEnabled ? 650 : 0;

            int extraLayerLoopMB = 0;
            if (layers != null)
            {
                extraLayerLoopMB = layers.Where(l => l.Enabled).Sum(l => Math.Max(0, (l.LoopCycles - 1) * 18));
            }

            int totalEstimatedMB = baseGameEngineMB + internalResMB + preChainMB + 
                                   neuralPass1MB + superResMB + dlssNrMB + 
                                   postStackMB + postChainMB + frameGenMB + extraLayerLoopMB;

            float usagePercent = (float)totalEstimatedMB / _cachedTotalVram * 100f;

            string status;
            string colorHex;

            if (usagePercent < 78.0f)
            {
                status = $"UNDER TARGET ({usagePercent:F1}%) - Headroom Available";
                colorHex = "#00B0FF";
            }
            else if (usagePercent <= 85.5f)
            {
                status = $"OPTIMAL SWEET SPOT ({usagePercent:F1}%) - Target 80-85% Locked";
                colorHex = "#00E676";
            }
            else if (usagePercent <= 91.0f)
            {
                status = $"HIGH LOAD ({usagePercent:F1}%) - VRAM Thrash Risk";
                colorHex = "#FFAA00";
            }
            else
            {
                status = $"OVERFLOW DANGER ({usagePercent:F1}%) - OOM Crash Warning";
                colorHex = "#FF3D00";
            }

            return new VramReport
            {
                GpuName = _cachedGpuName,
                TotalVramMB = _cachedTotalVram,
                CurrentUsedVramMB = GetCurrentLiveUsedMemoryMB(),
                EstimatedPipelineVramMB = totalEstimatedMB,
                StatusText = status,
                StatusColorHex = colorHex
            };
        }
    }
}
