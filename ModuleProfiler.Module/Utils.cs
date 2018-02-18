using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using Microsoft.Diagnostics.Runtime;
using NLog;

namespace ModuleProfiler.Module
{
    public static class Utils
    {
        private static readonly string[] _sizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        /// <summary>
        /// Walks the heap and checks all objects for string type.
        /// </summary>
        /// <returns>The number of found strings.</returns>
        public static int GetStringCount()
        {
            int numberOfStrings = 0;
            Process currentProcess = Process.GetCurrentProcess();

            using (DataTarget dataTarget = DataTarget.AttachToProcess(currentProcess.Id, 10000, AttachFlag.Passive))
            {
                ClrInfo clrVersion = dataTarget.ClrVersions.First();
                ClrRuntime runtime = clrVersion.CreateRuntime();
                ClrHeap heap = runtime.Heap;

                if (!heap.CanWalkHeap)
                    return 0;

                foreach (ulong ptr in heap.EnumerateObjectAddresses())
                {
                    ClrType type = heap.GetObjectType(ptr);

                    if (type == null || type.IsString == false)
                        continue;

                    numberOfStrings++;
                }
            }

            return numberOfStrings;
        }

        /// <summary>
        /// Transforms bytes into a display figure (i.e. 1024 bytes -> 1.0 kb)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SizeToDisplay(this long value)
        {
            int decimalPlaces = 1;

            if (decimalPlaces < 0)
                throw new ArgumentOutOfRangeException("decimalPlaces");

            if (value < 0)
                return "-" + SizeToDisplay(-value);

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