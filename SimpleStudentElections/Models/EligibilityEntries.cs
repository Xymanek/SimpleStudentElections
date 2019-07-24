using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleStudentElections.Models
{
    public class ElectionEligibilityEntry
    {
        [Required]
        [Key, Column(Order = 1)]
        public string Username { get; set; }

        [Required]
        [Key, Column(Order = 2)]
        [ForeignKey("Election")]
        public int ElectionId { get; set; }

        public Election Election { get; set; }
    }

    public class PositionEligibilityEntry
    {
        [Required]
        [Key, Column(Order = 1)]
        public string Username { get; set; }

        [Required]
        [Key, Column(Order = 2)]
        [ForeignKey("Position")]
        public int PositionId { get; set; }

        public virtual VotablePosition Position { get; set; }

        public bool CanNominate { get; set; } = true;
        public bool CanVote { get; set; } = true;
    }
}