using QSP.LibraryExtension;
using System.Collections.Generic;

namespace QSP.RouteFinding.Airports
{
    public class Airport : IAirport
    {
        public string Icao { get; private set; }
        public string Name { get; private set; }
        public double Lat { get; private set; }
        public double Lon { get; private set; }
        public int Elevation { get; private set; }
        public bool TransAvail { get; private set; }
        public int TransAlt { get; private set; }
        public int TransLvl { get; private set; }
        public int LongestRwyLengthFt { get; private set; }
        public IReadOnlyList<IRwyData> Rwys { get; private set; }

        public Airport(
            string Icao,
            string Name,
            double Lat,
            double Lon,
            int Elevation,
            bool TransAvail,
            int TransAlt,
            int TransLvl,
            int LongestRwyLengthFt,
            IReadOnlyList<RwyData> Rwys)
        {
            this.Icao = Icao;
            this.Name = Name;
            this.Lat = Lat;
            this.Lon = Lon;
            this.Elevation = Elevation;
            this.TransAvail = TransAvail;
            this.TransAlt = TransAlt;
            this.TransLvl = TransLvl;
            this.LongestRwyLengthFt = LongestRwyLengthFt;
            this.Rwys = Rwys;
        }

        public Airport(Airport item)
        {
            Icao = item.Icao;
            Name = item.Name;
            Lat = item.Lat;
            Lon = item.Lon;
            Elevation = item.Elevation;
            TransAvail = item.TransAvail;
            TransAlt = item.TransAlt;
            TransLvl = item.TransLvl;
            LongestRwyLengthFt = item.LongestRwyLengthFt;
            Rwys = new List<IRwyData>(item.Rwys);
        }

        public bool Equals(IAirport other)
        {
            return IAirportExtensions.Equals(this, other);
        }

        public override int GetHashCode()
        {
            var h1 = new object[]
            {
                Icao, Name, Lat, Lon, Elevation, TransAvail,
                TransAlt, TransLvl, LongestRwyLengthFt
            }.HashCodeByElem();

            var h2 = Rwys.HashCodeSymmetric();
            return new[] { h1, h2 }.HashCodeByElem();
        }
    }
}
