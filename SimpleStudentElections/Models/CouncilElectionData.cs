using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleStudentElections.Models
{
    public class CouncilElectionData
    {
        [Key, ForeignKey("Election")]
        public int ElectionId { get; set; }

        public Election Election { get; set; }

        // Cannot specify [Required] on these as SQL Server complains about "multiple cascade paths"
        // Will need to fix this someday (when election deletion is implemented) 
        
        public virtual EmailDefinition NominationsAlmostOverEmail { get; set; }
        public virtual EmailDefinition VotingStartedEmail { get; set; }
        public virtual EmailDefinition VotingAlmostOverEmail { get; set; }
    }
}