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
using Microsoft.Graphics.Canvas;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LanecoverToolsWUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ResizerPage : Page
    {
        public ObservableCollection<string> ResizePercentages { get; set; } = new()
        {
            "-50%", "-40%", "-30%", "-20%", "-10%", "0%", "+10%", "+20%", "+30%", "+40%", "+50%"
        };

        private string _selectedPercentageText = "0%";
        public string SelectedPercentageText
        {
            get => _selectedPercentageText;
            set
            {
                if (SetProperty(ref _selectedPercentageText, value))
                {
                    // Clean format entries typed manually by users (e.g., " 25 " -> "+25%")
                    string clean = value.Replace("%", "").Trim();
                    if (double.TryParse(clean, out double parsedValue))
                    {
                        SelectedPercentageValue = parsedValue / 100.0;
                    }
                }
            }
        }

        // Exposed numerical variable for business operations algorithm calculations
        public double SelectedPercentageValue { get; private set; } = 0.0;

        private bool _enablePath = false;
        public bool enablePath
        {
            get => _enablePath;
            set => SetProperty(ref _enablePath, value);
        }

        private bool _resizeOverlays = false;
        public bool ResizeOverlays
        {
            get => _resizeOverlays;
            set => SetProperty(ref _resizeOverlays, value);
        }
        public bool disableResizeButton = true;
        public string ChosenFolderPath { get; set; } = string.Empty;
        public ObservableCollection<InfoBar> Notifications { get; } = new();

        // Tracker lists for your discovered target elements
        private List<string> _foundBaseFruits = new();
        private List<string> _foundOverlayFruits = new();

        private readonly string[] _targetBaseFiles = { "fruit-pear.png", "fruit-grapes.png", "fruit-apple.png", "fruit-orange.png" };
        private readonly string[] _targetOverlayFiles = { "fruit-pear-overlay.png", "fruit-grapes-overlay.png", "fruit-apple-overlay.png", "fruit-orange-overlay.png" };

        public ResizerPage()
        {
            this.InitializeComponent();

            // Allow user to input custom entries exactly like resolution inputs
            PercentageSelector.IsEditable = true;
            ResizePanel.DataContext = this;
            NotificationItemsControl.ItemsSource = Notifications;
        }

        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // disable the button to avoid double-clicking
                button.IsEnabled = false;

                // Clear previous returned folder name
                PickedFolderTextBlock.Text = "";

                var picker = new FolderPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId);

                picker.CommitButtonText = "Pick Folder";
                picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                picker.ViewMode = PickerViewMode.List;

                // Show the picker dialog window
                var folder = await picker.PickSingleFolderAsync();
                PickedFolderTextBlock.Text = folder != null
                    ? folder.Path
                    : "No folder selected.";

                if (folder != null)
                {
                    ChosenFolderPath = folder.Path;
                    this.NoPathMessage.Visibility = Visibility.Collapsed;
                    if (!disableResizeButton) this.ResizeButton.IsEnabled = true;
                    enablePath = true;
                    VerifyFruitAssets(folder.Path);
                }

                // re-enable the button
                button.IsEnabled = true;
            }
        }

        private async void ResizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ChosenFolderPath)) return;

            try
            {
                VerifyFruitAssets(ChosenFolderPath);

                // --- SELECTIVE FILE TARGETING LOGIC ---
                List<string> filesToProcess = new();
                filesToProcess.AddRange(_foundBaseFruits);

                // Only include overlay elements if the checkbox condition evaluation returns true
                if (ResizeOverlays)
                {
                    filesToProcess.AddRange(_foundOverlayFruits);
                }

                if (filesToProcess.Count == 0)
                {
                    ShowNotification("Processing aborted: No target assets matching your selection flags were found.", InfoBarSeverity.Error);
                    return;
                }

                // --- BACKUP PIPELINE LOGIC BLOCK ---
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

                // --- HARDWARE-ACCELERATED RESIZING EXECUTION ---
                // Scale target calculation: e.g., 1.0 + (+0.20) = 1.20 scale factor factor multiplier
                float scaleFactor = (float)(1.0 + SelectedPercentageValue);

                // Edge-case safeguard logic rule
                if (scaleFactor <= 0.01f)
                {
                    ShowNotification("Resize error: Scaling modifier percentage cannot result in an image size of 0 or smaller.", InfoBarSeverity.Error);
                    return;
                }

                CanvasDevice device = CanvasDevice.GetSharedDevice();

                foreach (string fileName in filesToProcess)
                {
                    // Always read directly from the safe '.old' pristine backup file to prevent compounding artifacts
                    string backupFilePath = Path.Combine(ChosenFolderPath, fileName + ".old");
                    string outputFilePath = Path.Combine(ChosenFolderPath, fileName);

                    using (CanvasBitmap bitmap = await CanvasBitmap.LoadAsync(device, backupFilePath))
                    {
                        // Calculate explicit bounds target values
                        int newWidth = (int)Math.Max(1, Math.Round(bitmap.Size.Width * scaleFactor));
                        int newHeight = (int)Math.Max(1, Math.Round(bitmap.Size.Height * scaleFactor));

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

                string backupNotice = backupCount > 0 ? $" Created {backupCount} pristine backups (.old)." : "";
                ShowNotification($"Successfully resized {filesToProcess.Count} fruit assets down/up to {(scaleFactor * 100):0}%.{backupNotice}", InfoBarSeverity.Success);
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
                // Define all valid tracking outputs to inspect for state rollback conditions
                List<string> potentialFiles = new();
                potentialFiles.AddRange(_targetBaseFiles);
                potentialFiles.AddRange(_targetOverlayFiles);

                int restoredCount = 0;

                foreach (string fileName in potentialFiles)
                {
                    string targetFilePath = Path.Combine(ChosenFolderPath, fileName);
                    string backupFilePath = targetFilePath + ".old";

                    // If a backup exists, restore it over the modified one
                    if (File.Exists(backupFilePath))
                    {
                        // Overwrite the modified file back to its pristine condition
                        File.Copy(backupFilePath, targetFilePath, overwrite: true);

                        // Clean up the backup file safely to leave the folder pristine
                        File.Delete(backupFilePath);

                        restoredCount++;
                    }
                }

                if (restoredCount > 0)
                {
                    // Refresh tracking definitions states UI indicators
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

            // Check for standard base variations
            foreach (var file in _targetBaseFiles)
            {
                if (File.Exists(Path.Combine(folderPath, file)))
                {
                    _foundBaseFruits.Add(file);
                }
            }

            // Check for matching contextual overlays
            foreach (var file in _targetOverlayFiles)
            {
                if (File.Exists(Path.Combine(folderPath, file)))
                {
                    _foundOverlayFruits.Add(file);
                }
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

                // Keep button enabled so they can proceed anyway, or set to false to lock execution
                enablePath = true;
            }
        }

        private void ShowNotification(string message, InfoBarSeverity severity)
        {
            var infoBar = new InfoBar
            {
                Message = message,
                Severity = severity,
                IsOpen = true,
                IsClosable = true,
            };

            Notifications.Add(infoBar);
            infoBar.Closed += (s, e) => Notifications.Remove(infoBar);
        }

        // --- INotifyPropertyChanged standard structure boilerplate updates ---
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

