﻿using QSP.AviationTools;
using QSP.Common;
using QSP.MathTools;
using QSP.MathTools.Interpolation;
using QSP.MathTools.Tables;
using QSP.TOPerfCalculation.Airbus.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using static QSP.MathTools.Angles;

namespace QSP.TOPerfCalculation.Airbus
{
    public static class Calculator
    {
        /// <summary>
        /// Error can be None, NoDataForSelectedFlaps, or RunwayTooShort.
        /// </summary>
        /// <exception cref="RunwayTooShortException"></exception>
        /// <exception cref="Exception"></exception>
        public static TOReport TakeOffReport(AirbusPerfTable t, TOParameters p, 
            double tempIncrement = 1.0)
        {
            var d = TakeOffDistanceMeter(t, p);
            var primary = new TOReportRow(p.OatCelsius, d, p.RwyLengthMeter - d);
            if (primary.RwyRemainingMeter < 0) throw new RunwayTooShortException();

            var rows = new List<TOReportRow>();
            double maxOat = 67;

            for (double oat = p.OatCelsius + tempIncrement; oat <= maxOat; oat += tempIncrement)
            {
                try
                {
                    var q = p.CloneWithOat(oat);
                    d = TakeOffDistanceMeter(t, q);
                    var remaining = q.RwyLengthMeter - d;
                    if (remaining < 0) break;
                    rows.Add(new TOReportRow(oat, d, remaining));
                }
                catch { }
            }

            return new TOReport(primary, rows);
        }

        /// <summary>
        /// Computes the required takeoff distance.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static double TakeOffDistanceMeter(AirbusPerfTable t, TOParameters p)
        {
            var tables = GetTables(t, p);

            if (tables.Count == 0) throw new Exception("No data for selected flaps");
            var pressAlt = ConversionTools.PressureAltitudeFt(p.RwyElevationFt, p.QNH);
            var inverseTables = tables.Select(x => GetInverseTable(x, pressAlt, t, p));
            var distancesFt = inverseTables.Select(x =>
                x.ValueAt(p.WeightKg * 0.001 * Constants.KgLbRatio))
                .ToArray();

            var d = (distancesFt.Length == 1) ?
                distancesFt[0] :
                Interpolate1D.Interpolate(
                    tables.Select(x => x.IsaOffset).ToArray(),
                    distancesFt,
                    IsaOffset(p));

            // The slope and wind correction is not exactly correct according to
            // performance xml file comments. However, the table itsel is probably
            // not that precise anyways.
            return Constants.FtMeterRatio * (d - SlopeAndWindCorrectionFt(d, t, p));
        }

        // Use the length of first argument instead of the one in Parameters.
        private static double SlopeAndWindCorrectionFt(double lengthFt,
            AirbusPerfTable t, TOParameters p)
        {
            var windCorrectedFt = lengthFt + WindCorrectionFt(lengthFt, t, p);
            return windCorrectedFt - lengthFt + SlopeCorrectionFt(t, p, windCorrectedFt);
        }

        private static double WindCorrectionFt(double lengthFt,
            AirbusPerfTable t, TOParameters p)
        {
            var headwind = p.WindSpeedKnots *
               Math.Cos(ToRadian(p.RwyHeading - p.WindHeading));

            return (headwind >= 0 ?
               t.HeadwindCorrectionTable.ValueAt(lengthFt) :
               t.TailwindCorrectionTable.ValueAt(lengthFt)) * headwind;
        }

        private static double SlopeCorrectionFt(AirbusPerfTable t, TOParameters p,
            double windCorrectedLengthFt)
        {
            var len = windCorrectedLengthFt;
            var s = p.RwySlopePercent;
            return (s >= 0 ?
                t.UphillCorrectionTable.ValueAt(len) :
                t.DownHillCorrectionTable.ValueAt(len)) * -s;
        }

        private static double BleedAirCorrection1000LB(AirbusPerfTable t, TOParameters p)
        {
            if (p.PacksOn) return t.PacksOnCorrection;
            if (p.AntiIce == AntiIceOption.EngAndWing) return t.AllAICorrection;
            if (p.AntiIce == AntiIceOption.Engine) return t.EngineAICorrection;
            return 0.0;
        }

        private static double WetCorrection1000LB(double lengthFt,
            AirbusPerfTable t, TOParameters p)
        {
            if (!p.SurfaceWet) return 0.0;
            return t.WetCorrectionTable.ValueAt(lengthFt);
        }

        private static double IsaOffset(TOParameters p) =>
            p.OatCelsius - ConversionTools.IsaTemp(p.RwyElevationFt);

        // Returns best matching tables, returning list can have:
        // 0 element if no matching flaps, or
        // 1 element if only 1 table matches the flaps setting, or
        // 2 elements if more than 1 table match the flaps, these two tables are
        // the ones most suitable for ISA offset interpolation.
        private static List<TableDataNode> GetTables(AirbusPerfTable t, TOParameters p)
        {
            var allFlaps = t.AvailableFlaps().ToList();
            if (p.FlapsIndex >= allFlaps.Count) return new List<TableDataNode>();
            var flaps = allFlaps.ElementAt(p.FlapsIndex);
            var sameFlaps = t.Tables.Where(x => x.Flaps == flaps).ToList();
            if (sameFlaps.Count == 1) return sameFlaps;
            var ordered = sameFlaps.OrderBy(x => x.IsaOffset).ToList();
            var isaOffset = IsaOffset(p);
            var skip = ordered.Where(x => isaOffset > x.IsaOffset).Count() - 1;
            var actualSkip = Numbers.LimitToRange(skip, 0, ordered.Count - 2);
            return ordered.Skip(actualSkip).Take(2).ToList();
        }

        private static double WetAndBleedAirCorrection1000LB(double lengthFt,
            AirbusPerfTable t, TOParameters p) =>
            WetCorrection1000LB(lengthFt, t, p) + BleedAirCorrection1000LB(t, p);

        // The table is for limit weight. This method constructs a table of 
        // takeoff distance. (x: weight 1000 LB, f: runway length ft)
        // Wet runway and bleed air corrections are applied here.
        private static Table1D GetInverseTable(TableDataNode n, double pressAlt,
            AirbusPerfTable t, TOParameters p)
        {
            var table = n.Table;
            var len = table.y;
            var weight = len.Select(i =>
                table.ValueAt(pressAlt, i) - WetAndBleedAirCorrection1000LB(i, t, p));
            return new Table1D(weight.ToArray(), len.ToArray());
        }
    }
}
