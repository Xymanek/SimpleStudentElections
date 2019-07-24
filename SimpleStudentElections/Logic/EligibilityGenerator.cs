using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SimpleStudentElections.Models;

// There is quite a lot of messy code here going on it order to keep using
// IQueryable for as long as possible. While currently the benefit is probably
// very marginal, it might be significant in future - since this code iterates
// over the entire list of users it might be split into batches later  

namespace SimpleStudentElections.Logic
{
    public static class EligibilityGenerator
    {
        public static IEnumerable<ElectionEligibilityEntry> GenerateElectionEntries(
            Election election,
            IQueryable<TimetableUserEntry> users
        )
        {
            return GetRelevantTimetableUsers(users)
                .AsEnumerable() // Load results in memory for next operation
                .Select(user => new ElectionEligibilityEntry {Election = election, Username = user.UserId});
        }

        private static IQueryable<TimetableUserEntry> GetRelevantTimetableUsers(IQueryable<TimetableUserEntry> users)
        {
            // Possible optimization: user.IsStudentActive should never return true for Student Support
            // so the first check can be omitted

            return users
                .WhereIsNotStudentSupport()
                .WhereIsStudentActive();
        }

        public static IEnumerable<PositionEligibilityEntry> GeneratePositionEntries(
            Election election,
            IQueryable<TimetableUserEntry> users,
            VotingDbContext db
        )
        {
            switch (election.Type)
            {
                case ElectionType.StudentCouncil:
                    return GeneratePositionEntriesForCouncil(election, GetRelevantTimetableUsers(users));

                case ElectionType.CourseRepresentative:
                    return GeneratePositionEntriesForRepresentative(election, GetRelevantTimetableUsers(users), db);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IEnumerable<PositionEligibilityEntry> GeneratePositionEntriesForCouncil(
            Election election,
            IEnumerable<TimetableUserEntry> users // Not queryable as here we operate on things in memory
        )
        {
            return users
                .SelectMany(user => election.Positions, Tuple.Create) // Cross join
                .Select(tuple => new PositionEligibilityEntry
                {
                    Username = tuple.Item1.UserId,
                    Position = tuple.Item2,
                    CanNominate = true,
                    CanVote = true
                });
        }

        private static IEnumerable<PositionEligibilityEntry> GeneratePositionEntriesForRepresentative(
            Election election,
            IEnumerable<TimetableUserEntry> users, // Not queryable as here we operate on things in memory
            VotingDbContext db
        )
        {
            RepresentativePositionData[] representativePositions = db.RepresentativePositionData
                .Where(data => data.PositionCommon.ElectionId == election.Id)
                .Include(data => data.PositionCommon)
                .ToArray();

            return users.Select(user =>
            {
                RepresentativePositionData positionData = representativePositions.First(
                    data =>
                        data.ProgrammeName == user.ProgrammeName &&
                        data.ExpectedGraduationYear == user.ExpectedGraduationYear
                );

                return new PositionEligibilityEntry
                {
                    Username = user.UserId,
                    Position = positionData.PositionCommon,
                    CanNominate = true,
                    CanVote = true
                };
            });
        }
    }
}