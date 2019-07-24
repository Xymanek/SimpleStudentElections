using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using SimpleStudentElections.Auth;
using SimpleStudentElections.Logic;
using SimpleStudentElections.Models;

namespace SimpleStudentElections
{
    public partial class Startup
    {
        /// <summary>
        /// Configures the authentication system
        /// </summary>
        private static void ConfigureAuth(IAppBuilder app)
        {
            if (AppAuthConfiguration.Get().DebugMode)
            {
                // Fake login page with username only

                app.UseCookieAuthentication(new CookieAuthenticationOptions
                {
                    AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                    LoginPath = new PathString($"/{AuthHelpers.LoginPath}"),

                    Provider = new CookieAuthenticationProvider
                    {
                        OnValidateIdentity = context =>
                        {
                            if (context.Identity.Claims.All(claim => claim.Type != AuthHelpers.DebugModeClaim))
                            {
                                context.RejectIdentity();
                                context.OwinContext.Authentication.SignOut(context.Options.AuthenticationType);
                            }

                            return Task.CompletedTask;
                        }
                    }
                });
            }
            else
            {
                // Real SSO mode

                app.UseCookieAuthentication(new CookieAuthenticationOptions
                {
                    AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                    LoginPath = new PathString($"/{AuthHelpers.LoginPath}"),

                    ExpireTimeSpan = TimeSpan.FromDays(30),
                    SlidingExpiration = true,

                    Provider = new CookieAuthenticationProvider
                    {
                        OnValidateIdentity = async context =>
                        {
                            if (context.Identity.Claims.Any(claim => claim.Type == AuthHelpers.DebugModeClaim))
                            {
                                context.RejectIdentity();
                                context.OwinContext.Authentication.SignOut(context.Options.AuthenticationType);
                                return;
                            }

                            Claim sessionGuidClaim = context.Identity.Claims
                                .FirstOrDefault(claim => claim.Type == AuthHelpers.TimetableSessionClaim);

                            if (sessionGuidClaim == null)
                            {
                                context.RejectIdentity();
                                context.OwinContext.Authentication.SignOut(context.Options.AuthenticationType);
                                return;
                            }

                            TimetableDbContext timetableDb = new TimetableDbContext();
                            AuthSession session =
                                await timetableDb.AuthSessions.FindAsync(new Guid(sessionGuidClaim.Value));

                            if (session == null || session.ExpiresAt < DateTime.Now)
                            {
                                context.RejectIdentity();
                                context.OwinContext.Authentication.SignOut(context.Options.AuthenticationType);
                                return;
                            }

                            TimetableUserEntry user = await new TimetableUserRepository(timetableDb)
                                .GetByUsernameAsync(session.UserEmail);

                            if (user == null || user.UserId != context.Identity.GetUserId())
                            {
                                context.RejectIdentity();
                                context.OwinContext.Authentication.SignOut(context.Options.AuthenticationType);
                            }
                        }
                    }
                });
            }
        }
    }
}