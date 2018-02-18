using System;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using NLog;

namespace ModuleProfiler.Module.Models
{
    public class RequestAnalysis
    {
        public string RequestId { get; }

        public TimeSpan TotalRequestTime { get; set; }

        public TimeSpan ModuleRequestTime { get; set; }

        public long ResponseSize { get; set; }

        public long MemoryUsage { get; set; }

        public int AssemblyCount { get; set; }

        public int StringsCount { get; set; }

        public long MinimumSize { get; set; }

        public long AverageSize { get; set; }

        public long MaximumSize { get; set; }

        private static Logger _requestsLogger;

        public RequestAnalysis()
        {
            RequestId = Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");

            _requestsLogger = LogManager.GetLogger("requests");
        }

        public void WriteToLog()
        {
            Utils.Log("|" + RequestId + "|" + ModuleRequestTime.TotalMilliseconds + "|" + TotalRequestTime.TotalMilliseconds + "|" + ResponseSize, _requestsLogger);
        }

        public string BuildStatsOutput()
        {
            NameValueCollection _appSettings = Utils.GetConfigSection("appSettings");

            string FeatureToggle_Interface = _appSettings["FeatureToggle_Interface"];
            bool.TryParse(FeatureToggle_Interface, out bool enabled);

            if (!enabled)
                return "";

            // Although it's a decent measurement of the request it may be negative as the GC cleans things up. 
            string memory = MemoryUsage < 0 ? "N/A" : MemoryUsage.SizeToDisplay();

            var sB = new StringBuilder();
            sB.Append(
                @"<link href='https://cdnjs.cloudflare.com/ajax/libs/toastr.js/2.1.4/toastr.min.css' rel='stylesheet'></link>
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
            sB.Append($@"toastr['success']('Request Id: {RequestId}</br>Total time: {TotalRequestTime.TotalMilliseconds} ms<br/>Module time: {ModuleRequestTime.TotalMilliseconds} ms<br/>");
            sB.Append($@"Size: {ResponseSize.SizeToDisplay()}<br/>Min/Avg/Max: {MinimumSize.SizeToDisplay()} / {AverageSize.SizeToDisplay()} / {MaximumSize.SizeToDisplay()}<br/>Memory: {memory}<br/>Assemblies: {AssemblyCount}<br/>Strings: {StringsCount}");
            sB.Append("').css('width','400px');</script></body>");

            return sB.ToString();
        }
    }
}