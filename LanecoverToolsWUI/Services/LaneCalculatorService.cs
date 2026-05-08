using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanecoverToolsWUI.Services
{
    interface ILaneCalculatorService
    {
        List<ApproachRate> InitAr();
        ushort CalculateLaneHeight(double baseAr, double targetAr, double verticalRes);
        List<ApproachRate> CalculateAR();
    }
    class LaneCalculatorService : ILaneCalculatorService
    {
        // 2. Lazy Initialization/Caching for InitAr results
        private readonly List<ApproachRate> _approachRates;

        public LaneCalculatorService() {
            _approachRates = CalculateAR();
        }

        public List<ApproachRate> InitAr()
        {
            return _approachRates;
        }
        public List<ApproachRate> CalculateAR()
        {
            List<ApproachRate> ar = new List<ApproachRate>();
            ushort currentValue = 1800;

            const double arDeviation = 1.08;

            ar.Add(new ApproachRate(0, currentValue, double.Round(currentValue / arDeviation)));

            // This calculates the AR values (ms, ar & ms - catcher), we need a double cast on the incrementing index to prevent breaking the double rounding.
            // The + 0.1 is because we're calculating ARs under 5 and they increment by 120ms
            for (int i = 0; i < 50; i++)
            {
                currentValue -= 12;
                System.Diagnostics.Debug.WriteLine("MS: "+currentValue+" ,AR: "+ double.Round((double)i / 10 + 0.1, 1)+" ,Actual: "+ double.Round(currentValue / arDeviation));
                ar.Add(new ApproachRate(double.Round((double)i / 10 + 0.1, 1), currentValue, double.Round(currentValue / arDeviation)));
            };

            // Same here
            // The + 5.1 is because we're calculating ARs over 5 and they increment by 150ms
            for (int i = 0; i < 60; i++)
            {
                currentValue -= 15;
                System.Diagnostics.Debug.WriteLine("MS: " + currentValue + " ,AR: " + double.Round((double)i / 10 + 5.1, 1) + " ,Actual: " + double.Round(currentValue / arDeviation));
                ar.Add(new ApproachRate(double.Round((double)i / 10 + 5.1, 1), currentValue, double.Round(currentValue / arDeviation)));
            };

            return ar;
        }
        public ushort CalculateLaneHeight(double baseAr, double targetAr, double verticalRes)
        {
            List<ApproachRate> approachRates = InitAr();    

            ApproachRate? bAr = approachRates.Find((ar) => ar.Digit == baseAr); // We find the AR Object for the specified base AR
            ArgumentNullException.ThrowIfNull(bAr);

            ApproachRate? tAr = approachRates.Find((ar) => ar.Digit == targetAr); // We find the AR Object for the specified target AR
            ArgumentNullException.ThrowIfNull(tAr);

            const ushort catcherHeight = 340;
            double reductionRatio = double.Round(tAr.Actual / bAr.Actual, 4);
            double reducedHeight = (verticalRes - catcherHeight) * reductionRatio;

            return (ushort) double.Round(verticalRes - catcherHeight - reducedHeight);
        }
    }

    class ApproachRate
    {
        public double Digit { get; set; }
        public ushort Milliseconds { get; set; }
        public double Actual { get; set; }

        public ApproachRate(double digit, ushort milliseconds, double actual) 
        {
            Digit = digit;
            Milliseconds = milliseconds;
            Actual = actual;
        }
    }
}
