﻿using NUnit.Framework;
using QSP.Common;
using QSP.LandingPerfCalculation;
using QSP.LandingPerfCalculation.Boeing;
using QSP.LandingPerfCalculation.Boeing.PerfData;
using System;
using System.Xml.Linq;
using UnitTest.Common;

namespace UnitTest.LandingPerfCalculation.Boeing
{
    [TestFixture]
    public class LandingReportGeneratorTest
    {
        [Test]
        public void GetReportTest()
        {
            string text = new TestData().AllText;
            var doc = XDocument.Parse(text);
            var table = new PerfDataLoader().GetItem(doc);

            var para = new LandingParameters(
                55000.0,
                3000.0,
                1000.0,
                -10.0,
                -1.0,
                15.0,
                1013.0,
                5.0,
                (int)ReverserOption.NoRev,
                (int)SurfaceCondition.Good,
                0,
                0);

            var report = new LandingReportGenerator(table, para).GetReport();

            AssertMainResult(report, para, table);
            AssertOtherResult(report, para, table);
        }

        private void AssertMainResult(LandingReport report,
            LandingParameters para, BoeingPerfTable table)
        {
            var entry = report.SelectedBrake;
            var calc = new LandingCalculator(table, para);

            string brake = table.BrakesAvailable((SurfaceCondition)para.SurfaceCondition)[para.BrakeIndex];
            double rwyRequired = calc.DistanceRequiredMeter();

            Assert.IsTrue(entry.BrakeSetting == brake);
            Assert.AreEqual(rwyRequired, entry.RequiredDistanceMeter, 0.5);

            double disRemain = para.RwyLengthMeter - rwyRequired;

            Assert.AreEqual(disRemain, entry.RemainingDistanceMeter, 0.5);
        }

        private void AssertOtherResult(LandingReport report,
            LandingParameters para, BoeingPerfTable table)
        {
            var calc = new LandingCalculator(table, para);

            foreach (var i in report.AllBrakes)
            {
                int brakeIndex = Array.FindIndex(
                    table.BrakesAvailable((SurfaceCondition)para.SurfaceCondition),
                    x => x == i.BrakeSetting);

                PropertySetter.Set(para, "BrakeIndex", brakeIndex);

                double rwyRequired = calc.DistanceRequiredMeter();
                Assert.AreEqual(rwyRequired, i.RequiredDistanceMeter, 0.5);

                double disRemain = para.RwyLengthMeter - rwyRequired;
                Assert.AreEqual(disRemain, i.RemainingDistanceMeter, 0.5);
            }
        }

        [Test]
        public void WhenRwyIsTooShortShouldThrowException()
        {
            string text = new TestData().AllText;
            var doc = XDocument.Parse(text);
            var table = new PerfDataLoader().GetItem(doc);

            var para = new LandingParameters(
                55000.0,
                0.0,        // Too short runway
                1000.0,
                -10.0,
                -1.0,
                15.0,
                1013.0,
                5.0,
                (int)ReverserOption.NoRev,
                (int)SurfaceCondition.Good,
                0,
                0);

            Assert.Throws<RunwayTooShortException>(() =>
            {
                new LandingReportGenerator(table, para).GetReport();
            });
        }
    }
}
