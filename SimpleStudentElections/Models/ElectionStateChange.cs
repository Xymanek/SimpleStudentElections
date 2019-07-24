using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace SimpleStudentElections.Models
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class ElectionStateChange
    {
        [Key]
        public Guid Id { get; [UsedImplicitly] set; } = Guid.NewGuid();

        [ForeignKey("Election")]
        public int ElectionId { get; set; }

        public virtual Election Election { get; set; }

        public ElectionState? PreviousState { get; set; } // Null means that the election was just created
        public ElectionState TargetState { get; set; }

        public bool IsCausedByUser { get; set; } = false;
        public string InstigatorUsername { get; set; }

        public DateTime? CompletedAt { get; set; }

        [Obsolete] public string JobId { set; get; }
        public virtual ScheduledJobInfo JobInfo { get; set; }
    }
}