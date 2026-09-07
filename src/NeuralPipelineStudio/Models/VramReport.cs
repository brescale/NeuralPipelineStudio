using NeuralPipelineStudio.Common;

namespace NeuralPipelineStudio.Models
{
    public class VramReport
    {
        public string GpuName { get; set; } = "Unknown GPU";
        public int TotalVramMB { get; set; } = 12282;
        public int CurrentUsedVramMB { get; set; } = 2000;
        public int TargetMinMB => (int)(TotalVramMB * 0.80f);
        public int TargetMaxMB => (int)(TotalVramMB * 0.85f);
        public int EstimatedPipelineVramMB { get; set; } = 0;
        public float EstimatedUsagePercent => TotalVramMB > 0 ? (float)EstimatedPipelineVramMB / TotalVramMB * 100f : 0f;
        public string StatusText { get; set; } = "OPTIMAL";
        public string StatusColorHex { get; set; } = "#00E676";
    }

    public class GameTargetInfo
    {
        public string GameName { get; set; } = string.Empty;
        public string GameDirectory { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public GraphicsApi DetectedApi { get; set; } = GraphicsApi.DirectX12;
        public EngineType DetectedEngine { get; set; } = EngineType.RockstarRAGE;
        public bool IsUpgradeInstalled { get; set; } = false;
        public string InstallationDetails { get; set; } = string.Empty;
        public bool HasAntiCheat { get; set; } = false;
        public string AntiCheatName { get; set; } = string.Empty;

        public string StatusDisplay => IsUpgradeInstalled ? "🟢 UPGRADE ACTIVE" : "⚪ VANILLA";
        public string StatusColorHex => IsUpgradeInstalled ? "#00E676" : "#E2E8F0";
        public string EngineDisplay => DetectedEngine switch
        {
            EngineType.RockstarRAGE => "Rockstar RAGE Engine",
            EngineType.UnrealEngine => "Unreal Engine 4/5",
            EngineType.REDengine => "CD Projekt REDengine",
            EngineType.CreationEngine => "Bethesda Creation Engine",
            EngineType.Unity => "Unity Engine",
            EngineType.Anvil => "Ubisoft Anvil Engine",
            _ => "Custom 3D Engine"
        };
        public string ApiDisplay => DetectedApi switch
        {
            GraphicsApi.DirectX12 => "DirectX 12 (D3D12)",
            GraphicsApi.Vulkan => "Vulkan API",
            GraphicsApi.DirectX11 => "DirectX 11 (D3D11)",
            _ => "DirectX 9/10"
        };
        public string AntiCheatDisplay => HasAntiCheat ? AntiCheatName : (LocalizationManager.CurrentLanguage == AppLanguage.English ? "Clean (Singleplayer Safe)" : "Pulito (Sicuro Singleplayer)");
        public string AntiCheatColorHex => HasAntiCheat ? "#FBBF24" : "#5EEAD4";

        public string CardBtnConfigureText => LocalizationManager.Get("HomeCardConfigure");
        public string CardBtnConfigureTip => LocalizationManager.Get("HomeCardConfigureTip");
        public string CardBtnInjectText => LocalizationManager.Get("HomeCardInject");
        public string CardBtnInjectTip => LocalizationManager.Get("HomeCardInjectTip");
        public string CardBtnVanillaText => LocalizationManager.Get("HomeCardVanilla");
        public string CardBtnVanillaTip => LocalizationManager.Get("HomeCardVanillaTip");
        public string CardBtnLaunchText => LocalizationManager.Get("HomeCardLaunch");
        public string CardBtnLaunchTip => LocalizationManager.Get("HomeCardLaunchTip");
    }
}
