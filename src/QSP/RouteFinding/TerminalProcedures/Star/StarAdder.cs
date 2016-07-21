﻿using QSP.RouteFinding.Airports;
using QSP.RouteFinding.AirwayStructure;
using QSP.RouteFinding.Containers;
using QSP.RouteFinding.Data;
using System.Collections.Generic;
using static QSP.RouteFinding.WaypointAirwayConnector;
using static QSP.Utilities.LoggerInstance;

namespace QSP.RouteFinding.TerminalProcedures.Star
{
    // Adds necessary waypoints and neighbors to wptList for automatic route 
    // finder or route analyzer, and returns the index of origin rwy 
    // in wptList.
    // 
    // There are 4 cases:
    // 1. There's no STAR at all. 
    // 2. The first wpt in STAR is NOT connected to an airway but in wptList.
    // 3. The first wpt in STAR is connected to an airway.
    // 4. The first wpt in STAR is NOT connected to an airway and NOT 
    //    in wptList.
    //  
    public class StarAdder
    {
        private string icao;
        private WaypointList wptList;
        private WaypointListEditor editor;
        private AirportManager airportList;
        private StarCollection stars;
        private WptSearchOption option;

        public StarAdder(
            string icao,
            StarCollection stars,
            WaypointList wptList,
            WaypointListEditor editor,
            AirportManager airportList)
            : this(
                  icao,
                  stars,
                  wptList,
                  editor,
                  airportList,
                  new WptSearchOption())
        { }

        public StarAdder(
            string icao,
            StarCollection stars,
            WaypointList wptList,
            WaypointListEditor editor,
            AirportManager airportList,
            WptSearchOption option)
        {
            this.icao = icao;
            this.stars = stars;
            this.wptList = wptList;
            this.editor = editor;
            this.airportList = airportList;
            this.option = option;
        }

        // The 4 different cases are treated seperately.
        // The corresponding actions for each case are:
        //
        // (In all cases, destination runway is added to wptList.)
        //
        // Case 1. Adds destination runway as neighbors of some waypoints* 
        //         (use DCT as airway).      
        // Case 2. Finds neighbors of the first waypoint in STAR, and add 
        //         first waypoint in STAR as their neighbors 
        //         (use STAR name as airway).
        // Case 3. Adds the destination runway as a neighbor of first 
        //         waypoint in STAR (use STAR name as airway).
        // Case 4. Same as case 2. But also adds the first waypoint to wptList.
        //
        // * Using WptSearchOption.
        //

        /// <summary>
        /// Add necessary waypoints and neighbors for STAR computation 
        /// to WptList, and returns the index of Dest. rwy in WptList.
        /// </summary>
        public int AddStarsToWptList(string rwy, List<string> starsToAdd)
        {
            if (starsToAdd.Count == 0)
            {
                // Case 1
                return ProcessCase1(rwy);
            }
            else
            {
                // Case 2, 3, 4
                var latLon = airportList.RwyLatLon(icao, rwy);
                var wpt = new Waypoint(icao + rwy, latLon);
                int index = editor.AddWaypoint(wpt);

                foreach (var i in starsToAdd)
                {
                    try
                    {
                        // this is where case 2, 3, 4 are handled.
                        AddToWptList(index, rwy, i);
                    }
                    catch (WaypointNotFoundException ex)
                    {
                        WriteToLog(ex.ToString());
                    }
                }
                return index;
            }
        }

        private int ProcessCase1(string rwy)
        {
            var rwyLatLon = airportList.RwyLatLon(icao, rwy);
            var nearbyWpts = AirwayConnections(rwyLatLon.Lat, rwyLatLon.Lon);

            int index = editor.AddWaypoint(new Waypoint(icao + rwy, rwyLatLon));

            foreach (var i in nearbyWpts)
            {
                editor.AddNeighbor(i.Index, index, new Neighbor("DCT", i.Distance));
            }

            return index;
        }

        /// <exception cref="WaypointNotFoundException"></exception>
        private void AddToWptList(int rwyIndex, string rwy, string star)
        {
            var starInfo = stars.GetStarInfo(star, rwy, wptList[rwyIndex]);
            var firstWpt = starInfo.FirstWaypoint;
            int firstWptIndex = wptList.FindByWaypoint(firstWpt);

            if (firstWptIndex < 0)
            {
                // Case 4
                firstWptIndex = editor.AddWaypoint(firstWpt);
            }

            if (wptList.EdgesToCount(firstWptIndex) == 0)
            {
                // Case 2                                 
                foreach (var k in AirwayConnections(firstWpt.Lat, firstWpt.Lon))
                {
                    editor.AddNeighbor(
                        k.Index, firstWptIndex, new Neighbor("DCT", k.Distance));
                }
            }
            // For case 2, 3 and 4
            editor.AddNeighbor(
                firstWptIndex,
                rwyIndex,
                new Neighbor(star, starInfo.TotalDistance));
        }

        private void ProcessCase2(
            int rwyIndex, string star, Waypoint lastWpt, double disAdd)
        {
            var endPoints = AirwayConnections(lastWpt.Lat, lastWpt.Lon);

            foreach (var i in endPoints)
            {
                editor.AddNeighbor(
                    i.Index, rwyIndex, new Neighbor(star, i.Distance + disAdd));
            }
        }

        private List<IndexDistancePair> AirwayConnections(
            double lat, double lon)
        {
            return FromAirway(lat, lon, wptList, option);
        }

    }
}
