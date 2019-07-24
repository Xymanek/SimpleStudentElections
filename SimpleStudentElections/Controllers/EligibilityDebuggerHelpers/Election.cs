using System.ComponentModel.DataAnnotations;

namespace SimpleStudentElections.Controllers.EligibilityDebuggerHelpers
{
    public class ElectionTableRow : ITableRow
    {
        public string UserId { get; set; }
        public string UserFullName { get; set; }
    }
    
    public class NewElectionEntry
    {
        [Required] public int ElectionId { get; set; }
        [Required] public string UserId { get; set; }
    }
    
    public class ElectionRemovalEntry
    {
        [Required] public string Username { get; set; }
    }
}