using LanecoverToolsWUI.Services;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using OsuMemoryDataProvider;
using OsuMemoryDataProvider.OsuMemoryModels;
using OsuMemoryDataProvider.OsuMemoryModels.Direct;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LanecoverToolsWUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LanePage : Page
    {
        private Windows.UI.Color _chosenColor;
        public Windows.UI.Color ChosenColor
        {
            get { return _chosenColor; }
            set { SetProperty(ref _chosenColor, value); }
        }
        public ObservableCollection<Double> ResWidth { get; set; } = new ObservableCollection<Double>
        {
            360,
            720,
            1080,
            1440,
            1920, // Standard Full HD
            2560,
            3840  // Ultra Wide / High Res
        };
        public ObservableCollection<Double> ResHeight { get; set; } = new ObservableCollection<Double>
        {
            1080,
            1440,
            2160, // 4K UHD
            720,  // HD
            576   // Portrait HD
        };
        public ObservableCollection<String> GradientOptions { get; set; } = new ObservableCollection<String>
        {
            "Highest",
            "High",
            "Medium",
            "Low",
            "Lowest"
        };
        public ObservableCollection<InfoBar> Notifications { get; } = new();
        private double _selectedHeight = 1080;
        public double SelectedHeight
        {
            get => _selectedHeight;
            set { SetProperty(ref _selectedHeight, value); } // Calls OnPropertyChanged automatically
        }
        private double _selectedWidth = 1920;
        public double SelectedWidth
        {
            get => _selectedWidth;
            set { SetProperty(ref _selectedWidth, value); }
        }
        private double _baseAr = 5;
        private double _targetAr = 5;
        public double BaseAr
        {
            get { return _baseAr; }
            set { SetProperty(ref _baseAr, double.Round(value, 1)); }
        }
        public double TargetAr
        {
            get { return _targetAr; }
            set { SetProperty(ref _targetAr, double.Round(value, 1)); }
        }
        public ICommand ButtonCommand { get; set; }
        public bool GradientCb { get; set; } = false;
        public string GradientIntensity { get; set; }
        public bool AccNotchCb { get; set; } = false;
        private int _notchWidth = 200;
        public int NotchWidth { get => _notchWidth; set => SetProperty(ref _notchWidth, value); }
        private int _notchHeight = 100;
        public int NotchHeight { get => _notchHeight; set => SetProperty(ref _notchHeight, value); }
        private int _notchXOffset;
        public int NotchXOffset { get => _notchXOffset; set => SetProperty(ref _notchXOffset, value); }
        private int _notchYOffset;
        public int NotchYOffset { get => _notchYOffset; set => SetProperty(ref _notchYOffset, value); }
        public bool disableGenButton { get; set; } = false;
        private bool _enablePath = false;
        public bool enablePath
        {
            get => _enablePath;
            set => SetProperty(ref _enablePath, value);
        }
        public bool autoMode { get; set; } = false;
        public string ChosenFolderPath { get; set; }
        private readonly string _osuWindowTitleHint;

        public List<int> GenerationHotkey { get; set; }
        public List<int> RevertHotkey { get; set; }

        public DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        private StructuredOsuMemoryReader _sreader;
        private CancellationTokenSource _cts;
        private Task _loopForArTask;
        private int _readDelay = 33;

        public UserSettings Settings => UserSettingsService.Current;

        private bool _isInitialised = false;
        public LanePage()
        {
            this.InitializeComponent();

            HeightSelector.IsEditable = true;
            WidthSelector.IsEditable = true;

            ResPanel.DataContext = this;

            baseArText.Text = "Current value: " + BaseAr;
            targetArText.Text = "Current value: " + TargetAr;

            GradientIntensity = "High";

            ChosenColor = Microsoft.UI.Colors.Black;

            NotificationItemsControl.ItemsSource = Notifications;

            this.Loaded += LanePage_Loaded;
        }

        private void LanePage_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Pull the saved directory string from your JSON config model wrapper
            string savedPath = Settings?.ChosenFolderPath;

            if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
            {
                // 2. Hydrate your local runtime path property
                ChosenFolderPath = savedPath;
                PickedFolderTextBlock.Text = savedPath;

                // 3. Collapse warning panels and uncover execution actions
                this.NoPathMessage.Visibility = Visibility.Collapsed;
                if (!disableGenButton) this.GenerateButton.IsEnabled = true;
                enablePath = true;
                VerifyScorebarAsset(savedPath);
            }
            else
            {
                // Fallback baseline layout rules if configuration values resolve empty
                PickedFolderTextBlock.Text = "No folder selected.";
                enablePath = false;
            }
        }

        private async void loopForAr()
        {
            var baseAddresses = new OsuBaseAddresses();
            int initDiscard = 0;
            // Track whether the "not found" notification has already been triggered
            bool isNotFoundNotificationShown = false;

            while (true)
            {
                if (_cts.IsCancellationRequested)
                    return;

                if (!_sreader.CanRead && initDiscard > 5)
                {
                    // Only show the notification if we haven't already shown it
                    if (!isNotFoundNotificationShown)
                    {
                        dispatcherQueue.TryEnqueue(() =>
                        {
                            ShowNotification("osu! process not found", InfoBarSeverity.Warning, null, 0);
                        });
                        isNotFoundNotificationShown = true; // Block future spams
                    }

                    await Task.Delay(_readDelay);
                    continue;
                }

                try
                {
                    if (_sreader.CanRead)
                    {
                        // Reset the flag here so if the process closes *again* later, 
                        // the user will get a fresh notification.
                        isNotFoundNotificationShown = false;

                        _sreader.TryRead(baseAddresses.Beatmap);

                        dispatcherQueue.TryEnqueue(() =>
                        {
                            BaseArSlider.Value = baseAddresses.Beatmap.Ar;

                            if (BaseAr >= TargetAr)
                            {
                                disableGenButton = true;
                                ArErrorMessage.Visibility = Visibility.Visible;
                                GenerateButton.IsEnabled = false;
                            }
                            else
                            {
                                disableGenButton = false;
                                ArErrorMessage.Visibility = Visibility.Collapsed;
                                if (enablePath)
                                {
                                    GenerateButton.IsEnabled = true;
                                }
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }

                if (initDiscard <= 5)
                {
                    initDiscard++;
                }
                await Task.Delay(_readDelay);
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

        public void ClearNotifications() => Notifications.Clear();

        private void BaseArValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            string msg = String.Format("Current value: {0:N1}", e.NewValue);
            this.baseArText.Text = msg;
            if (BaseAr >= Settings.TargetAr)
            {
                disableGenButton = true;
                ArErrorMessage.Visibility = Visibility.Visible;
                GenerateButton.IsEnabled = false;
            }
            else
            {
                disableGenButton = false;
                ArErrorMessage.Visibility = Visibility.Collapsed;
                if (enablePath)
                {
                    GenerateButton.IsEnabled = true;
                }
            }
        }

        private void TargetArValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            string msg = String.Format("Current value: {0:N1}", e.NewValue);
            this.targetArText.Text = msg;
            if (BaseAr >= Settings.TargetAr)
            {
                disableGenButton = true;
                this.ArErrorMessage.Visibility = Visibility.Visible;
                GenerateButton.IsEnabled = false;
            }
            else
            {
                disableGenButton = false;
                this.ArErrorMessage.Visibility = Visibility.Collapsed;
                if (enablePath)
                {
                    GenerateButton.IsEnabled = true;
                }
            }
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
                    Settings.ChosenFolderPath = folder.Path;
                    this.NoPathMessage.Visibility = Visibility.Collapsed;
                    if (!disableGenButton) this.GenerateButton.IsEnabled = true;
                    enablePath = true;
                } else
                {
                    enablePath = false;
                }

                // re-enable the button
                button.IsEnabled = true;
            }
        }

        public async void GenerateLane()
        {
            LaneCalculatorService _laneCalculatorService = new();
            Debug.WriteLine(Settings.TargetAr);
            ushort laneHeight = _laneCalculatorService.CalculateLaneHeight(BaseAr, Math.Round(Settings.TargetAr, 2), Settings.SelectedHeight);

            LaneGeneratorService _laneGeneratorService = new();
            LaneGenerationSettings _laneGenerationSettings = new(
                BaseAr,
                Settings.TargetAr,
                (int)Settings.SelectedHeight,
                (int)Settings.SelectedWidth,
                Settings.ChosenColor,
                PickedFolderTextBlock.Text,
                laneHeight,
                Settings.GradientCb,
                Settings.GradientIntensity,
                Settings.AccNotchCb,
                Settings.NotchHeight,
                Settings.NotchWidth,
                Settings.NotchXOffset,
                Settings.NotchYOffset
                );
            Bitmap result = _laneGeneratorService.Generate(_laneGenerationSettings);

            try
            {
                System.Diagnostics.Debug.WriteLine(Path.GetFullPath(PickedFolderTextBlock.Text));
                if (File.Exists(Path.GetFullPath(PickedFolderTextBlock.Text) + @"\scorebar-bg.png"))
                {
                    if (!File.Exists(Path.GetFullPath(PickedFolderTextBlock.Text) + @"\scorebar-bg.old.png"))
                    {
                        File.Move((Path.GetFullPath(PickedFolderTextBlock.Text) + @"\scorebar-bg.png"), (Path.GetFullPath(PickedFolderTextBlock.Text) + @"\scorebar-bg.old.png")); //Save old scorebar as scorebar-bg.old
                    }
                }
                result.Save(Path.GetFullPath(PickedFolderTextBlock.Text) + @"\scorebar-bg.png", ImageFormat.Png);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                ShowNotification("The lane measures: " + laneHeight + "px", InfoBarSeverity.Success);
            }
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // disable the button to avoid double-clicking
                button.IsEnabled = false;

                GenerateLane();

                // re-enable the button
                button.IsEnabled = true;
            }
        }

        public string HandleButtonClick()
        {
            LaneCalculatorService _laneCalculatorService = new();
            ushort laneHeight = _laneCalculatorService.CalculateLaneHeight(BaseAr, TargetAr, SelectedHeight);

            return laneHeight.ToString();
        }

        // --- Helper Method for INotifyPropertyChanged (Cleaner code) ---
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // --- INotifyPropertyChanged Implementation ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void GradientCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb.Name == "GradientCheckbox") GradientCb = true; Settings.GradientCb = true;
            this.GradientPanel.Visibility = Visibility.Visible;
            this.GradientPanel.Opacity = 1;
            //System.Diagnostics.Debug.WriteLine(GradientOptions.ToString());
        }

        private void GradientCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb.Name == "GradientCheckbox") GradientCb = false; Settings.GradientCb = false;
            this.GradientPanel.Visibility = Visibility.Collapsed;
            this.GradientPanel.Opacity = 0;
        }

        private void AccNotchCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb.Name == "AccNotchCheckbox") AccNotchCb = true; Settings.AccNotchCb = true;
            this.NotchPanel.Visibility = Visibility.Visible;
            this.NotchPanel.Opacity = 1;
        }

        private void AccNotchCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb.Name == "AccNotchCheckbox") AccNotchCb = false; Settings.AccNotchCb = false;
            this.NotchPanel.Visibility = Visibility.Collapsed;
            this.NotchPanel.Opacity = 0;
        }

        public void RevertLane()
        {
            string targetPath = Path.GetFullPath(PickedFolderTextBlock.Text);
            string backupFile = Path.Combine(targetPath, "scorebar-bg.old.png");
            string activeFile = Path.Combine(targetPath, "scorebar-bg.png");

            if (File.Exists(backupFile))
            {
                try
                {
                    if (File.Exists(activeFile))
                    {
                        File.Delete(activeFile);
                    }
                    File.Move(backupFile, activeFile);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                    ShowNotification("Failed to swap backup file due to a system error.", InfoBarSeverity.Error);
                }
                finally
                {
                    ShowNotification("Lane reverted successfully", InfoBarSeverity.Success);
                }
            }
            else
            {
                // This is the missing validation notice flag block you wanted to bring over:
                ShowNotification("No backup profile (.old) was found in this folder to revert back to.", InfoBarSeverity.Warning);
            }
        }
        private void RevertButton_Click(object sender, RoutedEventArgs e)
        {
            RevertLane();
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            autoMode = true;

            _sreader = StructuredOsuMemoryReader.GetInstance(new("osu!"));
            dispatcherQueue.TryEnqueue(() => {
                BaseArSlider.IsEnabled = false;
            });
            _cts = new CancellationTokenSource();
            _loopForArTask = Task.Run(loopForAr, _cts.Token);
        }

        private async void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            try
            {
                await _loopForArTask;
            }
            catch (ObjectDisposedException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                _cts.Dispose();
                autoMode = false;
                dispatcherQueue.TryEnqueue(() => { BaseArSlider.IsEnabled = true; });
            }
        }

        private void VerifyScorebarAsset(string folderPath)
        {
            if (File.Exists(folderPath+"/scorebar-bg.png"))
            {
                AssetStatusTextBlock.Text = "Discovered scorebar-bg.png asset.";
                enablePath = true;
            } else
            {
                AssetStatusTextBlock.Text = "Warning: No target scorebar-bg found in this directory.";
                ShowNotification("No matching scorebar-bg.png discovered in the selected directory.", InfoBarSeverity.Warning);
                enablePath = false;
            }
        }
    }

    public class ThumbDouble : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return string.Format("{0:N1}", value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

