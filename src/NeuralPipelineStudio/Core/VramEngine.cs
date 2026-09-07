using System;
using System.Linq;
using NeuralPipelineStudio.Models;

namespace NeuralPipelineStudio.Core
{
    public static class VramEngine
    {
        private static int _cachedTotalVram = 12282;
        private static string _cachedGpuName = "NVIDIA GeForce RTX 4070";
        private static int _cachedUsedVram = 2150;

        static VramEngine()
        {
            RefreshHardwareGpuInfo();
        }

        public static void RefreshHardwareGpuInfo()
        {
            try
            {
                var prof = HardwareEngine.CurrentProfile;
                if (prof != null)
                {
                    if (!string.IsNullOrWhiteSpace(prof.GpuName))
                        _cachedGpuName = prof.GpuName;

                    if (prof.TotalVramMB > 0)
                        _cachedTotalVram = prof.TotalVramMB;

                    if (prof.TotalVramMB > 0 && prof.FreeVramMB > 0 && prof.TotalVramMB > prof.FreeVramMB)
                        _cachedUsedVram = prof.TotalVramMB - prof.FreeVramMB;
                    else
                        _cachedUsedVram = 2150;
                }
            }
            catch
            {
                _cachedGpuName = "NVIDIA GeForce RTX 4070";
                _cachedTotalVram = 12282;
                _cachedUsedVram = 2150;
            }
        }

        public static int GetCurrentLiveUsedMemoryMB()
        {
            return _cachedUsedVram;
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
