using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using StackExchange.Profiling;
using StackExchange.Profiling.EntityFramework6;
using StackExchange.Profiling.Mvc;
using StackExchange.Profiling.Storage;

namespace SimpleStudentElections
{
    public class Global : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            AutoMapperConfig.InitializeAutoMapper();
            LoggingConfig.InitializeAuditLogger();

            InitProfilerSettings();
        }

        protected void Application_BeginRequest()
        {
            if (Request.IsLocal)
            {
                MiniProfiler.StartNew();
            }
        }

        protected void Application_EndRequest()
        {
            MiniProfiler.Current?.Stop();
        }

        private static void InitProfilerSettings()
        {
            MiniProfilerOptions options = new MiniProfilerOptions()
                {
                    Storage = new MemoryCacheStorage(new TimeSpan(1, 0, 0)),

                    PopupRenderPosition = RenderPosition.Right, // defaults to left
                    PopupMaxTracesToShow = 10, // defaults to 15

                    // ResultsAuthorize (optional - open to all by default):
                    // because profiler results can contain sensitive data (e.g. sql queries with parameter values displayed), we
                    // can define a function that will authorize clients to see the JSON or full page results.
                    // we use it on http://stackoverflow.com to check that the request cookies belong to a valid developer.
                    ResultsAuthorize = request => request.IsLocal,

                    // the list of all sessions in the store is restricted by default, you must return true to allow it
                    ResultsListAuthorize = request => request.IsLocal,

                    // Stack trace settings
                    StackMaxLength = 256, // default is 120 characters,
                }
                .AddViewProfiling(); // Add MVC view profiling

            options.IgnoredPaths.Add("/hangfire");

            MiniProfiler.Configure(options);
            MiniProfilerEF6.Initialize();
        }
    }
}