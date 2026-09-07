using System;

namespace NeuralPipelineStudio.Models
{
    public class PipelineLayer
    {
        public string Id { get; set; } = string.Empty;
        public string InstanceId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string TechniqueName { get; set; } = string.Empty;
        public string ShaderFile { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public bool Enabled { get; set; } = true;
        public string Description { get; set; } = string.Empty;
        public string AccentColorHex { get; set; } = "#00E5FF";

        // Dedicated per-item parameters (individually editable even if duplicated)
        public int LoopCycles { get; set; } = 1;
        public string DllModelName { get; set; } = string.Empty;
        public string AddonName { get; set; } = string.Empty; // "renodx-dlss.addon64", "OptiScaler.dll", "ReShade64.dll", etc.
        public string ScaleMode { get; set; } = "None"; // "Ping-Pong (Down ⇄ Up)", "Downscale", "Upscale", "DLSS Super Resolution", "DLAA", "None"
        public float ScaleRatio { get; set; } = 1.0f;
        public float DownscaleRatio { get; set; } = 0.70f;
        public float UpscaleRatio { get; set; } = 1.428571f;
        public string UpscaleModel { get; set; } = "DLSS 4/4.5 (nvngx_dlss.dll)";
        public int NeuralPassIterations { get; set; } = 30;
        public float Intensity { get; set; } = 1.0f;
        public string QualityPreset { get; set; } = "Preset F";

        // Formatted indicators for UI display
        public string StatusDisplay => Enabled ? "ACTIVE" : "OFF";
        public string LoopDisplay => LoopCycles > 1 ? $"{LoopCycles}x Loop" : "1x";
        public string ScaleDisplay => ScaleMode switch
        {
            "Ping-Pong (Down ⇄ Up)" or "Ping-Pong" => $"{DownscaleRatio * 100:F0}% ⇄ {UpscaleRatio * 100:F0}%",
            "Downscale" => $"{DownscaleRatio * 100:F0}% Down",
            "Upscale" => $"{UpscaleRatio * 100:F0}% Up",
            "DLSS Super Resolution" or "DLSS" => $"{ScaleRatio * 100:F0}% DLSS",
            "DLAA" => "100% DLAA",
            _ => string.Empty
        };
        public string DllDisplay
        {
            get
            {
                if (!string.IsNullOrEmpty(DllModelName)) return DllModelName;
                if (!string.IsNullOrEmpty(UpscaleModel) && ScaleMode.Contains("Ping-Pong")) return UpscaleModel;
                return string.Empty;
            }
        }
        public string ShaderDisplay => !string.IsNullOrEmpty(ShaderFile) ? ShaderFile : string.Empty;
        public string AddonDisplay => !string.IsNullOrEmpty(AddonName) ? AddonName : string.Empty;

        public PipelineLayer Clone()
        {
            return new PipelineLayer
            {
                Id = this.Id,
                InstanceId = Guid.NewGuid().ToString(),
                Name = this.Name.EndsWith("(Copy)") ? this.Name : this.Name + " (Copy)",
                TechniqueName = this.TechniqueName,
                ShaderFile = this.ShaderFile,
                Category = this.Category,
                Enabled = this.Enabled,
                Description = this.Description,
                AccentColorHex = this.AccentColorHex,
                LoopCycles = this.LoopCycles,
                DllModelName = this.DllModelName,
                AddonName = this.AddonName,
                ScaleMode = this.ScaleMode,
                ScaleRatio = this.ScaleRatio,
                DownscaleRatio = this.DownscaleRatio,
                UpscaleRatio = this.UpscaleRatio,
                UpscaleModel = this.UpscaleModel,
                NeuralPassIterations = this.NeuralPassIterations,
                Intensity = this.Intensity,
                QualityPreset = this.QualityPreset
            };
        }

        public override string ToString()
        {
            return $"{(Enabled ? "[ACTIVE]" : "[OFF]")} {Name} ({Category}) - {LoopCycles}x";
        }
    }
}
