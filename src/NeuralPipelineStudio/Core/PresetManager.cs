using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NeuralPipelineStudio.Models;

namespace NeuralPipelineStudio.Core
{
    public class PresetData
    {
        public string PresetName { get; set; } = "Default Preset";
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = "Community";
        public string TargetHardware { get; set; } = "Universal";
        public NeuralPipelineSettings Settings { get; set; } = new();
        public string FilePath { get; set; } = string.Empty;

        public override string ToString() => PresetName;
    }

    public static class PresetManager
    {
        public static List<PresetData> LoadAllPresets(string presetsDirectory)
        {
            var list = new List<PresetData>();

            if (!Directory.Exists(presetsDirectory))
                return list;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var file in Directory.GetFiles(presetsDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var preset = JsonSerializer.Deserialize<PresetData>(json, options);
                    if (preset != null && preset.Settings != null)
                    {
                        preset.FilePath = file;
                        list.Add(preset);
                    }
                }
                catch { }
            }

            return list;
        }

        public static bool SavePreset(string filePath, PresetData preset)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(preset, options);
                File.WriteAllText(filePath, json);
                preset.FilePath = filePath;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
