using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleStudentElections.Models
{
    public class VotablePosition
    {
        [Key] public int Id { get; set; }

        [Index("IX_UniqueNamePerElection", 1, IsUnique = true)]
        [Required]
        public int ElectionId { get; set; }

        public virtual Election Election { get; set; }

        [Index("IX_UniqueNamePerElection", 2, IsUnique = true)]
        [Required, MaxLength(150)]
        public string HumanName { get; set; }

        public virtual ICollection<NominationEntry> NominationEntries { get; set; }
        public virtual ICollection<StudentVoteRecord> StudentVoteRecords { get; set; }
        public virtual ICollection<PositionEligibilityEntry> EligibilityEntries { get; set; }
    }
}