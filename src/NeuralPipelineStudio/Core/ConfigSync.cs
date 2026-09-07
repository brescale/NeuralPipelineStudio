using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NeuralPipelineStudio.Models;

namespace NeuralPipelineStudio.Core
{
    public static class ConfigSync
    {
        public static List<PipelineLayer> GetDefaultPipeline(HardwareProfile? profile = null)
        {
            profile ??= HardwareEngine.DetectHardware();
            var s = HardwareEngine.GenerateOptimalSettings(profile);

            return new List<PipelineLayer>
            {
                new PipelineLayer 
                { 
                    Id = "pre_stack", 
                    Name = "Lumenite Pre-Downscale Stack (Edge & Contrast Lock)", 
                    TechniqueName = "Lumenite_Pre_Stack", 
                    ShaderFile = "lumenite_Pre_Stack.fx", 
                    Category = "Pre-Downscale", 
                    Enabled = true, 
                    AccentColorHex = "#4CAF50", 
                    DllModelName = "sl.interposer.dll",
                    AddonName = "ReShade64.dll",
                    LoopCycles = 1,
                    Description = "Preserva micro-contrasto e bordi geometrici ad alta frequenza prima del downscaling" 
                },
                new PipelineLayer 
                { 
                    Id = "kernel", 
                    Name = $"Lumenite Optical Flow Velocity Kernel (@ {s.DownscaleRatio:F2}x)", 
                    TechniqueName = "Lumenite_Kernel", 
                    ShaderFile = "lumenite_Kernel.fx", 
                    Category = "Optical Flow", 
                    Enabled = true, 
                    AccentColorHex = "#FF9800", 
                    DllModelName = "sl.interposer.dll",
                    AddonName = "OptiScaler.dll",
                    LoopCycles = 1,
                    Description = "Stima vettoriale del moto GPU e buffer di flusso ottico temporale per il bridge neurale" 
                },
                new PipelineLayer 
                { 
                    Id = "feed", 
                    Name = "DLSS 5.0 Guide Feed & Depth Interposer", 
                    TechniqueName = "DLSS5_Feed", 
                    ShaderFile = "DLSS5_Feed.fx", 
                    Category = "Guide Feed", 
                    Enabled = true, 
                    AccentColorHex = "#E91E63", 
                    DllModelName = "sl.common.dll",
                    AddonName = "OptiScaler.dll",
                    LoopCycles = 1,
                    Description = "Validazione luma/depth, geometric agreement e maschera di transizione per il bridge neurale" 
                },
                new PipelineLayer 
                { 
                    Id = "neural_pass_1", 
                    Name = $"1° Neural Pass: RenoDX HDR Tensor ({s.NeuralPass1Iterations} Iterazioni @ {s.NeuralPass1Intensity:F1}x)", 
                    TechniqueName = "Neural_Pass_1_RenoDX", 
                    ShaderFile = "RenoDX_DLSS5", 
                    Category = "Neural Engine", 
                    Enabled = true, 
                    AccentColorHex = "#9C27B0", 
                    DllModelName = "renodx-dlss5.addon64",
                    AddonName = "renodx-dlss5.addon64",
                    LoopCycles = 1,
                    NeuralPassIterations = s.NeuralPass1Iterations,
                    Intensity = s.NeuralPass1Intensity,
                    Description = $"Sintesi neurale pre-upscale a {s.NeuralPass1Iterations} iterazioni tensor con illuminazione volumetrica potenziata per {profile.GpuName}" 
                },
                new PipelineLayer 
                { 
                    Id = "super_res", 
                    Name = $"Super Resolution Upscaler: NVIDIA DLSS ({s.SuperResolutionRatio * 100:F0}% Preset {s.SuperResolutionPreset})", 
                    TechniqueName = "DLSS_Super_Resolution", 
                    ShaderFile = "OptiScaler_DLSS", 
                    Category = "Super Resolution", 
                    Enabled = true, 
                    AccentColorHex = "#2196F3", 
                    ScaleMode = "DLSS Super Resolution",
                    ScaleRatio = s.SuperResolutionRatio,
                    UpscaleRatio = s.SuperResolutionRatio,
                    UpscaleModel = "DLSS 4/4.5 (nvngx_dlss.dll)",
                    DllModelName = "nvngx_dlss.dll",
                    AddonName = "OptiScaler.dll",
                    QualityPreset = "Preset F",
                    LoopCycles = 1,
                    Description = $"Ricostruzione temporale sub-pixel ad altissima densità ({s.SuperResolutionRatio * 100:F0}% Preset {s.SuperResolutionPreset}) guidata da motion vectors" 
                },
                new PipelineLayer 
                { 
                    Id = "dlss_nr", 
                    Name = $"2° Neural Pass: DLSS-NR ({s.DlssNrPasses} Passi - 100% Synth Layer)", 
                    TechniqueName = "Neural_Pass_2_DLSS_NR", 
                    ShaderFile = "OptiScaler_DLSSNR", 
                    Category = "Neural Engine", 
                    Enabled = true, 
                    AccentColorHex = "#673AB7", 
                    LoopCycles = 1,
                    NeuralPassIterations = s.DlssNrPasses,
                    Intensity = s.DlssNrTransferStrength,
                    DllModelName = "nvngx.dll_dlssnr.dll",
                    AddonName = "renodx-dlss5.addon64",
                    Description = $"Denoising neurale a {s.DlssNrPasses} passaggi con layer 100% sintetizzato, skin structure {s.DlssNrSkinStructure:F1}x" 
                },
                new PipelineLayer 
                { 
                    Id = "rtao", 
                    Name = "Lumenite RTAO (Ray Traced Ambient Occlusion)", 
                    TechniqueName = "Lumenite_RTAO", 
                    ShaderFile = "lumenite_RTAO.fx", 
                    Category = "Post-Neural", 
                    Enabled = s.RtaoEnabled, 
                    AccentColorHex = "#00BCD4", 
                    DllModelName = "nvngx_dlssd.dll",
                    AddonName = "ReShade64.dll",
                    LoopCycles = 1,
                    Description = "Occlusione ambientale a tracciamento di raggio screen-space in tempo reale" 
                },
                new PipelineLayer 
                { 
                    Id = "lsao", 
                    Name = "Lumenite LSAO (Large Scale Ambient Occlusion)", 
                    TechniqueName = "Lumenite_LSAO", 
                    ShaderFile = "lumenite_LSAO.fx", 
                    Category = "Post-Neural", 
                    Enabled = s.LsaoEnabled, 
                    AccentColorHex = "#009688", 
                    DllModelName = "nvngx_dlssd.dll",
                    AddonName = "ReShade64.dll",
                    LoopCycles = 1,
                    Description = s.LsaoEnabled 
                        ? "Ombreggiatura e occlusione diffusa su larga scala (Attiva su GPU con >=10GB VRAM)"
                        : "Bypassata automaticamente per restare rigidamente entro l'80-82% VRAM" 
                },
                new PipelineLayer 
                { 
                    Id = "sssr", 
                    Name = "Lumenite SSSR (Screen-Space Specular Reflections)", 
                    TechniqueName = "LUMENITE_SSSR", 
                    ShaderFile = "lumenite_SSSR.fx", 
                    Category = "Post-Neural", 
                    Enabled = s.SssrEnabled, 
                    AccentColorHex = "#3F51B5", 
                    DllModelName = "sl.dlss_d.dll",
                    AddonName = "ReShade64.dll",
                    LoopCycles = 1,
                    Description = "Riflessi speculari accurati con ray-marching temporale sub-pixel" 
                },
                new PipelineLayer 
                { 
                    Id = "bloom", 
                    Name = "Lumenite Anamorphic Bloom (Diffrazione Spettrale)", 
                    TechniqueName = "Lumenite_AnamorphicBloom", 
                    ShaderFile = "lumenite_AnamorphicBloom.fx", 
                    Category = "Post-Neural", 
                    Enabled = s.BloomEnabled, 
                    AccentColorHex = "#FF5722", 
                    DllModelName = "sl.deepdvc.dll",
                    AddonName = "renodx-dlss5.addon64",
                    LoopCycles = 1,
                    Description = "Diffrazione ottica anamorfica e dispersione spettrale delle luci ad alta luminanza" 
                },
                new PipelineLayer 
                { 
                    Id = "traa", 
                    Name = "Lumenite TRAA (Temporal Reconstruction Anti-Aliasing)", 
                    TechniqueName = "Lumenite_TRAA", 
                    ShaderFile = "lumenite_TRAA.fx", 
                    Category = "Post-Neural", 
                    Enabled = s.TraaEnabled, 
                    AccentColorHex = "#8BC34A", 
                    DllModelName = "sl.dlss.dll",
                    AddonName = "ReShade64.dll",
                    LoopCycles = 1,
                    Description = "Antialiasing di rifinitura con accumulo temporale sub-pixel" 
                },
                new PipelineLayer 
                { 
                    Id = "motion_blur", 
                    Name = $"Lumenite Cinematic Motion Blur ({s.MotionBlurSamples} Campioni)", 
                    TechniqueName = "Lumenite_MotionBlur", 
                    ShaderFile = "lumenite_MotionBlur.fx", 
                    Category = "Post-Neural", 
                    Enabled = s.MotionBlurEnabled, 
                    AccentColorHex = "#CDDC39", 
                    DllModelName = "nvngx_dlssg.dll",
                    AddonName = "OptiScaler.dll",
                    LoopCycles = 1,
                    Description = s.MotionBlurEnabled 
                        ? $"Sfuocatura cinematografica a {s.MotionBlurSamples} campioni guidata dal velocity buffer"
                        : "Bypassata per preservare frame-rate e budget memoria" 
                }
            };
        }

        public static (NeuralPipelineSettings settings, List<PipelineLayer> layers) LoadFromGameDirectory(string gameDir, HardwareProfile? profile = null)
        {
            profile ??= HardwareEngine.DetectHardware();
            var settings = HardwareEngine.GenerateOptimalSettings(profile);
            var layers = GetDefaultPipeline(profile);

            if (!Directory.Exists(gameDir))
                return (settings, layers);

            // 0. Check for saved full pipeline layers state
            string layersJsonPath = Path.Combine(gameDir, "pipeline_layers.json");
            if (File.Exists(layersJsonPath))
            {
                try
                {
                    string json = File.ReadAllText(layersJsonPath);
                    var loaded = System.Text.Json.JsonSerializer.Deserialize<List<PipelineLayer>>(json);
                    if (loaded != null && loaded.Count > 0)
                    {
                        layers = loaded;
                        // Auto-associate dedicated Shader, DLL Model, and Addon if missing or empty, preserving 100% of user settings!
                        var defaultRef = GetDefaultPipeline(profile);
                        foreach (var l in layers)
                        {
                            var matchDef = defaultRef.FirstOrDefault(d => d.Id.Equals(l.Id, StringComparison.OrdinalIgnoreCase) ||
                                                                          d.TechniqueName.Equals(l.TechniqueName, StringComparison.OrdinalIgnoreCase));
                            if (matchDef != null)
                            {
                                if (string.IsNullOrEmpty(l.ShaderFile)) l.ShaderFile = matchDef.ShaderFile;
                                if (string.IsNullOrEmpty(l.DllModelName)) l.DllModelName = matchDef.DllModelName;
                                if (string.IsNullOrEmpty(l.AddonName)) l.AddonName = matchDef.AddonName;
                                if (string.IsNullOrEmpty(l.UpscaleModel) && !string.IsNullOrEmpty(matchDef.UpscaleModel)) l.UpscaleModel = matchDef.UpscaleModel;
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(l.AddonName)) l.AddonName = "ReShade64.dll";
                                if (string.IsNullOrEmpty(l.DllModelName)) l.DllModelName = "nvngx_dlss.dll";
                            }
                        }
                    }
                }
                catch { }
            }
            else
            {
                // 1. Read ReShadePreset.ini
                string presetPath = Path.Combine(gameDir, "ReShadePreset.ini");
                if (File.Exists(presetPath))
                {
                    try
                    {
                        string content = File.ReadAllText(presetPath);
                        var match = Regex.Match(content, @"^Techniques\s*=\s*(.+)$", RegexOptions.Multiline);
                    if (match.Success)
                    {
                        var activeTechs = match.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        var activeSet = new HashSet<string>(activeTechs, StringComparer.OrdinalIgnoreCase);

                        var reordered = new List<PipelineLayer>();
                        foreach (var tech in activeTechs)
                        {
                            string pureTech = tech.Contains('@') ? tech.Split('@')[0] : tech;
                            var layer = layers.FirstOrDefault(l => l.TechniqueName.Equals(pureTech, StringComparison.OrdinalIgnoreCase) ||
                                                                   l.TechniqueName.Equals(tech, StringComparison.OrdinalIgnoreCase));
                            if (layer != null)
                            {
                                layer.Enabled = true;
                                if (!reordered.Contains(layer))
                                    reordered.Add(layer);
                            }
                        }

                        foreach (var l in layers)
                        {
                            if (!reordered.Contains(l))
                            {
                                l.Enabled = false;
                                reordered.Add(l);
                            }
                        }

                        layers = reordered;
                    }
                }
                catch { }
            }
            }

            // 2. Read ReShade.ini
            string reshadeIniPath = Path.Combine(gameDir, "ReShade.ini");
            if (File.Exists(reshadeIniPath))
            {
                try
                {
                    string content = File.ReadAllText(reshadeIniPath);
                    var match = Regex.Match(content, @"DirectNeuralRenderingPassCount\s*=\s*(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int passes))
                        settings.NeuralPass1Iterations = passes;

                    var matchWhite = Regex.Match(content, @"NRDiffuseWhiteNits\s*=\s*([0-9.]+)");
                    if (matchWhite.Success && float.TryParse(matchWhite.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float nits))
                        settings.NeuralPass1DiffuseWhiteNits = nits;
                }
                catch { }
            }

            // 3. Read OptiScaler.ini
            string optiIniPath = Path.Combine(gameDir, "OptiScaler.ini");
            if (File.Exists(optiIniPath))
            {
                try
                {
                    string content = File.ReadAllText(optiIniPath);
                    
                    var matchPasses = Regex.Match(content, @"^\s*Passes\s*=\s*(\d+)", RegexOptions.Multiline);
                    if (matchPasses.Success && int.TryParse(matchPasses.Groups[1].Value, out int passes))
                        settings.DlssNrPasses = passes;

                    var matchTransfer = Regex.Match(content, @"^\s*TransferStrength\s*=\s*([0-9.]+)", RegexOptions.Multiline);
                    if (matchTransfer.Success && float.TryParse(matchTransfer.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float strength))
                        settings.DlssNrTransferStrength = strength;

                    var matchRatio = Regex.Match(content, @"^\s*UpscaleRatioOverrideValue\s*=\s*([0-9.]+)", RegexOptions.Multiline);
                    if (matchRatio.Success)
                    {
                        settings.UpscaleRatioOverrideValue = matchRatio.Groups[1].Value;
                        if (float.TryParse(matchRatio.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float rVal) && rVal > 0)
                            settings.DownscaleRatio = 1.0f / rVal;
                    }

                    var matchMax = Regex.Match(content, @"^\s*MaxRatio\s*=\s*([0-9.]+)", RegexOptions.Multiline);
                    if (matchMax.Success && float.TryParse(matchMax.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float maxR))
                        settings.SuperResolutionRatio = maxR;
                }
                catch { }
            }

            // 4. Read dlss5-bridge.cfg
            string bridgeCfgPath = Path.Combine(gameDir, "dlss5-bridge.cfg");
            if (File.Exists(bridgeCfgPath))
            {
                try
                {
                    string content = File.ReadAllText(bridgeCfgPath);
                    var matchVk = Regex.Match(content, @"^vk_mirror\s*=\s*(\d+)", RegexOptions.Multiline);
                    if (matchVk.Success)
                        settings.VkMirror = matchVk.Groups[1].Value == "1";

                    var matchStage = Regex.Match(content, @"^stage\s*=\s*(\d+)", RegexOptions.Multiline);
                    if (matchStage.Success && int.TryParse(matchStage.Groups[1].Value, out int st))
                        settings.BridgeStage = st;
                }
                catch { }
            }

            return (settings, layers);
        }

        public static void SaveToGameDirectory(string gameDir, NeuralPipelineSettings settings, List<PipelineLayer> layers)
        {
            if (!Directory.Exists(gameDir)) return;

            // 1. Update ReShadePreset.ini
            string presetPath = Path.Combine(gameDir, "ReShadePreset.ini");
            var activeTechs = layers.Where(l => l.Enabled && !string.IsNullOrEmpty(l.ShaderFile))
                                    .Select(l => $"{l.TechniqueName}@{l.ShaderFile}")
                                    .ToList();

            string techniquesLine = "Techniques=" + string.Join(",", activeTechs);

            if (File.Exists(presetPath))
            {
                string text = File.ReadAllText(presetPath);
                if (Regex.IsMatch(text, @"^Techniques\s*=", RegexOptions.Multiline))
                    text = Regex.Replace(text, @"^Techniques\s*=.*$", techniquesLine, RegexOptions.Multiline);
                else
                    text = techniquesLine + "\n" + text;

                text = UpdateIniKey(text, "lumenite_MotionBlur.fx", "BLUR_SAMPLES", settings.MotionBlurSamples.ToString());
                text = UpdateIniKey(text, "lumenite_MotionBlur.fx", "BLUR_STRENGTH", settings.MotionBlurStrength.ToString("0.000000", CultureInfo.InvariantCulture));

                File.WriteAllText(presetPath, text, Encoding.UTF8);
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine(techniquesLine);
                sb.AppendLine("TechniqueSorting=" + string.Join(",", activeTechs));
                sb.AppendLine();
                sb.AppendLine("[lumenite_MotionBlur.fx]");
                sb.AppendLine($"BLUR_SAMPLES={settings.MotionBlurSamples}");
                sb.AppendLine($"BLUR_STRENGTH={settings.MotionBlurStrength.ToString("0.000000", CultureInfo.InvariantCulture)}");
                File.WriteAllText(presetPath, sb.ToString(), Encoding.UTF8);
            }

            // 2. Update ReShade.ini
            string reshadeIniPath = Path.Combine(gameDir, "ReShade.ini");
            if (File.Exists(reshadeIniPath))
            {
                string text = File.ReadAllText(reshadeIniPath);
                text = UpdateIniKey(text, "RENODX-DLSS-preset1", "DirectNeuralRenderingPassCount", settings.NeuralPass1Iterations.ToString());
                text = UpdateIniKey(text, "RENODX-DLSS-preset1", "DirectNeuralRenderingIntensity", settings.NeuralPass1Intensity.ToString("0", CultureInfo.InvariantCulture));
                text = UpdateIniKey(text, "RENODX-DLSS-preset1", "DirectNeuralRenderingStyle", settings.NeuralPass1Style.ToString());
                text = UpdateIniKey(text, "RenoDX.DLSS5", "NRDiffuseWhiteNits", settings.NeuralPass1DiffuseWhiteNits.ToString("0", CultureInfo.InvariantCulture));
                File.WriteAllText(reshadeIniPath, text, Encoding.UTF8);
            }

            // 3. Update OptiScaler.ini
            string optiIniPath = Path.Combine(gameDir, "OptiScaler.ini");
            if (File.Exists(optiIniPath))
            {
                string text = File.ReadAllText(optiIniPath);
                text = UpdateIniKey(text, "DlssNr", "Passes", settings.DlssNrPasses.ToString());
                text = UpdateIniKey(text, "DlssNr", "TransferStrength", settings.DlssNrTransferStrength.ToString("0.000000", CultureInfo.InvariantCulture));
                text = UpdateIniKey(text, "DlssNr", "LocalStructure", settings.DlssNrLocalStructure.ToString("0.000000", CultureInfo.InvariantCulture));
                text = UpdateIniKey(text, "DlssNr", "SkinStructure", settings.DlssNrSkinStructure.ToString("0.000000", CultureInfo.InvariantCulture));
                text = UpdateIniKey(text, "DlssNr", "Intensity", settings.DlssNrIntensity.ToString("0.000000", CultureInfo.InvariantCulture));
                text = UpdateIniKey(text, "UpscaleRatio", "UpscaleRatioOverrideValue", (1.0f / settings.DownscaleRatio).ToString("0.000000", CultureInfo.InvariantCulture));
                File.WriteAllText(optiIniPath, text, Encoding.UTF8);
            }

            // 4. Update dlss5-bridge.cfg
            string bridgeCfgPath = Path.Combine(gameDir, "dlss5-bridge.cfg");
            if (File.Exists(bridgeCfgPath))
            {
                string text = File.ReadAllText(bridgeCfgPath);
                text = Regex.Replace(text, @"^vk_mirror\s*=.*$", $"vk_mirror={(settings.VkMirror ? 1 : 0)}", RegexOptions.Multiline);
                text = Regex.Replace(text, @"^stage\s*=.*$", $"stage={settings.BridgeStage}", RegexOptions.Multiline);
                File.WriteAllText(bridgeCfgPath, text, Encoding.UTF8);
            }

            // 5. Generate shaders if shader dir exists
            string shaderDir = Path.Combine(gameDir, "reshade-shaders", "Shaders");
            if (Directory.Exists(shaderDir))
            {
                ShaderGenerator.GenerateChainShaders(shaderDir, settings.PreChainCycles, settings.PostChainCycles, 
                                                    settings.DownscaleRatio, settings.UpscaleRatio, settings.VramAutoBalance);
            }

            // 6. Save complete pipeline layers metadata
            try
            {
                string layersJsonPath = Path.Combine(gameDir, "pipeline_layers.json");
                string json = System.Text.Json.JsonSerializer.Serialize(layers, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(layersJsonPath, json, Encoding.UTF8);
            }
            catch { }
        }

        private static string UpdateIniKey(string text, string section, string key, string value)
        {
            var pattern = $@"(\[{Regex.Escape(section)}\][\s\S]*?^\s*{Regex.Escape(key)}\s*=)[^\r\n]*";
            if (Regex.IsMatch(text, pattern, RegexOptions.Multiline))
            {
                return Regex.Replace(text, pattern, $"$1 {value}", RegexOptions.Multiline);
            }
            return text;
        }
    }
}
