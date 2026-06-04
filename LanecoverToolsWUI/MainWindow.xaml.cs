using LanecoverToolsWUI.Services;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
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
using Windows.System;
using Windows.UI.WindowManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LanecoverToolsWUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private StructuredOsuMemoryReader _sreader;
        private IntPtr _hookId;
        private OsuBaseAddresses baseAddresses = new();

        public List<int> GenerationHotkey { get; set; }
        public List<int> RevertHotkey { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            this.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.Dark;
            _hookId = KeyboardHook.SetHook(this);

            this.AppWindow.Closing += MainWindow_Closing;

            NavView.SelectedItem = Lane;
            ContentFrame.Navigate(typeof(LanePage));

            var manager = WinUIEx.WindowManager.Get(this);
            manager.PersistenceId = "MainWindow";
            manager.MinWidth = 600;
            manager.MinHeight = 600;

            manager.Width = 1200;
            manager.Height = 700;
        }

        private void MainWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {

            // Commits everything cleanly to userSettings.json without async thread clipping
            UserSettingsService.SaveUserSettings();
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            // FIX: Guard against execution before the XAML parser instantiates the Frame
            if (ContentFrame == null) return;

            if (args.IsSettingsSelected)
            {
                // Navigate to the built-in Settings page
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.SelectedItemContainer is NavigationViewItem selectedItem)
            {
                // Map tags to actual Page types
                Type pageType = selectedItem.Tag switch
                {
                    "LanePage" => typeof(LanePage),
                    "ResizerPage" => typeof(ResizerPage),
                    _ => null
                };

                // Prevent redundant navigation to the page we are already viewing
                if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
                {
                    ContentFrame.Navigate(pageType);
                }
            }
        }

        private void ContentFrame_NavigationFailed(object sender, Microsoft.UI.Xaml.Navigation.NavigationFailedEventArgs e)
        {
            throw new Exception($"Failed to load Page {e.SourcePageType.FullName}");
        }

        public void HandleGenerationMacroPress()
        {
            _sreader.TryRead(baseAddresses.GeneralData);

            if (ContentFrame.Content is LanePage lanePage)
            {
                if (lanePage.autoMode)
                {
                    if (lanePage.ChosenFolderPath != null)
                    {
                        if (baseAddresses.GeneralData.RawStatus == 5 || baseAddresses.GeneralData.RawStatus == 12)
                        {
                            lanePage.GenerateLane();
                        }
                    }
                    else
                    {
                        lanePage.dispatcherQueue.TryEnqueue(() =>
                        {
                            lanePage.ShowNotification("Folder path is empty.", InfoBarSeverity.Error);
                        });
                    }
                }        
            }
        }

        public void HandleRevertMacroPress()
        {
            _sreader.TryRead(baseAddresses.GeneralData);

            if (ContentFrame.Content is LanePage lanePage)
            {
                if (lanePage.autoMode)
                {
                    if (lanePage.ChosenFolderPath != null)
                    {
                        if (baseAddresses.GeneralData.RawStatus == 5 || baseAddresses.GeneralData.RawStatus == 12)
                        {
                            lanePage.RevertLane();
                        }
                    }
                    else
                    {
                        lanePage.dispatcherQueue.TryEnqueue(() =>
                        {
                            lanePage.ShowNotification("Folder path is empty.", InfoBarSeverity.Error);
                        });
                    }
                }
            }
        }
    }
}
