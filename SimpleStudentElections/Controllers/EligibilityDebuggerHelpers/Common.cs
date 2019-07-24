using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SimpleStudentElections.Controllers.EligibilityDebuggerHelpers
{
    public interface ITableRow
    {
        string UserId { get; set; }
        string UserFullName { get; set; }
    }

    public class RemovalModel<TEntry>
    {
        [Required] public IList<TEntry> Entries { get; set; }
    }
}