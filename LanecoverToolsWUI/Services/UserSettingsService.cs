using LanecoverToolsWUI.Ressources;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LanecoverToolsWUI.Services
{
    public static class UserSettingsService
    {
        private static readonly string SettingsFilePath = "userSettings.json";

        public static async Task SaveUserSettingsAsync(
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
            List<int> generationHotkey,
            List<int> revertHotkey)
        {
            var settings = new UserSettings(
                chosenFolderPath,
                selectedHeight,
                selectedWidth,
                targetAr,
                accNotchCb,
                notchHeight,
                notchWidth,
                notchXOffset,
                notchYOffset,
                gradientCb,
                gradientIntensity,
                generationHotkey,
                revertHotkey
            );

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsFilePath, json);
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
        }
    }
}