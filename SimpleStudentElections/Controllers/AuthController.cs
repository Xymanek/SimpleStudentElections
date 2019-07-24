using System;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using SimpleStudentElections.Auth;
using SimpleStudentElections.Logic;
using SimpleStudentElections.Models;
using SimpleStudentElections.Models.Forms;

namespace SimpleStudentElections.Controllers
{
    /// <summary>
    /// This controller holds all authentication-related actions
    /// </summary>
    /// <remarks>
    /// The methods in this class are mostly split in 2 groups: 
    /// <list type="bullet">
    /// <item>Debug authorization mode (login page with username only)</item>
    /// <item>SSO authorization mode (redirect to the timetable system)</item>
    /// </list>
    /// The 2 are differentiated on the router level
    /// </remarks>
    public class AuthController : Controller
    {
        private const string FailedSsoAttemptsKey = "FailedSsoAttempts";
        private const string ReturnUrlKey = "SsoReturnUrl";

        public ActionResult SiteHomepage()
        {
            return RedirectToRoute("AuthLogin");
        }

        #region DebugMode

        public ActionResult Login()
        {
            if (AuthHelpers.IsAuthenticated(User)) return RedirectAfterLogin(true);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginForm form)
        {
            if (AuthHelpers.IsAuthenticated(User)) return RedirectAfterLogin(true);

            if (!ModelState.IsValid)
            {
                // Something went wrong during binding probably
                return View();
            }

            string username = TimetableUserEntry.NormalizeUsernameToId(form.Username);
            TimetableUserEntry user = new TimetableUserRepository().GetByUsername(username);

            if (user == null)
            {
                ModelState.AddModelError("Username", "This username doesn't exist");
                return View();
            }

            // https://stackoverflow.com/a/31585768/2588539
            ClaimsIdentity identity = new ClaimsIdentity(
                new[]
                {
                    // These 2 are required for default antiforgery provider
                    new Claim(ClaimTypes.NameIdentifier, username),
                    new Claim(
                        "http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider",
                        "ASP.NET Identity",
                        "http://www.w3.org/2001/XMLSchema#string"
                    ),

                    // Additional stuff
                    new Claim(ClaimTypes.Role, user.IsStudentSupport ? Roles.StudentSupport : Roles.Student),
                    new Claim(ClaimTypes.Name, user.Fullname),
                    new Claim(AuthHelpers.DebugModeClaim, "1")
                },
                DefaultAuthenticationTypes.ApplicationCookie
            );

            HttpContext.GetOwinContext().Authentication.SignIn(
                new AuthenticationProperties {IsPersistent = false},
                identity
            );

            return RedirectAfterLogin();
        }

        public ActionResult Logout()
        {
            HttpContext
                .GetOwinContext()
                .Authentication
                .SignOut(DefaultAuthenticationTypes.ApplicationCookie);

            return RedirectToAction("Login");
        }

        #endregion

        #region SSO Mode

        public ActionResult LoginSso(string timetableToken, string returnUrl)
        {
            if (AuthHelpers.IsAuthenticated(User)) return RedirectAfterLogin(true);

            if (timetableToken == null)
            {
                // Store the return URL in session to use it when the user comes back
                if (!string.IsNullOrWhiteSpace(returnUrl))
                {
                    Session[ReturnUrlKey] = returnUrl;
                }

                return RedirectToSso();
            }

            if (!Guid.TryParse(timetableToken, out Guid tokenGuid))
            {
                AuthHelpers.Logger.Information(
                    "SSO Fail: failed to parse token GUID '{token}' from {UserHostAddress} ",
                    timetableToken, Request.UserHostAddress
                );

                return FailCallback();
            }

            TimetableDbContext timetableDb = new TimetableDbContext();
            AuthToken token = timetableDb.AuthTokens.Find(tokenGuid);

            if (token == null || token.UserHostAddress != Request.UserHostAddress)
            {
                AuthHelpers.Logger.Information(
                    "SSO Fail: Token {Guid} not found or UserHostAddress ({UserHostAddress}) doesn't match",
                    tokenGuid, Request.UserHostAddress
                );

                return FailCallback();
            }

            AuthSession session = timetableDb.AuthSessions.Find(token.SessionGuid);
            if (session == null || session.ExpiresAt < DateTime.Now)
            {
                AuthHelpers.Logger.Information(
                    "SSO Fail: Session for token {Guid} not found or it has expired. UserHostAddress: {UserHostAddress}",
                    tokenGuid, Request.UserHostAddress
                );

                return FailCallback();
            }

            TimetableUserEntry user = new TimetableUserRepository(timetableDb)
                .GetByUsername(session.UserEmail);

            if (user == null || user.UserId != TimetableUserEntry.NormalizeUsernameToId(session.UserEmail))
            {
                AuthHelpers.Logger.Information(
                    "SSO Fail: Session for token {Guid} failed to match a timetable user. UserHostAddress: {UserHostAddress}",
                    tokenGuid, Request.UserHostAddress
                );

                return FailCallback();
            }

            // All good, sign in

            timetableDb.AuthTokens.Remove(token);
            timetableDb.SaveChanges();

            ClaimsIdentity identity = new ClaimsIdentity(
                new[]
                {
                    // These 2 are required for default antiforgery provider
                    new Claim(ClaimTypes.NameIdentifier, user.UserId),
                    new Claim(
                        "http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider",
                        "ASP.NET Identity",
                        "http://www.w3.org/2001/XMLSchema#string"
                    ),

                    // Additional stuff
                    new Claim(ClaimTypes.Role, user.IsStudentSupport ? Roles.StudentSupport : Roles.Student),
                    new Claim(ClaimTypes.Name, user.Fullname),
                    new Claim(AuthHelpers.TimetableSessionClaim, session.Guid.ToString())
                },
                DefaultAuthenticationTypes.ApplicationCookie
            );

            Session[FailedSsoAttemptsKey] = 0;
            HttpContext.GetOwinContext().Authentication.SignIn(
                new AuthenticationProperties
                {
                    IsPersistent = true // We validate on every request anyway, so prevent needless redirects
                },
                identity
            );

            AuthHelpers.Logger.Information(
                "SSO Success: token {Guid} was used for successful sign in by {UserId}. UserHostAddress: {UserHostAddress}",
                tokenGuid, user.UserId, Request.UserHostAddress
            );

            return RedirectAfterLogin();
        }

        private ActionResult FailCallback()
        {
            if (!(Session[FailedSsoAttemptsKey] is int))
            {
                Session[FailedSsoAttemptsKey] = 0;
            }

            Session[FailedSsoAttemptsKey] = ((int) Session[FailedSsoAttemptsKey]) + 1;
            if ((int) Session[FailedSsoAttemptsKey] > AppAuthConfiguration.Get().MaxSsoAttempts)
            {
                Session[FailedSsoAttemptsKey] = 0;
                return RedirectToAction("SsoFailed");
            }

            return RedirectToSso();
        }

        private ActionResult RedirectToSso()
        {
            return Redirect(AppAuthConfiguration.Get().GetTimetableLoginUrlOrFail());
        }

        public ActionResult SsoFailed()
        {
            return View();
        }

        #endregion

        private ActionResult RedirectAfterLogin(bool ignoreReturnUrl = false)
        {
            if (!ignoreReturnUrl)
            {
                string returnUrl = Request.QueryString.Get("ReturnUrl");

                if (string.IsNullOrWhiteSpace(returnUrl))
                {
                    returnUrl = (string) Session[ReturnUrlKey];
                    Session.Remove(ReturnUrlKey);
                }

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
            }

            return User.IsInRole(Roles.StudentSupport)
                ? RedirectToAction("Current", "AdminElections")
                : RedirectToAction("Index", "Student");
        }
    }
}