using System;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace SimpleStudentElections.Models
{
    public class ScheduledJobInfo
    {
        [Key]
        public Guid Id { get; [UsedImplicitly] set; } = Guid.NewGuid();

        public string JobId { set; get; }
        public DateTime RunAt { get; set; }
    }
}