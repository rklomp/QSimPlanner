﻿using QSP.Common.Options;
using System;
using System.IO;
using System.Xml.Linq;
using static QSP.Utilities.ExceptionHelpers;
using static QSP.Utilities.LoggerInstance;

namespace QSP.Updates
{
    public static class Utilities
    {
        public const string VersionXmlPath = "../version.xml";

        public static string GetFolder(Version ver)
        {
            return Path.Combine("..", ver.ToString());
        }

        /// <summary>
        /// The versions are strings of the format major.minor.build.
        /// Backup version is empty string if the application was never updated.
        /// Do NOT use reflection to get the current version so that it is easier to test the
        /// updater system.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static VersionInfo GetVersions()
        {
            var root = GetVersionXDoc().Root;

            return new VersionInfo()
            {
                Backup = root.Element("backup").Value,
                Current = root.Element("current").Value
            };
        }

        /// <exception cref="Exception"></exception>
        public static XDocument GetVersionXDoc()
        {
            return XDocument.Load(VersionXmlPath);
        }

        public class VersionInfo { public string Backup, Current; }
        
        /// <summary>
        /// Shows the license only if the current version of application is never run.
        /// </summary>
        public static bool ShouldShowLicense()
        {
            try
            {
                if (File.Exists(OptionManager.DefaultPath)) return false;

                var ver = GetVersions();
                return ver.Backup == "";
            }
            catch (Exception ex)
            {
                Log(ex);
                return true;
            }
        }

        /// <summary>
        /// Returns the current version. If failed, returns empty string.
        /// </summary>
        public static string TryGetVersion()
        {
            return DefaultIfThrows(() => GetVersions().Current, "");
        }
    }
}
