using LanecoverToolsWUI.Services;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LanecoverToolsWUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
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
        private double _targetAr= 5;
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
        public int NotchWidth { get; set; } = 200;
        public int NotchHeight { get; set; } = 100;
        public int NotchXOffset { get; set; }
        public int NotchYOffset { get; set; }
        public bool disableGenButton { get; set; } = false;
        public bool enablePath { get; set; } = false;
        public string chosenFolderPath { get; set; }

        DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public MainWindow()
        {
            this.InitializeComponent();

            var manager = WinUIEx.WindowManager.Get(this);
            manager.PersistenceId = "MainWindow";
            manager.MinWidth = 600;
            manager.MinHeight = 600;

            manager.Width = 600;
            manager.Height = 900;

            HeightSelector.IsEditable = true;
            WidthSelector.IsEditable = true;

            ResPanel.DataContext = this;

            baseArText.Text = "Current value: "+BaseAr;
            targetArText.Text = "Current value: "+TargetAr;

            GradientIntensity = "High";

            ChosenColor = Colors.Black;

            NotificationItemsControl.ItemsSource = Notifications;
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

            infoBar.Closed += (s, e) => Notifications.Remove(infoBar);

            System.Diagnostics.Debug.WriteLine(infoBar.Message);

            if (autoCloseDurationMs > 0)
            {
                var timer = new System.Threading.Timer(_ =>
                {
                    dispatcherQueue.TryEnqueue(() =>
                    {
                        infoBar.IsOpen = false;
                    });
                }, null, autoCloseDurationMs, System.Threading.Timeout.Infinite);
            }
        }

        public void ClearNotifications() => Notifications.Clear();

        private void BaseArValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            string msg = String.Format("Current value: {0:N1}", e.NewValue);
            this.baseArText.Text = msg;
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
        }

        private void TargetArValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            string msg = String.Format("Current value: {0:N1}", e.NewValue);
            this.targetArText.Text = msg;
            if (BaseAr >= TargetAr)
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
                    chosenFolderPath = folder.Path;
                    this.NoPathMessage.Visibility = Visibility.Collapsed;
                    if (!disableGenButton) this.GenerateButton.IsEnabled = true;
                    enablePath = true;
                }

                // re-enable the button
                button.IsEnabled = true;
            }
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // disable the button to avoid double-clicking
                button.IsEnabled = false;

                LaneCalculatorService _laneCalculatorService = new();
                ushort laneHeight = _laneCalculatorService.CalculateLaneHeight(BaseAr, TargetAr, SelectedHeight);

                LaneGeneratorService _laneGeneratorService = new();
                LaneGenerationSettings _laneGenerationSettings = new(
                    BaseAr,
                    TargetAr,
                    (int)SelectedHeight,
                    (int)SelectedWidth,
                    ChosenColor,
                    PickedFolderTextBlock.Text,
                    laneHeight,
                    GradientCb,
                    GradientIntensity,
                    AccNotchCb,
                    NotchHeight,
                    NotchWidth,
                    NotchXOffset,
                    NotchYOffset
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
                catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
                finally
                {
                    ShowNotification("The lane measures: " + laneHeight + "px", InfoBarSeverity.Success);
                }
                
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
            if (cb.Name == "GradientCheckbox") GradientCb = true;
            this.GradientPanel.Visibility = Visibility.Visible;
            this.GradientPanel.Opacity = 1;
            //System.Diagnostics.Debug.WriteLine(GradientOptions.ToString());
        }

        private void GradientCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb.Name == "GradientCheckbox") GradientCb = false;
            this.GradientPanel.Visibility = Visibility.Collapsed;
            this.GradientPanel.Opacity = 0;
        }

        private void AccNotchCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb.Name == "AccNotchCheckbox") AccNotchCb = true;
            this.NotchPanel.Visibility = Visibility.Visible;
            this.NotchPanel.Opacity = 1;
        }

        private void AccNotchCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb.Name == "AccNotchCheckbox") AccNotchCb = false;
            this.NotchPanel.Visibility = Visibility.Collapsed;
            this.NotchPanel.Opacity = 0;
        }

        private void RevertButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Path.GetFullPath(PickedFolderTextBlock.Text) + @"\scorebar-bg.old.png"))
            {
                try 
                {
                    File.Delete((Path.GetFullPath(PickedFolderTextBlock.Text) + @"\scorebar-bg.png"));
                    File.Move((Path.GetFullPath(PickedFolderTextBlock.Text) + @"\scorebar-bg.old.png"), (Path.GetFullPath(PickedFolderTextBlock.Text) + @"\scorebar-bg.png"));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
                finally
                {
                    ShowNotification("Lane reverted", InfoBarSeverity.Informational);
                }
                
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
