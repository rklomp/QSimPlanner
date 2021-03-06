﻿using QSP.AircraftProfiles.Configs;
using System.Collections.Generic;
using System.Linq;

namespace QSP.TOPerfCalculation
{
    public static class FindTable
    {
        /// <summary>
        /// Return values are null if not found.
        /// </summary>
        public static (AircraftConfig, PerfTable) Find(IReadOnlyList<PerfTable> tables,
            AcConfigManager aircrafts, string registration)
        {
            if (tables != null && tables.Count > 0)
            {
                var config = aircrafts.Find(registration);
                if (config == null) return (null, null);
                var ac = config.Config;
                var profileName = ac.TOProfile;
                return (config, tables.First(t => t.Entry.ProfileName == profileName));
            }

            return (null, null);
        }
    }
}
