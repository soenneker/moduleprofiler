using System;
using System.Diagnostics;
using System.Web;
using ModuleProfiler.Module.Models;
using NLog;

namespace ModuleProfiler.Module
{
    public class RequestCaptureModule : IHttpModule
    {
        public RequestAnalysis RequestAnalysis { get; private set; }

        private Stopwatch _moduleSW;
        private Stopwatch _requestSW;

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

        private static Logger _logger;

        public RequestCaptureModule() { }

        public string ModuleName => "RequestCaptureModule";

        public void Init(HttpApplication application)
        {
            LogManager.ThrowExceptions = true;

            _logger = LogManager.GetLogger("module");

            _moduleSW = new Stopwatch();
            _requestSW = new Stopwatch();

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
            _requestSW.Restart();
            _moduleSW.Restart();

            RequestAnalysis = new RequestAnalysis();

            _numRequests++;

            try
            {
                _capture = new StreamCapture(httpResponseBase.Filter);
                httpResponseBase.Filter = _capture;
            }
            catch (Exception ex)
            {
                Utils.Log("RequestId " + RequestAnalysis.RequestId + "Exception " + ex, _logger);
            }

            _startStrings = Utils.GetStringCount();

            _startMemory = GC.GetTotalMemory(true);

            _moduleSW.Stop();
        }

        private void Application_EndRequest(object source, EventArgs e)
        {
            var wrapper = new HttpContextWrapper(((HttpApplication)source).Context);
            EndRequest(wrapper);
        }

        public void EndRequest(HttpContextBase contextBase)
        {
            _moduleSW.Start();

            long size = _capture.Length;

            _totalSize += size;

            if (size < _minimumSize || _minimumSize == 0)
                _minimumSize = size;

            if (size > _maximumSize)
                _maximumSize = size;

            _averageSize = _totalSize / _numRequests;

            RequestAnalysis.MinimumSize = _minimumSize;
            RequestAnalysis.MaximumSize = _maximumSize;
            RequestAnalysis.AverageSize = _averageSize;

            try
            {
                string response = contextBase.Response.Filter.ToString();

                contextBase.Response.Clear();

                RequestAnalysis.AssemblyCount = AppDomain.CurrentDomain.GetAssemblies().Length;

                _endStrings = Utils.GetStringCount();

                RequestAnalysis.StringsCount = _endStrings - _startStrings;

                _endMemory = GC.GetTotalMemory(true);

                RequestAnalysis.MemoryUsage = _endMemory - _startMemory;
                RequestAnalysis.ResponseSize = size;

                _moduleSW.Stop();
                _requestSW.Stop();

                RequestAnalysis.ModuleRequestTime = _moduleSW.Elapsed;
                RequestAnalysis.TotalRequestTime = _requestSW.Elapsed;

                RequestAnalysis.WriteToLog();

                // Building the stats and writing the response will take some time but it can't be displayed to the user
                response = response.Replace("</body>", RequestAnalysis.BuildStatsOutput());

                contextBase.Response.Write(response);
            }
            catch (Exception ex)
            {
                Utils.Log("RequestId " + RequestAnalysis.RequestId + "Exception " + ex, _logger);
            }
        }

        public void Dispose()
        {
        }
    }
}