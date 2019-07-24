using System.Collections.Generic;
using System.Diagnostics;

namespace SimpleStudentElections.Helpers
{
    /// <summary>
    /// Provides a default list of roles for new council election
    /// </summary>
    public static class DefaultCouncilRoles
    {
        private static readonly string[] DefaultRoles =
        {
            "President",
            "Secretary",
            "Treasury",
            "Activities rep"
        };

        public static IEnumerable<string> GenerateRoles(int numRoles)
        {
            Debug.Assert(numRoles > 0, nameof(numRoles) + " > 0");
            List<string> roles = new List<string>(DefaultRoles);

            if (roles.Count > numRoles)
            {
                int excess = roles.Count - numRoles;
                roles.RemoveRange(roles.Count - excess - 1, excess);
            }
            else
            {
                while (roles.Count < numRoles)
                {
                    roles.Add("Role " + (roles.Count + 1));
                }
            }

            return roles;
        }
    }
}