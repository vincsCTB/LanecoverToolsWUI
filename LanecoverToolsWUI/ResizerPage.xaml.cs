using LanecoverToolsWUI.Services;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LanecoverToolsWUI
{
    public sealed partial class ResizerPage : Page, INotifyPropertyChanged
    {
        // Lists holding static choices
        public ObservableCollection<string> UnitModes { get; set; } = new() { "%", "px" };
        private readonly List<string> _percentageChoices = new() { "-50%", "-40%", "-30%", "-20%", "-10%", "+10%", "+20%", "+30%", "+40%", "+50%" };
        private readonly List<string> _pixelChoices = new() { "128px", "144px", "155px", "200px", "224px", "240px" };

        // The active items bound to the resize value picker dropdown
        private ObservableCollection<string> _dynamicResizeOptions = new();
        public DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public ObservableCollection<string> DynamicResizeOptions
        {
            get => _dynamicResizeOptions;
            set => SetProperty(ref _dynamicResizeOptions, value);
        }

        private string _resizeUnitHeader = "Resize Scale Percentage";
        public string ResizeUnitHeader
        {
            get => _resizeUnitHeader;
            set => SetProperty(ref _resizeUnitHeader, value);
        }

        private string _selectedUnitMode = "%";
        public string SelectedUnitMode
        {
            get => _selectedUnitMode;
            set
            {
                if (SetProperty(ref _selectedUnitMode, value))
                {
                    UpdateUnitLayout(value);
                }
            }
        }

        private string _selectedPercentage = "0%";
        public string SelectedPercentage
        {
            get => _selectedPercentage;
            set
            {
                if (SetProperty(ref _selectedPercentage, value))
                {
                    ParseSelectedValue(value);
                }
            }
        }

        // Numerical backing metrics processed by the hardware-accelerated loops
        public double ExtractedNumericValue { get; private set; } = 0.0;

        private bool _enablePath = false;
        public bool enablePath
        {
            get => _enablePath;
            set => SetProperty(ref _enablePath, value);
        }

        public bool disableResizeButton { get; set; } = false;
        public string ChosenFolderPath { get; set; } = string.Empty;
        public ObservableCollection<InfoBar> Notifications { get; } = new();
        public UserSettings Settings => UserSettingsService.Current;

        private List<string> _foundBaseFruits = new();
        private List<string> _foundOverlayFruits = new();
        private readonly string[] _targetBaseFiles = { "fruit-pear.png", "fruit-grapes.png", "fruit-apple.png", "fruit-orange.png" };
        private readonly string[] _targetOverlayFiles = { "fruit-pear-overlay.png", "fruit-grapes-overlay.png", "fruit-apple-overlay.png", "fruit-orange-overlay.png" };

        public ResizerPage()
        {
            this.InitializeComponent();
            PercentageSelector.IsEditable = true;
            ResizePanel.DataContext = this;
            NotificationItemsControl.ItemsSource = Notifications;

            // Initialize to initial baseline state selection configuration rules
            UpdateUnitLayout(_selectedUnitMode);

            this.Loaded += ResizerPage_Loaded;
        }

        private void UpdateUnitLayout(string currentUnitMode)
        {
            DynamicResizeOptions.Clear();
            if (currentUnitMode.StartsWith("%"))
            {
                ResizeUnitHeader = "Resize Scale Percentage Modifier";
                _percentageChoices.ForEach(x => DynamicResizeOptions.Add(x));
                SelectedPercentage = "10%";
            }
            else
            {
                ResizeUnitHeader = "Target Width Frame Size (px)";
                _pixelChoices.ForEach(x => DynamicResizeOptions.Add(x));
                SelectedPercentage = "128px";
            }
        }

        private void ParseSelectedValue(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            // Strip suffix modifiers cleanly 
            string clean = input.Replace("%", "").Replace("px", "").Replace("+", "").Trim();

            if (double.TryParse(clean, out double parsedValue))
            {
                ExtractedNumericValue = parsedValue;
            }
        }

        private async void ResizerPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Settings.SelectedPercentage))
            {
                // Fallback validation layout configurations
                if (Settings.SelectedPercentage.Contains("px"))
                {
                    SelectedUnitMode = "px";
                }
                SelectedPercentage = Settings.SelectedPercentage;
            }

            string savedPath = Settings?.ChosenFolderPath;
            if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
            {
                ChosenFolderPath = savedPath;
                PickedFolderTextBlock.Text = savedPath;
                this.NoPathMessage.Visibility = Visibility.Collapsed;
                if (!disableResizeButton) this.ResizeButton.IsEnabled = true;
                enablePath = true;
                VerifyFruitAssets(savedPath);
            }
            else
            {
                PickedFolderTextBlock.Text = "No folder selected.";
                enablePath = false;
            }
        }

        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                button.IsEnabled = false;
                PickedFolderTextBlock.Text = "";

                var picker = new FolderPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId);
                picker.CommitButtonText = "Pick Folder";
                picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                picker.ViewMode = PickerViewMode.List;

                var folder = await picker.PickSingleFolderAsync();
                PickedFolderTextBlock.Text = folder != null ? folder.Path : "No folder selected.";

                if (folder != null)
                {
                    ChosenFolderPath = folder.Path;
                    Settings.ChosenFolderPath = folder.Path;
                    this.NoPathMessage.Visibility = Visibility.Collapsed;
                    if (!disableResizeButton) this.ResizeButton.IsEnabled = true;
                    enablePath = true;
                    VerifyFruitAssets(folder.Path);
                }
                button.IsEnabled = true;
            }
        }

        private async void ResizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ChosenFolderPath)) return;

            try
            {
                VerifyFruitAssets(ChosenFolderPath);

                List<string> filesToProcess = new();
                filesToProcess.AddRange(_foundBaseFruits);

                if (Settings.ResizeOverlaysCb)
                {
                    filesToProcess.AddRange(_foundOverlayFruits);
                }

                if (filesToProcess.Count == 0)
                {
                    ShowNotification("Processing aborted: No target assets matching your selection flags were found.", InfoBarSeverity.Error);
                    return;
                }

                // Create backups
                int backupCount = 0;
                foreach (string fileName in filesToProcess)
                {
                    string originalFilePath = Path.Combine(ChosenFolderPath, fileName);
                    string backupFilePath = originalFilePath + ".old";

                    if (!File.Exists(backupFilePath))
                    {
                        File.Copy(originalFilePath, backupFilePath, overwrite: false);
                        backupCount++;
                    }
                }

                bool isPercentageMode = SelectedUnitMode.StartsWith("%");
                CanvasDevice device = CanvasDevice.GetSharedDevice();

                foreach (string fileName in filesToProcess)
                {
                    string backupFilePath = Path.Combine(ChosenFolderPath, fileName + ".old");
                    string outputFilePath = Path.Combine(ChosenFolderPath, fileName);

                    using (CanvasBitmap bitmap = await CanvasBitmap.LoadAsync(device, backupFilePath))
                    {
                        int newWidth = 1;
                        int newHeight = 1;

                        if (isPercentageMode)
                        {
                            // Scale target calculation: e.g. 1.0 + (+20 / 100) = 1.20 factor
                            float scaleFactor = (float)(1.0 + (ExtractedNumericValue / 100.0));
                            if (scaleFactor <= 0.01f) scaleFactor = 0.01f;

                            newWidth = (int)Math.Max(1, Math.Round(bitmap.Size.Width * scaleFactor));
                            newHeight = (int)Math.Max(1, Math.Round(bitmap.Size.Height * scaleFactor));
                        }
                        else
                        {
                            // Absolute pixel sizing operation (Scales height proportionally to match target layout Width value)
                            int targetWidth = (int)Math.Max(1, ExtractedNumericValue);
                            double aspectRatio = bitmap.Size.Height / bitmap.Size.Width;

                            newWidth = targetWidth;
                            newHeight = (int)Math.Max(1, Math.Round(targetWidth * aspectRatio));
                        }

                        using (CanvasRenderTarget renderTarget = new CanvasRenderTarget(device, newWidth, newHeight, bitmap.Dpi))
                        {
                            using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
                            {
                                ds.Clear(Microsoft.UI.Colors.Transparent);
                                ds.DrawImage(bitmap, new Rect(0, 0, newWidth, newHeight), bitmap.Bounds, 1.0f, CanvasImageInterpolation.MultiSampleLinear);
                            }
                            await renderTarget.SaveAsync(outputFilePath, CanvasBitmapFileFormat.Png);
                        }
                    }
                }

                // Save setting choice state
                Settings.SelectedPercentage = SelectedPercentage;

                string backupNotice = backupCount > 0 ? $" Created {backupCount} pristine backups (.old)." : "";
                string displayUnit = isPercentageMode ? $"{ExtractedNumericValue}%" : $"{ExtractedNumericValue}px wide";
                ShowNotification($"Successfully resized {filesToProcess.Count} fruit assets down/up to target dimensions ({displayUnit}).{backupNotice}", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowNotification($"Pipeline execution error: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void RevertButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ChosenFolderPath))
            {
                ShowNotification("Please select a target path folder location first.", InfoBarSeverity.Warning);
                return;
            }

            try
            {
                List<string> potentialFiles = new();
                potentialFiles.AddRange(_targetBaseFiles);
                potentialFiles.AddRange(_targetOverlayFiles);

                int restoredCount = 0;
                foreach (string fileName in potentialFiles)
                {
                    string targetFilePath = Path.Combine(ChosenFolderPath, fileName);
                    string backupFilePath = targetFilePath + ".old";

                    if (File.Exists(backupFilePath))
                    {
                        File.Copy(backupFilePath, targetFilePath, overwrite: true);
                        File.Delete(backupFilePath);
                        restoredCount++;
                    }
                }

                if (restoredCount > 0)
                {
                    VerifyFruitAssets(ChosenFolderPath);
                    ShowNotification($"Successfully reverted {restoredCount} fruit assets back to their original states.", InfoBarSeverity.Success);
                }
                else
                {
                    ShowNotification("No pristine (.old) backup entries discovered for restoration inside this directory.", InfoBarSeverity.Warning);
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"Error occurred during restoration phase sequence: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void VerifyFruitAssets(string folderPath)
        {
            _foundBaseFruits.Clear();
            _foundOverlayFruits.Clear();

            foreach (var file in _targetBaseFiles)
            {
                if (File.Exists(Path.Combine(folderPath, file))) _foundBaseFruits.Add(file);
            }
            foreach (var file in _targetOverlayFiles)
            {
                if (File.Exists(Path.Combine(folderPath, file))) _foundOverlayFruits.Add(file);
            }

            int totalDiscovered = _foundBaseFruits.Count + _foundOverlayFruits.Count;
            if (totalDiscovered > 0)
            {
                AssetStatusTextBlock.Text = $"Discovered {totalDiscovered} fruit assets ({_foundBaseFruits.Count}/4 fruits, {_foundOverlayFruits.Count}/4 overlays found).";
                enablePath = true;
            }
            else
            {
                AssetStatusTextBlock.Text = "Warning: No target fruit files or overlays found in this directory.";
                ShowNotification("No matching fruit elements discovered in the selected directory.", InfoBarSeverity.Warning);
                enablePath = true;
            }
        }

        public void ShowNotification(string message, InfoBarSeverity severity, string? title = null, int autoCloseDurationMs = 5000)
        {
            var infoBar = new InfoBar
            {
                Message = message,
                Severity = severity,
                Title = title,
                IsOpen = true,
                IsClosable = true,
            };

            Notifications.Add(infoBar);

            if (autoCloseDurationMs > 0)
            {
                var timer = new System.Threading.Timer(_ =>
                {
                    dispatcherQueue.TryEnqueue(() =>
                    {
                        Notifications.Remove(infoBar);
                    });
                }, null, autoCloseDurationMs, System.Threading.Timeout.Infinite);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
