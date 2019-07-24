using System.ComponentModel.DataAnnotations;

namespace SimpleStudentElections.Models
{
    public class EmailDefinition
    {
        [Key]
        public int Id { get; set; }
        
        public string Subject { get; set; }
        public string Body { get; set; }

        public bool IsEnabled => !string.IsNullOrWhiteSpace(Subject) && !string.IsNullOrWhiteSpace(Body);
    }
}