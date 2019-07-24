using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleStudentElections.Models
{
    public class Vote
    {
        [Key] public int Id { get; set; }

        [Required] public NominationEntry NominationEntry { get; set; }
        [Required] public DateTime VotedAt { get; set; }
    }

    public class StudentVoteRecord
    {
        [Required, Key]
        [MaxLength(100)]
        [Column(Order = 1)]
        public string Username { get; set; }

        [Required, Key]
        [ForeignKey("Position")]
        [Column(Order = 2)]
        public int PositionId { get; set; }

        public VotablePosition Position { get; set; }
    }
}