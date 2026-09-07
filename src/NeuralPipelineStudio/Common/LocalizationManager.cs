using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace NeuralPipelineStudio.Common
{
    public enum AppLanguage
    {
        Italian,
        English
    }

    public static class LocalizationManager
    {
        private static AppLanguage _currentLanguage = AppLanguage.English;
        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "studio_settings.json");

        public static event Action? LanguageChanged;

        public static AppLanguage CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    SaveLanguagePreference(value);
                    LanguageChanged?.Invoke();
                }
            }
        }

        static LocalizationManager()
        {
            _currentLanguage = LoadLanguagePreference();
        }

        public static string Get(string key)
        {
            if (Strings.TryGetValue(key, out var dict))
            {
                if (dict.TryGetValue(_currentLanguage, out var val))
                    return val;
                if (dict.TryGetValue(AppLanguage.English, out var fallback))
                    return fallback;
            }
            return key;
        }

        public static string GetLayerDescription(string layerName, AppLanguage lang)
        {
            string lwr = (layerName ?? string.Empty).ToLowerInvariant();

            if (lang == AppLanguage.English)
            {
                if (lwr.Contains("downscale") && (lwr.Contains("ping-pong") || lwr.Contains("10x") || lwr.Contains("chain")))
                    return "10 alternating Ping-Pong cycles: Downscale 0.70x (70%) -> intermediate ReShade filtering -> Upscale with neural model. Employs dual recycled RGBA8 buffers (zero VRAM overhead).";
                if (lwr.Contains("pre-downscale") || lwr.Contains("edge"))
                    return "Preserves high-frequency geometric edges and sub-pixel micro-contrast before internal downscaling passes.";
                if (lwr.Contains("optical flow") || lwr.Contains("lumenite optical"))
                    return "GPU motion vector estimation and temporal optical flow velocity buffers for the neural bridge.";
                if (lwr.Contains("guide feed") || lwr.Contains("interposer"))
                    return "Luma/depth validation, geometric motion agreement, and jitter transition mask for the neural reconstruction engine.";
                if (lwr.Contains("renodx") || lwr.Contains("1° neural") || lwr.Contains("1 neural"))
                    return "Pre-upscale neural synthesis with 30 tensor iterations and volumetric HDR diffuse white point calibration (498 nits).";
                if (lwr.Contains("super resolution") || lwr.Contains("dlss super"))
                    return "Ultra-dense sub-pixel temporal reconstruction (900% Super Resolution, Preset F/G) guided by motion vectors.";
                if (lwr.Contains("dlss-nr") || lwr.Contains("2° neural") || lwr.Contains("2 neural") || lwr.Contains("denoiser"))
                    return "Neural denoising across 15 passes with 100% synthesized layer transfer, preserving delicate skin, metal, and foliage structures.";
                if (lwr.Contains("rtao"))
                    return "Real-time screen-space ray-traced ambient occlusion with depth-guided ray marching.";
                if (lwr.Contains("lsao"))
                    return "Large-scale screen-space ambient obscurance and broad diffuse contact shadowing.";
                if (lwr.Contains("sssr"))
                    return "Temporal ray-marched specular reflections with roughness attenuation and sub-pixel tracing.";
                if (lwr.Contains("bloom"))
                    return "Anamorphic optical diffraction and high-luminance spectral light dispersal.";
                if (lwr.Contains("traa"))
                    return "Temporal reconstruction anti-aliasing with sub-pixel jitter accumulation.";
                if (lwr.Contains("motion blur"))
                    return "Velocity-buffer guided cinematic shutter motion blur with 14 velocity-aligned samples.";
                if (lwr.Contains("post-neural") || lwr.Contains("output consolidation"))
                    return "Final 10x alternating Ping-Pong output consolidation: Downscale 0.85x -> ReShade tone filtering -> Upscale 1.18x for micro-contrast lock.";
                return "Configured GPU neural pipeline processing stage.";
            }
            else
            {
                if (lwr.Contains("downscale") && (lwr.Contains("ping-pong") || lwr.Contains("10x") || lwr.Contains("chain")))
                    return "10 cicli alternati Ping-Pong: Downscale 0.70x (70%) -> ReShade intermedio -> Upscale con modello neurale. Impiega buffer doppi riciclati RGBA8 (zero VRAM overhead).";
                if (lwr.Contains("pre-downscale") || lwr.Contains("edge"))
                    return "Preserva micro-contrasto e bordi geometrici ad alta frequenza prima del downscaling interno.";
                if (lwr.Contains("optical flow") || lwr.Contains("lumenite optical"))
                    return "Stima vettoriale del moto GPU e buffer di flusso ottico temporale per il bridge neurale.";
                if (lwr.Contains("guide feed") || lwr.Contains("interposer"))
                    return "Validazione luma/depth, geometric agreement e maschera di transizione per il bridge neurale.";
                if (lwr.Contains("renodx") || lwr.Contains("1° neural") || lwr.Contains("1 neural"))
                    return "Sintesi neurale pre-upscale a 30 iterazioni tensor con illuminazione volumetrica potenziata in spazio HDR (498 nits).";
                if (lwr.Contains("super resolution") || lwr.Contains("dlss super"))
                    return "Ricostruzione temporale sub-pixel ad altissima densit\u00E0 (900% Super Resolution, Preset F/G) guidata da motion vectors.";
                if (lwr.Contains("dlss-nr") || lwr.Contains("2° neural") || lwr.Contains("2 neural") || lwr.Contains("denoiser"))
                    return "Denoising neurale a 15 passaggi con layer 100% sintetizzato, preservando dettagli di pelle, metallo e vegetazione.";
                if (lwr.Contains("rtao"))
                    return "Occlusione ambientale a tracciamento di raggio screen-space in tempo reale con ray-marching guidato da profondit\u00E0.";
                if (lwr.Contains("lsao"))
                    return "Ombreggiatura e occlusione diffusa su larga scala per profondit\u00E0 di scena.";
                if (lwr.Contains("sssr"))
                    return "Riflessi speculari accurati con ray-marching temporale sub-pixel e attenuazione rugosit\u00E0.";
                if (lwr.Contains("bloom"))
                    return "Diffrazione ottica anamorfica e dispersione spettrale delle luci ad alta luminanza.";
                if (lwr.Contains("traa"))
                    return "Antialiasing di rifinitura con accumulo temporale sub-pixel.";
                if (lwr.Contains("motion blur"))
                    return "Sfuocatura cinematica guidata dal velocity buffer con 14 campioni allineati al moto.";
                if (lwr.Contains("post-neural") || lwr.Contains("output consolidation"))
                    return "10 cicli alternati finali: Downscale 0.85x -> filtraggio ReShade -> Upscale 1.18x per consolidamento contrasto prima del Present.";
                return "Passaggio di rendering configurato nella pipeline GPU.";
            }
        }

        private static AppLanguage LoadLanguagePreference()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("Language", out var langProp))
                    {
                        string? lang = langProp.GetString();
                        if (string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase))
                            return AppLanguage.English;
                        if (string.Equals(lang, "it", StringComparison.OrdinalIgnoreCase))
                            return AppLanguage.Italian;
                    }
                }
            }
            catch { }

            // Default to English for international GitHub releases
            return AppLanguage.English;
        }

        private static void SaveLanguagePreference(AppLanguage lang)
        {
            try
            {
                var dict = new Dictionary<string, string>
                {
                    ["Language"] = lang == AppLanguage.English ? "en" : "it"
                };
                string json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch { }
        }

        private static readonly Dictionary<string, Dictionary<AppLanguage, string>> Strings = new()
        {
            ["AppTitle"] = new()
            {
                [AppLanguage.Italian] = "Neural Pipeline Studio v2.1 - Universal AAA Neural Rendering Suite",
                [AppLanguage.English] = "Neural Pipeline Studio v2.1 - Universal AAA Neural Rendering Suite"
            },
            ["BrandingSub"] = new()
            {
                [AppLanguage.Italian] = "Universal Fleet Edition v2.1",
                [AppLanguage.English] = "Universal Fleet Edition v2.1"
            },
            ["NavHome"] = new()
            {
                [AppLanguage.Italian] = "\uD83C\uDFE0   Home & Giochi PC",
                [AppLanguage.English] = "\uD83C\uDFE0   Home & PC Games"
            },
            ["NavHomeTip"] = new()
            {
                [AppLanguage.Italian] = "Visualizza la libreria giochi rilevati nel PC e avvia la scansione automatica",
                [AppLanguage.English] = "View all games detected on your PC and run automated storage scans"
            },
            ["NavActiveGameLabel"] = new()
            {
                [AppLanguage.Italian] = "STUDIO GIOCO ATTIVO:",
                [AppLanguage.English] = "ACTIVE GAME STUDIO:"
            },
            ["NavNoGame"] = new()
            {
                [AppLanguage.Italian] = "(Seleziona un gioco)",
                [AppLanguage.English] = "(Select a game)"
            },
            ["NavPipeline"] = new()
            {
                [AppLanguage.Italian] = "\uD83C\uDF9B   Pipeline Matrix",
                [AppLanguage.English] = "\uD83C\uDF9B   Pipeline Matrix"
            },
            ["NavPipelineTip"] = new()
            {
                [AppLanguage.Italian] = "Visualizza e riordina i layer di rendering GPU con moltiplicatori di loop e scaling",
                [AppLanguage.English] = "View and reorder GPU rendering layers with loop multipliers and scaling ratios"
            },
            ["NavNeural"] = new()
            {
                [AppLanguage.Italian] = "\u26A1   Neural & DLSS",
                [AppLanguage.English] = "\u26A1   Neural & DLSS Engine"
            },
            ["NavNeuralTip"] = new()
            {
                [AppLanguage.Italian] = "Configura DLSS 2/3/3.5/4/5, RenoDX HDR, e i controlli avanzati del denoiser DLSS-NR",
                [AppLanguage.English] = "Configure DLSS 2/3/3.5/4/5, RenoDX HDR, and DLSS-NR neural denoiser controls"
            },
            ["NavVram"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDCCA   VRAM & Telemetria",
                [AppLanguage.English] = "\uD83D\uDCCA   VRAM & Telemetry"
            },
            ["NavVramTip"] = new()
            {
                [AppLanguage.Italian] = "Telemetria memoria GPU in tempo reale con calibrazione automatica al target 80-85%",
                [AppLanguage.English] = "Real-time GPU memory telemetry with automatic calibration to 80-85% sweet spot"
            },
            ["NavPresets"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDCBE   Preset & Profili",
                [AppLanguage.English] = "\uD83D\uDCBE   Presets & Profiles"
            },
            ["NavPresetsTip"] = new()
            {
                [AppLanguage.Italian] = "Profili di calibrazione certificati per hardware Enthusiast, High-End, Mainstream ed Entry",
                [AppLanguage.English] = "Certified calibration profiles for Enthusiast, High-End, Mainstream, and Entry hardware"
            },
            ["NavDlss5"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDCE6   Modelli DLSS 4/5",
                [AppLanguage.English] = "\uD83D\uDCE6   DLSS 4/5 Models"
            },
            ["NavDlss5Tip"] = new()
            {
                [AppLanguage.Italian] = "Libreria modelli DLL e runtime Streamline, Frame Gen, Ray Reconstruction e DLSS-NR",
                [AppLanguage.English] = "DLL model library and runtime Streamline, Frame Gen, Ray Reconstruction, and DLSS-NR modules"
            },
            ["NavWiki"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDCDA   Studio Wiki & Guide",
                [AppLanguage.English] = "\uD83D\uDCDA   Studio Wiki & Docs"
            },
            ["NavWikiTip"] = new()
            {
                [AppLanguage.Italian] = "Manuale tecnico integrato, spiegazione matematica dei cicli e guida alla risoluzione problemi",
                [AppLanguage.English] = "Built-in technical manual, mathematical breakdown of cycles, and troubleshooting guide"
            },
            ["HeaderBack"] = new()
            {
                [AppLanguage.Italian] = "\u2B05 Torna alla Home",
                [AppLanguage.English] = "\u2B05 Back to Home"
            },
            ["HeaderBackTip"] = new()
            {
                [AppLanguage.Italian] = "Torna alla schermata iniziale per selezionare o scansionare altri giochi",
                [AppLanguage.English] = "Return to the home library to select or scan other PC games"
            },
            ["HeaderAutoCalibrate"] = new()
            {
                [AppLanguage.Italian] = "\u26A1 Auto-Calibrate PC",
                [AppLanguage.English] = "\u26A1 Auto-Calibrate PC"
            },
            ["HeaderAutoCalibrateTip"] = new()
            {
                [AppLanguage.Italian] = "Adatta automaticamente tutti i parametri della pipeline alle specifiche esatte di questo PC, bloccando il carico all'82.5% VRAM",
                [AppLanguage.English] = "Automatically adapt all pipeline parameters to this PC's hardware specs, locking VRAM at the 82.5% sweet spot"
            },
            ["HeaderInject"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDE80 Inietta",
                [AppLanguage.English] = "\uD83D\uDE80 Inject"
            },
            ["HeaderInjectTip"] = new()
            {
                [AppLanguage.Italian] = "Installa la suite completa nel gioco attualmente attivo",
                [AppLanguage.English] = "Install the full rendering suite into the active game directory"
            },
            ["HeaderVanilla"] = new()
            {
                [AppLanguage.Italian] = "\uD83E\uDDF9 Vanilla",
                [AppLanguage.English] = "\uD83E\uDDF9 Vanilla"
            },
            ["HeaderVanillaTip"] = new()
            {
                [AppLanguage.Italian] = "Ripristina il gioco attualmente attivo allo stato 100% vanilla",
                [AppLanguage.English] = "Restore the active game to 100% clean vanilla factory state"
            },
            ["HeaderLaunch"] = new()
            {
                [AppLanguage.Italian] = "\u25B6 Avvia",
                [AppLanguage.English] = "\u25B6 Launch"
            },
            ["HeaderLaunchTip"] = new()
            {
                [AppLanguage.Italian] = "Avvia l'eseguibile del gioco attualmente attivo",
                [AppLanguage.English] = "Launch the active game executable"
            },
            ["HeaderSave"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDCBE Salva",
                [AppLanguage.English] = "\uD83D\uDCBE Save"
            },
            ["HeaderSaveTip"] = new()
            {
                [AppLanguage.Italian] = "Salva e sincronizza la configurazione nei file del gioco",
                [AppLanguage.English] = "Save and synchronize pipeline configuration to game directory"
            },
            ["TitleHome"] = new()
            {
                [AppLanguage.Italian] = "Libreria Giochi PC & Flotta Neurale",
                [AppLanguage.English] = "PC Games Library & Neural Fleet Hub"
            },
            ["TitlePipeline"] = new()
            {
                [AppLanguage.Italian] = "Studio Pipeline di Rendering: ",
                [AppLanguage.English] = "Rendering Pipeline Studio: "
            },
            ["TitleNeural"] = new()
            {
                [AppLanguage.Italian] = "Architettura Modelli Neurali, RenoDX & DLSS-NR: ",
                [AppLanguage.English] = "Neural Models Architecture, RenoDX & DLSS-NR: "
            },
            ["TitleVram"] = new()
            {
                [AppLanguage.Italian] = "Telemetria Memoria Video & Sweet Spot VRAM: ",
                [AppLanguage.English] = "GPU Memory Telemetry & VRAM Sweet Spot: "
            },
            ["TitlePresets"] = new()
            {
                [AppLanguage.Italian] = "Profili & Preset Calibrati per Hardware: ",
                [AppLanguage.English] = "Hardware-Calibrated Profiles & Presets: "
            },
            ["TitleDlss5"] = new()
            {
                [AppLanguage.Italian] = "Repository Modelli DLSS 4/5 & Moduli Streamline: ",
                [AppLanguage.English] = "DLSS 4/5 Models Repository & Streamline Modules: "
            },
            ["TitleWiki"] = new()
            {
                [AppLanguage.Italian] = "Studio Wiki, Manuale Tecnico & Risoluzione Problemi",
                [AppLanguage.English] = "Studio Wiki, Technical Manual & Troubleshooting"
            },
            ["HomeHeroTitle"] = new()
            {
                [AppLanguage.Italian] = "\uD83C\uDFAE Libreria Giochi & Suite Neurale PC",
                [AppLanguage.English] = "\uD83C\uDFAE PC Games Library & Neural Rendering Fleet"
            },
            ["HomeHeroSub"] = new()
            {
                [AppLanguage.Italian] = "Scansiona tutti i dischi del PC per rilevare i giochi compatibili (Steam, Xbox Game Pass, Epic Games, GOG). Clicca su un gioco per accedere allo studio dedicato e configurare la sequenza di rendering, il DLSS e i preset.",
                [AppLanguage.English] = "Scan all PC storage drives to detect compatible games (Steam, Xbox Game Pass, Epic Games, GOG). Click on any game to enter its dedicated studio and configure rendering passes, DLSS models, and presets."
            },
            ["HomeScanBtn"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDD0D Scansiona Giochi nel Tuo PC",
                [AppLanguage.English] = "\uD83D\uDD0D Scan PC Games"
            },
            ["HomeScanBtnTip"] = new()
            {
                [AppLanguage.Italian] = "Scansiona tutti i dischi fissi (C:, E:, ecc.) e le librerie Steam, Xbox Game Pass, Epic e GOG",
                [AppLanguage.English] = "Scan all local drives (C:, D:, E:, etc.) and Steam, Xbox Game Pass, Epic, and GOG libraries"
            },
            ["HomeFolderBtn"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDCC1 Cartella Manuale...",
                [AppLanguage.English] = "\uD83D\uDCC1 Add Folder Manually..."
            },
            ["HomeFolderBtnTip"] = new()
            {
                [AppLanguage.Italian] = "Seleziona manualmente la cartella di qualsiasi gioco installato",
                [AppLanguage.English] = "Manually select any installed game directory"
            },
            ["HomeSearchTip"] = new()
            {
                [AppLanguage.Italian] = "Cerca per titolo di gioco, motore grafico (RAGE, Unreal, REDengine, Creation) o API",
                [AppLanguage.English] = "Search by game title, graphics engine (RAGE, Unreal, REDengine, Creation), or API"
            },
            ["HomeFilterAll"] = new()
            {
                [AppLanguage.Italian] = "Tutti i Giochi",
                [AppLanguage.English] = "All Games"
            },
            ["HomeFilterInjected"] = new()
            {
                [AppLanguage.Italian] = "Solo Upgrade Attivo",
                [AppLanguage.English] = "Only Upgrade Injected"
            },
            ["HomeFilterVanilla"] = new()
            {
                [AppLanguage.Italian] = "Solo Vanilla",
                [AppLanguage.English] = "Only Vanilla"
            },
            ["HomeEmptyTitle"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDD0D Nessun gioco rilevato al momento",
                [AppLanguage.English] = "\uD83D\uDD0D No games detected yet"
            },
            ["HomeEmptySub"] = new()
            {
                [AppLanguage.Italian] = "Clicca sul pulsante '\uD83D\uDD0D Scansiona Giochi nel Tuo PC' in alto per cercare tutti i titoli compatibili installati sul computer.",
                [AppLanguage.English] = "Click '\uD83D\uDD0D Scan PC Games' above to detect all compatible titles installed on this computer."
            },
            ["HomeEmptyBtn"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDD0D Avvia Scansione PC Ora",
                [AppLanguage.English] = "\uD83D\uDD0D Run Storage Scan Now"
            },
            ["HomeCardConfigure"] = new()
            {
                [AppLanguage.Italian] = "\uD83C\uDF9B Configura Gioco & Pipeline",
                [AppLanguage.English] = "\uD83C\uDF9B Configure Game & Pipeline"
            },
            ["HomeCardConfigureTip"] = new()
            {
                [AppLanguage.Italian] = "Entra nello studio dedicato a questo gioco per configurare la sequenza di rendering, il DLSS e i preset",
                [AppLanguage.English] = "Open dedicated workstation for this game to configure rendering passes, DLSS models, and presets"
            },
            ["HomeCardInject"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDE80 Inietta",
                [AppLanguage.English] = "\uD83D\uDE80 Inject"
            },
            ["HomeCardInjectTip"] = new()
            {
                [AppLanguage.Italian] = "Installa proxy, shader e modelli neurali in questo gioco",
                [AppLanguage.English] = "Install proxies, shaders, and neural models into this game directory"
            },
            ["HomeCardVanilla"] = new()
            {
                [AppLanguage.Italian] = "\uD83E\uDDF9 Vanilla",
                [AppLanguage.English] = "\uD83E\uDDF9 Vanilla"
            },
            ["HomeCardVanillaTip"] = new()
            {
                [AppLanguage.Italian] = "Ripristina il gioco al 100% allo stato vanilla",
                [AppLanguage.English] = "Cleanly restore this game to 100% stock vanilla state"
            },
            ["HomeCardLaunch"] = new()
            {
                [AppLanguage.Italian] = "\u25B6 Avvia",
                [AppLanguage.English] = "\u25B6 Launch"
            },
            ["HomeCardLaunchTip"] = new()
            {
                [AppLanguage.Italian] = "Avvia l'eseguibile di questo gioco",
                [AppLanguage.English] = "Launch this game executable"
            },
            ["HomeTerminalHeader"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDDA5 Terminale Diagnostico di Scansione & Fleet Log (Clicca per espandere / comprimere)",
                [AppLanguage.English] = "\uD83D\uDDA5 Diagnostic Storage Scanner & Fleet Terminal (Click to expand / collapse)"
            },
            ["StudioSequenceLabel"] = new()
            {
                [AppLanguage.Italian] = "Sequenza di Esecuzione Rendering (Ordine GPU):",
                [AppLanguage.English] = "Rendering Execution Sequence (GPU Pipeline Order):"
            },
            ["StudioAddLayer"] = new()
            {
                [AppLanguage.Italian] = "\u2795 Add (+)",
                [AppLanguage.English] = "\u2795 Add (+)"
            },
            ["StudioAddLayerTip"] = new()
            {
                [AppLanguage.Italian] = "Aggiunge un nuovo layer neurale, catena ping-pong o effetto alla sequenza",
                [AppLanguage.English] = "Add a new neural layer, ping-pong chain, or effect to the GPU sequence"
            },
            ["StudioRemoveLayer"] = new()
            {
                [AppLanguage.Italian] = "\u2796 Remove (-)",
                [AppLanguage.English] = "\u2796 Remove (-)"
            },
            ["StudioRemoveLayerTip"] = new()
            {
                [AppLanguage.Italian] = "Rimuove il passaggio attualmente selezionato dalla sequenza",
                [AppLanguage.English] = "Remove currently selected pass from the pipeline sequence"
            },
            ["StudioCopyLayer"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDCCB Copy",
                [AppLanguage.English] = "\uD83D\uDCCB Copy"
            },
            ["StudioCopyLayerTip"] = new()
            {
                [AppLanguage.Italian] = "Copia il layer selezionato con tutti i suoi parametri dedicati negli appunti",
                [AppLanguage.English] = "Copy selected layer and all dedicated parameters to clipboard"
            },
            ["StudioPasteLayer"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDCE5 Paste",
                [AppLanguage.English] = "\uD83D\uDCE5 Paste"
            },
            ["StudioPasteLayerTip"] = new()
            {
                [AppLanguage.Italian] = "Incolla il layer duplicato subito sotto la voce selezionata",
                [AppLanguage.English] = "Paste duplicated layer immediately below selected entry"
            },
            ["StudioMoveUp"] = new()
            {
                [AppLanguage.Italian] = "\u25B2 Up",
                [AppLanguage.English] = "\u25B2 Up"
            },
            ["StudioMoveUpTip"] = new()
            {
                [AppLanguage.Italian] = "Sposta il passaggio prima nella sequenza di esecuzione GPU",
                [AppLanguage.English] = "Move selected pass earlier in the GPU execution sequence"
            },
            ["StudioMoveDown"] = new()
            {
                [AppLanguage.Italian] = "\u25BC Down",
                [AppLanguage.English] = "\u25BC Down"
            },
            ["StudioMoveDownTip"] = new()
            {
                [AppLanguage.Italian] = "Sposta il passaggio dopo nella sequenza di esecuzione GPU",
                [AppLanguage.English] = "Move selected pass later in the GPU execution sequence"
            },
            ["StudioToggle"] = new()
            {
                [AppLanguage.Italian] = "Toggle",
                [AppLanguage.English] = "Toggle"
            },
            ["StudioToggleTip"] = new()
            {
                [AppLanguage.Italian] = "Attiva o disattiva il passaggio selezionato",
                [AppLanguage.English] = "Enable or bypass selected pipeline pass"
            },
            ["StudioReset"] = new()
            {
                [AppLanguage.Italian] = "\u21BA Reset",
                [AppLanguage.English] = "\u21BA Reset"
            },
            ["StudioResetTip"] = new()
            {
                [AppLanguage.Italian] = "Ripristina l'ordine ottimale certificato dei passaggi",
                [AppLanguage.English] = "Restore certified optimal sequence order"
            },
            ["InspectorSec1Title"] = new()
            {
                [AppLanguage.Italian] = "\u2699 RIDIMENSIONAMENTO & PING-PONG RESCALE (DOWN \u21C4 UP):",
                [AppLanguage.English] = "\u2699 RESCALING & PING-PONG RESCALE ENGINE (DOWN \u21C4 UP):"
            },
            ["InspectorScaleMode"] = new()
            {
                [AppLanguage.Italian] = "Modalit\u00E0 Rescaling / Ping-Pong:",
                [AppLanguage.English] = "Rescaling / Ping-Pong Mode:"
            },
            ["InspectorUpscaleModel"] = new()
            {
                [AppLanguage.Italian] = "Modello / Algoritmo Upscaling in Uso:",
                [AppLanguage.English] = "Active Upscaling Model / Algorithm:"
            },
            ["InspectorDownscale"] = new()
            {
                [AppLanguage.Italian] = "Downscale (Compressione):",
                [AppLanguage.English] = "Downscale (Compression):"
            },
            ["InspectorUpscale"] = new()
            {
                [AppLanguage.Italian] = "Upscale (Espansione):",
                [AppLanguage.English] = "Upscale (Expansion):"
            },
            ["InspectorSec2Title"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDD04 ITERAZIONI, PASSI NEURALI & INTENSIT\u00C0 (TOTALMENTE EDITABILI):",
                [AppLanguage.English] = "\uD83D\uDD04 ITERATIONS, NEURAL PASSES & INTENSITIES (FULLY EDITABLE):"
            },
            ["InspectorLoopCycles"] = new()
            {
                [AppLanguage.Italian] = "Cicli Iterazione (Loop):",
                [AppLanguage.English] = "Iteration Cycles (Loop):"
            },
            ["InspectorNeuralPass"] = new()
            {
                [AppLanguage.Italian] = "Iterazioni Neurali (Tensor):",
                [AppLanguage.English] = "Neural Passes (Tensor):"
            },
            ["InspectorIntensity"] = new()
            {
                [AppLanguage.Italian] = "Intensit\u00E0 / Synthetic Blend:",
                [AppLanguage.English] = "Intensity / Synthetic Blend:"
            },
            ["InspectorQualityPreset"] = new()
            {
                [AppLanguage.Italian] = "Profilo Preset Qualit\u00E0:",
                [AppLanguage.English] = "Quality Preset Profile:"
            },
            ["InspectorSec3Title"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDD17 MODELLI SHADER, DLL ED ADDON DEDICATI (DLSS 5 & STREAMLINE):",
                [AppLanguage.English] = "\uD83D\uDD17 DEDICATED SHADER, DLL & ADDON MAPPINGS (DLSS 5 & STREAMLINE):"
            },
            ["InspectorDedicatedShader"] = new()
            {
                [AppLanguage.Italian] = "\uD83C\uDFA8 Shader ReShade Dedicato (.fx):",
                [AppLanguage.English] = "\uD83C\uDFA8 Dedicated ReShade Shader (.fx):"
            },
            ["InspectorDedicatedAddon"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDD0C Addon Dedicato (.addon64 / .dll):",
                [AppLanguage.English] = "\uD83D\uDD0C Dedicated Runtime Addon (.addon64 / .dll):"
            },
            ["InspectorDedicatedDll"] = new()
            {
                [AppLanguage.Italian] = "\uD83E\uDDE0 Modello Neurale / DLL Dedicata (da dlss5extracted):",
                [AppLanguage.English] = "\uD83E\uDDE0 Neural Model / Dedicated DLL (from dlss5extracted):"
            },
            ["InspectorDescTitle"] = new()
            {
                [AppLanguage.Italian] = "Dettagli e Specifiche di Esecuzione GPU:",
                [AppLanguage.English] = "GPU Execution Specifications & Details:"
            },
            ["WikiTocTitle"] = new()
            {
                [AppLanguage.Italian] = "\uD83D\uDCDA Indice dei Contenuti / Capitoli:",
                [AppLanguage.English] = "\uD83D\uDCDA Table of Contents / Chapters:"
            }
        };
    }
}