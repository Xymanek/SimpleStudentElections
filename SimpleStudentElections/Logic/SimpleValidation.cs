using System.Collections.Generic;

namespace SimpleStudentElections.Logic
{
    public class SimpleValidationResult
    {
        public readonly List<Violation> Violations = new List<Violation>();

        public bool IsNoErrors()
        {
            return Violations.Count == 0;
        }

        public static implicit operator bool(SimpleValidationResult result)
        {
            return result.IsNoErrors();
        }

        public abstract class Violation
        {
            public abstract string HumanError { get; }
        }
    }
}