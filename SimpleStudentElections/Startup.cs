using JetBrains.Annotations;
using Microsoft.Owin;
using Owin;
using SimpleStudentElections;

[assembly: OwinStartup(typeof(Startup))]
namespace SimpleStudentElections
{
    public partial class Startup
    {
        [UsedImplicitly]
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            ConfigureHangfire(app);
        }
    }
}