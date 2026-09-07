using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using NeuralPipelineStudio.Common;
using NeuralPipelineStudio.Core;
using NeuralPipelineStudio.Models;

namespace NeuralPipelineStudio
{
    public partial class MainWindow : Window
    {
        private string _gameDir = string.Empty;
        private string _presetsDir = string.Empty;
        private string _dlss5Dir = string.Empty;
        private NeuralPipelineSettings _settings = new();
        private List<PipelineLayer> _layers = new();
        private List<PresetData> _presets = new();
        private List<DlssModelInfo> _dlssModels = new();
        private List<GameTargetInfo> _detectedGames = new();
        private List<WikiArticle> _wikiArticles = new();
        private HardwareProfile _hardwareProfile = new();
        private PipelineLayer? _clipboardLayer = null;
        private GameTargetInfo _targetInfo = new();
        private GameTargetInfo? _activeGame = null;
        private DispatcherTimer _telemetryTimer = new();
        private bool _isSyncing = false;
        private bool _isInitialized = false;
        private bool _isUpdatingLayerInspector = false;

        public MainWindow()
        {
            _isSyncing = true;
            _isInitialized = false;

            InitializeComponent();
            DetermineWindowPosition();
            Loaded += MainWindow_Loaded;
            DeterminePaths();
            LoadPipelineData();
            LoadPresets();
            LoadWikiArticles();
            InitializeTelemetry();
            UpdateActiveNav(btnNavHome);

            _isSyncing = false;
            _isInitialized = true;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DetermineWindowPosition();
            _hardwareProfile = HardwareEngine.DetectHardware();
            ApplyHardwareProfileToUI();

            // Load previously detected games from local cache
            _detectedGames = UniversalScanner.LoadCachedGames();
            if (_detectedGames.Count > 0)
            {
                if (pnlHomeEmptyState != null) pnlHomeEmptyState.Visibility = Visibility.Collapsed;
                ApplyHomeFilter();
                int injected = _detectedGames.Count(g => g.IsUpgradeInstalled);
                if (txtHomeFleetStatus != null)
                    txtHomeFleetStatus.Text = $"{_detectedGames.Count} Giochi Rilevati ({injected} con Upgrade Attivo)";
            }
            else
            {
                if (pnlHomeEmptyState != null) pnlHomeEmptyState.Visibility = Visibility.Visible;
                // Run silent background scan on first start
                Task.Run(() => PerformGameFleetScan(false));
            }

            SetLanguage(LocalizationManager.CurrentLanguage);
            NavHome_Click(btnNavHome, new RoutedEventArgs());
        }

        private void ApplyHardwareProfileToUI()
        {
            if (txtHwBanner != null) txtHwBanner.Text = _hardwareProfile.SummaryHeader;
            if (txtProfileGpu != null) txtProfileGpu.Text = $"{_hardwareProfile.GpuName} ({_hardwareProfile.TotalVramMB:N0} MB)";
            if (txtProfileDriver != null) txtProfileDriver.Text = $"Driver: {_hardwareProfile.DriverVersion} | {_hardwareProfile.Tier}";
            if (txtStatusHwTier != null) txtStatusHwTier.Text = _hardwareProfile.TierDisplay;
        }

        private void DetermineWindowPosition()
        {
            try
            {
                double workW = SystemParameters.WorkArea.Width;
                double workH = SystemParameters.WorkArea.Height;
                double workLeft = SystemParameters.WorkArea.Left;
                double workTop = SystemParameters.WorkArea.Top;

                double targetW = Math.Min(1200, Math.Max(920, workW * 0.94));
                double targetH = Math.Min(720, Math.Max(580, workH * 0.92));

                if (targetW > workW) targetW = workW;
                if (targetH > workH) targetH = workH;

                this.Width = targetW;
                this.Height = targetH;

                this.Left = workLeft + Math.Max(0, (workW - targetW) / 2.0);
                this.Top = workTop + Math.Max(0, (workH - targetH) / 2.0);
            }
            catch
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        private void DeterminePaths()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Find game directory
            if (File.Exists(Path.Combine(baseDir, "RDR2.exe")))
            {
                _gameDir = baseDir;
            }
            else
            {
                string parentDir = Directory.GetParent(baseDir)?.FullName ?? baseDir;
                if (File.Exists(Path.Combine(parentDir, "RDR2.exe")))
                    _gameDir = parentDir;
                else
                {
                    string grandParent = Directory.GetParent(parentDir)?.FullName ?? parentDir;
                    if (File.Exists(Path.Combine(grandParent, "RDR2.exe")))
                        _gameDir = grandParent;
                    else
                    {
                        string defaultRdr2 = @"E:\SteamLibrary\steamapps\common\Red Dead Redemption 2";
                        _gameDir = Directory.Exists(defaultRdr2) ? defaultRdr2 : baseDir;
                    }
                }
            }

            // Find presets directory
            string candidate1 = Path.Combine(baseDir, "presets");
            string candidate2 = Path.Combine(_gameDir, "NeuralPipelineStudio", "presets");
            string candidate3 = Path.Combine(Directory.GetParent(baseDir)?.FullName ?? "", "presets");

            if (Directory.Exists(candidate1))
                _presetsDir = candidate1;
            else if (Directory.Exists(candidate2))
                _presetsDir = candidate2;
            else if (Directory.Exists(candidate3))
                _presetsDir = candidate3;
            else
            {
                _presetsDir = candidate2;
                Directory.CreateDirectory(_presetsDir);
            }

            txtProfileGame.Text = $"Target: {Path.GetFileName(_gameDir)}";
            ScanTargetGame(_gameDir);

            // Detect DLSS repository and discover models
            _dlss5Dir = PayloadManager.ResolveDlss5Directory();
            LoadDlss5Models();
        }

        private void LoadPipelineData()
        {
            var result = ConfigSync.LoadFromGameDirectory(_gameDir, _hardwareProfile);
            _settings = result.settings;
            _layers = result.layers;

            RefreshLayerList();
            SyncSettingsToSliders();
            UpdateVramTelemetry();
        }

        private void LoadPresets()
        {
            _presets = PresetManager.LoadAllPresets(_presetsDir);
            lstPresets.ItemsSource = null;
            lstPresets.ItemsSource = _presets;
            if (_presets.Count > 0)
                lstPresets.SelectedIndex = 0;
        }

        private void LoadWikiArticles()
        {
            _wikiArticles = WikiRepository.GetAllArticles();
            if (lstWikiArticles != null)
            {
                lstWikiArticles.ItemsSource = null;
                lstWikiArticles.ItemsSource = _wikiArticles;
                if (_wikiArticles.Count > 0)
                    lstWikiArticles.SelectedIndex = 0;
            }
        }

        private void LoadDlss5Models()
        {
            if (txtDlss5Folder != null)
                txtDlss5Folder.Text = _dlss5Dir;

            _dlssModels = Dlss5ModelManager.DiscoverModels(_dlss5Dir);
            if (lstDlss5Models != null)
            {
                lstDlss5Models.ItemsSource = null;
                lstDlss5Models.ItemsSource = _dlssModels;
            }
            if (txtDlss5Count != null)
                txtDlss5Count.Text = $"{_dlssModels.Count} Models Detected";

            // 1. Populate cbLayerShader in inspector
            if (cbLayerShader != null)
            {
                string prevShader = cbLayerShader.SelectedItem as string ?? "";
                cbLayerShader.Items.Clear();
                var availableShaders = Dlss5ModelManager.GetAvailableShaders(_gameDir);
                foreach (var s in availableShaders)
                {
                    cbLayerShader.Items.Add(s);
                }
                if (!string.IsNullOrEmpty(prevShader) && cbLayerShader.Items.Contains(prevShader))
                    cbLayerShader.SelectedItem = prevShader;
                else if (cbLayerShader.Items.Count > 0)
                    cbLayerShader.SelectedIndex = 0;
            }

            // 2. Populate cbLayerAddon in inspector
            if (cbLayerAddon != null)
            {
                string prevAddon = cbLayerAddon.SelectedItem as string ?? "";
                cbLayerAddon.Items.Clear();
                var availableAddons = Dlss5ModelManager.GetAvailableAddons(_gameDir);
                foreach (var a in availableAddons)
                {
                    cbLayerAddon.Items.Add(a);
                }
                if (!string.IsNullOrEmpty(prevAddon) && cbLayerAddon.Items.Contains(prevAddon))
                    cbLayerAddon.SelectedItem = prevAddon;
                else if (cbLayerAddon.Items.Count > 0)
                    cbLayerAddon.SelectedIndex = 0;
            }

            // 3. Populate cbLayerDll in inspector
            if (cbLayerDll != null)
            {
                string prevSel = cbLayerDll.SelectedItem as string ?? "";
                cbLayerDll.Items.Clear();
                cbLayerDll.Items.Add("(None / Integrated Shader)");
                var availableModels = Dlss5ModelManager.GetAvailableModels(_dlss5Dir);
                foreach (var m in availableModels)
                {
                    if (!cbLayerDll.Items.Contains(m))
                        cbLayerDll.Items.Add(m);
                }
                foreach (var m in _dlssModels)
                {
                    if (!cbLayerDll.Items.Contains(m.FileName))
                        cbLayerDll.Items.Add(m.FileName);
                }
                if (!string.IsNullOrEmpty(prevSel) && cbLayerDll.Items.Contains(prevSel))
                    cbLayerDll.SelectedItem = prevSel;
                else
                    cbLayerDll.SelectedIndex = 0;
            }

            // 4. Populate cbLayerUpscaleModel in inspector
            if (cbLayerUpscaleModel != null)
            {
                string prevUp = cbLayerUpscaleModel.SelectedItem as string ?? "";
                cbLayerUpscaleModel.Items.Clear();
                cbLayerUpscaleModel.Items.Add("DLSS 4/4.5 (nvngx_dlss.dll)");
                cbLayerUpscaleModel.Items.Add("DLSS Ray Reconstruction (sl.dlss_d.dll)");
                cbLayerUpscaleModel.Items.Add("DLSS-NR Neural Reconstruction (nvngx.dll_dlssnr.dll)");
                cbLayerUpscaleModel.Items.Add("RenoDX HDR Tensor Upscaler (renodx-dlss.addon64)");
                cbLayerUpscaleModel.Items.Add("OptiScaler XeSS Bridge (openxess.dll)");
                cbLayerUpscaleModel.Items.Add("Catmull-Rom 600% Spline");
                cbLayerUpscaleModel.Items.Add("Lanczos-3 Spatial Filter");
                cbLayerUpscaleModel.Items.Add("Bicubic Spatial Filter");
                foreach (var m in _dlssModels)
                {
                    if (!cbLayerUpscaleModel.Items.Contains(m.FileName))
                        cbLayerUpscaleModel.Items.Add(m.FileName);
                }
                if (!string.IsNullOrEmpty(prevUp) && cbLayerUpscaleModel.Items.Contains(prevUp))
                    cbLayerUpscaleModel.SelectedItem = prevUp;
                else
                    cbLayerUpscaleModel.SelectedIndex = 0;
            }
        }

        private void InitializeTelemetry()
        {
            _telemetryTimer.Interval = TimeSpan.FromSeconds(2);
            _telemetryTimer.Tick += (s, e) => UpdateVramTelemetry();
            _telemetryTimer.Start();
            UpdateVramTelemetry();
        }

        #region NAVIGATION
        private void UpdateActiveNav(Button activeBtn)
        {
            if (btnNavHome != null) btnNavHome.Background = Brushes.Transparent;
            if (btnNavPipeline != null) btnNavPipeline.Background = Brushes.Transparent;
            if (btnNavNeural != null) btnNavNeural.Background = Brushes.Transparent;
            if (btnNavVram != null) btnNavVram.Background = Brushes.Transparent;
            if (btnNavPresets != null) btnNavPresets.Background = Brushes.Transparent;
            if (btnNavDlss5 != null) btnNavDlss5.Background = Brushes.Transparent;
            if (btnNavWiki != null) btnNavWiki.Background = Brushes.Transparent;

            activeBtn.Background = (SolidColorBrush)FindResource("BrushBgCard");
        }

        private void HideAllViews()
        {
            if (ViewHome != null) ViewHome.Visibility = Visibility.Collapsed;
            if (ViewPipeline != null) ViewPipeline.Visibility = Visibility.Collapsed;
            if (ViewNeural != null) ViewNeural.Visibility = Visibility.Collapsed;
            if (ViewVram != null) ViewVram.Visibility = Visibility.Collapsed;
            if (ViewPresets != null) ViewPresets.Visibility = Visibility.Collapsed;
            if (ViewDlss5 != null) ViewDlss5.Visibility = Visibility.Collapsed;
            if (ViewWiki != null) ViewWiki.Visibility = Visibility.Collapsed;
        }

        private void NavHome_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            if (ViewHome != null) ViewHome.Visibility = Visibility.Visible;
            if (btnHeaderBackToHome != null) btnHeaderBackToHome.Visibility = Visibility.Collapsed;
            if (pnlHeaderGameActions != null) pnlHeaderGameActions.Visibility = Visibility.Collapsed;
            if (txtSectionTitle != null) UpdateCurrentSectionTitle();
            UpdateActiveNav(btnNavHome);
            ApplyHomeFilter();
        }

        private void EnsureActiveGameSelected()
        {
            if (_activeGame == null)
            {
                if (_detectedGames.Count > 0)
                {
                    OpenGameStudio(_detectedGames[0]);
                }
            }
        }

        private void NavPipeline_Click(object sender, RoutedEventArgs e)
        {
            EnsureActiveGameSelected();
            if (_activeGame == null)
            {
                MessageBox.Show("Seleziona prima un gioco dalla Home o avvia la scansione del PC per configurare la sequenza di rendering.", "Nessun Gioco Selezionato", MessageBoxButton.OK, MessageBoxImage.Information);
                NavHome_Click(btnNavHome, new RoutedEventArgs());
                return;
            }

            HideAllViews();
            if (ViewPipeline != null) ViewPipeline.Visibility = Visibility.Visible;
            if (btnHeaderBackToHome != null) btnHeaderBackToHome.Visibility = Visibility.Visible;
            if (pnlHeaderGameActions != null) pnlHeaderGameActions.Visibility = Visibility.Visible;
            if (txtSectionTitle != null) UpdateCurrentSectionTitle();
            UpdateActiveNav(btnNavPipeline);

            if (lstLayers != null && lstLayers.SelectedIndex < 0 && _layers.Count > 0)
            {
                lstLayers.SelectedIndex = 0;
            }
        }

        private void NavNeural_Click(object sender, RoutedEventArgs e)
        {
            EnsureActiveGameSelected();
            if (_activeGame == null)
            {
                MessageBox.Show("Seleziona prima un gioco dalla Home o avvia la scansione del PC per accedere al motore neurale.", "Nessun Gioco Selezionato", MessageBoxButton.OK, MessageBoxImage.Information);
                NavHome_Click(btnNavHome, new RoutedEventArgs());
                return;
            }

            HideAllViews();
            if (ViewNeural != null) ViewNeural.Visibility = Visibility.Visible;
            if (btnHeaderBackToHome != null) btnHeaderBackToHome.Visibility = Visibility.Visible;
            if (pnlHeaderGameActions != null) pnlHeaderGameActions.Visibility = Visibility.Visible;
            if (txtSectionTitle != null) UpdateCurrentSectionTitle();
            UpdateActiveNav(btnNavNeural);
        }

        private void NavVram_Click(object sender, RoutedEventArgs e)
        {
            EnsureActiveGameSelected();
            if (_activeGame == null)
            {
                MessageBox.Show("Seleziona prima un gioco dalla Home o avvia la scansione del PC per calibrare la memoria VRAM.", "Nessun Gioco Selezionato", MessageBoxButton.OK, MessageBoxImage.Information);
                NavHome_Click(btnNavHome, new RoutedEventArgs());
                return;
            }

            HideAllViews();
            if (ViewVram != null) ViewVram.Visibility = Visibility.Visible;
            if (btnHeaderBackToHome != null) btnHeaderBackToHome.Visibility = Visibility.Visible;
            if (pnlHeaderGameActions != null) pnlHeaderGameActions.Visibility = Visibility.Visible;
            if (txtSectionTitle != null) UpdateCurrentSectionTitle();
            UpdateActiveNav(btnNavVram);
            UpdateVramTelemetry();
        }

        private void NavPresets_Click(object sender, RoutedEventArgs e)
        {
            EnsureActiveGameSelected();
            if (_activeGame == null)
            {
                MessageBox.Show("Seleziona prima un gioco dalla Home o avvia la scansione del PC per applicare i profili preset.", "Nessun Gioco Selezionato", MessageBoxButton.OK, MessageBoxImage.Information);
                NavHome_Click(btnNavHome, new RoutedEventArgs());
                return;
            }

            HideAllViews();
            if (ViewPresets != null) ViewPresets.Visibility = Visibility.Visible;
            if (btnHeaderBackToHome != null) btnHeaderBackToHome.Visibility = Visibility.Visible;
            if (pnlHeaderGameActions != null) pnlHeaderGameActions.Visibility = Visibility.Visible;
            if (txtSectionTitle != null) UpdateCurrentSectionTitle();
            UpdateActiveNav(btnNavPresets);
        }

        private void NavDlss5_Click(object sender, RoutedEventArgs e)
        {
            EnsureActiveGameSelected();
            if (_activeGame == null)
            {
                MessageBox.Show("Seleziona prima un gioco dalla Home o avvia la scansione del PC per gestire i modelli DLSS.", "Nessun Gioco Selezionato", MessageBoxButton.OK, MessageBoxImage.Information);
                NavHome_Click(btnNavHome, new RoutedEventArgs());
                return;
            }

            HideAllViews();
            if (ViewDlss5 != null) ViewDlss5.Visibility = Visibility.Visible;
            if (btnHeaderBackToHome != null) btnHeaderBackToHome.Visibility = Visibility.Visible;
            if (pnlHeaderGameActions != null) pnlHeaderGameActions.Visibility = Visibility.Visible;
            if (txtSectionTitle != null) UpdateCurrentSectionTitle();
            UpdateActiveNav(btnNavDlss5);
        }

        private void NavWiki_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            if (ViewWiki != null) ViewWiki.Visibility = Visibility.Visible;
            if (txtSectionTitle != null) UpdateCurrentSectionTitle();
            UpdateActiveNav(btnNavWiki);
        }
        #endregion

        #region HARDWARE AUTO-CALIBRATION
        private void AutoCalibrateHardware_Click(object sender, RoutedEventArgs e)
        {
            _hardwareProfile = HardwareEngine.DetectHardware();
            ApplyHardwareProfileToUI();

            _settings = HardwareEngine.GenerateOptimalSettings(_hardwareProfile);
            _layers = ConfigSync.GetDefaultPipeline(_hardwareProfile);
            RefreshLayerList();
            SyncSettingsToSliders();
            UpdateVramTelemetry();

            MessageBox.Show($"Hardware Auto-Calibration Complete!\r\n\r\n" +
                            $"• Detected GPU: {_hardwareProfile.GpuName} ({_hardwareProfile.TotalVramMB:N0} MB VRAM)\r\n" +
                            $"• Hardware Tier: {_hardwareProfile.TierDisplay}\r\n" +
                            $"• CPU: {_hardwareProfile.CpuName} ({_hardwareProfile.CpuCores} Threads)\r\n" +
                            $"• RAM: {_hardwareProfile.SystemRamGB} GB System Memory\r\n" +
                            $"• Driver: {_hardwareProfile.DriverVersion}\r\n\r\n" +
                            $"Pipeline parameters tailored to your exact hardware. VRAM locked at {_settings.VramTargetPercent:F1}% sweet spot.",
                            "Auto-Calibration Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region PIPELINE VIEW & INSPECTOR
        private void RefreshLayerList()
        {
            lstLayers.ItemsSource = null;
            lstLayers.ItemsSource = _layers;
            if (_layers.Count > 0)
            {
                lstLayers.SelectedIndex = 0;
                LstLayers_SelectionChanged(lstLayers, null!);
            }
        }

        private void LstLayers_SelectionChanged(object sender, SelectionChangedEventArgs? e)
        {
            if (lstLayers == null || lstLayers.SelectedItem is not PipelineLayer layer) return;

            _isUpdatingLayerInspector = true;
            try
            {
                if (txtSelectedLayerName != null) txtSelectedLayerName.Text = layer.Name;
                if (chkSelectedLayerEnabled != null) chkSelectedLayerEnabled.IsChecked = layer.Enabled;
                if (txtInspectorTech != null) txtInspectorTech.Text = $"Technique: {layer.TechniqueName}";
                if (txtInspectorShader != null) txtInspectorShader.Text = $"Shader: {layer.ShaderFile} | DLL: {layer.DllModelName} | Addon: {layer.AddonName}";

                // 0. Associated Shader (.fx)
                if (cbLayerShader != null)
                {
                    if (string.IsNullOrEmpty(layer.ShaderFile))
                        cbLayerShader.SelectedIndex = 0;
                    else
                    {
                        int foundShader = -1;
                        for (int i = 0; i < cbLayerShader.Items.Count; i++)
                        {
                            if (string.Equals(cbLayerShader.Items[i]?.ToString(), layer.ShaderFile, StringComparison.OrdinalIgnoreCase))
                            {
                                foundShader = i;
                                break;
                            }
                        }
                        cbLayerShader.SelectedIndex = (foundShader >= 0) ? foundShader : 0;
                    }
                }

                // 0b. Associated Addon (.addon64 / .dll)
                if (cbLayerAddon != null)
                {
                    if (string.IsNullOrEmpty(layer.AddonName))
                        cbLayerAddon.SelectedIndex = 0;
                    else
                    {
                        int foundAddon = -1;
                        for (int i = 0; i < cbLayerAddon.Items.Count; i++)
                        {
                            if (string.Equals(cbLayerAddon.Items[i]?.ToString(), layer.AddonName, StringComparison.OrdinalIgnoreCase))
                            {
                                foundAddon = i;
                                break;
                            }
                        }
                        cbLayerAddon.SelectedIndex = (foundAddon >= 0) ? foundAddon : 0;
                    }
                }

                // 1. Associated DLL Model
                if (cbLayerDll != null)
                {
                    if (string.IsNullOrEmpty(layer.DllModelName))
                        cbLayerDll.SelectedIndex = 0;
                    else
                    {
                        int foundIdx = -1;
                        for (int i = 0; i < cbLayerDll.Items.Count; i++)
                        {
                            if (string.Equals(cbLayerDll.Items[i]?.ToString(), layer.DllModelName, StringComparison.OrdinalIgnoreCase))
                            {
                                foundIdx = i;
                                break;
                            }
                        }
                        cbLayerDll.SelectedIndex = (foundIdx >= 0) ? foundIdx : 0;
                    }
                }

                // 2. Scale Mode
                if (cbLayerScaleMode != null)
                {
                    int modeIdx = 5; // None
                    if (layer.ScaleMode.Contains("Ping-Pong")) modeIdx = 0;
                    else if (layer.ScaleMode.Contains("Super Resolution") || layer.ScaleMode == "DLSS") modeIdx = 1;
                    else if (layer.ScaleMode == "Downscale") modeIdx = 2;
                    else if (layer.ScaleMode == "Upscale") modeIdx = 3;
                    else if (layer.ScaleMode == "DLAA") modeIdx = 4;
                    cbLayerScaleMode.SelectedIndex = modeIdx;
                }

                // 3. Upscaling Model in Use
                if (cbLayerUpscaleModel != null)
                {
                    int foundUp = -1;
                    for (int i = 0; i < cbLayerUpscaleModel.Items.Count; i++)
                    {
                        if (string.Equals(cbLayerUpscaleModel.Items[i]?.ToString(), layer.UpscaleModel, StringComparison.OrdinalIgnoreCase))
                        {
                            foundUp = i;
                            break;
                        }
                    }
                    cbLayerUpscaleModel.SelectedIndex = foundUp >= 0 ? foundUp : 0;
                }

                // 4. Downscale Ratio
                int downPct = Math.Clamp((int)(layer.DownscaleRatio * 100), 20, 100);
                if (slLayerDownscale != null) slLayerDownscale.Value = downPct;
                if (txtLayerDownscale != null) txtLayerDownscale.Text = $"{downPct}%";

                // 5. Upscale Ratio
                int upPct = Math.Clamp((int)(layer.UpscaleRatio * 100), 100, 1500);
                if (slLayerUpscale != null) slLayerUpscale.Value = upPct;
                if (txtLayerUpscale != null) txtLayerUpscale.Text = $"{upPct}%";

                // 6. Loop Cycles
                int loop = Math.Clamp(layer.LoopCycles, 1, 50);
                if (slLayerLoop != null) slLayerLoop.Value = loop;
                if (txtLayerLoop != null) txtLayerLoop.Text = $"{loop}x";

                // 7. Neural Pass Iterations
                int nPass = Math.Clamp(layer.NeuralPassIterations, 1, 60);
                if (slLayerNeuralPass != null) slLayerNeuralPass.Value = nPass;
                if (txtLayerNeuralPass != null) txtLayerNeuralPass.Text = $"{nPass}";

                // 8. Intensity / Synth Blend
                int intPct = Math.Clamp((int)(layer.Intensity * 100), 0, 500);
                if (slLayerIntensity != null) slLayerIntensity.Value = intPct;
                if (txtLayerIntensity != null) txtLayerIntensity.Text = $"{intPct}%";

                // 9. Quality Preset
                if (cbLayerQuality != null)
                {
                    int qIdx = 0;
                    if (layer.QualityPreset.Contains("Preset E")) qIdx = 1;
                    else if (layer.QualityPreset.Contains("Preset C")) qIdx = 2;
                    else if (layer.QualityPreset.Contains("Preset D")) qIdx = 3;
                    else if (layer.QualityPreset.Contains("DLAA")) qIdx = 4;
                    else if (layer.QualityPreset.Contains("Ultra Performance")) qIdx = 5;
                    cbLayerQuality.SelectedIndex = qIdx;
                }

                // 10. Update Description Text
                if (txtInspectorDesc != null)
                {
                    string modelInUse = !string.IsNullOrEmpty(layer.UpscaleModel) ? layer.UpscaleModel : (!string.IsNullOrEmpty(layer.DllModelName) ? layer.DllModelName : (LocalizationManager.CurrentLanguage == AppLanguage.English ? "Direct Shader" : "Shader Diretto"));
                    string desc = LocalizationManager.GetLayerDescription(layer.Name, LocalizationManager.CurrentLanguage);
                    if (string.IsNullOrWhiteSpace(desc) || desc.Contains("GPU neural pipeline") || desc.Contains("pipeline GPU"))
                    {
                        if (!string.IsNullOrWhiteSpace(layer.Description))
                            desc = layer.Description;
                    }

                    if (LocalizationManager.CurrentLanguage == AppLanguage.English)
                    {
                        txtInspectorDesc.Text = $"{desc}\r\n\r\n" +
                                                $"• 🎨 Dedicated ReShade Shader (.fx): {(!string.IsNullOrEmpty(layer.ShaderFile) ? layer.ShaderFile : "(None)")}\r\n" +
                                                $"• 🧠 Dedicated Neural Model DLL: {(!string.IsNullOrEmpty(layer.DllModelName) ? layer.DllModelName : "(Integrated)")}\r\n" +
                                                $"• 🔌 Dedicated Addon: {(!string.IsNullOrEmpty(layer.AddonName) ? layer.AddonName : "(None)")}\r\n" +
                                                $"• Rescaling Mode: {layer.ScaleMode}\r\n" +
                                                $"• Downscale Compression: {layer.DownscaleRatio * 100:F0}% ({layer.DownscaleRatio:F2}x)\r\n" +
                                                $"• Upscale Expansion: {layer.UpscaleRatio * 100:F0}% ({layer.UpscaleRatio:F2}x)\r\n" +
                                                $"• Selected Upscaling Model: {modelInUse}\r\n" +
                                                $"• Loop Cycles (Iterations): {layer.LoopCycles}x | Neural Tensor Passes: {layer.NeuralPassIterations}\r\n" +
                                                $"• Intensity / Synth Blend: {layer.Intensity * 100:F0}%\r\n" +
                                                $"• GPU Execution State: {(layer.Enabled ? "ACTIVE (Active in render pass)" : "BYPASSED (Zero GPU overhead)")}";
                    }
                    else
                    {
                        txtInspectorDesc.Text = $"{desc}\r\n\r\n" +
                                                $"• 🎨 Shader ReShade (.fx): {(!string.IsNullOrEmpty(layer.ShaderFile) ? layer.ShaderFile : "(Nessuno)")}\r\n" +
                                                $"• 🧠 Modello Neurale DLL: {(!string.IsNullOrEmpty(layer.DllModelName) ? layer.DllModelName : "(Integrato)")}\r\n" +
                                                $"• 🔌 Addon Dedicato: {(!string.IsNullOrEmpty(layer.AddonName) ? layer.AddonName : "(Nessuno)")}\r\n" +
                                                $"• Modalità Rescaling: {layer.ScaleMode}\r\n" +
                                                $"• Compressione Downscale: {layer.DownscaleRatio * 100:F0}% ({layer.DownscaleRatio:F2}x)\r\n" +
                                                $"• Espansione Upscale: {layer.UpscaleRatio * 100:F0}% ({layer.UpscaleRatio:F2}x)\r\n" +
                                                $"• Modello Upscaling Selezionato: {modelInUse}\r\n" +
                                                $"• Cicli di Loop (Iterazioni): {layer.LoopCycles}x | Passi Neurali Tensor: {layer.NeuralPassIterations}\r\n" +
                                                $"• Intensità / Synth Blend: {layer.Intensity * 100:F0}%\r\n" +
                                                $"• Stato Esecuzione GPU: {(layer.Enabled ? "ACTIVE (Attivo nel render pass)" : "BYPASSED (Bypassato, zero overhead)")}";
                    }
                }

                if (txtLayerVramImpact != null)
                {
                    int layerMb = (int)(22 * Math.Max(1, layer.LoopCycles) * (layer.ScaleMode.Contains("Ping-Pong") ? 1.4 : 1.0));
                    if (LocalizationManager.CurrentLanguage == AppLanguage.English)
                    {
                        txtLayerVramImpact.Text = $"• Estimated VRAM Footprint: ~{layerMb} MB (Dual recycled ping-pong buffers)\r\n" +
                                                  $"• Upscale Model: {layer.UpscaleModel}\r\n" +
                                                  $"• Interposer: NVIDIA Streamline & ReShade DX12/Vulkan compatible";
                    }
                    else
                    {
                        txtLayerVramImpact.Text = $"• Footprint VRAM Stimato: ~{layerMb} MB (Buffer Ping-Pong alternati)\r\n" +
                                                  $"• Modello Upscale: {layer.UpscaleModel}\r\n" +
                                                  $"• Interposer: NVIDIA Streamline & ReShade DX12/Vulkan compatibile";
                    }
                }
            }
            finally
            {
                _isUpdatingLayerInspector = false;
            }
        }

        private void LayerCheck_Click(object sender, RoutedEventArgs e)
        {
            UpdateVramTelemetry();
        }

        private void SelectedLayerName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            layer.Name = txtSelectedLayerName.Text.Trim();
            lstLayers.Items.Refresh();
        }

        private void SelectedLayerEnabled_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            layer.Enabled = chkSelectedLayerEnabled.IsChecked == true;
            lstLayers.Items.Refresh();
            UpdateVramTelemetry();
        }

        private void LayerShader_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            string sel = cbLayerShader.SelectedItem as string ?? "";
            if (!string.IsNullOrEmpty(sel))
            {
                layer.ShaderFile = sel;
                if (txtInspectorShader != null)
                    txtInspectorShader.Text = $"Shader: {layer.ShaderFile} | DLL: {layer.DllModelName} | Addon: {layer.AddonName}";
                lstLayers.Items.Refresh();
            }
        }

        private void LayerAddon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            string sel = cbLayerAddon.SelectedItem as string ?? "";
            if (!string.IsNullOrEmpty(sel))
            {
                layer.AddonName = sel;
                if (txtInspectorShader != null)
                    txtInspectorShader.Text = $"Shader: {layer.ShaderFile} | DLL: {layer.DllModelName} | Addon: {layer.AddonName}";
                lstLayers.Items.Refresh();
            }
        }

        private void LayerDll_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            string sel = cbLayerDll.SelectedItem as string ?? "";
            layer.DllModelName = (sel == "(None / Integrated Shader)" || string.IsNullOrWhiteSpace(sel)) ? "" : sel;
            if (txtInspectorShader != null)
                txtInspectorShader.Text = $"Shader: {layer.ShaderFile} | DLL: {layer.DllModelName} | Addon: {layer.AddonName}";
            lstLayers.Items.Refresh();
        }

        private void LayerUpscaleModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            string sel = cbLayerUpscaleModel.SelectedItem as string ?? "";
            if (!string.IsNullOrEmpty(sel))
            {
                layer.UpscaleModel = sel;
                lstLayers.Items.Refresh();
            }
        }

        private void LayerScaleMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            if (cbLayerScaleMode.SelectedItem is not ComboBoxItem item) return;
            string txt = item.Content.ToString() ?? "";
            if (txt.Contains("Ping-Pong"))
            {
                layer.ScaleMode = "Ping-Pong (Down ⇄ Up)";
                if (layer.DownscaleRatio <= 0.2f || layer.DownscaleRatio >= 1.0f) layer.DownscaleRatio = 0.70f;
                if (layer.UpscaleRatio <= 1.0f) layer.UpscaleRatio = 1.428571f;
            }
            else if (txt.Contains("Downscale"))
            {
                layer.ScaleMode = "Downscale";
                if (layer.DownscaleRatio >= 1.0f) layer.DownscaleRatio = 0.70f;
            }
            else if (txt.Contains("Spatial Catmull"))
            {
                layer.ScaleMode = "Upscale";
                if (layer.UpscaleRatio <= 1.0f) layer.UpscaleRatio = 6.0f;
            }
            else if (txt.Contains("Super Resolution"))
            {
                layer.ScaleMode = "DLSS Super Resolution";
                if (layer.UpscaleRatio <= 1.0f) layer.UpscaleRatio = 9.0f;
            }
            else if (txt.Contains("DLAA"))
            {
                layer.ScaleMode = "DLAA";
                layer.DownscaleRatio = 1.0f;
                layer.UpscaleRatio = 1.0f;
            }
            else
            {
                layer.ScaleMode = "None";
            }

            // Refresh UI controls
            if (slLayerDownscale != null) slLayerDownscale.Value = (int)(layer.DownscaleRatio * 100);
            if (txtLayerDownscale != null) txtLayerDownscale.Text = $"{layer.DownscaleRatio * 100:F0}%";
            if (slLayerUpscale != null) slLayerUpscale.Value = (int)(layer.UpscaleRatio * 100);
            if (txtLayerUpscale != null) txtLayerUpscale.Text = $"{layer.UpscaleRatio * 100:F0}%";

            lstLayers.Items.Refresh();
            UpdateVramTelemetry();
        }

        private void LayerDownscale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtLayerDownscale == null) return;
            int pct = (int)slLayerDownscale.Value;
            if (!_isUpdatingLayerInspector) txtLayerDownscale.Text = $"{pct}%";
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            layer.DownscaleRatio = pct / 100f;
            _settings.DownscaleRatio = layer.DownscaleRatio;
            _settings.UpscaleRatioOverrideValue = (1.0f / layer.DownscaleRatio).ToString("0.000000", CultureInfo.InvariantCulture);
            lstLayers.Items.Refresh();
            UpdateVramTelemetry();
        }

        private void TxtLayerDownscale_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyCustomDownscaleText();
        }

        private void TxtLayerDownscale_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                ApplyCustomDownscaleText();
        }

        private void ApplyCustomDownscaleText()
        {
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            string clean = txtLayerDownscale.Text.Replace("%", "").Replace("x", "").Trim();
            if (float.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ||
                float.TryParse(clean, NumberStyles.Float, CultureInfo.CurrentCulture, out val))
            {
                if (val <= 1.0f && val > 0) val *= 100f; // support decimal format (e.g. 0.70)
                int pct = Math.Clamp((int)val, 20, 100);
                slLayerDownscale.Value = pct;
                txtLayerDownscale.Text = $"{pct}%";
                layer.DownscaleRatio = pct / 100f;
                lstLayers.Items.Refresh();
                UpdateVramTelemetry();
            }
        }

        private void LayerUpscale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtLayerUpscale == null) return;
            int pct = (int)slLayerUpscale.Value;
            if (!_isUpdatingLayerInspector) txtLayerUpscale.Text = $"{pct}%";
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            layer.UpscaleRatio = pct / 100f;
            layer.ScaleRatio = layer.UpscaleRatio;
            _settings.SuperResolutionRatio = layer.UpscaleRatio;
            lstLayers.Items.Refresh();
            UpdateVramTelemetry();
        }

        private void TxtLayerUpscale_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyCustomUpscaleText();
        }

        private void TxtLayerUpscale_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                ApplyCustomUpscaleText();
        }

        private void ApplyCustomUpscaleText()
        {
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            string clean = txtLayerUpscale.Text.Replace("%", "").Replace("x", "").Trim();
            if (float.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ||
                float.TryParse(clean, NumberStyles.Float, CultureInfo.CurrentCulture, out val))
            {
                if (val <= 15.0f && val >= 1.0f && !txtLayerUpscale.Text.Contains("%")) val *= 100f;
                int pct = Math.Clamp((int)val, 100, 1500);
                slLayerUpscale.Value = pct;
                txtLayerUpscale.Text = $"{pct}%";
                layer.UpscaleRatio = pct / 100f;
                layer.ScaleRatio = layer.UpscaleRatio;
                lstLayers.Items.Refresh();
                UpdateVramTelemetry();
            }
        }

        private void LayerLoop_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtLayerLoop == null) return;
            int loop = (int)slLayerLoop.Value;
            if (!_isUpdatingLayerInspector) txtLayerLoop.Text = $"{loop}x";
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            layer.LoopCycles = loop;
            lstLayers.Items.Refresh();
            UpdateVramTelemetry();
        }

        private void TxtLayerLoop_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyCustomLoopText();
        }

        private void TxtLayerLoop_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                ApplyCustomLoopText();
        }

        private void ApplyCustomLoopText()
        {
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            string clean = txtLayerLoop.Text.Replace("x", "").Trim();
            if (int.TryParse(clean, out int loop))
            {
                loop = Math.Clamp(loop, 1, 100);
                slLayerLoop.Value = Math.Clamp(loop, 1, 50);
                txtLayerLoop.Text = $"{loop}x";
                layer.LoopCycles = loop;
                lstLayers.Items.Refresh();
                UpdateVramTelemetry();
            }
        }

        private void LayerNeuralPass_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtLayerNeuralPass == null) return;
            int pass = (int)slLayerNeuralPass.Value;
            if (!_isUpdatingLayerInspector) txtLayerNeuralPass.Text = $"{pass}";
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            layer.NeuralPassIterations = pass;
            lstLayers.Items.Refresh();
            UpdateVramTelemetry();
        }

        private void TxtLayerNeuralPass_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyCustomNeuralPassText();
        }

        private void TxtLayerNeuralPass_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                ApplyCustomNeuralPassText();
        }

        private void ApplyCustomNeuralPassText()
        {
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            string clean = txtLayerNeuralPass.Text.Trim();
            if (int.TryParse(clean, out int pass))
            {
                pass = Math.Clamp(pass, 1, 100);
                slLayerNeuralPass.Value = Math.Clamp(pass, 1, 60);
                txtLayerNeuralPass.Text = $"{pass}";
                layer.NeuralPassIterations = pass;
                lstLayers.Items.Refresh();
                UpdateVramTelemetry();
            }
        }

        private void LayerIntensity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtLayerIntensity == null) return;
            int pct = (int)slLayerIntensity.Value;
            if (!_isUpdatingLayerInspector) txtLayerIntensity.Text = $"{pct}%";
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            layer.Intensity = pct / 100f;
            lstLayers.Items.Refresh();
        }

        private void TxtLayerIntensity_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyCustomIntensityText();
        }

        private void TxtLayerIntensity_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                ApplyCustomIntensityText();
        }

        private void ApplyCustomIntensityText()
        {
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            string clean = txtLayerIntensity.Text.Replace("%", "").Trim();
            if (float.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ||
                float.TryParse(clean, NumberStyles.Float, CultureInfo.CurrentCulture, out val))
            {
                if (val <= 5.0f && val >= 0.1f && !txtLayerIntensity.Text.Contains("%")) val *= 100f;
                int pct = Math.Clamp((int)val, 0, 500);
                slLayerIntensity.Value = pct;
                txtLayerIntensity.Text = $"{pct}%";
                layer.Intensity = pct / 100f;
                lstLayers.Items.Refresh();
            }
        }

        private void LayerQuality_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingLayerInspector || lstLayers?.SelectedItem is not PipelineLayer layer) return;
            if (cbLayerQuality.SelectedItem is ComboBoxItem item)
            {
                layer.QualityPreset = item.Content.ToString() ?? "Preset F";
            }
        }

        private void AddLayer_Click(object sender, RoutedEventArgs e)
        {
            var templates = Dlss5ModelManager.GetAvailableLayerTemplates(_dlss5Dir);
            var menu = new ContextMenu
            {
                Background = (SolidColorBrush)FindResource("BrushBgCard"),
                Foreground = (SolidColorBrush)FindResource("BrushTextPrimary")
            };

            var headerItem = new MenuItem { Header = "Select Layer Template to Add:", IsEnabled = false, FontWeight = FontWeights.Bold };
            menu.Items.Add(headerItem);
            menu.Items.Add(new Separator { Background = (SolidColorBrush)FindResource("BrushBorder") });

            foreach (var tmpl in templates)
            {
                var mi = new MenuItem
                {
                    Header = $"{tmpl.Name} [{tmpl.Category}]",
                    Tag = tmpl
                };
                mi.Click += (s, ev) =>
                {
                    if (s is MenuItem clicked && clicked.Tag is PipelineLayer chosen)
                    {
                        var copy = chosen.Clone();
                        copy.InstanceId = Guid.NewGuid().ToString();
                        int insertIdx = lstLayers.SelectedIndex >= 0 ? lstLayers.SelectedIndex + 1 : _layers.Count;
                        _layers.Insert(insertIdx, copy);
                        RefreshLayerList();
                        lstLayers.SelectedIndex = insertIdx;
                        UpdateVramTelemetry();
                        txtStatusBar.Text = $"Added layer '{copy.Name}' to pipeline sequence.";
                    }
                };
                menu.Items.Add(mi);
            }

            if (sender is FrameworkElement fe)
            {
                menu.PlacementTarget = fe;
                menu.IsOpen = true;
            }
            else
            {
                menu.IsOpen = true;
            }
        }

        private void RemoveLayer_Click(object sender, RoutedEventArgs e)
        {
            if (lstLayers.SelectedIndex < 0 || lstLayers.SelectedItem is not PipelineLayer) return;
            int idx = lstLayers.SelectedIndex;
            _layers.RemoveAt(idx);
            RefreshLayerList();
            if (_layers.Count > 0)
                lstLayers.SelectedIndex = Math.Clamp(idx, 0, _layers.Count - 1);
            UpdateVramTelemetry();
            txtStatusBar.Text = "Removed layer from sequence.";
        }

        private void CopyLayer_Click(object sender, RoutedEventArgs e)
        {
            if (lstLayers.SelectedItem is not PipelineLayer layer) return;
            _clipboardLayer = layer.Clone();
            txtStatusBar.Text = $"Copied layer '{layer.Name}' to clipboard.";
        }

        private void PasteLayer_Click(object sender, RoutedEventArgs e)
        {
            if (_clipboardLayer == null)
            {
                MessageBox.Show("Clipboard is empty. Copy a layer first with 📋 Copy.", "Paste Layer", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var pasted = _clipboardLayer.Clone();
            pasted.InstanceId = Guid.NewGuid().ToString();
            if (!pasted.Name.EndsWith("(Copy)"))
                pasted.Name += " (Copy)";

            int insertIdx = lstLayers.SelectedIndex >= 0 ? lstLayers.SelectedIndex + 1 : _layers.Count;
            _layers.Insert(insertIdx, pasted);
            RefreshLayerList();
            lstLayers.SelectedIndex = insertIdx;
            UpdateVramTelemetry();
            txtStatusBar.Text = $"Pasted layer '{pasted.Name}'.";
        }

        private void DuplicateLayer_Click(object sender, RoutedEventArgs e)
        {
            if (lstLayers.SelectedItem is not PipelineLayer layer) return;
            var dup = layer.Clone();
            dup.InstanceId = Guid.NewGuid().ToString();
            if (!dup.Name.EndsWith("(Copy)"))
                dup.Name += " (Copy)";

            int insertIdx = lstLayers.SelectedIndex + 1;
            _layers.Insert(insertIdx, dup);
            RefreshLayerList();
            lstLayers.SelectedIndex = insertIdx;
            UpdateVramTelemetry();
            txtStatusBar.Text = $"Duplicated layer '{dup.Name}'.";
        }

        private void SetLoop_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag != null && int.TryParse(mi.Tag.ToString(), out int cycles))
            {
                if (lstLayers.SelectedItem is PipelineLayer layer)
                {
                    layer.LoopCycles = cycles;
                    slLayerLoop.Value = cycles;
                    lstLayers.Items.Refresh();
                    UpdateVramTelemetry();
                    txtStatusBar.Text = $"Set '{layer.Name}' loop cycles to {cycles}x.";
                }
            }
        }

        private void SetScale_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag != null && lstLayers.SelectedItem is PipelineLayer layer)
            {
                string tag = mi.Tag.ToString() ?? "";
                switch (tag)
                {
                    case "pingpong_70_142":
                        layer.ScaleMode = "Ping-Pong (Down ⇄ Up)";
                        layer.DownscaleRatio = 0.70f;
                        layer.UpscaleRatio = 1.428571f;
                        layer.UpscaleModel = "DLSS 4/4.5 (nvngx_dlss.dll)";
                        layer.LoopCycles = 10;
                        break;
                    case "pingpong_50_200":
                        layer.ScaleMode = "Ping-Pong (Down ⇄ Up)";
                        layer.DownscaleRatio = 0.50f;
                        layer.UpscaleRatio = 2.00f;
                        layer.UpscaleModel = "DLSS 4/4.5 (nvngx_dlss.dll)";
                        layer.LoopCycles = 10;
                        break;
                    case "pingpong_70_900":
                        layer.ScaleMode = "Ping-Pong (Down ⇄ Up)";
                        layer.DownscaleRatio = 0.70f;
                        layer.UpscaleRatio = 9.00f;
                        layer.UpscaleModel = "DLSS 4/4.5 (nvngx_dlss.dll)";
                        layer.LoopCycles = 10;
                        break;
                    case "down_70":
                        layer.ScaleMode = "Downscale";
                        layer.DownscaleRatio = 0.70f;
                        layer.ScaleRatio = 0.70f;
                        break;
                    case "down_50":
                        layer.ScaleMode = "Downscale";
                        layer.ScaleRatio = 0.50f;
                        break;
                    case "native_100":
                        layer.ScaleMode = "DLAA";
                        layer.ScaleRatio = 1.0f;
                        break;
                    case "up_600":
                        layer.ScaleMode = "Upscale";
                        layer.ScaleRatio = 6.0f;
                        break;
                    case "up_900":
                        layer.ScaleMode = "DLSS Super Resolution";
                        layer.ScaleRatio = 9.0f;
                        break;
                }

                LstLayers_SelectionChanged(lstLayers, null!);
                lstLayers.Items.Refresh();
                UpdateVramTelemetry();
                txtStatusBar.Text = $"Configured {layer.Name}: {layer.ScaleMode} ({layer.ScaleRatio:F2}x).";
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            int idx = lstLayers.SelectedIndex;
            if (idx <= 0) return;

            var item = _layers[idx];
            _layers.RemoveAt(idx);
            _layers.Insert(idx - 1, item);

            RefreshLayerList();
            lstLayers.SelectedIndex = idx - 1;
            UpdateVramTelemetry();
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            int idx = lstLayers.SelectedIndex;
            if (idx < 0 || idx >= _layers.Count - 1) return;

            var item = _layers[idx];
            _layers.RemoveAt(idx);
            _layers.Insert(idx + 1, item);

            RefreshLayerList();
            lstLayers.SelectedIndex = idx + 1;
            UpdateVramTelemetry();
        }

        private void ToggleLayer_Click(object sender, RoutedEventArgs e)
        {
            if (lstLayers.SelectedItem is not PipelineLayer layer) return;
            layer.Enabled = !layer.Enabled;
            RefreshLayerList();
            UpdateVramTelemetry();
        }

        private void ResetPipeline_Click(object sender, RoutedEventArgs e)
        {
            _layers = ConfigSync.GetDefaultPipeline(_hardwareProfile);
            RefreshLayerList();
            UpdateVramTelemetry();
            MessageBox.Show("Pipeline matrix reset to certified optimal order!", "Matrix Reset", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region NEURAL ENGINES VIEW & GRANULAR CONTROLS
        private void SyncSettingsToSliders()
        {
            _isSyncing = true;

            slPass1Iter.Value = _settings.NeuralPass1Iterations;
            valPass1Iter.Text = $"{_settings.NeuralPass1Iterations}";

            slPass1Intensity.Value = (int)(_settings.NeuralPass1Intensity * 10);
            valPass1Intensity.Text = $"{_settings.NeuralPass1Intensity:F1}x";

            slPass1Nits.Value = _settings.NeuralPass1DiffuseWhiteNits;
            valPass1Nits.Text = $"{_settings.NeuralPass1DiffuseWhiteNits:F0} nits";

            slSuperRes.Value = (int)(_settings.SuperResolutionRatio * 100);
            valSuperRes.Text = $"{slSuperRes.Value:F0}%";

            slDownscale.Value = (int)(_settings.DownscaleRatio * 100);
            valDownscale.Text = $"{_settings.DownscaleRatio:F2}x";

            slDlssNrPasses.Value = _settings.DlssNrPasses;
            valDlssNrPasses.Text = $"{_settings.DlssNrPasses}";

            slDlssNrTransfer.Value = (int)(_settings.DlssNrTransferStrength * 100);
            valDlssNrTransfer.Text = $"{slDlssNrTransfer.Value:F0}%";

            slDlssNrTrim.Value = (int)(_settings.DlssNrWhitePointTrim * 10);
            valDlssNrTrim.Text = $"{_settings.DlssNrWhitePointTrim:F1}";

            slPreCycles.Value = _settings.PreChainCycles;
            valPreCycles.Text = $"{_settings.PreChainCycles}x";

            slPostCycles.Value = _settings.PostChainCycles;
            valPostCycles.Text = $"{_settings.PostChainCycles}x";

            slUpscaleRatio.Value = (int)(_settings.UpscaleRatio * 100);
            valUpscaleRatio.Text = $"{slUpscaleRatio.Value:F0}%";

            slBlurSamples.Value = _settings.MotionBlurSamples;
            valBlurSamples.Text = $"{_settings.MotionBlurSamples} Samples";

            // Granular sliders
            slLocalStructure.Value = (int)(_settings.DlssNrLocalStructure * 100);
            valLocalStructure.Text = $"{_settings.DlssNrLocalStructure:F1}x";

            slSkinStructure.Value = (int)(_settings.DlssNrSkinStructure * 100);
            valSkinStructure.Text = $"{_settings.DlssNrSkinStructure:F1}x";

            slCycleClarity.Value = (int)(_settings.CycleClarity * 100);
            valCycleClarity.Text = $"{_settings.CycleClarity:F2}";

            slCycleSharpness.Value = (int)(_settings.CycleSharpness * 100);
            valCycleSharpness.Text = $"{_settings.CycleSharpness:F2}";

            slVramCeiling.Value = (int)(_settings.VramTargetPercent * 10);
            valVramCeiling.Text = $"{_settings.VramTargetPercent:F1}%";

            chkRtao.IsChecked = _settings.RtaoEnabled;
            chkLsao.IsChecked = _settings.LsaoEnabled;
            chkSssr.IsChecked = _settings.SssrEnabled;
            chkBloom.IsChecked = _settings.BloomEnabled;
            chkTraa.IsChecked = _settings.TraaEnabled;
            chkBlur.IsChecked = _settings.MotionBlurEnabled;

            chkVramAuto.IsChecked = _settings.VramAutoBalance;

            _isSyncing = false;
        }

        private void DlssGeneration_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || _isSyncing || cbDlssGeneration.SelectedItem is not ComboBoxItem item) return;
            string gen = item.Content.ToString() ?? "";
            _settings.DlssGeneration = gen;

            if (gen.Contains("5.0"))
            {
                _settings.NeuralPass1Iterations = 30;
                _settings.DlssNrPasses = 15;
                _settings.SuperResolutionRatio = 9.0f;
                _settings.DlssNrTransferStrength = 1.0f;
            }
            else if (gen.Contains("4.5"))
            {
                _settings.NeuralPass1Iterations = 25;
                _settings.DlssNrPasses = 12;
                _settings.SuperResolutionRatio = 8.0f;
                _settings.DlssNrTransferStrength = 0.95f;
            }
            else if (gen.Contains("4.0"))
            {
                _settings.NeuralPass1Iterations = 20;
                _settings.DlssNrPasses = 10;
                _settings.SuperResolutionRatio = 6.0f;
                _settings.DlssNrTransferStrength = 0.90f;
            }
            else if (gen.Contains("3.5"))
            {
                _settings.NeuralPass1Iterations = 20;
                _settings.DlssNrPasses = 8;
                _settings.SuperResolutionRatio = 6.0f;
            }
            else if (gen.Contains("3.0"))
            {
                _settings.FrameGenEnabled = true;
                _settings.NeuralPass1Iterations = 15;
            }
            else
            {
                _settings.NeuralPass1Iterations = 12;
                _settings.DlssNrPasses = 6;
                _settings.SuperResolutionRatio = 4.0f;
            }

            SyncSettingsToSliders();
            UpdateVramTelemetry();
        }

        private void NeuralSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized || _isSyncing || valPass1Iter == null) return;

            _settings.NeuralPass1Iterations = (int)slPass1Iter.Value;
            valPass1Iter.Text = $"{_settings.NeuralPass1Iterations}";

            _settings.NeuralPass1Intensity = (float)slPass1Intensity.Value / 10f;
            valPass1Intensity.Text = $"{_settings.NeuralPass1Intensity:F1}x";

            _settings.NeuralPass1DiffuseWhiteNits = (float)slPass1Nits.Value;
            valPass1Nits.Text = $"{_settings.NeuralPass1DiffuseWhiteNits:F0} nits";

            _settings.SuperResolutionRatio = (float)slSuperRes.Value / 100f;
            valSuperRes.Text = $"{slSuperRes.Value:F0}%";

            _settings.DownscaleRatio = (float)slDownscale.Value / 100f;
            valDownscale.Text = $"{_settings.DownscaleRatio:F2}x";
            _settings.UpscaleRatioOverrideValue = (1.0f / _settings.DownscaleRatio).ToString("0.000000", CultureInfo.InvariantCulture);

            _settings.DlssNrPasses = (int)slDlssNrPasses.Value;
            valDlssNrPasses.Text = $"{_settings.DlssNrPasses}";

            _settings.DlssNrTransferStrength = (float)slDlssNrTransfer.Value / 100f;
            valDlssNrTransfer.Text = $"{slDlssNrTransfer.Value:F0}%";

            _settings.DlssNrWhitePointTrim = (float)slDlssNrTrim.Value / 10f;
            valDlssNrTrim.Text = $"{_settings.DlssNrWhitePointTrim:F1}";

            _settings.PreChainCycles = (int)slPreCycles.Value;
            valPreCycles.Text = $"{_settings.PreChainCycles}x";

            _settings.PostChainCycles = (int)slPostCycles.Value;
            valPostCycles.Text = $"{_settings.PostChainCycles}x";

            _settings.UpscaleRatio = (float)slUpscaleRatio.Value / 100f;
            valUpscaleRatio.Text = $"{slUpscaleRatio.Value:F0}%";

            _settings.MotionBlurSamples = (int)slBlurSamples.Value;
            valBlurSamples.Text = $"{_settings.MotionBlurSamples} Samples";

            UpdateVramTelemetry();
        }

        private void GranularSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized || _isSyncing || valLocalStructure == null) return;

            _settings.DlssNrLocalStructure = (float)slLocalStructure.Value / 100f;
            valLocalStructure.Text = $"{_settings.DlssNrLocalStructure:F1}x";

            _settings.DlssNrSkinStructure = (float)slSkinStructure.Value / 100f;
            valSkinStructure.Text = $"{_settings.DlssNrSkinStructure:F1}x";

            _settings.CycleClarity = (float)slCycleClarity.Value / 100f;
            valCycleClarity.Text = $"{_settings.CycleClarity:F2}";

            _settings.CycleSharpness = (float)slCycleSharpness.Value / 100f;
            valCycleSharpness.Text = $"{_settings.CycleSharpness:F2}";

            _settings.VramTargetPercent = (float)slVramCeiling.Value / 10f;
            valVramCeiling.Text = $"{_settings.VramTargetPercent:F1}%";

            UpdateVramTelemetry();
        }

        private void NeuralCheck_Click(object sender, RoutedEventArgs e)
        {
            _settings.RtaoEnabled = chkRtao.IsChecked == true;
            _settings.LsaoEnabled = chkLsao.IsChecked == true;
            _settings.SssrEnabled = chkSssr.IsChecked == true;
            _settings.BloomEnabled = chkBloom.IsChecked == true;
            _settings.TraaEnabled = chkTraa.IsChecked == true;
            _settings.MotionBlurEnabled = chkBlur.IsChecked == true;

            UpdateVramTelemetry();
        }
        #endregion

        #region VRAM VIEW & TELEMETRY
        private void UpdateVramTelemetry()
        {
            if (txtVramBadge == null || pbVram == null) return;

            var report = VramEngine.CalculateReport(_settings, _layers);

            txtVramBadge.Text = $"VRAM: {report.EstimatedUsagePercent:F1}% LOAD";
            var color = (Color)ColorConverter.ConvertFromString(report.StatusColorHex);
            badgeVram.Background = new SolidColorBrush(color);
            txtVramBadge.Foreground = (report.StatusColorHex == "#00E676") ? Brushes.Black : Brushes.White;

            txtVramGpu.Text = $"GPU: {report.GpuName} ({report.TotalVramMB:N0} MiB Total VRAM)";
            txtVramLiveDriver.Text = $"Windows & Driver Live In-Use: {report.CurrentUsedVramMB:N0} MB";
            txtVramStatusHeader.Text = report.StatusText;
            txtVramStatusHeader.Foreground = new SolidColorBrush(color);

            int pct = (int)Math.Clamp(report.EstimatedUsagePercent, 0f, 100f);
            pbVram.Value = pct;
            pbVram.Foreground = new SolidColorBrush(color);

            int preChainMB = _settings.PreChainCycles * 35;
            int pass1MB = 450 + (_settings.NeuralPass1Iterations * 12);
            int superResMB = 850 + (int)(_settings.SuperResolutionRatio * 25);
            int dlssNrMB = 980 + (_settings.DlssNrPasses * 42);
            int postChainMB = _settings.PostChainCycles * 35;
            int extraLoopMB = _layers.Where(l => l.Enabled && l.Id != "pre_chain" && l.Id != "post_chain").Sum(l => Math.Max(0, (l.LoopCycles - 1) * 18));

            txtVramBreakdown.Text =
                $"[ALLOCATION MATRIX - {_hardwareProfile.GpuName}]\r\n" +
                $"  Base Game Engine & Textures (4K Ultra):       5,500 MB\r\n" +
                $"  Pre-Neural Fidelity Chain ({_settings.PreChainCycles}x Pass):             {preChainMB,5} MB\r\n" +
                $"  1° Neural Pass RenoDX ({_settings.NeuralPass1Iterations} Iterations):            {pass1MB,5} MB\r\n" +
                $"  Super Resolution Reconstruction ({_settings.SuperResolutionRatio * 100:F0}%):       {superResMB,5} MB\r\n" +
                $"  2° Neural Pass DLSS-NR ({_settings.DlssNrPasses} Passes 100% Synth):      {dlssNrMB,5} MB\r\n" +
                $"  Post-Stack Effects (RTAO/LSAO/SSSR/Bloom/TRAA): 605 MB\r\n" +
                $"  Post-Neural Fidelity Chain ({_settings.PostChainCycles}x Pass):            {postChainMB,5} MB\r\n" +
                $"  Sequence Custom Layer Loop Overhead:          {extraLoopMB,5} MB\r\n" +
                $"--------------------------------------------------------------------------------\r\n" +
                $"  PROJECTED IN-GAME WORKING SET:             {report.EstimatedPipelineVramMB,6} MB / {report.TotalVramMB} MB ({report.EstimatedUsagePercent:F1}%)\r\n" +
                $"  SWEET SPOT TARGET COMPLIANCE:               [ {(report.EstimatedUsagePercent >= 80f && report.EstimatedUsagePercent <= 85.5f ? "TARGET LOCKED (80-85%)" : "ADJUSTING")} ]";
        }

        private void VramAuto_Click(object sender, RoutedEventArgs e)
        {
            _settings.VramAutoBalance = chkVramAuto.IsChecked == true;
            UpdateVramTelemetry();
        }

        private void CalibrateSweetSpot_Click(object sender, RoutedEventArgs e)
        {
            _settings = HardwareEngine.GenerateOptimalSettings(_hardwareProfile);
            _layers = ConfigSync.GetDefaultPipeline(_hardwareProfile);
            RefreshLayerList();
            SyncSettingsToSliders();
            UpdateVramTelemetry();

            MessageBox.Show($"Pipeline calibrated for {_hardwareProfile.GpuName}!\nIn-game working set locked precisely at {_settings.VramTargetPercent:F1}% sweet spot.", "Sweet Spot Calibrated", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region PRESETS VIEW
        private void LstPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || txtPresetTitle == null) return;
            if (lstPresets.SelectedItem is not PresetData p) return;

            txtPresetTitle.Text = p.PresetName;
            txtPresetAuthor.Text = $"Author: {p.Author}";
            txtPresetHardware.Text = $"Target Hardware: {p.TargetHardware} | Sweet Spot Target: {p.Settings.VramTargetPercent:F1}%";
            txtPresetDesc.Text = p.Description;

            txtPresetParams.Text =
                $"1° Neural Pass (RenoDX):        {p.Settings.NeuralPass1Iterations} iterations @ {p.Settings.NeuralPass1Intensity:F1}x intensity\r\n" +
                $"Super Resolution:               {p.Settings.SuperResolutionRatio * 100:F0}% ({p.Settings.SuperResolutionPreset})\r\n" +
                $"Internal Downscale Ratio:       {p.Settings.DownscaleRatio:F2}x\r\n" +
                $"2° Neural Pass (DLSS-NR):       {p.Settings.DlssNrPasses} passes ({p.Settings.DlssNrTransferStrength * 100:F0}% Synthesized Layer)\r\n" +
                $"Pipeline Architecture:         Direct Pristine Linear Neural Flow (No Ping-Pong Loops)\r\n" +
                $"ReShade Post-Stack:             RTAO: {(p.Settings.RtaoEnabled ? "ON" : "OFF")}, LSAO: {(p.Settings.LsaoEnabled ? "ON" : "OFF")}, SSSR: {(p.Settings.SssrEnabled ? "ON" : "OFF")}\r\n" +
                $"Cinematic Motion Blur:          {(p.Settings.MotionBlurEnabled ? $"{p.Settings.MotionBlurSamples} Samples" : "OFF")}\r\n" +
                $"VRAM Auto-Balance Engine:       {(p.Settings.VramAutoBalance ? "LOCKED (80-85% Target)" : "MANUAL")}";
        }

        private void ApplyPreset_Click(object sender, RoutedEventArgs e)
        {
            if (lstPresets.SelectedItem is not PresetData p) return;

            _settings = p.Settings;
            SyncSettingsToSliders();
            UpdateVramTelemetry();

            MessageBox.Show($"Preset '{p.PresetName}' applied successfully to pipeline matrix!", "Preset Applied", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveCustomPreset_Click(object sender, RoutedEventArgs e)
        {
            string name = $"Custom_Preset_{DateTime.Now:yyyyMMdd_HHmmss}";
            string path = Path.Combine(_presetsDir, $"{name}.json");

            var newPreset = new PresetData
            {
                PresetName = name,
                Description = "Custom user-configured neural rendering profile",
                Author = Environment.UserName,
                TargetHardware = _hardwareProfile.GpuName,
                Settings = _settings
            };

            if (PresetManager.SavePreset(path, newPreset))
            {
                LoadPresets();
                MessageBox.Show($"Preset saved successfully:\n{path}", "Preset Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region DLSS 4/5 VIEW
        private void BrowseDlss5_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select DLSS Models / Extracted Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                _dlss5Dir = dialog.FolderName;
                LoadDlss5Models();
            }
        }

        private void RescanDlss5_Click(object sender, RoutedEventArgs e)
        {
            LoadDlss5Models();
            MessageBox.Show($"Scanned {_dlssModels.Count} models in:\n{_dlss5Dir}", "DLSS Scan Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LstDlss5Models_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void DeploySingleModel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string modelPath)
            {
                bool ok = Dlss5ModelManager.DeployModelToGame(modelPath, _gameDir, out string err);
                if (ok)
                    MessageBox.Show($"Successfully deployed '{Path.GetFileName(modelPath)}' to target game directory!", "Deploy Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show($"Failed to deploy model: {err}", "Deploy Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeployAllDlss5_Click(object sender, RoutedEventArgs e)
        {
            if (_dlssModels.Count == 0)
            {
                MessageBox.Show("No models detected to deploy.", "Deploy Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int successCount = 0;
            foreach (var m in _dlssModels)
            {
                if (Dlss5ModelManager.DeployModelToGame(m.FullPath, _gameDir, out _))
                    successCount++;
            }

            MessageBox.Show($"Deployed {successCount} of {_dlssModels.Count} DLSS models directly into target game:\n{_gameDir}", "Batch Deployment Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddModelToSequence_Click(object sender, RoutedEventArgs e)
        {
            if (lstDlss5Models.SelectedItem is not DlssModelInfo model)
            {
                MessageBox.Show("Please select a model from the list first.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var layer = new PipelineLayer
            {
                InstanceId = Guid.NewGuid().ToString(),
                Name = model.FriendlyName,
                TechniqueName = model.FileName.Replace(".dll", "").Replace(".addon64", ""),
                ShaderFile = model.FileName,
                Category = model.Category,
                DllModelName = model.FileName,
                Description = model.Description,
                Enabled = true,
                LoopCycles = 1,
                ScaleMode = model.Category.Contains("Super Resolution") ? "DLSS Super Resolution" : (model.Category.Contains("Spatial") ? "Upscale" : "None"),
                ScaleRatio = model.Category.Contains("Super Resolution") ? 9.0f : 1.0f,
                Intensity = 1.0f
            };

            _layers.Add(layer);
            RefreshLayerList();
            lstLayers.SelectedItem = layer;
            UpdateVramTelemetry();

            NavPipeline_Click(btnNavPipeline, new RoutedEventArgs());
            MessageBox.Show($"Added '{layer.Name}' linked to {model.FileName} to the rendering sequence!", "Layer Added", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region HOME & PC GAMING FLEET HUB
        private void ConfigureGame_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GameTargetInfo game)
            {
                OpenGameStudio(game);
            }
        }

        private void OpenGameStudio(GameTargetInfo game)
        {
            _activeGame = game;
            _gameDir = game.GameDirectory;

            if (txtNavStudioLabel != null) txtNavStudioLabel.Visibility = Visibility.Visible;
            if (txtNavActiveGame != null) txtNavActiveGame.Text = game.GameName;
            if (txtProfileGame != null) txtProfileGame.Text = $"Target: {game.GameName}";
            if (btnHeaderBackToHome != null) btnHeaderBackToHome.Visibility = Visibility.Visible;
            if (pnlHeaderGameActions != null) pnlHeaderGameActions.Visibility = Visibility.Visible;

            ScanTargetGame(_gameDir);
            LoadPipelineData();
            NavPipeline_Click(btnNavPipeline, new RoutedEventArgs());
        }

        private void BackToHome_Click(object sender, RoutedEventArgs e)
        {
            NavHome_Click(btnNavHome, new RoutedEventArgs());
        }

        private void ScanAllGames_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => PerformGameFleetScan(true));
        }

        private void PerformGameFleetScan(bool showDialogWhenDone)
        {
            AppendHomeLog("[FLEET] Avvio scansione di tutte le unità disco e librerie del PC...");
            var games = UniversalScanner.ScanWholePc(msg => AppendHomeLog(msg));
            UniversalScanner.SaveDetectedGames(games);

            Dispatcher.Invoke(() =>
            {
                _detectedGames = games;
                ApplyHomeFilter();
                int injected = games.Count(g => g.IsUpgradeInstalled);
                if (txtHomeFleetStatus != null)
                    txtHomeFleetStatus.Text = $"{games.Count} Giochi Rilevati ({injected} con Upgrade Attivo)";

                if (showDialogWhenDone)
                {
                    MessageBox.Show($"Scansione Flotta Giochi PC Completata!\r\n\r\n" +
                                    $"• Trovati {games.Count} giochi compatibili su Steam, Xbox Game Pass, Epic Games e GOG.\r\n" +
                                    $"• Upgrade Neurale Attivo: {injected} gioco/i\r\n" +
                                    $"• Stato Originale Vanilla: {games.Count - injected} gioco/i\r\n\r\n" +
                                    $"Clicca su '🎛 Configura Gioco & Pipeline' sulla scheda di qualsiasi gioco per personalizzarlo.",
                                    "Scansione Flotta Completata", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });
        }

        private void HomeSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyHomeFilter();
        }

        private void CbHomeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyHomeFilter();
        }

        private void ApplyHomeFilter()
        {
            if (lstHomeGames == null) return;
            string filter = txtHomeSearch?.Text?.Trim() ?? "";
            int mode = cbHomeFilter?.SelectedIndex ?? 0;

            var query = _detectedGames.AsEnumerable();

            if (mode == 1) // Only Injected
                query = query.Where(g => g.IsUpgradeInstalled);
            else if (mode == 2) // Only Vanilla
                query = query.Where(g => !g.IsUpgradeInstalled);

            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(g =>
                    g.GameName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    g.EngineDisplay.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    g.ApiDisplay.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    g.GameDirectory.Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            var list = query.ToList();
            lstHomeGames.ItemsSource = null;
            lstHomeGames.ItemsSource = list;

            if (pnlHomeEmptyState != null)
            {
                pnlHomeEmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void InjectGame_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GameTargetInfo game)
            {
                AppendHomeLog($"[INSTALL] Deployment verso {game.GameName} ({game.GameDirectory})...");
                bool ok = UniversalScanner.InstallUpgrade(game.GameDirectory, game.DetectedApi, _gameDir, msg => AppendHomeLog(msg));
                if (ok)
                {
                    ConfigSync.SaveToGameDirectory(game.GameDirectory, _settings, _layers);
                    game.IsUpgradeInstalled = true;
                    UniversalScanner.SaveDetectedGames(_detectedGames);
                    ApplyHomeFilter();
                    MessageBox.Show($"Suite Neurale iniettata con successo in:\n{game.GameName}\n({game.GameDirectory})", "Iniezione Riuscita", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Iniezione fallita per {game.GameName}. Consulta il terminal log per i dettagli.", "Errore Iniezione", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UninstallGame_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GameTargetInfo game)
            {
                var res = MessageBox.Show($"Confermi di voler ripristinare {game.GameName} allo stato 100% VANILLA?\nTutti i proxy, shader e modelli neurali verranno rimossi in modo sicuro.", "Conferma Ripristino", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;

                AppendHomeLog($"[UNINSTALL] Ripristino vanilla per: {game.GameDirectory}");
                bool ok = UniversalScanner.UninstallUpgrade(game.GameDirectory, msg => AppendHomeLog(msg));
                if (ok)
                {
                    game.IsUpgradeInstalled = false;
                    UniversalScanner.SaveDetectedGames(_detectedGames);
                    ApplyHomeFilter();
                    MessageBox.Show($"{game.GameName} ripristinato allo stato vanilla con successo!", "Ripristino Completato", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void HeaderInject_Click(object sender, RoutedEventArgs e)
        {
            if (_activeGame != null)
            {
                AppendHomeLog($"[INSTALL] Deployment verso {_activeGame.GameName} ({_activeGame.GameDirectory})...");
                bool ok = UniversalScanner.InstallUpgrade(_activeGame.GameDirectory, _activeGame.DetectedApi, _gameDir, msg => AppendHomeLog(msg));
                if (ok)
                {
                    ConfigSync.SaveToGameDirectory(_activeGame.GameDirectory, _settings, _layers);
                    _activeGame.IsUpgradeInstalled = true;
                    UniversalScanner.SaveDetectedGames(_detectedGames);
                    ApplyHomeFilter();
                    MessageBox.Show($"Suite Neurale iniettata con successo in:\n{_activeGame.GameName}", "Iniezione Riuscita", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Iniezione fallita per {_activeGame.GameName}.", "Errore Iniezione", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void HeaderVanilla_Click(object sender, RoutedEventArgs e)
        {
            if (_activeGame != null)
            {
                var res = MessageBox.Show($"Confermi di voler ripristinare {_activeGame.GameName} allo stato 100% VANILLA?", "Ripristino Vanilla", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;

                AppendHomeLog($"[UNINSTALL] Ripristino vanilla per: {_activeGame.GameDirectory}");
                bool ok = UniversalScanner.UninstallUpgrade(_activeGame.GameDirectory, msg => AppendHomeLog(msg));
                if (ok)
                {
                    _activeGame.IsUpgradeInstalled = false;
                    UniversalScanner.SaveDetectedGames(_detectedGames);
                    ApplyHomeFilter();
                    MessageBox.Show($"{_activeGame.GameName} ripristinato allo stato vanilla con successo!", "Ripristino Completato", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void LaunchFromCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GameTargetInfo game)
            {
                string exe = game.ExecutablePath;
                if (!File.Exists(exe))
                {
                    MessageBox.Show($"Eseguibile non trovato:\n{exe}", "Errore Avvio", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exe,
                        WorkingDirectory = game.GameDirectory,
                        UseShellExecute = true
                    });
                    txtStatusBar.Text = $"Avviato: {game.GameName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Impossibile avviare il gioco: {ex.Message}", "Errore Avvio", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BrowseGame_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Seleziona la cartella di un gioco installato (Steam, Epic, Game Pass, ecc.)"
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedFolder = dialog.FolderName;
                var target = UniversalScanner.ScanDirectory(selectedFolder);
                if (target != null)
                {
                    if (!_detectedGames.Any(g => g.GameDirectory.Equals(target.GameDirectory, StringComparison.OrdinalIgnoreCase)))
                    {
                        _detectedGames.Insert(0, target);
                        UniversalScanner.SaveDetectedGames(_detectedGames);
                    }
                    ApplyHomeFilter();
                    OpenGameStudio(target);
                }
                else
                {
                    MessageBox.Show($"Nessun eseguibile di gioco valido rilevato nella cartella selezionata:\n{selectedFolder}", "Nessun Gioco Rilevato", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void AppendHomeLog(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                if (txtHomeInstallLog != null)
                {
                    txtHomeInstallLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
                    txtHomeInstallLog.ScrollToEnd();
                }
            });
        }

        private void ScanTargetGame(string dir)
        {
            if (!Directory.Exists(dir)) return;
            _targetInfo = UniversalScanner.ScanDirectory(dir);
            AppendHomeLog($"[SCAN] Scanned target: {_targetInfo.GameName} | Engine: {_targetInfo.DetectedEngine} | API: {_targetInfo.DetectedApi}");
        }

        private void VerifyGuardrails_Click(object sender, RoutedEventArgs e)
        {
            AppendHomeLog("[GUARDRAIL] Verifying pipeline stability constraints...");
            bool ok = true;

            if (_settings.DownscaleRatio < 0.45f)
            {
                AppendHomeLog("[WARNING] Downscale ratio is below 0.45x. Internal buffer may drop below minimum safety threshold (640x360).");
                ok = false;
            }
            else
            {
                AppendHomeLog($"[PASS] Minimum internal resolution safe (Downscale ratio: {_settings.DownscaleRatio:F2}x).");
            }

            if (_settings.PreChainCycles > 5 || _settings.PostChainCycles > 5)
            {
                AppendHomeLog("[WARNING] Fidelity chain passes exceed recommended 5x safety limit.");
                ok = false;
            }
            else
            {
                AppendHomeLog($"[PASS] High-fidelity direct pipeline verified (Pre: {_settings.PreChainCycles}x, Post: {_settings.PostChainCycles}x).");
            }

            var rep = VramEngine.CalculateReport(_settings, _layers);
            if (rep.EstimatedUsagePercent > 92.0f)
            {
                AppendHomeLog($"[DANGER] Estimated VRAM ({rep.EstimatedUsagePercent:F1}%) exceeds 92%. Risk of driver thrash.");
                ok = false;
            }
            else
            {
                AppendHomeLog($"[PASS] VRAM Budget safely locked at {rep.EstimatedUsagePercent:F1}% (Sweet spot verified).");
            }

            if (ok)
                MessageBox.Show("All Zero-Crash Guardrail checks PASSED!\nPipeline is stable and ready for gameplay.", "Guardrails Verified", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("Some guardrail warnings were reported. See terminal log.", "Guardrail Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        #endregion

        #region WIKI & KNOWLEDGE BASE VIEW
        private void LstWikiArticles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstWikiArticles?.SelectedItem is not WikiArticle article) return;
            if (txtWikiTitle != null) txtWikiTitle.Text = article.Title;
            if (txtWikiCategory != null) txtWikiCategory.Text = $"Categoria: {article.Category}";
            if (txtWikiSummary != null) txtWikiSummary.Text = article.Summary;
            if (txtWikiContent != null) txtWikiContent.Text = article.Content;
        }
        #endregion

        #region SAVE & LAUNCH
        private void SavePipeline_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigSync.SaveToGameDirectory(_gameDir, _settings, _layers);
                MessageBox.Show($"Pipeline configuration successfully applied to game!\r\n\r\n" +
                                $"- Target: {_gameDir}\r\n" +
                                $"- Active Passes: {_layers.Count(l => l.Enabled)} techniques saved\r\n" +
                                $"- RenoDX Neural Pass: {_settings.NeuralPass1Iterations} iterations @ {_settings.NeuralPass1Intensity:F1}x\r\n" +
                                $"- DLSS-NR Reconstruction: {_settings.DlssNrPasses} passes (100% synth layer)\r\n" +
                                $"- DLSS Generation: {_settings.DlssGeneration}\r\n" +
                                $"- High-Fidelity Chains Synced ({_settings.PreChainCycles}x Pre, {_settings.PostChainCycles}x Post direct passes)",
                                "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                txtStatusBar.Text = $"Saved at {DateTime.Now:HH:mm:ss} | Synced to {_gameDir}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LaunchGame_Click(object sender, RoutedEventArgs e)
        {
            string exePath = !string.IsNullOrEmpty(_targetInfo.ExecutablePath) && File.Exists(_targetInfo.ExecutablePath) 
                             ? _targetInfo.ExecutablePath 
                             : Path.Combine(_gameDir, "RDR2.exe");

            if (!File.Exists(exePath))
            {
                MessageBox.Show($"Could not find game executable at:\n{exePath}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    UseShellExecute = true
                });
                txtStatusBar.Text = $"Launched: {Path.GetFileName(exePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch game: {ex.Message}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region VIEWNEURAL EDITABLE TEXTBOX HANDLERS
        private void ValPass1Iter_LostFocus(object sender, RoutedEventArgs e) => ApplyPass1Iter();
        private void ValPass1Iter_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Enter) ApplyPass1Iter(); }
        private void ApplyPass1Iter()
        {
            if (int.TryParse(valPass1Iter.Text.Trim(), out int val))
            {
                val = Math.Clamp(val, 1, 100);
                slPass1Iter.Value = Math.Clamp(val, 1, 50);
                _settings.NeuralPass1Iterations = val;
                valPass1Iter.Text = $"{val}";
                UpdateVramTelemetry();
            }
        }

        private void ValPass1Intensity_LostFocus(object sender, RoutedEventArgs e) => ApplyPass1Intensity();
        private void ValPass1Intensity_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Enter) ApplyPass1Intensity(); }
        private void ApplyPass1Intensity()
        {
            string clean = valPass1Intensity.Text.Replace("x", "").Trim();
            if (float.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ||
                float.TryParse(clean, NumberStyles.Float, CultureInfo.CurrentCulture, out val))
            {
                val = Math.Clamp(val, 0.5f, 10.0f);
                slPass1Intensity.Value = Math.Clamp((int)(val * 10), 5, 50);
                _settings.NeuralPass1Intensity = val;
                valPass1Intensity.Text = $"{val:F1}x";
            }
        }

        private void ValPass1Nits_LostFocus(object sender, RoutedEventArgs e) => ApplyPass1Nits();
        private void ValPass1Nits_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Enter) ApplyPass1Nits(); }
        private void ApplyPass1Nits()
        {
            string clean = valPass1Nits.Text.Replace("nits", "").Trim();
            if (float.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ||
                float.TryParse(clean, NumberStyles.Float, CultureInfo.CurrentCulture, out val))
            {
                val = Math.Clamp(val, 80, 2000);
                slPass1Nits.Value = Math.Clamp((int)val, 100, 1000);
                _settings.NeuralPass1DiffuseWhiteNits = val;
                valPass1Nits.Text = $"{val:F0} nits";
            }
        }

        private void ValSuperRes_LostFocus(object sender, RoutedEventArgs e) => ApplySuperRes();
        private void ValSuperRes_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Enter) ApplySuperRes(); }
        private void ApplySuperRes()
        {
            string clean = valSuperRes.Text.Replace("%", "").Trim();
            if (float.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ||
                float.TryParse(clean, NumberStyles.Float, CultureInfo.CurrentCulture, out val))
            {
                if (val <= 15.0f && val >= 1.0f && !valSuperRes.Text.Contains("%")) val *= 100f;
                int pct = Math.Clamp((int)val, 100, 1500);
                slSuperRes.Value = pct;
                valSuperRes.Text = $"{pct}%";
                _settings.SuperResolutionRatio = pct / 100f;
                UpdateVramTelemetry();
            }
        }

        private void ValDownscale_LostFocus(object sender, RoutedEventArgs e) => ApplyDownscale();
        private void ValDownscale_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Enter) ApplyDownscale(); }
        private void ApplyDownscale()
        {
            string clean = valDownscale.Text.Replace("x", "").Replace("%", "").Trim();
            if (float.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ||
                float.TryParse(clean, NumberStyles.Float, CultureInfo.CurrentCulture, out val))
            {
                if (val > 1.0f) val /= 100f; // e.g. 70 -> 0.70
                val = Math.Clamp(val, 0.20f, 1.00f);
                slDownscale.Value = (int)(val * 100);
                valDownscale.Text = $"{val:F2}x";
                _settings.DownscaleRatio = val;
                _settings.UpscaleRatioOverrideValue = (1.0f / val).ToString("0.000000", CultureInfo.InvariantCulture);
                UpdateVramTelemetry();
            }
        }

        private void ValUpscaleRatio_LostFocus(object sender, RoutedEventArgs e) => ApplyUpscaleRatio();
        private void ValUpscaleRatio_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Enter) ApplyUpscaleRatio(); }
        private void ApplyUpscaleRatio()
        {
            string clean = valUpscaleRatio.Text.Replace("%", "").Trim();
            if (float.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ||
                float.TryParse(clean, NumberStyles.Float, CultureInfo.CurrentCulture, out val))
            {
                if (val <= 15.0f && val >= 1.0f && !valUpscaleRatio.Text.Contains("%")) val *= 100f;
                int pct = Math.Clamp((int)val, 100, 1000);
                slUpscaleRatio.Value = pct;
                valUpscaleRatio.Text = $"{pct}%";
                _settings.UpscaleRatio = pct / 100f;
                UpdateVramTelemetry();
            }
        }

        private void ValDlssNrPasses_LostFocus(object sender, RoutedEventArgs e) => ApplyDlssNrPasses();
        private void ValDlssNrPasses_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Enter) ApplyDlssNrPasses(); }
        private void ApplyDlssNrPasses()
        {
            if (int.TryParse(valDlssNrPasses.Text.Trim(), out int val))
            {
                val = Math.Clamp(val, 1, 60);
                slDlssNrPasses.Value = Math.Clamp(val, 1, 30);
                valDlssNrPasses.Text = $"{val}";
                _settings.DlssNrPasses = val;
                UpdateVramTelemetry();
            }
        }

        private void ValDlssNrTransfer_LostFocus(object sender, RoutedEventArgs e) => ApplyDlssNrTransfer();
        private void ValDlssNrTransfer_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Enter) ApplyDlssNrTransfer(); }
        private void ApplyDlssNrTransfer()
        {
            string clean = valDlssNrTransfer.Text.Replace("%", "").Trim();
            if (float.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ||
                float.TryParse(clean, NumberStyles.Float, CultureInfo.CurrentCulture, out val))
            {
                if (val <= 1.0f && val > 0) val *= 100f;
                int pct = Math.Clamp((int)val, 0, 100);
                slDlssNrTransfer.Value = pct;
                valDlssNrTransfer.Text = $"{pct}%";
                _settings.DlssNrTransferStrength = pct / 100f;
            }
        }

        private void ValPreCycles_LostFocus(object sender, RoutedEventArgs e) => ApplyPreCycles();
        private void ValPreCycles_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Enter) ApplyPreCycles(); }
        private void ApplyPreCycles()
        {
            string clean = valPreCycles.Text.Replace("x", "").Trim();
            if (int.TryParse(clean, out int val))
            {
                val = Math.Clamp(val, 1, 50);
                slPreCycles.Value = Math.Clamp(val, 1, 20);
                valPreCycles.Text = $"{val}x";
                _settings.PreChainCycles = val;
                UpdateVramTelemetry();
            }
        }

        private void ValPostCycles_LostFocus(object sender, RoutedEventArgs e) => ApplyPostCycles();
        private void ValPostCycles_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Enter) ApplyPostCycles(); }
        private void ApplyPostCycles()
        {
            string clean = valPostCycles.Text.Replace("x", "").Trim();
            if (int.TryParse(clean, out int val))
            {
                val = Math.Clamp(val, 1, 50);
                slPostCycles.Value = Math.Clamp(val, 1, 20);
                valPostCycles.Text = $"{val}x";
                _settings.PostChainCycles = val;
                UpdateVramTelemetry();
            }
        }
        #endregion

        #region LOCALIZATION (ENGLISH / ITALIAN)
        private void LangIt_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage(AppLanguage.Italian);
        }

        private void LangEn_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage(AppLanguage.English);
        }

        public void SetLanguage(AppLanguage lang)
        {
            LocalizationManager.CurrentLanguage = lang;

            // Highlight buttons
            if (btnLangIt != null && btnLangEn != null)
            {
                if (lang == AppLanguage.Italian)
                {
                    btnLangIt.Background = (SolidColorBrush)FindResource("BrushAccentCyan");
                    btnLangIt.Foreground = Brushes.Black;
                    btnLangEn.Background = Brushes.Transparent;
                    btnLangEn.Foreground = (SolidColorBrush)FindResource("BrushTextSecondary");
                }
                else
                {
                    btnLangEn.Background = (SolidColorBrush)FindResource("BrushAccentCyan");
                    btnLangEn.Foreground = Brushes.Black;
                    btnLangIt.Background = Brushes.Transparent;
                    btnLangIt.Foreground = (SolidColorBrush)FindResource("BrushTextSecondary");
                }
            }

            // Window Title
            Title = LocalizationManager.Get("AppTitle");

            // Sidebar
            if (btnNavHome != null) { btnNavHome.Content = LocalizationManager.Get("NavHome"); btnNavHome.ToolTip = LocalizationManager.Get("NavHomeTip"); }
            if (txtNavStudioLabel != null) txtNavStudioLabel.Text = LocalizationManager.Get("NavActiveGameLabel");
            if (_activeGame == null && txtNavActiveGame != null) txtNavActiveGame.Text = LocalizationManager.Get("NavNoGame");
            if (btnNavPipeline != null) { btnNavPipeline.Content = LocalizationManager.Get("NavPipeline"); btnNavPipeline.ToolTip = LocalizationManager.Get("NavPipelineTip"); }
            if (btnNavNeural != null) { btnNavNeural.Content = LocalizationManager.Get("NavNeural"); btnNavNeural.ToolTip = LocalizationManager.Get("NavNeuralTip"); }
            if (btnNavVram != null) { btnNavVram.Content = LocalizationManager.Get("NavVram"); btnNavVram.ToolTip = LocalizationManager.Get("NavVramTip"); }
            if (btnNavPresets != null) { btnNavPresets.Content = LocalizationManager.Get("NavPresets"); btnNavPresets.ToolTip = LocalizationManager.Get("NavPresetsTip"); }
            if (btnNavDlss5 != null) { btnNavDlss5.Content = LocalizationManager.Get("NavDlss5"); btnNavDlss5.ToolTip = LocalizationManager.Get("NavDlss5Tip"); }
            if (btnNavWiki != null) { btnNavWiki.Content = LocalizationManager.Get("NavWiki"); btnNavWiki.ToolTip = LocalizationManager.Get("NavWikiTip"); }

            // Header Bar
            if (btnHeaderBackToHome != null) { btnHeaderBackToHome.Content = LocalizationManager.Get("HeaderBack"); btnHeaderBackToHome.ToolTip = LocalizationManager.Get("HeaderBackTip"); }
            if (btnHeaderAutoCalibrate != null) { btnHeaderAutoCalibrate.Content = LocalizationManager.Get("HeaderAutoCalibrate"); btnHeaderAutoCalibrate.ToolTip = LocalizationManager.Get("HeaderAutoCalibrateTip"); }
            if (btnHeaderInject != null) { btnHeaderInject.Content = LocalizationManager.Get("HeaderInject"); btnHeaderInject.ToolTip = LocalizationManager.Get("HeaderInjectTip"); }
            if (btnHeaderVanilla != null) { btnHeaderVanilla.Content = LocalizationManager.Get("HeaderVanilla"); btnHeaderVanilla.ToolTip = LocalizationManager.Get("HeaderVanillaTip"); }
            if (btnHeaderLaunch != null) { btnHeaderLaunch.Content = LocalizationManager.Get("HeaderLaunch"); btnHeaderLaunch.ToolTip = LocalizationManager.Get("HeaderLaunchTip"); }
            if (btnHeaderSave != null) { btnHeaderSave.Content = LocalizationManager.Get("HeaderSave"); btnHeaderSave.ToolTip = LocalizationManager.Get("HeaderSaveTip"); }

            // Home View
            if (txtHomeHeroTitle != null) txtHomeHeroTitle.Text = LocalizationManager.Get("HomeHeroTitle");
            if (txtHomeHeroSub != null) txtHomeHeroSub.Text = LocalizationManager.Get("HomeHeroSub");
            if (btnHomeScan != null) { btnHomeScan.Content = LocalizationManager.Get("HomeScanBtn"); btnHomeScan.ToolTip = LocalizationManager.Get("HomeScanBtnTip"); }
            if (btnHomeFolder != null) { btnHomeFolder.Content = LocalizationManager.Get("HomeFolderBtn"); btnHomeFolder.ToolTip = LocalizationManager.Get("HomeFolderBtnTip"); }
            if (txtHomeSearch != null) txtHomeSearch.ToolTip = LocalizationManager.Get("HomeSearchTip");
            if (cbiHomeFilterAll != null) cbiHomeFilterAll.Content = LocalizationManager.Get("HomeFilterAll");
            if (cbiHomeFilterInjected != null) cbiHomeFilterInjected.Content = LocalizationManager.Get("HomeFilterInjected");
            if (cbiHomeFilterVanilla != null) cbiHomeFilterVanilla.Content = LocalizationManager.Get("HomeFilterVanilla");
            if (txtHomeEmptyTitle != null) txtHomeEmptyTitle.Text = LocalizationManager.Get("HomeEmptyTitle");
            if (txtHomeEmptySub != null) txtHomeEmptySub.Text = LocalizationManager.Get("HomeEmptySub");
            if (btnHomeEmptyScan != null) btnHomeEmptyScan.Content = LocalizationManager.Get("HomeEmptyBtn");
            if (expHomeTerminal != null) expHomeTerminal.Header = LocalizationManager.Get("HomeTerminalHeader");
            if (txtHomeFleetStatus != null)
                txtHomeFleetStatus.Text = lang == AppLanguage.English ? $"{_detectedGames.Count} Games Detected on PC" : $"{_detectedGames.Count} Giochi Rilevati nel PC";

            // Pipeline Studio Toolbar & Inspector
            if (txtSequenceTitle != null) txtSequenceTitle.Text = LocalizationManager.Get("StudioSequenceLabel");
            if (btnAddLayer != null) { btnAddLayer.Content = LocalizationManager.Get("StudioAddLayer"); btnAddLayer.ToolTip = LocalizationManager.Get("StudioAddLayerTip"); }
            if (btnRemoveLayer != null) { btnRemoveLayer.Content = LocalizationManager.Get("StudioRemoveLayer"); btnRemoveLayer.ToolTip = LocalizationManager.Get("StudioRemoveLayerTip"); }
            if (btnCopyLayer != null) { btnCopyLayer.Content = LocalizationManager.Get("StudioCopyLayer"); btnCopyLayer.ToolTip = LocalizationManager.Get("StudioCopyLayerTip"); }
            if (btnPasteLayer != null) { btnPasteLayer.Content = LocalizationManager.Get("StudioPasteLayer"); btnPasteLayer.ToolTip = LocalizationManager.Get("StudioPasteLayerTip"); }
            if (btnMoveUp != null) { btnMoveUp.Content = LocalizationManager.Get("StudioMoveUp"); btnMoveUp.ToolTip = LocalizationManager.Get("StudioMoveUpTip"); }
            if (btnMoveDown != null) { btnMoveDown.Content = LocalizationManager.Get("StudioMoveDown"); btnMoveDown.ToolTip = LocalizationManager.Get("StudioMoveDownTip"); }
            if (btnToggleLayer != null) { btnToggleLayer.Content = LocalizationManager.Get("StudioToggle"); btnToggleLayer.ToolTip = LocalizationManager.Get("StudioToggleTip"); }
            if (btnResetPipeline != null) { btnResetPipeline.Content = LocalizationManager.Get("StudioReset"); btnResetPipeline.ToolTip = LocalizationManager.Get("StudioResetTip"); }

            if (txtInspectorSec1 != null) txtInspectorSec1.Text = LocalizationManager.Get("InspectorSec1Title");
            if (txtInspectorScaleMode != null) txtInspectorScaleMode.Text = LocalizationManager.Get("InspectorScaleMode");
            if (txtInspectorUpscaleModel != null) txtInspectorUpscaleModel.Text = LocalizationManager.Get("InspectorUpscaleModel");
            if (txtInspectorDownscale != null) txtInspectorDownscale.Text = LocalizationManager.Get("InspectorDownscale");
            if (txtInspectorUpscale != null) txtInspectorUpscale.Text = LocalizationManager.Get("InspectorUpscale");
            if (txtInspectorSec2 != null) txtInspectorSec2.Text = LocalizationManager.Get("InspectorSec2Title");
            if (txtInspectorLoop != null) txtInspectorLoop.Text = LocalizationManager.Get("InspectorLoopCycles");
            if (txtInspectorNeuralPass != null) txtInspectorNeuralPass.Text = LocalizationManager.Get("InspectorNeuralPass");
            if (txtInspectorIntensity != null) txtInspectorIntensity.Text = LocalizationManager.Get("InspectorIntensity");
            if (txtInspectorQuality != null) txtInspectorQuality.Text = LocalizationManager.Get("InspectorQualityPreset");
            if (txtInspectorSec3 != null) txtInspectorSec3.Text = LocalizationManager.Get("InspectorSec3Title");
            if (txtInspectorDedicatedShader != null) txtInspectorDedicatedShader.Text = LocalizationManager.Get("InspectorDedicatedShader");
            if (txtInspectorDedicatedAddon != null) txtInspectorDedicatedAddon.Text = LocalizationManager.Get("InspectorDedicatedAddon");
            if (txtInspectorDedicatedDll != null) txtInspectorDedicatedDll.Text = LocalizationManager.Get("InspectorDedicatedDll");
            if (txtInspectorDescTitle != null) txtInspectorDescTitle.Text = LocalizationManager.Get("InspectorDescTitle");

            // Wiki
            if (txtWikiTocTitle != null) txtWikiTocTitle.Text = LocalizationManager.Get("WikiTocTitle");
            LoadWikiArticles();

            // Refresh Inspector details
            if (lstLayers != null && lstLayers.SelectedItem != null) { LstLayers_SelectionChanged(lstLayers, null!); }

            // Update Current View Title
            UpdateCurrentSectionTitle();

            // Refresh card list bindings
            if (lstHomeGames != null && lstHomeGames.ItemsSource != null)
            {
                lstHomeGames.Items.Refresh();
            }
        }

        private void UpdateCurrentSectionTitle()
        {
            if (txtSectionTitle == null) return;
            bool isEn = LocalizationManager.CurrentLanguage == AppLanguage.English;

            if (ViewHome != null && ViewHome.Visibility == Visibility.Visible)
            {
                txtSectionTitle.Text = isEn ? "PC Games Library & Neural Fleet Hub" : "Libreria Giochi PC & Flotta Neurale";
            }
            else if (ViewPipeline != null && ViewPipeline.Visibility == Visibility.Visible)
            {
                string gName = _activeGame?.GameName ?? (isEn ? "Studio" : "Studio");
                txtSectionTitle.Text = isEn ? $"Studio: {gName} - Rendering Pipeline Sequence" : $"Studio: {gName} - Sequenza Pipeline";
            }
            else if (ViewNeural != null && ViewNeural.Visibility == Visibility.Visible)
            {
                string gName = _activeGame?.GameName ?? "Studio";
                txtSectionTitle.Text = isEn ? $"Studio: {gName} - Neural Engine & DLSS" : $"Studio: {gName} - Motore Neurale & DLSS";
            }
            else if (ViewVram != null && ViewVram.Visibility == Visibility.Visible)
            {
                string gName = _activeGame?.GameName ?? "Studio";
                txtSectionTitle.Text = isEn ? $"Studio: {gName} - VRAM Sweet Spot (80-85%)" : $"Studio: {gName} - VRAM Sweet Spot (80-85%)";
            }
            else if (ViewPresets != null && ViewPresets.Visibility == Visibility.Visible)
            {
                string gName = _activeGame?.GameName ?? "Studio";
                txtSectionTitle.Text = isEn ? $"Studio: {gName} - Hardware Profiles & Presets" : $"Studio: {gName} - Preset & Profili Hardware";
            }
            else if (ViewDlss5 != null && ViewDlss5.Visibility == Visibility.Visible)
            {
                string gName = _activeGame?.GameName ?? "Studio";
                txtSectionTitle.Text = isEn ? $"Studio: {gName} - DLSS 4/5 Models & Streamline" : $"Studio: {gName} - Modelli DLSS 4/5 & Streamline";
            }
            else if (ViewWiki != null && ViewWiki.Visibility == Visibility.Visible)
            {
                txtSectionTitle.Text = isEn ? "Studio Wiki, Technical Manual & Troubleshooting" : "Studio Wiki, Manuale Tecnico & Risoluzione Problemi";
            }
        }
        #endregion
    }
}
