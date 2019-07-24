using System.ComponentModel.DataAnnotations;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Controllers.EligibilityDebuggerHelpers
{
    public class PositionsTableRow : ITableRow
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; }

        public string UserId { get; set; }
        public string UserFullName { get; set; }

        public bool CanNominate { get; set; }
        public bool CanVote { get; set; }

        public PositionsTableRow()
        {
        }

        public PositionsTableRow(PositionEligibilityEntry dbEntry)
        {
            PositionId = dbEntry.Position.Id;
            PositionName = dbEntry.Position.HumanName;

            UserId = dbEntry.Username;

            CanNominate = dbEntry.CanNominate;
            CanVote = dbEntry.CanVote;
        }
    }

    public class NewPositionEntry
    {
        [Required] public int ElectionId { get; set; }

        [Required] public int PositionId { get; set; }
        [Required] public string UserId { get; set; }
        
        public bool CanNominate { get; set; }
        public bool CanVote { get; set; }
    }
    
    public class PositionEntryEdit
    {
        // PKs
        
        [Required] public string UserId { get; set; }
        [Required] public int PositionId { get; set; }
        
        // New values
        
        public bool CanNominate { get; set; }
        public bool CanVote { get; set; }
    }
    
    public class PositionRemovalEntry
    {
        [Required] public string Username { get; set; }
        [Required] public int PositionId { get; set; }
    }
}