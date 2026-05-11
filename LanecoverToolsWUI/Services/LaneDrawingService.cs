using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanecoverToolsWUI.Services
{
    public record LaneGenerationSettings(
        double BaseAr,
        double TargetAr,
        int VerticalRes,
        int HorizontalRes,
        Windows.UI.Color LaneColor,
        string SavePath,
        ushort laneHeight,
        bool IsGradualLane,
        string GradientIntensity,
        bool HasNotch,
        int NotchHeight,
        int NotchWidth,
        int NotchOffsetX,
        int NotchOffsetY
    );

    public class LaneGeneratorService
    {
        public Bitmap Generate(LaneGenerationSettings settings)
        {
            int correctedWidth = CalculateRes(settings.HorizontalRes, settings.VerticalRes);

            Bitmap bitmap = new(correctedWidth, settings.laneHeight);
            Graphics graphics = Graphics.FromImage(bitmap);

            float[] factorsHighest = { 0f, 0f, .4f, .6f, .8f, 1f, 1f };
            float[] factorsHigh = { 0f, 0f, .2f, .4f, .6f, .8f, 1f };
            float[] factorsMedium = { 0f, 0f, 0f, .2f, .4f, .5f, 1f };
            float[] factorsLow = { 0f, 0f, 0f, 0f, 0f, .4f, 1f };
            float[] factorsLowest = { 0f, 0f, 0f, 0f, 0f, 0f, 1f };

            float[] myPositions = { 0f, .2f, .4f, .6f, .8f, .9f, 1f };

            Color fillColor = Color.FromArgb(settings.LaneColor.A, settings.LaneColor.R, settings.LaneColor.G, settings.LaneColor.B);

            GraphicsPath mainPath = new GraphicsPath();
            mainPath.AddRectangle(new RectangleF(0, 0, correctedWidth, settings.laneHeight));
            var rgn1 = new Region(mainPath);

            if (settings.IsGradualLane)
            {
                Rectangle rect = new(0, 0, correctedWidth, settings.laneHeight);
                Blend blend = new();
                blend.Positions = myPositions;
                System.Diagnostics.Debug.WriteLine(settings.GradientIntensity);
                switch (settings.GradientIntensity)
                {
                    case "Highest":
                        blend.Factors = factorsHighest;
                        break;
                    case "High":
                        blend.Factors = factorsHigh;
                        break;
                    case "Medium":
                        blend.Factors = factorsMedium;
                        break;
                    case "Low":
                        blend.Factors = factorsLow;
                        break;
                    case "Lowest":
                        blend.Factors = factorsLowest;
                        break;
                }
                System.Drawing.Drawing2D.LinearGradientBrush linearGradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        rect,
                        fillColor,
                        Color.Transparent,
                        LinearGradientMode.Vertical
                        );
                linearGradientBrush.Blend = blend;
                graphics.FillRectangle(linearGradientBrush, 0, 0, correctedWidth, settings.laneHeight);
                //graphics.FillRegion(linearGradientBrush, rgn1);
            } else
            {
                graphics.FillRectangle(new SolidBrush(fillColor), 0, 0, correctedWidth, settings.laneHeight);
                //graphics.FillRegion(new SolidBrush(fillColor), rgn1);
            }

            if (settings.HasNotch)
            {
                int xStart = correctedWidth - settings.NotchOffsetX - settings.NotchWidth;
                int yStart = settings.NotchOffsetY;

                Rectangle removeArea = new(
                    xStart, yStart, settings.NotchWidth, settings.NotchHeight
                );

                graphics.SetClip(removeArea);
                graphics.Clear(Color.Transparent);
            }

            return bitmap;       
        }

        public static int CalculateRes(int width, int height)
        {
            string aspectRatio = CheckAspectRatio(width, height);

            if (aspectRatio == "16:9" || (width == 1366 && height == 768))
            {
                return width > 1366 ? 1366 : width;
            }
            else if (aspectRatio == "16:10")
            {
                return width > 1229 ? 1229 : width;
            }
            else if (aspectRatio == "4:3")
            {
                return width > 1280 ? 1280 : width;
            }
            return 1366;
        }

        private static string CheckAspectRatio(int width, int height)
        {
            if (width <= 0 || height <= 0) return "invalid";

            int gcd = Gcd(width, height);
            int ratioW = width / gcd;
            int ratioH = height / gcd;

            return $"{ratioW}:{ratioH}";
        }

        private static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }
    }
}
