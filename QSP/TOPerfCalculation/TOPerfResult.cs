using QSP.AviationTools;
using System;
using System.Collections.Generic;
using System.Text;
using static QSP.AviationTools.Constants;

namespace QSP.TOPerfCalculation
{
    public class TOPerfResult
    {
        public DataRow PrimaryResult { get; private set; }
        public List<DataRow> AssumedTemp { get; private set; }

        public TOPerfResult()
        {
            AssumedTemp = new List<DataRow>();
        }

        public void SetPrimaryResult(int OatCelsius,
            int RwyRequiredMeter,
            int RwyRemainingMeter)
        {
            PrimaryResult =
                new DataRow(OatCelsius, RwyRequiredMeter, RwyRemainingMeter);
        }

        public void AddAssumedTemp(int OatCelsius,
            int RwyRequiredMeter,
            int RwyRemainingMeter)
        {
            AssumedTemp.Add(
                new DataRow(OatCelsius, RwyRequiredMeter, RwyRemainingMeter));
        }

        public string ToString(TemperatureUnit tempUnit, LengthUnit lenUnit)
        {
            string tUnit = tempUnit == TemperatureUnit.Celsius ? "°C" : "°F";
            string lUnit = lenUnit == LengthUnit.Meter ? "M" : "FT";

            var str = new StringBuilder();

            str.AppendLine();

            str.AppendLine("                (   OAT " +
                tempConvertUnit(PrimaryResult.OatCelsius, tempUnit) +
                tUnit + "   )");

            str.AppendLine("          Required distance     " +
                lengthConvertUnit(PrimaryResult.RwyRequiredMeter, lenUnit)
                + lUnit);

            str.AppendLine("          Runway remaining      " +
                lengthConvertUnit(PrimaryResult.RwyRemainingMeter, lenUnit)
                + lUnit);

            str.AppendLine();
            str.AppendLine(new string('-', 50));

            if (AssumedTemp.Count > 0)
            {
                str.AppendLine("             ( ASSUMED TEMPERATURE )");
                str.AppendLine("  Temp(" + tUnit +
                    ")  Required distance   Runway remaining");

                foreach (var i in AssumedTemp)
                {
                    str.Append(tempConvertUnit(i.OatCelsius, tempUnit)
                                .ToString()
                                .PadLeft(6, ' '));

                    str.Append(lengthConvertUnit(i.RwyRequiredMeter, lenUnit)
                                .ToString()
                                .PadLeft(15, ' ')
                                + " " + lUnit);

                    str.AppendLine(lengthConvertUnit(i.RwyRemainingMeter, lenUnit)
                                .ToString()
                                .PadLeft(17, ' ')
                                + " " + lUnit);
                }
            }

            return str.ToString();
        }

        private int tempConvertUnit(int tempCelsuis, TemperatureUnit tempUnit)
        {
            return tempUnit == TemperatureUnit.Fahrenheit ?
                Convert.ToInt32(CoversionTools.ToFahrenheit(tempCelsuis)) :
                tempCelsuis;
        }

        private int lengthConvertUnit(int lengthMeter, LengthUnit unit)
        {
            return unit == LengthUnit.Feet ?
                (int)(lengthMeter * MeterFtRatio) :
                lengthMeter;
        }

        public class DataRow
        {
            public int OatCelsius { get; private set; }
            public int RwyRequiredMeter { get; private set; }
            public int RwyRemainingMeter { get; private set; }

            public DataRow(int OatCelsius,
                int RwyRequiredMeter,
                int RwyRemainingMeter)
            {
                this.OatCelsius = OatCelsius;
                this.RwyRequiredMeter = RwyRequiredMeter;
                this.RwyRemainingMeter = RwyRemainingMeter;
            }
        }
    }
}