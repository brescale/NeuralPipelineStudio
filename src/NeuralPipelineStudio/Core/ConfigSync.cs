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
                    Description = "Preserva micro-contrasto e bordi geometrici ad alta frequenza sul frame nativo prima del downscaling" 
                },
                new PipelineLayer 
                { 
                    Id = "pre_chain", 
                    Name = $"{s.PreChainCycles}x Pre-Neural Resolution & Fidelity Chain", 
                    TechniqueName = "Lumenite_IterativeDownUpChain_Pre", 
                    ShaderFile = "lumenite_IterativeDownUpChain_Pre.fx", 
                    Category = "Pre-Downscale", 
                    Enabled = true, 
                    AccentColorHex = "#00F0FF", 
                    LoopCycles = s.PreChainCycles,
                    ScaleMode = "High-Fidelity Spline",
                    DownscaleRatio = s.DownscaleRatio,
                    UpscaleRatio = s.UpscaleRatio,
                    UpscaleModel = "Catmull-Rom Bicubic Spline",
                    DllModelName = "nvngx_dlss.dll",
                    AddonName = "ReShade64.dll",
                    Intensity = 1.0f,
                    Description = "Campionamento ad alta fedelta e pre-condizionamento nitidezza pre-upscale in pass diretto senza rimasticamento" 
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
                    DllModelName = "renodx-dlss.addon64",
                    AddonName = "renodx-dlss.addon64",
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
                    AddonName = "OptiScaler.dll",
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
                    AddonName = "ReShade64.dll",
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
                },
                new PipelineLayer 
                { 
                    Id = "post_chain", 
                    Name = $"{s.PostChainCycles}x Post-Neural Output Fidelity & Sharpening", 
                    TechniqueName = "Lumenite_IterativeDownUpChain", 
                    ShaderFile = "lumenite_IterativeDownUpChain.fx", 
                    Category = "Post-Chain", 
                    Enabled = true, 
                    AccentColorHex = "#00F0FF", 
                    LoopCycles = s.PostChainCycles,
                    ScaleMode = "High-Fidelity Spline",
                    DownscaleRatio = 0.85f,
                    UpscaleRatio = 1.18f,
                    UpscaleModel = "Catmull-Rom Bicubic Spline",
                    DllModelName = "sl.nis.dll",
                    AddonName = "ReShade64.dll",
                    Intensity = 1.0f,
                    Description = "Consolidamento contrasto e nitidezza con spline bicubica Catmull-Rom in pass diretto ad alta fedelta" 
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
                        foreach (var l in loaded)
                        {
                            if (l.AddonName == "renodx-dlss5.addon64") l.AddonName = "renodx-dlss.addon64";
                            if (l.DllModelName == "renodx-dlss5.addon64") l.DllModelName = "renodx-dlss.addon64";
                            if (l.Id == "pre_chain" && l.AddonName == "renodx-dlss.addon64") l.AddonName = "ReShade64.dll";
                        }

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
            string sortingLine = "TechniqueSorting=" + string.Join(",", activeTechs);

            if (File.Exists(presetPath))
            {
                string text = File.ReadAllText(presetPath);
                if (Regex.IsMatch(text, @"^Techniques\s*=", RegexOptions.Multiline))
                    text = Regex.Replace(text, @"^Techniques\s*=.*$", techniquesLine, RegexOptions.Multiline);
                else
                    text = techniquesLine + "\n" + text;

                if (Regex.IsMatch(text, @"^TechniqueSorting\s*=", RegexOptions.Multiline))
                    text = Regex.Replace(text, @"^TechniqueSorting\s*=.*$", sortingLine, RegexOptions.Multiline);
                else
                    text = text + "\n" + sortingLine;

                text = UpdateIniKey(text, "lumenite_MotionBlur.fx", "BLUR_SAMPLES", settings.MotionBlurSamples.ToString());
                text = UpdateIniKey(text, "lumenite_MotionBlur.fx", "BLUR_STRENGTH", settings.MotionBlurStrength.ToString("0.000000", CultureInfo.InvariantCulture));

                // Calibrated Anamorphic Bloom (excl. skybox to stop blinding haze)
                text = UpdateIniKey(text, "lumenite_AnamorphicBloom.fx", "BLOOM_INTENSITY", "0.080000");
                text = UpdateIniKey(text, "lumenite_AnamorphicBloom.fx", "BLOOM_THRESHOLD", "0.200000");
                text = UpdateIniKey(text, "lumenite_AnamorphicBloom.fx", "BLOOM_STRETCH", "4.000000");
                text = UpdateIniKey(text, "lumenite_AnamorphicBloom.fx", "BLOOM_SKIP_SKYBOX", "1");
                text = UpdateIniKey(text, "lumenite_AnamorphicBloom.fx", "STREAK_INTENSITY", "0.100000");
                text = UpdateIniKey(text, "lumenite_AnamorphicBloom.fx", "STREAK_THRESHOLD", "0.250000");
                text = UpdateIniKey(text, "lumenite_AnamorphicBloom.fx", "STREAK_SKIP_SKYBOX", "1");

                // Calibrated Specular Reflections (SSSR)
                text = UpdateIniKey(text, "lumenite_SSSR.fx", "ROUGHNESS", "0.250000");
                text = UpdateIniKey(text, "lumenite_SSSR.fx", "F0", "0.040000");
                text = UpdateIniKey(text, "lumenite_SSSR.fx", "BUMP_SCALE", "0.400000");
                text = UpdateIniKey(text, "lumenite_SSSR.fx", "DEPTH_BOUNDARY", "20.000000");
                text = UpdateIniKey(text, "lumenite_SSSR.fx", "TAIL_FEATHERING", "0.850000");

                // Calibrated Ambient Occlusion
                text = UpdateIniKey(text, "lumenite_RTAO.fx", "AO_INTENSITY", "1.000000");
                text = UpdateIniKey(text, "lumenite_RTAO.fx", "DEPTH_BOUNDARY", "25.000000");
                text = UpdateIniKey(text, "lumenite_LSAO.fx", "AO_INTENSITY", "0.750000");
                text = UpdateIniKey(text, "lumenite_LSAO.fx", "DEPTH_BOUNDARY", "50.000000");

                // Micro-contrast & Clarity
                text = UpdateIniKey(text, "lumenite_Pre_Stack.fx", "PRE_CONTRAST", "1.000000");
                text = UpdateIniKey(text, "lumenite_Pre_Stack.fx", "PRE_CLARITY", "0.150000");

                // Pre & Post High-Fidelity Spline & Sharpening
                text = UpdateIniKey(text, "lumenite_IterativeDownUpChain_Pre.fx", "CYCLE_CONTRAST", "1.000000");
                text = UpdateIniKey(text, "lumenite_IterativeDownUpChain_Pre.fx", "CYCLE_CLARITY", "0.150000");
                text = UpdateIniKey(text, "lumenite_IterativeDownUpChain_Pre.fx", "CYCLE_SHARPNESS", "0.200000");
                text = UpdateIniKey(text, "lumenite_IterativeDownUpChain.fx", "CYCLE_CONTRAST", "1.000000");
                text = UpdateIniKey(text, "lumenite_IterativeDownUpChain.fx", "CYCLE_CLARITY", "0.100000");
                text = UpdateIniKey(text, "lumenite_IterativeDownUpChain.fx", "CYCLE_SHARPNESS", "0.250000");

                File.WriteAllText(presetPath, text, Encoding.UTF8);
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine(techniquesLine);
                sb.AppendLine(sortingLine);
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
                // Strip obsolete RenoDX.DLSS5 section if present
                text = Regex.Replace(text, @"\[RenoDX\.DLSS5\][\s\S]*?(?=(\r?\n\[|\Z))", "", RegexOptions.Multiline);

                text = UpdateIniKey(text, "RENODX-DLSS", "DirectNeuralRenderingEncoding", "0"); // Auto BT.709 colorspace
                text = UpdateIniKey(text, "RENODX-DLSS", "DirectNeuralRenderingDiffuseWhiteOverride", "0"); // Auto diffuse white (100 nits SDR)
                text = UpdateIniKey(text, "RENODX-DLSS", "DirectNeuralRenderingDiffuseWhiteNits", "100");
                text = UpdateIniKey(text, "RENODX-DLSS", "DirectNeuralRenderingUiCorrectionMode", "0");
                text = UpdateIniKey(text, "RENODX-DLSS", "DLSSAutoExposure", "0");
                text = UpdateIniKey(text, "RENODX-DLSS-preset1", "DirectNeuralRenderingPassCount", "1");
                text = UpdateIniKey(text, "RENODX-DLSS-preset1", "DirectNeuralRenderingIntensity", "1.0");
                text = UpdateIniKey(text, "RENODX-DLSS-preset1", "DirectNeuralRenderingStyle", "0");
                text = UpdateIniKey(text, "RENODX-DLSS-preset1", "DirectNeuralRenderingGlobalToneStrength", "1");
                text = UpdateIniKey(text, "RENODX-DLSS-preset1", "DirectNeuralRenderingLocalToneStrength", "1");
                text = UpdateIniKey(text, "RENODX-DLSS-preset1", "DirectNeuralRenderingLocalStructureStrength", "1");
                text = UpdateIniKey(text, "RENODX-DLSS-preset1", "DirectNeuralRenderingSkinStructureStrength", "1");
                text = UpdateIniKey(text, "RENODX-DLSS-preset1", "DirectNeuralRenderingAutoMask", "1");
                File.WriteAllText(reshadeIniPath, text, Encoding.UTF8);
            }

            // 3. Update OptiScaler.ini with Universal Stability Guards & Heavy AI-Baked Parameters
            string optiIniPath = Path.Combine(gameDir, "OptiScaler.ini");
            if (File.Exists(optiIniPath))
            {
                string text = File.ReadAllText(optiIniPath);

                var hw = HardwareEngine.CurrentProfile;
                bool isVulkanGame = File.Exists(Path.Combine(gameDir, "vulkan-1.dll")) || 
                                    File.Exists(Path.Combine(gameDir, "RDR2.exe")) || 
                                    File.Exists(Path.Combine(gameDir, "NvLowLatencyVk.dll"));

                // STABILITY GUARDS: Safe loader integration across all games and APIs (Prevents crashes in all engines)
                text = UpdateIniKey(text, "Hotfix", "RestoreComputeSignature", "true");
                text = UpdateIniKey(text, "Hotfix", "RestoreGraphicSignature", "true");
                text = UpdateIniKey(text, "Hotfix", "PreferFirstDedicatedGpu", "true");
                text = UpdateIniKey(text, "Hotfix", "ManualInputPolling", "auto"); // 'auto' avoids fatal DirectInput8Create hook collision
                text = UpdateIniKey(text, "Hooks", "EarlyHooking", "auto");         // 'auto' avoids premature hook crashes
                text = UpdateIniKey(text, "Hooks", "UseNtdllHooks", "auto");        // 'auto' avoids loader deadlocks
                text = UpdateIniKey(text, "FrameGen", "SkipResizeBuffers", "true");
                text = UpdateIniKey(text, "FrameGen", "ModifyBufferState", "true");
                text = UpdateIniKey(text, "ProcessFilter", "TargetProcessName", "auto");
                text = UpdateIniKey(text, "ProcessFilter", "ProcessExclusionList", "auto");
                text = UpdateIniKey(text, "QualityOverrides", "QualityRatioOverrideEnabled", "false");
                text = UpdateIniKey(text, "DRS", "DrsMinOverrideEnabled", "false");
                text = UpdateIniKey(text, "DRS", "DrsMaxOverrideEnabled", "false");
                text = UpdateIniKey(text, "UpscaleRatio", "UpscaleRatioOverrideEnabled", "false");

                // API & DXGI SPOOFING GUARDS
                if (isVulkanGame)
                {
                    text = UpdateIniKey(text, "Spoofing", "Dxgi", "false"); // CRITICAL for Vulkan stability
                }
                else
                {
                    text = UpdateIniKey(text, "Spoofing", "Dxgi", "auto");
                }

                // Exposure & Contrast Control
                text = UpdateIniKey(text, "InitFlags", "AutoExposure", "auto"); // Prevents unnatural blowout in physical lighting engines

                // MULTI-GPU VENDOR HANDLING (NVIDIA RTX / GTX / AMD Radeon / Intel Arc)
                if (hw.SupportsTensorCores) // NVIDIA RTX
                {
                    text = UpdateIniKey(text, "Upscalers", "Dx12Upscaler", "dlss");
                    text = UpdateIniKey(text, "Upscalers", "VulkanUpscaler", "dlss");
                    text = UpdateIniKey(text, "DlssNr", "Enabled", "true");
                    text = UpdateIniKey(text, "DlssNr", "ApplyModel", "true");
                    text = UpdateIniKey(text, "DlssNr", "UseProxy", "true");
                    text = UpdateIniKey(text, "DlssNr", "ProxyProbe", "true");
                    text = UpdateIniKey(text, "DlssNr", "ReversibleMode", "4");
                    text = UpdateIniKey(text, "DlssNr", "Passes", settings.DlssNrPasses.ToString());
                    text = UpdateIniKey(text, "DlssNr", "TransferStrength", settings.DlssNrTransferStrength.ToString("0.000000", CultureInfo.InvariantCulture));
                    text = UpdateIniKey(text, "DlssNr", "ColourStrength", "1.000000");   // Standard color fidelity
                    text = UpdateIniKey(text, "DlssNr", "Style", "0");                   // Standard default style
                    text = UpdateIniKey(text, "DlssNr", "MaxRatio", "1.500000");         // Strictly caps luminance boost to 1.5x, eliminating all blown out ceilings
                    text = UpdateIniKey(text, "DlssNr", "WhitePointFromExposure", "false"); // Directly sample real frame luminance without engine exposure division
                    text = UpdateIniKey(text, "DlssNr", "WhitePointSource", "auto");
                    text = UpdateIniKey(text, "DlssNr", "WhitePointTrim", settings.DlssNrWhitePointTrim.ToString("0.000000", CultureInfo.InvariantCulture));
                    text = UpdateIniKey(text, "DlssNr", "WorkingScale", "auto");         // Model resolution adaptive to FPS
                    text = UpdateIniKey(text, "DlssNr", "Preset", "auto");               // Model preset adaptive
                    text = UpdateIniKey(text, "DlssNr", "ScalingDownscaler", "4");       // Lanczos3
                    text = UpdateIniKey(text, "DlssNr", "LocalStructure", settings.DlssNrLocalStructure.ToString("0.000000", CultureInfo.InvariantCulture));
                    text = UpdateIniKey(text, "DlssNr", "LocalTone", "1.000000");        // 1:1 neutral tone mapping (no white ceiling clipping)
                    text = UpdateIniKey(text, "DlssNr", "SkinStructure", settings.DlssNrSkinStructure.ToString("0.000000", CultureInfo.InvariantCulture));
                    text = UpdateIniKey(text, "DlssNr", "Intensity", settings.DlssNrIntensity.ToString("0.000000", CultureInfo.InvariantCulture));
                    text = UpdateIniKey(text, "DlssNr", "AutoMask", "true");             // Auto skin mask active
                }
                else if (hw.GpuVendor == "AMD") // AMD Radeon
                {
                    text = UpdateIniKey(text, "Upscalers", "Dx12Upscaler", "ffx");
                    text = UpdateIniKey(text, "Upscalers", "VulkanUpscaler", "ffx");
                    text = UpdateIniKey(text, "DlssNr", "Enabled", "false");
                }
                else if (hw.GpuVendor == "Intel") // Intel Arc
                {
                    text = UpdateIniKey(text, "Upscalers", "Dx12Upscaler", "xess");
                    text = UpdateIniKey(text, "Upscalers", "VulkanUpscaler", "ffx");
                    text = UpdateIniKey(text, "DlssNr", "Enabled", "false");
                }
                else // GTX / Non-RTX
                {
                    text = UpdateIniKey(text, "Upscalers", "Dx12Upscaler", "ffx");
                    text = UpdateIniKey(text, "Upscalers", "VulkanUpscaler", "ffx");
                    text = UpdateIniKey(text, "DlssNr", "Enabled", "false");
                }

                // Sharpness
                text = UpdateIniKey(text, "Sharpness", "Shader", "lcda");
                text = UpdateIniKey(text, "Sharpness", "OverrideSharpness", "true");
                text = UpdateIniKey(text, "Sharpness", "Sharpness", "0.650000");

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

            // 5. Generate high-fidelity clean chain shaders
            string shaderDir = Path.Combine(gameDir, "reshade-shaders", "Shaders");
            if (Directory.Exists(shaderDir))
            {
                ShaderGenerator.GenerateChainShaders(shaderDir, settings.PreChainCycles, settings.PostChainCycles, 
                                                    settings.DownscaleRatio, settings.UpscaleRatio, settings.VramAutoBalance);
                string oldDll = Path.Combine(shaderDir, "nvngx.dll_dlssnr.dll");
                try { if (File.Exists(oldDll)) File.Delete(oldDll); } catch { }
            }

            string fakeAddon = Path.Combine(gameDir, "renodx-dlss5.addon64");
            try { if (File.Exists(fakeAddon)) File.Delete(fakeAddon); } catch { }

            // 6. Save complete pipeline layers metadata
            try
            {
                string layersJsonPath = Path.Combine(gameDir, "pipeline_layers.json");
                string json = System.Text.Json.JsonSerializer.Serialize(layers, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(layersJsonPath, json, Encoding.UTF8);
            }
            catch { }
        }

        public static string UpdateIniKey(string text, string section, string key, string value)
        {
            var pattern = $@"(\[{Regex.Escape(section)}\][\s\S]*?^\s*{Regex.Escape(key)}\s*=)[^\r\n]*";
            if (Regex.IsMatch(text, pattern, RegexOptions.Multiline))
            {
                return Regex.Replace(text, pattern, $"$1 {value}", RegexOptions.Multiline);
            }
            var secPattern = $@"(\[{Regex.Escape(section)}\][\r\n]+)";
            if (Regex.IsMatch(text, secPattern))
            {
                return Regex.Replace(text, secPattern, $"$1{key} = {value}\r\n");
            }
            return text;
        }
    }
}
