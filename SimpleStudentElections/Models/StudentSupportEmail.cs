using System;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace SimpleStudentElections.Models
{
    /// <summary>
    /// A holder for a system-generated email that's supposed to be sent to Student Support members 
    /// </summary>
    public class StudentSupportEmail
    {
        [Key]
        public Guid Id { get; [UsedImplicitly] set; } = Guid.NewGuid();

        [Required] public string Subject { get; set; }
        [Required] public string Body { get; set; }
    }
}