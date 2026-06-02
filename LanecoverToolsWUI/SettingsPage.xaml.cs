using LanecoverToolsWUI.Ressources;
using LanecoverToolsWUI.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LanecoverToolsWUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public ObservableCollection<ComboBoxItem> GenerateModifier1Data { get; set; } = new([
            new ComboBoxItem { Content = "CTRL", IsEnabled = true, Tag = VirtualKeys.VK_CONTROL},
            new ComboBoxItem { Content = "SHIFT", IsEnabled = true, Tag = VirtualKeys.VK_SHIFT},
            new ComboBoxItem { Content = "ALT", IsEnabled = true, Tag = VirtualKeys.VK_MENU}
            ]);
        public ObservableCollection<ComboBoxItem> GenerateModifier2Data { get; set; } = new([
            new ComboBoxItem { Content = "CTRL", IsEnabled = true, Tag = VirtualKeys.VK_CONTROL},
            new ComboBoxItem { Content = "SHIFT", IsEnabled = true, Tag = VirtualKeys.VK_SHIFT},
            new ComboBoxItem { Content = "ALT", IsEnabled = true, Tag = VirtualKeys.VK_MENU}
            ]);
        public ComboBoxItem SelectedGMod1 { get; set; }
        public ComboBoxItem SelectedGMod2 { get; set; }
        public ComboBoxItem SelectedGKey { get; set; }

        public ObservableCollection<ComboBoxItem> RevertModifier1Data { get; set; } = new([
            new ComboBoxItem { Content = "CTRL", IsEnabled = true, Tag = VirtualKeys.VK_CONTROL},
            new ComboBoxItem { Content = "SHIFT", IsEnabled = true, Tag = VirtualKeys.VK_SHIFT},
            new ComboBoxItem { Content = "ALT", IsEnabled = true, Tag = VirtualKeys.VK_MENU}
            ]);
        public ObservableCollection<ComboBoxItem> RevertModifier2Data { get; set; } = new([
            new ComboBoxItem { Content = "CTRL", IsEnabled = true, Tag = VirtualKeys.VK_CONTROL},
            new ComboBoxItem { Content = "SHIFT", IsEnabled = true, Tag = VirtualKeys.VK_SHIFT},
            new ComboBoxItem { Content = "ALT", IsEnabled = true, Tag = VirtualKeys.VK_MENU}
            ]);
        public ComboBoxItem SelectedRMod1 { get; set; }
        public ComboBoxItem SelectedRMod2 { get; set; }
        public ComboBoxItem SelectedRKey { get; set; }
        public UserSettings UserSettings { get; set; }

        private MainWindow _mainWindow;

        public SettingsPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            PopulateKeyComboBox(GenerateKey);
            PopulateKeyComboBox(RevertKey);

            initUserSettings();
        }

        private void updateHotkeys()
        {   
            if (_mainWindow == null || _mainWindow.GenerationHotkey == null || _mainWindow.RevertHotkey == null)
                return;

            // Update Generate hotkey
            if (SelectedGMod1?.Tag != null && _mainWindow.GenerationHotkey.Count > 0)
                _mainWindow.GenerationHotkey[0] = (int)SelectedGMod1.Tag;
            if (SelectedGMod2?.Tag != null && _mainWindow.GenerationHotkey.Count > 1)
                _mainWindow.GenerationHotkey[1] = (int)SelectedGMod2.Tag;
            if (SelectedGKey?.Tag != null && _mainWindow.GenerationHotkey.Count > 2)
                _mainWindow.GenerationHotkey[2] = (int)SelectedGKey.Tag;

            // Update Revert hotkey
            if (SelectedRMod1?.Tag != null && _mainWindow.RevertHotkey.Count > 0)
                _mainWindow.RevertHotkey[0] = (int)SelectedRMod1.Tag;
            if (SelectedRMod2?.Tag != null && _mainWindow.RevertHotkey.Count > 1)
                _mainWindow.RevertHotkey[1] = (int)SelectedRMod2.Tag;
            if (SelectedRKey?.Tag != null && _mainWindow.RevertHotkey.Count > 2)
                _mainWindow.RevertHotkey[2] = (int)SelectedRKey.Tag;
        }

        private async void initUserSettings()
        {
#pragma warning disable CS8601
            if (_mainWindow?.GenerationHotkey != null && _mainWindow.RevertHotkey != null
        && _mainWindow.GenerationHotkey.Count >= 3 && _mainWindow.RevertHotkey.Count >= 3)
            {
                SelectedGMod1 = GenerateModifier1Data.FirstOrDefault(
                    item => (int)item.Tag == _mainWindow.GenerationHotkey[0]
                );
                SelectedGMod2 = GenerateModifier2Data.FirstOrDefault(
                    item => (int)item.Tag == _mainWindow.GenerationHotkey[1]
                );
                SelectedGKey = GenerateKey.Items.Cast<ComboBoxItem>().FirstOrDefault(
                    item => (int)item.Tag == _mainWindow.GenerationHotkey[2]
                );

                SelectedRMod1 = RevertModifier1Data.FirstOrDefault(
                    item => (int)item.Tag == _mainWindow.RevertHotkey[0]
                );
                SelectedRMod2 = RevertModifier2Data.FirstOrDefault(
                    item => (int)item.Tag == _mainWindow.RevertHotkey[1]
                );
                SelectedRKey = RevertKey.Items.Cast<ComboBoxItem>().FirstOrDefault(
                    item => (int)item.Tag == _mainWindow.RevertHotkey[2]
                );
            }
            else
            {
                // Fallback to defaults: CTRL + SHIFT + X for Generate, CTRL + SHIFT + Q for Revert
                SelectedGMod1 = GenerateModifier1Data.FirstOrDefault(
                    item => (int)item.Tag == VirtualKeys.VK_CONTROL
                );
                SelectedGMod2 = GenerateModifier2Data.FirstOrDefault(
                    item => (int)item.Tag == VirtualKeys.VK_SHIFT
                );
                SelectedGKey = GenerateKey.Items.Cast<ComboBoxItem>().FirstOrDefault(
                    item => (int)item.Tag == VirtualKeys.VK_X
                );

                SelectedRMod1 = RevertModifier1Data.FirstOrDefault(
                    item => (int)item.Tag == VirtualKeys.VK_CONTROL
                );
                SelectedRMod2 = RevertModifier2Data.FirstOrDefault(
                    item => (int)item.Tag == VirtualKeys.VK_SHIFT
                );
                SelectedRKey = RevertKey.Items.Cast<ComboBoxItem>().FirstOrDefault(
                    item => (int)item.Tag == VirtualKeys.VK_Q
                );
            }

            GenerateModifier1.SelectedItem = SelectedGMod1;
            GenerateModifier2.SelectedItem = SelectedGMod2;
            GenerateKey.SelectedItem = SelectedGKey;
            RevertModifier1.SelectedItem = SelectedRMod1;
            RevertModifier2.SelectedItem = SelectedRMod2;
            RevertKey.SelectedItem = SelectedRKey;
        }

        private void PopulateKeyComboBox(ComboBox comboBox)
        {
            comboBox.Items.Clear();

            for (char c = 'A'; c <= 'Z'; c++)
            {
                var item = new ComboBoxItem { Content = c.ToString() };
                item.Tag = (int)typeof(VirtualKeys).GetField($"VK_{c}").GetValue(null);
                comboBox.Items.Add(item);
            }
        }

        private void Modifier_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox currentComboBox = (ComboBox)sender;
            updateHotkeys();

            if (currentComboBox == GenerateKey)
            {
                SelectedGKey = GenerateKey.SelectedItem as ComboBoxItem;
            }
            else if (currentComboBox == RevertKey)
            {
                SelectedRKey = RevertKey.SelectedItem as ComboBoxItem;
            }
            else if (currentComboBox == GenerateModifier1)
            {
                SelectedGMod1 = GenerateModifier1.SelectedItem as ComboBoxItem;
                if (SelectedGMod1 != null)
                {
                    string selectedValue = SelectedGMod1.Content.ToString();
                    foreach (ComboBoxItem item in GenerateModifier2Data)
                    {
                        item.IsEnabled = (item.Content.ToString() != selectedValue);
                    }
                    if (SelectedGMod2 != null && SelectedGMod2.Content.ToString() == selectedValue)
                    {
                        SelectedGMod2 = null;
                        GenerateModifier2.SelectedItem = null;
                    }
                }
            }
            else if (currentComboBox == GenerateModifier2)
            {
                SelectedGMod2 = GenerateModifier2.SelectedItem as ComboBoxItem;
                if (SelectedGMod2 != null)
                {
                    string selectedValue = SelectedGMod2.Content.ToString();
                    foreach (ComboBoxItem item in GenerateModifier1Data)
                    {
                        item.IsEnabled = (item.Content.ToString() != selectedValue);
                    }
                    if (SelectedGMod1 != null && SelectedGMod1.Content.ToString() == selectedValue)
                    {
                        SelectedGMod1 = null;
                        GenerateModifier1.SelectedItem = null;
                    }
                }
            }
            else if (currentComboBox == RevertModifier1)
            {
                SelectedRMod1 = RevertModifier1.SelectedItem as ComboBoxItem;
                if (SelectedRMod1 != null)
                {
                    string selectedValue = SelectedRMod1.Content.ToString();
                    foreach (ComboBoxItem item in RevertModifier2Data)
                    {
                        item.IsEnabled = (item.Content.ToString() != selectedValue);
                    }
                    if (SelectedRMod2 != null && SelectedRMod2.Content.ToString() == selectedValue)
                    {
                        SelectedRMod2 = null;
                        RevertModifier2.SelectedItem = null;
                    }
                }
            }
            else if (currentComboBox == RevertModifier2)
            {
                SelectedRMod2 = RevertModifier2.SelectedItem as ComboBoxItem;
                if (SelectedRMod2 != null)
                {
                    string selectedValue = SelectedRMod2.Content.ToString();
                    foreach (ComboBoxItem item in RevertModifier1Data)
                    {
                        item.IsEnabled = (item.Content.ToString() != selectedValue);
                    }
                    if (SelectedRMod1 != null && SelectedRMod1.Content.ToString() == selectedValue)
                    {
                        SelectedRMod1 = null;
                        RevertModifier1.SelectedItem = null;
                    }
                }
            }

            
        }
    }
}
