using System;
using System.IO;

namespace NeuralPipelineStudio.Core
{
    public static class PayloadManager
    {
        public static string ResolvePayloadDirectory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // 1. Check for local payload folder (standalone zip extracted on Desktop or elsewhere)
            string localPayload = Path.Combine(baseDir, "payload");
            if (Directory.Exists(localPayload))
                return localPayload;

            // 2. Check if running inside or beside RDR2
            if (File.Exists(Path.Combine(baseDir, "RDR2.exe")) || Directory.Exists(Path.Combine(baseDir, "reshade-shaders")))
                return baseDir;

            string parent = Directory.GetParent(baseDir)?.FullName ?? "";
            if (!string.IsNullOrEmpty(parent) && Directory.Exists(Path.Combine(parent, "reshade-shaders")))
                return parent;

            // 3. Fallback to primary production path
            string defaultMaster = @"E:\SteamLibrary\steamapps\common\Red Dead Redemption 2";
            if (Directory.Exists(defaultMaster))
                return defaultMaster;

            return baseDir;
        }

        public static string ResolveDlss5Directory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // 1. Local payload dlss5 folder
            string localDlss = Path.Combine(baseDir, "payload", "dlss5");
            if (Directory.Exists(localDlss)) return localDlss;

            string localDlss2 = Path.Combine(baseDir, "dlss5");
            if (Directory.Exists(localDlss2)) return localDlss2;

            // 2. Extracted models folder
            string ext = @"E:\dssl5\dlss5_extracted";
            if (Directory.Exists(ext)) return ext;

            return Dlss5ModelManager.DetectDlss5Folder();
        }
    }
}