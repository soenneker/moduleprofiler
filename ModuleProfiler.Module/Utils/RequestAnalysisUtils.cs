using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Diagnostics.Runtime;
using ModuleProfiler.Module.Models;
using NLog;

namespace ModuleProfiler.Module.Utils
{
    /// <summary>
    /// Provides utilities for request analysis specific functionality. 
    /// </summary>
    public static class RequestAnalysisUtils
    {
        public static void WriteToLog(RequestAnalysis analysis, Logger logger)
        {
            SharedUtils.Log("|" + analysis.RequestId + "|" + analysis.ModuleRequestTime.TotalMilliseconds + "|" + analysis.TotalRequestTime.TotalMilliseconds + "|" + analysis.ResponseSize, logger);
        }

        /// <summary>
        /// Constructs an output of the analysis that can be injected into the request's body.
        /// </summary>
        /// <param name="analysis"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        public static string BuildStatsOutput(RequestAnalysis analysis, bool enabled)
        {
            if (!enabled)
                return "";

            // Although it's a decent measurement of the request it may be negative as the GC cleans things up. 
            string memory = analysis.MemoryUsage < 0 ? "N/A" : analysis.MemoryUsage.ToBytesDisplay();

            var sB = new StringBuilder();
            sB.Append(
                @"<link href='https://cdnjs.cloudflare.com/ajax/libs/toastr.js/2.1.4/toastr.min.css' rel='stylesheet'></link>
                <script src='https://cdnjs.cloudflare.com/ajax/libs/jquery/3.3.1/jquery.min.js'></script>
                <script src='https://cdnjs.cloudflare.com/ajax/libs/toastr.js/2.1.4/toastr.min.js'></script>
                <script>
                toastr.options = {
                  'closeButton': false,
                            'debug': false,
                            'newestOnTop': false,
                            'progressBar': false,
                            'positionClass': 'toast-top-right',
                            'preventDuplicates': false,
                            'onclick': null,
                            'showDuration': '300',
                            'hideDuration': '0',
                            'timeOut': '0',
                            'extendedTimeOut': '0',
                            'showEasing': 'swing',
                            'hideEasing': 'linear',
                            'showMethod': 'fadeIn',
                            'hideMethod': 'fadeOut'
                        };");
            sB.Append(Environment.NewLine);
            sB.AppendFormat(@"toastr['success']('Request Id: {0}</br>Total time: {1} ms<br/>Module time: {2} ms<br/>Size: {3}<br/>Min/Avg/Max: {4} / {5} / {6}<br/>Memory: {7}<br/>Assemblies: {8}<br/>Strings: {9}').css('width','400px');</script></body>",
                analysis.RequestId,
                analysis.TotalRequestTime.TotalMilliseconds,
                analysis.ModuleRequestTime.TotalMilliseconds,
                analysis.ResponseSize.ToBytesDisplay(),
                analysis.MinimumSize.ToBytesDisplay(),
                analysis.AverageSize.ToBytesDisplay(),
                analysis.MaximumSize.ToBytesDisplay(),
                memory,
                analysis.AssemblyCount,
                analysis.StringsCount);

            return sB.ToString();
        }

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
    }
}
