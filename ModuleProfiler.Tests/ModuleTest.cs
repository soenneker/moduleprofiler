using System;
using System.IO;
using System.Text;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModuleProfiler.Module;
using ModuleProfiler.Module.Utils;
using Moq;

namespace ModuleProfiler.Tests
{
    [TestClass]
    public class ModuleTest
    {
        [TestMethod]
        public void ProcessStringCounter()
        {
            int count = RequestAnalysisUtils.GetStringCount();

            Assert.IsTrue(count != 0);
        }

        [TestMethod]
        public void FrontToBackModuleTest()
        {
            var context = new Mock<HttpContextBase>();
            var request = new Mock<HttpRequestBase>();
            var response = new Mock<HttpResponseBase>();

            request.Setup(r => r.UrlReferrer).Returns(new Uri("https://encrypted.google.com"));
            response.Setup(r => r.Cookies).Returns(new HttpCookieCollection());
            context.Setup(c => c.Request).Returns(request.Object);
            context.Setup(c => c.Response).Returns(response.Object);

            using (var testStream = new MemoryStream(Encoding.UTF8.GetBytes("<html><body></body></html>")))
            {
                context.Setup(c => c.Response.Filter).Returns(testStream);
            }

            var module = new RequestCaptureModule();

            module.Init(new HttpApplication());

            module.BeginRequest(response.Object);

            module.EndRequest(context.Object);

            Assert.IsNotNull(module.RequestAnalysis);
            Assert.IsTrue(module.RequestAnalysis.AssemblyCount != 0);
            Assert.IsTrue(module.RequestAnalysis.MemoryUsage != 0);
            Assert.IsTrue(module.RequestAnalysis.TotalRequestTime != TimeSpan.Zero);
        }
    }
}