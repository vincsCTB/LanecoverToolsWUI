using LanecoverToolsWUI.Ressources;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LanecoverToolsWUI.Services
{
    public class UserSettingsService
    {
        private static readonly string SettingsFilePath = "userSettings.json";
        //public static UserSettings Current { get; set; } = new UserSettings();
        private static UserSettings _current;

        public static UserSettings Current
        {
            get
            {
                // If it hasn't been initialized or loaded yet, create a default one safely
                if (_current == null)
                {
                    _current = new UserSettings();
                }
                return _current;
            }
            set
            {
                _current = value;
            }
        }

        public static void SaveUserSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });

                // FIX 3: Use synchronous write so the app doesn't close mid-operation
                Debug.WriteLine("This is current: ");
                Debug.WriteLine(JsonSerializer.Serialize(Current));
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        public static void LoadUserSettings()
        {
            if (!File.Exists(SettingsFilePath))
            {
                Current = new UserSettings(); // Fallback to defaults
                return;
            }

            try
            {
                string json = File.ReadAllText(SettingsFilePath);
                Current = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
                Debug.WriteLine("This is being loaded: ");
                Debug.WriteLine(json);
            }
            catch (Exception)
            {
                Current = new UserSettings(); // Fallback to defaults if corrupted
            }
        }

        public static async Task<UserSettings?> LoadUserSettingsAsync()
        {
            if (!File.Exists(SettingsFilePath))
                return null;

            string json = await File.ReadAllTextAsync(SettingsFilePath);
            return JsonSerializer.Deserialize<UserSettings>(json);
        }
    }

    public class UserSettings
    {
        public string ChosenFolderPath { get; set; } = String.Empty;
        public double SelectedHeight { get; set; }
        public double SelectedWidth { get; set; }
        public double TargetAr { get; set; }
        public bool AccNotchCb { get; set; }
        public int NotchHeight { get; set; }
        public int NotchWidth { get; set; }
        public int NotchXOffset { get; set; }
        public int NotchYOffset { get; set; }
        public bool GradientCb { get; set; }
        public string GradientIntensity { get; set; } = String.Empty;
        public Windows.UI.Color ChosenColor { get; set; }
        public List<int> GenerationHotkey { get; set; } 
        public List<int> RevertHotkey { get; set; } 

        public UserSettings() { }

        public UserSettings(
            string chosenFolderPath,
            double selectedHeight,
            double selectedWidth,
            double targetAr,
            bool accNotchCb,
            int notchHeight,
            int notchWidth,
            int notchXOffset,
            int notchYOffset,
            bool gradientCb,
            string gradientIntensity,
            Windows.UI.Color chosenColor,
            List<int> generationHotkey,
            List<int> revertHotkey)
        {
            ChosenFolderPath = chosenFolderPath;
            SelectedHeight = selectedHeight;
            SelectedWidth = selectedWidth;
            TargetAr = targetAr;
            AccNotchCb = accNotchCb;
            NotchHeight = notchHeight;
            NotchWidth = notchWidth;
            NotchXOffset = notchXOffset;
            NotchYOffset = notchYOffset;
            GradientCb = gradientCb;
            GradientIntensity = gradientIntensity;
            GenerationHotkey = generationHotkey;
            RevertHotkey = revertHotkey;
            ChosenColor = chosenColor;
        }
    }
}