using System;
using System.Collections.Specialized;
using System.Configuration;
using NLog;

namespace ModuleProfiler.Module.Utils
{
    /// <summary>
    /// Performs utilities that are not specific to request analysis.
    /// </summary>
    public static class SharedUtils
    {
        private static readonly string[] _sizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        /// <summary>
        /// Transforms bytes into a display figure (i.e. 1024 bytes -> 1.0 kb)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToBytesDisplay(this long value)
        {
            const int decimalPlaces = 1;

            if (value < 0)
                return "-" + ToBytesDisplay(-value);

            if (value == 0)
                return string.Format("{0:n" + decimalPlaces + "} bytes", 0);

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", adjustedSize, _sizeSuffixes[mag]);
        }

        /// <summary>
        /// Writes to console as well as the specified logger.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logger"></param>
        public static void Log(string message, Logger logger)
        {
            Console.WriteLine(message);

            logger.Trace(message);
        }

        public static NameValueCollection GetConfigSection(string sectionName)
        {
            return (NameValueCollection)ConfigurationManager.GetSection(sectionName);
        }
    }
}