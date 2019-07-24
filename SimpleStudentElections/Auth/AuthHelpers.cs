using System.Security.Principal;
using Serilog.Core;

namespace SimpleStudentElections.Auth
{
    public static class AuthHelpers
    {
        public const string TimetableSessionClaim = "TIMETABLE_SESSION_CLAIM";
        public const string DebugModeClaim = "DEBUG_MODE_CLAIM";

        public const string LoginPath = "auth/login";
        public const string LogoutPath = "auth/logout";

        public static Logger Logger;

        public static bool IsAuthenticated(IPrincipal user)
        {
            // https://stackoverflow.com/a/6086652/2588539
            return user != null && user.Identity.IsAuthenticated;
        }
    }
}