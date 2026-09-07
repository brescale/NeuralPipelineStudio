using System;
using System.IO;
using System.Text;

namespace NeuralPipelineStudio.Core
{
    public static class ShaderGenerator
    {
        public static void GenerateChainShaders(string targetShaderDir, int preCycles, int postCycles, float downRatio, float upRatio, bool vramOptimized)
        {
            if (!Directory.Exists(targetShaderDir)) return;

            string preCode = GenerateCleanChainCode(true);
            string postCode = GenerateCleanChainCode(false);

            string prePath = Path.Combine(targetShaderDir, "lumenite_IterativeDownUpChain_Pre.fx");
            string postPath = Path.Combine(targetShaderDir, "lumenite_IterativeDownUpChain.fx");

            File.WriteAllText(prePath, preCode, Encoding.UTF8);
            File.WriteAllText(postPath, postCode, Encoding.UTF8);
        }

        private static string GenerateCleanChainCode(bool isPre)
        {
            string prefix = isPre ? "Pre" : "Post";
            string chainName = isPre ? "Pre-Downscale Fidelity Chain" : "Post-Neural Output Fidelity Chain";
            string techName = isPre ? "Lumenite_IterativeDownUpChain_Pre" : "Lumenite_IterativeDownUpChain";

            var sb = new StringBuilder();
            sb.AppendLine("/*");
            sb.AppendLine($"    lumenite_IterativeDownUpChain_{(isPre ? "Pre" : "")}.fx");
            sb.AppendLine("    Neural Pipeline Studio - Clean High-Fidelity Direct Pipeline");
            sb.AppendLine("    (Pristine single-pass processing: Zero ping-pong buffer degradation)");
            sb.AppendLine("*/");
            sb.AppendLine();
            sb.AppendLine("#include \"ReShade.fxh\"");
            sb.AppendLine();
            sb.AppendLine("// UI Controls");
            sb.AppendLine($"uniform float CYCLE_CONTRAST < ui_type = \"slider\"; ui_min = 0.8; ui_max = 1.3; ui_step = 0.005; ui_label = \"{chainName} Contrast\"; > = 1.00;");
            sb.AppendLine($"uniform float CYCLE_CLARITY < ui_type = \"slider\"; ui_min = 0.0; ui_max = 1.0; ui_step = 0.01; ui_label = \"{chainName} Clarity\"; > = {(isPre ? "0.15" : "0.10")};");
            sb.AppendLine($"uniform float CYCLE_SHARPNESS < ui_type = \"slider\"; ui_min = 0.0; ui_max = 1.0; ui_step = 0.01; ui_label = \"{chainName} Sharpness\"; > = {(isPre ? "0.20" : "0.25")};");
            sb.AppendLine();

            if (isPre)
            {
                sb.AppendLine("// Single-Pass Pre-Downscale Conditioning: Micro-Clarity & High-Frequency Edge Lock");
                sb.AppendLine("float4 PS_PreChain_Main(float4 vpos : SV_Position, float2 uv : TEXCOORD) : SV_Target");
                sb.AppendLine("{");
                sb.AppendLine("    float4 col = tex2D(ReShade::sBackBuffer, uv);");
                sb.AppendLine("    float2 tx = ReShade::PixelSize;");
                sb.AppendLine("    float4 blur = (");
                sb.AppendLine("        tex2D(ReShade::sBackBuffer, uv + float2( tx.x, 0)) +");
                sb.AppendLine("        tex2D(ReShade::sBackBuffer, uv + float2(-tx.x, 0)) +");
                sb.AppendLine("        tex2D(ReShade::sBackBuffer, uv + float2(0,  tx.y)) +");
                sb.AppendLine("        tex2D(ReShade::sBackBuffer, uv + float2(0, -tx.y))");
                sb.AppendLine("    ) * 0.25;");
                sb.AppendLine("    float4 delta = col - blur;");
                sb.AppendLine("    col += delta * (CYCLE_CLARITY * 0.5);");
                sb.AppendLine("    col.rgb = lerp(float3(0.5, 0.5, 0.5), col.rgb, CYCLE_CONTRAST);");
                sb.AppendLine("    return saturate(col);");
                sb.AppendLine("}");
                sb.AppendLine();
                sb.AppendLine($"technique {techName}");
                sb.AppendLine("{");
                sb.AppendLine("    pass Pre_Direct_Filter");
                sb.AppendLine("    {");
                sb.AppendLine("        VertexShader = PostProcessVS;");
                sb.AppendLine("        PixelShader = PS_PreChain_Main;");
                sb.AppendLine("    }");
                sb.AppendLine("}");
            }
            else
            {
                sb.AppendLine("// Single-Pass Post-Neural Spline & Contrast Consolidation");
                sb.AppendLine("float4 PS_PostChain_Main(float4 vpos : SV_Position, float2 uv : TEXCOORD) : SV_Target");
                sb.AppendLine("{");
                sb.AppendLine("    float2 tx = ReShade::PixelSize;");
                sb.AppendLine("    float4 center = tex2D(ReShade::sBackBuffer, uv);");
                sb.AppendLine("    float4 n = tex2D(ReShade::sBackBuffer, uv - float2(0, tx.y));");
                sb.AppendLine("    float4 s = tex2D(ReShade::sBackBuffer, uv + float2(0, tx.y));");
                sb.AppendLine("    float4 w = tex2D(ReShade::sBackBuffer, uv - float2(tx.x, 0));");
                sb.AppendLine("    float4 e = tex2D(ReShade::sBackBuffer, uv + float2(tx.x, 0));");
                sb.AppendLine("    float4 sharp = center * (1.0 + 4.0 * CYCLE_SHARPNESS * 0.2) - (n + s + w + e) * (CYCLE_SHARPNESS * 0.2);");
                sb.AppendLine("    float4 result = lerp(center, sharp, saturate(CYCLE_SHARPNESS));");
                sb.AppendLine("    result.rgb = lerp(float3(0.5, 0.5, 0.5), result.rgb, CYCLE_CONTRAST);");
                sb.AppendLine("    return saturate(result);");
                sb.AppendLine("}");
                sb.AppendLine();
                sb.AppendLine($"technique {techName}");
                sb.AppendLine("{");
                sb.AppendLine("    pass Post_Direct_Spline");
                sb.AppendLine("    {");
                sb.AppendLine("        VertexShader = PostProcessVS;");
                sb.AppendLine("        PixelShader = PS_PostChain_Main;");
                sb.AppendLine("    }");
                sb.AppendLine("}");
            }

            return sb.ToString();
        }
    }
}
