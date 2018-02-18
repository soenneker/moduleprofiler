using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Web;
using ModuleProfiler.Module.Models;
using NLog;
using ModuleProfiler.Module.Utils;

namespace ModuleProfiler.Module
{
    /// <summary>
    /// Subscribes to the beginning and end of the requests to the host web application.
    /// </summary>
    public class RequestCaptureModule : IHttpModule
    {
        public RequestAnalysis RequestAnalysis { get; private set; }

        private Stopwatch _moduleStopwatch;
        private Stopwatch _totalStopwatch;

        private StreamCapture _capture;

        private long _startMemory;
        private long _endMemory;

        private int _startStrings;
        private int _endStrings;

        private long _minimumSize;
        private long _averageSize;
        private long _maximumSize;
        private long _totalSize;
        private long _numRequests;

        private Logger _logger;
        private Logger _requestsLogger;

        public void Init(HttpApplication application)
        {
            LogManager.ThrowExceptions = true;

            _logger = LogManager.GetLogger("module");
            _requestsLogger = LogManager.GetLogger("requests");

            _totalStopwatch = new Stopwatch();
            _moduleStopwatch = new Stopwatch();

            _minimumSize = 0;
            _averageSize = 0;
            _maximumSize = 0;

            application.BeginRequest += Application_BeginRequest;
            application.EndRequest += Application_EndRequest;
        }

        private void Application_BeginRequest(object source, EventArgs e)
        {
            var wrapper = new HttpResponseWrapper(((HttpApplication)source).Response);
            BeginRequest(wrapper);
        }

        public void BeginRequest(HttpResponseBase httpResponseBase)
        {
            _totalStopwatch.Restart();
            _moduleStopwatch.Restart();

            RequestAnalysis = new RequestAnalysis
            {
                RequestId = Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "")
            };

            _numRequests++;

            try
            {
                _capture = new StreamCapture(httpResponseBase.Filter);
                httpResponseBase.Filter = _capture;
            }
            catch (Exception ex)
            {
                SharedUtils.Log("RequestId " + RequestAnalysis.RequestId + "Exception " + ex, _logger);
            }

            _startStrings = RequestAnalysisUtils.GetStringCount();

            _startMemory = GC.GetTotalMemory(true);

            _moduleStopwatch.Stop();
        }

        private void Application_EndRequest(object source, EventArgs e)
        {
            var wrapper = new HttpContextWrapper(((HttpApplication)source).Context);
            EndRequest(wrapper);
        }

        public void EndRequest(HttpContextBase contextBase)
        {
            _moduleStopwatch.Start();

            SizeCalculations(_capture.Length);

            try
            {
                string response = contextBase.Response.Filter.ToString();

                contextBase.Response.Clear();

                RequestAnalysis.AssemblyCount = AppDomain.CurrentDomain.GetAssemblies().Length;

                _endStrings = RequestAnalysisUtils.GetStringCount();
                RequestAnalysis.StringsCount = _endStrings - _startStrings;

                _endMemory = GC.GetTotalMemory(true);
                RequestAnalysis.MemoryUsage = _endMemory - _startMemory;

                _moduleStopwatch.Stop();
                _totalStopwatch.Stop();
                
                RequestAnalysis.TotalRequestTime = _totalStopwatch.Elapsed;
                RequestAnalysis.ModuleRequestTime = _moduleStopwatch.Elapsed;

                RequestAnalysisUtils.WriteToLog(RequestAnalysis, _requestsLogger);

                NameValueCollection appSettings = SharedUtils.GetConfigSection("appSettings");
                string featureToggle_Interface = appSettings["FeatureToggle_Interface"];
                bool.TryParse(featureToggle_Interface, out bool enabled);

                // Building the stats and writing the response will take some time but it can't be displayed to the user
                response = response.Replace("</body>", RequestAnalysisUtils.BuildStatsOutput(RequestAnalysis, enabled));

                contextBase.Response.Write(response);
            }
            catch (Exception ex)
            {
                SharedUtils.Log("RequestId " + RequestAnalysis.RequestId + "Exception " + ex, _logger);
            }
        }

        private void SizeCalculations(long size)
        {
            _totalSize += size;

            if (size < _minimumSize || _minimumSize == 0)
                _minimumSize = size;

            if (size > _maximumSize)
                _maximumSize = size;

            _averageSize = _totalSize / _numRequests;

            RequestAnalysis.MinimumSize = _minimumSize;
            RequestAnalysis.MaximumSize = _maximumSize;
            RequestAnalysis.AverageSize = _averageSize;
            RequestAnalysis.ResponseSize = size;
        }

        public void Dispose()
        {
        }
    }
}