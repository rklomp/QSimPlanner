﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace QSP.RouteFinding.Routes
{
    // For example, we have a route: (P A1 Q A2 R A3 S), where P, Q, R, S are waypoints, and A1, A2, A3 are airways.
    // However, the actual representation of (Q A2 R) is (Q B1 X B2 Y R). So when we 'Expand' the route, it becomes
    // (P A1 Q B1 X B2 Y R A3 S). Now by calling 'Collapse' the route will be restored.
    //
    // The use of this class is to display a route with NATs with 2 different forms:
    // (1) Collapsed form: ... ERAKA NATA SAVRY ...
    // (2) Expanded form: ... ERAKA N60W020 N62W030 N62W040 N61W050 SAVRY ...
    //
    // In the above example, Route in RouteEntry is (ERAKA N60W020 N62W030 N62W040 N61W050 SAVRY), and RouteName is NATA.
    // 

    public class RouteToggler
    {
        private LinkedList<RouteNode> route;
        private TrackEntries tracks;
        private bool Expanded;

        public RouteToggler(LinkedList<RouteNode> route)
        {
            this.route = route;
            tracks = new TrackEntries();
        }

        public void Expand()
        {
            if (Expanded)
            {
                return;
            }

            foreach (var i in tracks.AllEntries)
            {
                RouteExpand.InsertRoute(route, i.Route, i.RouteName);
            }
            Expanded = true;
        }

        public void Collapse()
        {
            if (Expanded == false)
            {
                return;
            }
            
            foreach (var i in tracks.AllEntries)
            {
                RouteCollapse.Collapse(route, i.Route, i.RouteName);
            }
            Expanded = false;
        }

        private class TrackEntries
        {           
            private List<RouteEntry> Nats;
            private List<RouteEntry> Pacots;
            private List<RouteEntry> Ausots;

            public TrackEntries()
            {
                Nats = new List<RouteEntry>();
                Pacots = new List<RouteEntry>();
                Ausots = new List<RouteEntry>();
            }

            public ReadOnlyCollection<RouteEntry> AllEntries
            {
                get
                {
                    var result = new List<RouteEntry>(Nats);
                    result.AddRange(Pacots);
                    result.AddRange(Ausots);
                    return result.AsReadOnly();
                }
            }
        }

        private struct RouteEntry
        {
            public LinkedList<RouteNode> Route;
            public string RouteName;
        }
    }
}
