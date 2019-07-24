using System;
using System.Collections.Generic;
using System.Web;
using Hangfire;
using Owin;
using SimpleStudentElections.Logic;

namespace SimpleStudentElections
{
    /// <summary>
    /// Configuration of HangFire library (background job scheduler)
    /// </summary>
    public partial class Startup
    {
        public const string HangfireConnectionName = "BackgroundJobStorage";

        public static IEnumerable<IDisposable> GetHangfireConfiguration()
        {
            GlobalConfiguration.Configuration.UseSqlServerStorage(HangfireConnectionName);

            yield return new BackgroundJobServer(new BackgroundJobServerOptions
            {
                Queues = new[]
                {
                    BackgroundJobConstants.CriticalQueueName,
                    BackgroundJobConstants.DefaultQueueName
                }
            });
        }


        private static void ConfigureHangfire(IAppBuilder app)
        {
            app.UseHangfireAspNet(GetHangfireConfiguration);

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                AppPath = VirtualPathUtility.ToAbsolute("~")
            });
        }
    }
}