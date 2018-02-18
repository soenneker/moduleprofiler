using System;

namespace ModuleProfiler.Module.Models
{
    /// <summary>
    /// POCO that is injected at the end of the request displaying analysis.
    /// </summary>
    public class RequestAnalysis
    {
        public string RequestId { get; set; }

        public TimeSpan TotalRequestTime { get; set; }

        public TimeSpan ModuleRequestTime { get; set; }

        public long ResponseSize { get; set; }

        public long MemoryUsage { get; set; }

        public int AssemblyCount { get; set; }

        public int StringsCount { get; set; }

        public long MinimumSize { get; set; }

        public long AverageSize { get; set; }

        public long MaximumSize { get; set; }
    }
}