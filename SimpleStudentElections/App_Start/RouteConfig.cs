using System.Web.Mvc;
using System.Web.Routing;
using SimpleStudentElections.Auth;

namespace SimpleStudentElections
{
    /// <summary>
    /// Routes mapping for application
    /// </summary>
    /// <remarks>
    /// I decided not to use automatic/default route mapping since (a) I wanted more granular control over how URLs look
    /// and (b) some URLs require more arguments than Controller/Action/id
    /// </remarks>
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "SiteHomepage", "",
                new {controller = "Auth", action = "SiteHomepage"}
            );

            RegisterAuthRoutes(routes);
            RegisterStudentRoutes(routes);
            RegisterAdminRoutes(routes);
            RegisterEligibilityDebuggerRoutes(routes);
        }

        private static void RegisterAuthRoutes(RouteCollection routes)
        {
            if (AppAuthConfiguration.Get().DebugMode)
            {
                routes.MapRoute(
                    "AuthLogin", AuthHelpers.LoginPath,
                    new { controller = "Auth", action = "Login" }
                );

                routes.MapRoute(
                    "AuthLogout", AuthHelpers.LogoutPath,
                    new { controller = "Auth", action = "Logout" }
                );
            }
            else
            {
                routes.MapRoute(
                    "AuthLogin", AuthHelpers.LoginPath,
                    new { controller = "Auth", action = "LoginSso" }
                );

                routes.MapRoute(
                    "AuthSsoFailed", "auth/sso-failed",
                    new { controller = "Auth", action = "SsoFailed" }
                );
            }
        }

        private static void RegisterStudentRoutes(RouteCollection routes)
        {
            routes.MapRoute(
                "StudentHome", "student",
                new {controller = "Student", action = "Index"}
            );

            routes.MapRoute(
                "StudentNominations", "student/elections/{id}/nominations",
                new {controller = "Student", action = "Nominations"}
            );

            routes.MapRoute(
                "StudentNominationsUpdateStatus", "student/update-nomination/{positionId}",
                new {controller = "Student", action = "UpdateNominationsStatus"}
            );

            routes.MapRoute(
                "StudentVoting", "student/elections/{id}/vote",
                new {controller = "Student", action = "Vote"}
            );

            routes.MapRoute(
                "StudentVoteConfirmation", "student/vote-confirmation",
                new {controller = "Student", action = "VoteConfirmation"}
            );

            routes.MapRoute(
                "StudentDoVote", "student/do-vote",
                new {controller = "Student", action = "DoVote"}
            );
        }

        private static void RegisterAdminRoutes(RouteCollection routes)
        {
            routes.MapRoute(
                "AdminElectionsCurrent", "admin/elections",
                new {controller = "AdminElections", action = "Current"}
            );

            routes.MapRoute(
                "AdminElectionsArchived", "admin/elections/archived",
                new {controller = "AdminElections", action = "Archived"}
            );

            routes.MapRoute(
                "AdminElectionsNewSelectType", "admin/elections/new",
                new {controller = "AdminElections", action = "SelectNewType"}
            );

            routes.MapRoute(
                "AdminElectionsNewCouncil", "admin/elections/new/council",
                new {controller = "AdminElections", action = "NewCouncil"}
            );

            routes.MapRoute(
                "AdminElectionsNewCourseRep", "admin/elections/new/course-rep",
                new {controller = "AdminElections", action = "NewCourseRep"}
            );

            routes.MapRoute(
                "AdminElectionsViewVotes", "admin/elections/view-votes",
                new {controller = "AdminElections", action = "ViewVotes"}
            );

            routes.MapRoute(
                "AdminElectionsVotesDetails", "admin/elections/votes-details",
                new {controller = "AdminElections", action = "VotesDetails"}
            );

            routes.MapRoute(
                "AdminElectionsDetails", "admin/elections/{id}",
                new {controller = "AdminElections", action = "Details"}
            );

            routes.MapRoute(
                "AdminElectionsEdit", "admin/elections/{id}/edit",
                new {controller = "AdminElections", action = "Edit"}
            );

            routes.MapRoute(
                "AdminElectionsRegeneratePositions", "admin/elections/{id}/regenerate-positions",
                new {controller = "AdminElections", action = "RegeneratePositions"}
            );

            routes.MapRoute(
                "AdminElectionsActivate", "admin/elections/{id}/activate",
                new {controller = "AdminElections", action = "Activate"}
            );

            routes.MapRoute(
                "AdminElectionsDeativate", "admin/elections/{id}/deactivate",
                new {controller = "AdminElections", action = "Deactivate"}
            );

            routes.MapRoute(
                "AdminElectionsPublishResults", "admin/elections/{id}/publish-results",
                new {controller = "AdminElections", action = "PublishResults"}
            );

            routes.MapRoute(
                "AdminElectionsEventLog", "admin/elections/{id}/events",
                new {controller = "AdminElections", action = "EventLog"}
            );

            routes.MapRoute(
                "AdminElectionsAbort", "admin/elections/{id}/abort",
                new {controller = "AdminElections", action = "Abort"}
            );
            routes.MapRoute(
                "AdminElectionsDelete", "admin/elections/{id}/delete",
                new {controller = "AdminElections", action = "Delete" }
            );

            routes.MapRoute(
                "AdminElectionsHelp", "admin/help",
                new {controller = "AdminElections", action = "Help"}
            );
        }

        private static void RegisterEligibilityDebuggerRoutes(RouteCollection routes)
        {
            // Election

            routes.MapRoute(
                "EligibilityDebuggerElection", "admin/elections/{id}/eligibility/election",
                new {controller = "EligibilityDebugger", action = "Election"}
            );

            routes.MapRoute(
                "EligibilityDebuggerElectionData", "admin/elections/{id}/eligibility/election-data",
                new {controller = "EligibilityDebugger", action = "ElectionData"}
            );

            routes.MapRoute(
                "EligibilityDebuggerNewElectionEntry", "admin/eligibility/new-election-entry",
                new {controller = "EligibilityDebugger", action = "NewElectionEntry"}
            );

            routes.MapRoute(
                "EligibilityDebuggerRemoveElectionEntries", "admin/eligibility/remove-election-entries/{electionId}",
                new {controller = "EligibilityDebugger", action = "RemoveElectionEntries"}
            );

            // Positions

            routes.MapRoute(
                "EligibilityDebuggerPositions", "admin/elections/{electionId}/eligibility/positions",
                new {controller = "EligibilityDebugger", action = "Positions"}
            );

            routes.MapRoute(
                "EligibilityDebuggerPositionsData", "admin/elections/{electionId}/eligibility/positions-data",
                new {controller = "EligibilityDebugger", action = "PositionsData"}
            );

            routes.MapRoute(
                "EligibilityDebuggerRemovePositionEntries", "admin/eligibility/remove-position-entries",
                new {controller = "EligibilityDebugger", action = "RemovePositionEntries"}
            );

            routes.MapRoute(
                "EligibilityDebuggerEditPositionEntry", "admin/eligibility/edit-position-entry",
                new {controller = "EligibilityDebugger", action = "EditPositionEntry"}
            );

            routes.MapRoute(
                "EligibilityDebuggerNewPositionEntry", "admin/eligibility/new-position-entry",
                new {controller = "EligibilityDebugger", action = "NewPositionEntry"}
            );
        }
    }
}