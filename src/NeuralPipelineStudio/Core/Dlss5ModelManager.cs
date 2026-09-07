using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeuralPipelineStudio.Models;

namespace NeuralPipelineStudio.Core
{
    public class DlssModelInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string Category { get; set; } = "DLSS5";
        public string Description { get; set; } = string.Empty;
        public long SizeBytes { get; set; } = 0;
        public string SizeDisplay => $"{(SizeBytes / (1024.0 * 1024.0)):F2} MB";
        public string FriendlyName => Path.GetFileNameWithoutExtension(FileName);

        public bool IsInstalledInGame(string gameDir)
        {
            if (string.IsNullOrEmpty(gameDir) || !Directory.Exists(gameDir)) return false;
            return File.Exists(Path.Combine(gameDir, FileName));
        }
    }

    public static class Dlss5ModelManager
    {
        private static readonly string[] SearchFolders = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "payload", "dlss5"),
            Path.Combine(AppContext.BaseDirectory, "payload"),
            @"E:\dssl5\dlss5_extracted",
            @"E:\dlss5\dlss5extracted",
            @"E:\dlss5_extracted",
            @"E:\dssl5",
            @"E:\dlss5",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NeuralPipelineStudio_v2.0_Portable", "payload", "dlss5"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "agent", "dlss5"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "DLSS.5.Visual.Enhancer.v7.0", "bin", "runtime")
        };

        public static string DetectDlss5Folder(string gameDir = "")
        {
            if (!string.IsNullOrEmpty(gameDir))
            {
                string localPayloadDlss = Path.Combine(gameDir, "payload", "dlss5");
                if (Directory.Exists(localPayloadDlss)) return localPayloadDlss;

                string localDlss = Path.Combine(gameDir, "dlss5");
                if (Directory.Exists(localDlss)) return localDlss;
            }

            foreach (var folder in SearchFolders)
            {
                if (Directory.Exists(folder))
                    return folder;
            }

            return @"E:\dssl5\dlss5_extracted";
        }

        public static List<DlssModelInfo> DiscoverModels(string folder)
        {
            var list = new List<DlssModelInfo>();
            if (!Directory.Exists(folder)) return list;

            var files = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                                 .Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".addon64", StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                string name = Path.GetFileName(file);
                var fi = new FileInfo(file);
                string cat = DetermineCategory(name);
                string desc = DetermineDescription(name);

                list.Add(new DlssModelInfo
                {
                    FileName = name,
                    FullPath = file,
                    Category = cat,
                    Description = desc,
                    SizeBytes = fi.Length
                });
            }

            return list;
        }

        public static List<string> GetAvailableShaders(string gameDir = "")
        {
            var shaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string[] knownShaders = new[]
            {
                "lumenite_IterativeDownUpChain_Pre.fx",
                "lumenite_Pre_Stack.fx",
                "lumenite_Kernel.fx",
                "DLSS5_Feed.fx",
                "RenoDX_DLSS5",
                "OptiScaler_DLSS",
                "OptiScaler_DLSSNR",
                "lumenite_RTAO.fx",
                "lumenite_LSAO.fx",
                "lumenite_SSSR.fx",
                "lumenite_AnamorphicBloom.fx",
                "lumenite_TRAA.fx",
                "lumenite_MotionBlur.fx",
                "lumenite_IterativeDownUpChain.fx",
                "lumenite_QuantAO.fx",
                "lumenite_QuantMotion.fx"
            };
            foreach (var ks in knownShaders) shaders.Add(ks);

            var scanDirs = new List<string>();
            if (!string.IsNullOrEmpty(gameDir))
            {
                scanDirs.Add(Path.Combine(gameDir, "reshade-shaders", "Shaders"));
                scanDirs.Add(Path.Combine(gameDir, "payload", "reshade-shaders", "Shaders"));
            }
            scanDirs.Add(Path.Combine(AppContext.BaseDirectory, "payload", "reshade-shaders", "Shaders"));
            scanDirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NeuralPipelineStudio_v2.0_Portable", "payload", "reshade-shaders", "Shaders"));

            foreach (var dir in scanDirs)
            {
                if (Directory.Exists(dir))
                {
                    foreach (var f in Directory.GetFiles(dir, "*.fx", SearchOption.TopDirectoryOnly))
                    {
                        shaders.Add(Path.GetFileName(f));
                    }
                }
            }

            return shaders.OrderBy(s => s).ToList();
        }

        public static List<string> GetAvailableAddons(string gameDir = "")
        {
            var addons = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "renodx-dlss5.addon64",
                "renodx-dlss.addon64",
                "OptiScaler.dll",
                "ReShade64.dll",
                "dxgi.dll"
            };

            var scanDirs = new List<string>();
            if (!string.IsNullOrEmpty(gameDir))
            {
                scanDirs.Add(gameDir);
                scanDirs.Add(Path.Combine(gameDir, "payload"));
                scanDirs.Add(Path.Combine(gameDir, "payload", "dlss5"));
            }
            scanDirs.Add(Path.Combine(AppContext.BaseDirectory, "payload"));
            scanDirs.Add(Path.Combine(AppContext.BaseDirectory, "payload", "dlss5"));
            scanDirs.Add(@"E:\dssl5\dlss5_extracted");

            foreach (var dir in scanDirs)
            {
                if (Directory.Exists(dir))
                {
                    foreach (var f in Directory.GetFiles(dir, "*.addon*", SearchOption.TopDirectoryOnly))
                    {
                        addons.Add(Path.GetFileName(f));
                    }
                }
            }

            return addons.OrderBy(a => a).ToList();
        }

        public static List<string> GetAvailableModels(string dlssFolder = "")
        {
            if (string.IsNullOrEmpty(dlssFolder) || !Directory.Exists(dlssFolder))
                dlssFolder = DetectDlss5Folder();

            var models = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "nvngx_dlss.dll",
                "nvngx.dll_dlssnr.dll",
                "nvngx_dlssd.dll",
                "nvngx_dlssg.dll",
                "sl.dlss.dll",
                "sl.dlss_d.dll",
                "sl.dlss_g.dll",
                "sl.dlss_nr.dll",
                "sl.interposer.dll",
                "sl.deepdvc.dll",
                "sl.nis.dll",
                "sl.reflex.dll",
                "sl.common.dll"
            };

            if (Directory.Exists(dlssFolder))
            {
                foreach (var f in Directory.GetFiles(dlssFolder, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    models.Add(Path.GetFileName(f));
                }
            }

            return models.OrderBy(m => m).ToList();
        }

        private static string DetermineCategory(string name)
        {
            string l = name.ToLowerInvariant();
            if (l.Contains("dlssnr") || l.Contains("dlss_nr")) return "Neural Denoising";
            if (l.Contains("dlssd") || l.Contains("dlss_d")) return "Ray Reconstruction";
            if (l.Contains("dlssg") || l.Contains("dlss_g")) return "Frame Generation";
            if (l.Contains("renodx")) return "HDR Tensor Engine";
            if (l.Contains("nis")) return "Spatial Upscaler";
            if (l.Contains("deepdvc")) return "Neural Vibrance";
            if (l.Contains("reflex") || l.Contains("pcl")) return "Latency Reduction";
            if (l.Contains("dlss")) return "Super Resolution";
            return "Streamline Module";
        }

        private static string DetermineDescription(string name)
        {
            string l = name.ToLowerInvariant();
            if (l.Contains("dlssnr") || l.Contains("dlss_nr")) return "DLSS-NR Reconstruction: 100% Synthetic Layer Denoising with zero film grain";
            if (l.Contains("dlssd") || l.Contains("dlss_d")) return "DLSS Ray Reconstruction: Deep Direct neural lighting & temporal stability";
            if (l.Contains("dlssg") || l.Contains("dlss_g")) return "DLSS Frame Generation: Optical multi-frame optical flow synthesis";
            if (l.Contains("renodx")) return "RenoDX DLSS5 HDR Engine: Direct volume tensor passes & HDR mastering";
            if (l.Contains("nis")) return "NVIDIA Image Scaling: High-performance spatial sharpness & upscaler";
            if (l.Contains("deepdvc")) return "Deep Dynamic Vibrance: Neural contrast and clarity preservation";
            if (l.Contains("reflex")) return "NVIDIA Reflex: Low latency driver pipeline alignment";
            if (l.Contains("interposer")) return "Streamline Interposer: Cross-API Vulkan/DX12 dispatch bridge";
            if (l.Contains("dlss")) return "DLSS Super Resolution: Ultra Quality temporal reconstruction up to 1500%";
            return "NVIDIA Streamline framework support component";
        }

        public static List<PipelineLayer> GetAvailableLayerTemplates(string dlssFolder)
        {
            var models = DiscoverModels(dlssFolder);
            var templates = new List<PipelineLayer>
            {
                new PipelineLayer
                {
                    Id = "dlss_sr",
                    Name = "DLSS Super Resolution (nvngx_dlss.dll)",
                    TechniqueName = "DLSS_Super_Resolution",
                    ShaderFile = "OptiScaler_DLSS",
                    Category = "Super Resolution",
                    DllModelName = models.FirstOrDefault(m => m.FileName.Equals("nvngx_dlss.dll", StringComparison.OrdinalIgnoreCase))?.FileName ?? "nvngx_dlss.dll",
                    AddonName = "OptiScaler.dll",
                    ScaleMode = "DLSS",
                    ScaleRatio = 9.0f,
                    QualityPreset = "Preset F",
                    LoopCycles = 1,
                    AccentColorHex = "#2196F3",
                    Description = "DLSS Super Resolution reconstruction model (100% - 1500% scaling) guidato da OptiScaler"
                },
                new PipelineLayer
                {
                    Id = "dlss_nr_denoiser",
                    Name = "DLSS-NR Neural Reconstruction (nvngx.dll_dlssnr.dll)",
                    TechniqueName = "Neural_Pass_2_DLSS_NR",
                    ShaderFile = "OptiScaler_DLSSNR",
                    Category = "Neural Engine",
                    DllModelName = models.FirstOrDefault(m => m.FileName.Contains("dlssnr"))?.FileName ?? "nvngx.dll_dlssnr.dll",
                    AddonName = "renodx-dlss5.addon64",
                    ScaleMode = "None",
                    Intensity = 1.0f,
                    LoopCycles = 15,
                    AccentColorHex = "#673AB7",
                    Description = "DLSS-NR neural denoiser pass at 15 iterations with 100% synthesized layer"
                },
                new PipelineLayer
                {
                    Id = "renodx_tensor",
                    Name = "RenoDX DLSS5 HDR Tensor Engine (renodx-dlss5.addon64)",
                    TechniqueName = "Neural_Pass_1_RenoDX",
                    ShaderFile = "RenoDX_DLSS5",
                    Category = "Neural Engine",
                    DllModelName = models.FirstOrDefault(m => m.FileName.Contains("renodx"))?.FileName ?? "renodx-dlss5.addon64",
                    AddonName = "renodx-dlss5.addon64",
                    ScaleMode = "None",
                    Intensity = 2.0f,
                    LoopCycles = 30,
                    AccentColorHex = "#9C27B0",
                    Description = "1° Neural Pass RenoDX tensor engine with enhanced volumetric illumination"
                },
                new PipelineLayer
                {
                    Id = "pre_downscale_chain",
                    Name = "Ping-Pong Rescale Loop (Downscale 0.70x ⇄ Upscale 1.42x)",
                    TechniqueName = "Lumenite_IterativeDownUpChain_Pre",
                    ShaderFile = "lumenite_IterativeDownUpChain_Pre.fx",
                    Category = "Ping-Pong Rescale",
                    ScaleMode = "Ping-Pong (Down ⇄ Up)",
                    DownscaleRatio = 0.70f,
                    UpscaleRatio = 1.428571f,
                    UpscaleModel = "DLSS 4/4.5 (nvngx_dlss.dll)",
                    DllModelName = "nvngx_dlss.dll",
                    AddonName = "renodx-dlss5.addon64",
                    LoopCycles = 10,
                    AccentColorHex = "#00F0FF",
                    Description = "Ciclo iterativo Ping-Pong alternato tra Downscale (0.70x) e Upscale (1.42x o DLSS) con riciclo buffer"
                },
                new PipelineLayer
                {
                    Id = "pre_stack",
                    Name = "Lumenite Pre-Downscale Stack (Edge & Contrast Lock)",
                    TechniqueName = "Lumenite_Pre_Stack",
                    ShaderFile = "lumenite_Pre_Stack.fx",
                    Category = "Pre-Downscale",
                    DllModelName = "sl.interposer.dll",
                    AddonName = "ReShade64.dll",
                    LoopCycles = 1,
                    AccentColorHex = "#4CAF50",
                    Description = "Preserva micro-contrasto e bordi geometrici ad alta frequenza prima del downscaling"
                },
                new PipelineLayer
                {
                    Id = "kernel",
                    Name = "Lumenite Optical Flow Velocity Kernel (@ 0.70x)",
                    TechniqueName = "Lumenite_Kernel",
                    ShaderFile = "lumenite_Kernel.fx",
                    Category = "Optical Flow",
                    DllModelName = "sl.interposer.dll",
                    AddonName = "OptiScaler.dll",
                    LoopCycles = 1,
                    AccentColorHex = "#FF9800",
                    Description = "Stima vettoriale del moto GPU e buffer di flusso ottico temporale per il bridge neurale"
                },
                new PipelineLayer
                {
                    Id = "feed",
                    Name = "DLSS 5.0 Guide Feed & Depth Interposer",
                    TechniqueName = "DLSS5_Feed",
                    ShaderFile = "DLSS5_Feed.fx",
                    Category = "Guide Feed",
                    DllModelName = "sl.common.dll",
                    AddonName = "OptiScaler.dll",
                    LoopCycles = 1,
                    AccentColorHex = "#E91E63",
                    Description = "Validazione luma/depth, geometric agreement e maschera di transizione per il bridge neurale"
                },
                new PipelineLayer
                {
                    Id = "sl_ray_reconstruct",
                    Name = "Streamline Ray Reconstruction (sl.dlss_d.dll)",
                    TechniqueName = "Streamline_DLSS_D",
                    ShaderFile = "sl.dlss_d.dll",
                    Category = "Ray Reconstruction",
                    DllModelName = "sl.dlss_d.dll",
                    AddonName = "OptiScaler.dll",
                    LoopCycles = 1,
                    AccentColorHex = "#00E676",
                    Description = "Direct neural ray reconstruction for reflections and bounce lighting"
                },
                new PipelineLayer
                {
                    Id = "sl_optical_flow",
                    Name = "Streamline Optical Flow & Interposer (sl.interposer.dll)",
                    TechniqueName = "Streamline_Interposer",
                    ShaderFile = "sl.interposer.dll",
                    Category = "Optical Flow",
                    DllModelName = "sl.interposer.dll",
                    AddonName = "OptiScaler.dll",
                    LoopCycles = 1,
                    AccentColorHex = "#FF9800",
                    Description = "Optical flow motion estimation and frame timing interposer"
                },
                new PipelineLayer
                {
                    Id = "sl_vibrance",
                    Name = "Streamline Deep Dynamic Vibrance (sl.deepdvc.dll)",
                    TechniqueName = "Streamline_DeepDVC",
                    ShaderFile = "sl.deepdvc.dll",
                    Category = "Color & Vibrance",
                    DllModelName = "sl.deepdvc.dll",
                    AddonName = "renodx-dlss5.addon64",
                    LoopCycles = 1,
                    AccentColorHex = "#E91E63",
                    Description = "Deep dynamic range clarity and local tone curve optimization"
                },
                new PipelineLayer
                {
                    Id = "lumenite_rtao",
                    Name = "Lumenite RTAO (Ray Traced Ambient Occlusion)",
                    TechniqueName = "Lumenite_RTAO",
                    ShaderFile = "lumenite_RTAO.fx",
                    Category = "Post-Neural",
                    DllModelName = "nvngx_dlssd.dll",
                    AddonName = "ReShade64.dll",
                    LoopCycles = 1,
                    AccentColorHex = "#00BCD4",
                    Description = "Screen-space ray traced ambient occlusion pass"
                },
                new PipelineLayer
                {
                    Id = "lumenite_lsao",
                    Name = "Lumenite LSAO (Large Scale Ambient Occlusion)",
                    TechniqueName = "Lumenite_LSAO",
                    ShaderFile = "lumenite_LSAO.fx",
                    Category = "Post-Neural",
                    DllModelName = "nvngx_dlssd.dll",
                    AddonName = "ReShade64.dll",
                    LoopCycles = 1,
                    AccentColorHex = "#009688",
                    Description = "Ombreggiatura e occlusione diffusa su larga scala"
                },
                new PipelineLayer
                {
                    Id = "lumenite_sssr",
                    Name = "Lumenite SSSR (Screen-Space Specular Reflections)",
                    TechniqueName = "LUMENITE_SSSR",
                    ShaderFile = "lumenite_SSSR.fx",
                    Category = "Post-Neural",
                    DllModelName = "sl.dlss_d.dll",
                    AddonName = "ReShade64.dll",
                    LoopCycles = 1,
                    AccentColorHex = "#3F51B5",
                    Description = "Temporal ray-marched specular reflections pass"
                },
                new PipelineLayer
                {
                    Id = "lumenite_bloom",
                    Name = "Lumenite Anamorphic Bloom (Diffrazione Spettrale)",
                    TechniqueName = "Lumenite_AnamorphicBloom",
                    ShaderFile = "lumenite_AnamorphicBloom.fx",
                    Category = "Post-Neural",
                    DllModelName = "sl.deepdvc.dll",
                    AddonName = "renodx-dlss5.addon64",
                    LoopCycles = 1,
                    AccentColorHex = "#FF5722",
                    Description = "Diffrazione ottica anamorfica e dispersione spettrale delle luci"
                },
                new PipelineLayer
                {
                    Id = "lumenite_traa",
                    Name = "Lumenite TRAA (Temporal Reconstruction Anti-Aliasing)",
                    TechniqueName = "Lumenite_TRAA",
                    ShaderFile = "lumenite_TRAA.fx",
                    Category = "Post-Neural",
                    DllModelName = "sl.dlss.dll",
                    AddonName = "ReShade64.dll",
                    LoopCycles = 1,
                    AccentColorHex = "#8BC34A",
                    Description = "Antialiasing di rifinitura con accumulo temporale sub-pixel"
                },
                new PipelineLayer
                {
                    Id = "lumenite_motion_blur",
                    Name = "Lumenite Cinematic Motion Blur (14 Samples)",
                    TechniqueName = "Lumenite_MotionBlur",
                    ShaderFile = "lumenite_MotionBlur.fx",
                    Category = "Post-Neural",
                    DllModelName = "nvngx_dlssg.dll",
                    AddonName = "OptiScaler.dll",
                    LoopCycles = 1,
                    AccentColorHex = "#CDDC39",
                    Description = "Velocity-buffer guided cinematic shutter motion blur"
                },
                new PipelineLayer
                {
                    Id = "post_neural_chain",
                    Name = "Post-Neural Output Iterative Chain (10x Cycles)",
                    TechniqueName = "Lumenite_IterativeDownUpChain",
                    ShaderFile = "lumenite_IterativeDownUpChain.fx",
                    Category = "Post-Chain",
                    DllModelName = "sl.nis.dll",
                    AddonName = "ReShade64.dll",
                    ScaleMode = "Upscale",
                    ScaleRatio = 6.0f,
                    LoopCycles = 10,
                    AccentColorHex = "#00F0FF",
                    Description = "Post-neural contrast and sharpness consolidation chain"
                },
                new PipelineLayer
                {
                    Id = "quant_ao",
                    Name = "Lumenite QuantAO (Quantized Ambient Occlusion)",
                    TechniqueName = "Lumenite_QuantAO",
                    ShaderFile = "lumenite_QuantAO.fx",
                    Category = "Post-Neural",
                    DllModelName = "nvngx_dlssd.dll",
                    AddonName = "ReShade64.dll",
                    LoopCycles = 1,
                    AccentColorHex = "#00BCD4",
                    Description = "Quantized ambient occlusion pass for ultra-fast depth ambient shading"
                },
                new PipelineLayer
                {
                    Id = "quant_motion",
                    Name = "Lumenite QuantMotion (Quantized Motion Estimation)",
                    TechniqueName = "Lumenite_QuantMotion",
                    ShaderFile = "lumenite_QuantMotion.fx",
                    Category = "Optical Flow",
                    DllModelName = "nvngx_dlssg.dll",
                    AddonName = "OptiScaler.dll",
                    LoopCycles = 1,
                    AccentColorHex = "#FF9800",
                    Description = "Subpixel quantized velocity vector approximation pass"
                }
            };

            return templates;
        }

        public static bool DeployModelToGame(string modelPath, string targetGameDir, out string error)
        {
            error = string.Empty;
            try
            {
                if (!File.Exists(modelPath) || !Directory.Exists(targetGameDir))
                {
                    error = "Source file or target directory does not exist.";
                    return false;
                }

                string dest = Path.Combine(targetGameDir, Path.GetFileName(modelPath));
                File.Copy(modelPath, dest, true);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}