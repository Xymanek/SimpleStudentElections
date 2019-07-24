using System.Collections.Generic;
using JetBrains.Annotations;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic
{
    public class NomineeFetcher
    {
        private readonly TimetableUserRepository _timetableUserRepository;

        public NomineeFetcher(TimetableUserRepository timetableUserRepository)
        {
            _timetableUserRepository = timetableUserRepository;
        }

        public IDictionary<VotablePosition, ISet<DisplayNomineeEntry>> Fetch(IEnumerable<VotablePosition> positions)
        {
            var result = new Dictionary<VotablePosition, ISet<DisplayNomineeEntry>>();

            foreach (VotablePosition position in positions)
            {
                HashSet<DisplayNomineeEntry> set = new HashSet<DisplayNomineeEntry>();
                result[position] = set;

                foreach (NominationEntry entry in position.NominationEntries)
                {
                    TimetableUserEntry user = _timetableUserRepository.GetByUsername(entry.Username);
                    set.Add(new DisplayNomineeEntry(entry, user != null, user?.Fullname));
                }
            }

            return result;
        }
    }

    public class DisplayNomineeEntry
    {
        [NotNull] public readonly NominationEntry ModelEntry;
        [CanBeNull] public readonly string FullName;

        public readonly bool IsFound;

        public DisplayNomineeEntry([NotNull] NominationEntry modelEntry, bool isFound, [CanBeNull] string fullName)
        {
            FullName = fullName;
            ModelEntry = modelEntry;
            IsFound = isFound;
        }
    }
}