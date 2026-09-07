namespace NeuralPipelineStudio.Models
{
    public class NeuralPipelineSettings
    {
        // 1° Neural Pass (RenoDX)
        public int NeuralPass1Iterations { get; set; } = 1;
        public float NeuralPass1Intensity { get; set; } = 1.0f;
        public float NeuralPass1GlobalTone { get; set; } = 1.0f;
        public float NeuralPass1LocalStructure { get; set; } = 1.0f;
        public float NeuralPass1SkinStructure { get; set; } = 1.0f;
        public int NeuralPass1Style { get; set; } = 0;
        public bool NeuralPass1AutoMask { get; set; } = true;
        public float NeuralPass1DiffuseWhiteNits { get; set; } = 100.0f;

        // Super Resolution Upscale
        public float SuperResolutionRatio { get; set; } = 1.5f; // Safe highlight guard
        public string SuperResolutionPreset { get; set; } = "F";

        // 2° Neural Pass (DLSS-NR)
        public int DlssNrPasses { get; set; } = 15;
        public float DlssNrTransferStrength { get; set; } = 1.00f; // 100% neural detail transfer
        public float DlssNrWhitePointTrim { get; set; } = 1.0f;
        public float DlssNrLocalStructure { get; set; } = 1.0f;
        public float DlssNrSkinStructure { get; set; } = 1.0f;
        public float DlssNrIntensity { get; set; } = 1.0f;
        public int DlssNrStyle { get; set; } = 0;
        public bool DlssNrEnabled { get; set; } = true;
        public bool DlssNrUseProxy { get; set; } = true;

        // Iterative Downscale / Upscale Chains
        public int PreChainCycles { get; set; } = 10;
        public int PostChainCycles { get; set; } = 10;
        public float DownscaleRatio { get; set; } = 0.70f;
        public float UpscaleRatio { get; set; } = 1.428571f;
        public float PreContrast { get; set; } = 1.00f;
        public float PreClarity { get; set; } = 0.15f;
        public float CycleContrast { get; set; } = 1.00f;
        public float CycleSharpness { get; set; } = 0.25f;

        // ReShade Post-Stack
        public bool RtaoEnabled { get; set; } = true;
        public bool LsaoEnabled { get; set; } = true;
        public bool SssrEnabled { get; set; } = true;
        public bool BloomEnabled { get; set; } = true;
        public bool TraaEnabled { get; set; } = true;
        public bool MotionBlurEnabled { get; set; } = true;
        public int MotionBlurSamples { get; set; } = 14;
        public float MotionBlurStrength { get; set; } = 0.75f;

        // OptiScaler / Streamline settings
        public string UpscaleRatioOverrideValue { get; set; } = "1.428571"; // 0.70x
        public bool FrameGenEnabled { get; set; } = false;
        public bool OutputScalingEnabled { get; set; } = false;
        public float OutputScalingMultiplier { get; set; } = 1.5f;

        // Bridge & Vulkan
        public bool VkMirror { get; set; } = false;
        public int Synth { get; set; } = 0;
        public int BridgeStage { get; set; } = 3;

        // VRAM Budget Target
        public float VramTargetPercent { get; set; } = 82.5f; // 80 - 85%
        public bool VramAutoBalance { get; set; } = true;

        // Neural Generation Target
        public string DlssGeneration { get; set; } = "DLSS 5.0 (Synthetic Master & RenoDX)";
        public float CycleClarity { get; set; } = 0.20f;
    }
}
