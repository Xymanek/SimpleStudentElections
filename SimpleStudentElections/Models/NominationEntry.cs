using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleStudentElections.Models
{
    public class NominationEntry
    {
        [Key] public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Index("IX_UniqueNamePerPosition", 1, IsUnique = true)]
        public string Username { get; set; }

        [Required]
        [Index("IX_UniqueNamePerPosition", 2, IsUnique = true)]
        [ForeignKey("Position")]
        public int PositionId { get; set; }

        public virtual VotablePosition Position { get; set; }

        public virtual ICollection<Vote> Votes { get; set; }
    }
}