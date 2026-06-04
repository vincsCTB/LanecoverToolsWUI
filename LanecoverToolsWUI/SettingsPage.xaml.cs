using LanecoverToolsWUI.Ressources;
using LanecoverToolsWUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LanecoverToolsWUI
{
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

        private bool _isInitialized = false;

        public SettingsPage()
        {
            InitializeComponent();

            PopulateKeyComboBox(GenerateKey);
            PopulateKeyComboBox(RevertKey);

            InitUserSettings();

            _isInitialized = true;
        }

        /// <summary>
        /// Reads the hotkeys from the central service and matches them to UI dropdown items.
        /// </summary>
        private void InitUserSettings()
        {
            var settings = UserSettingsService.Current;

            // Ensure the hotkey collections exist in settings
            if (settings.GenerationHotkey == null || settings.GenerationHotkey.Count < 3)
            {
                settings.GenerationHotkey = new List<int> { VirtualKeys.VK_CONTROL, VirtualKeys.VK_SHIFT, VirtualKeys.VK_X };
            }
            if (settings.RevertHotkey == null || settings.RevertHotkey.Count < 3)
            {
                settings.RevertHotkey = new List<int> { VirtualKeys.VK_CONTROL, VirtualKeys.VK_SHIFT, VirtualKeys.VK_Q };
            }

            // Map Generate UI elements from Settings Cache
            SelectedGMod1 = GenerateModifier1Data.FirstOrDefault(item => (int)item.Tag == settings.GenerationHotkey[0]);
            SelectedGMod2 = GenerateModifier2Data.FirstOrDefault(item => (int)item.Tag == settings.GenerationHotkey[1]);
            SelectedGKey = GenerateKey.Items.Cast<ComboBoxItem>().FirstOrDefault(item => (int)item.Tag == settings.GenerationHotkey[2]);

            // Map Revert UI elements from Settings Cache
            SelectedRMod1 = RevertModifier1Data.FirstOrDefault(item => (int)item.Tag == settings.RevertHotkey[0]);
            SelectedRMod2 = RevertModifier2Data.FirstOrDefault(item => (int)item.Tag == settings.RevertHotkey[1]);
            SelectedRKey = RevertKey.Items.Cast<ComboBoxItem>().FirstOrDefault(item => (int)item.Tag == settings.RevertHotkey[2]);

            // Formally push the selection directly to the ComboBox Controls
            GenerateModifier1.SelectedItem = SelectedGMod1;
            GenerateModifier2.SelectedItem = SelectedGMod2;
            GenerateKey.SelectedItem = SelectedGKey;

            RevertModifier1.SelectedItem = SelectedRMod1;
            RevertModifier2.SelectedItem = SelectedRMod2;
            RevertKey.SelectedItem = SelectedRKey;
        }

        /// <summary>
        /// Flushes the active UI configurations directly into the global cache.
        /// </summary>
        private void UpdateServiceHotkeys()
        {
            // 1. Exit immediately if the page is still loading
            if (!_isInitialized) return;

            var settings = UserSettingsService.Current;
            if (settings == null) return;

            // 2. Safety Net: If the lists don't exist yet in memory, instantiate them
            if (settings.GenerationHotkey == null) settings.GenerationHotkey = new List<int> { 0, 0, 0 };
            while (settings.GenerationHotkey.Count < 3) settings.GenerationHotkey.Add(0);

            if (settings.RevertHotkey == null) settings.RevertHotkey = new List<int> { 0, 0, 0 };
            while (settings.RevertHotkey.Count < 3) settings.RevertHotkey.Add(0);

            // 3. Now it is 100% safe to update the indexes
            if (SelectedGMod1?.Tag != null) settings.GenerationHotkey[0] = (int)SelectedGMod1.Tag;
            if (SelectedGMod2?.Tag != null) settings.GenerationHotkey[1] = (int)SelectedGMod2.Tag;
            if (SelectedGKey?.Tag != null) settings.GenerationHotkey[2] = (int)SelectedGKey.Tag;

            if (SelectedRMod1?.Tag != null) settings.RevertHotkey[0] = (int)SelectedRMod1.Tag;
            if (SelectedRMod2?.Tag != null) settings.RevertHotkey[1] = (int)SelectedRMod2.Tag;
            if (SelectedRKey?.Tag != null) settings.RevertHotkey[2] = (int)SelectedRKey.Tag;
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
            if (sender is not ComboBox currentComboBox) return;

            // Step 1: Track properties based on user interactions
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

            // Step 2: Push these visual changes instantly to our central UserSettings service RAM cache
            UpdateServiceHotkeys();
        }
    }
}