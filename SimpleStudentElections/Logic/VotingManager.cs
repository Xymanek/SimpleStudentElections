using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic
{
    public class VotingManager
    {
        /// <summary>
        /// Gathers information about voting for a specific student:
        /// <ul>
        ///     <li>What positions he already voted for</li>
        ///     <li>What positions he cannot vote for</li>
        ///     <li>What positions he can vote for and the list of candidates</li>
        /// </ul>
        /// </summary>
        /// <param name="username"></param>
        /// <param name="election"></param>
        /// <param name="positionsFilter">
        ///     Pass not-null if you are interested only in a subset of positions
        /// </param>
        /// <returns></returns>
        public IDictionary<VotablePosition, PositionDisplayDataForVoting> PrepareVotingDataFor(
            string username,
            Election election,
            Func<IEnumerable<VotablePosition>, IEnumerable<VotablePosition>> positionsFilter = null
        )
        {
            IEnumerable<VotablePosition> query = election.Positions
                .Where(position => position.EligibilityEntries.Any(entry => entry.Username == username));

            if (positionsFilter != null)
            {
                query = positionsFilter(query);
            }

            List<VotablePosition> positions = query.ToList();

            IDictionary<VotablePosition, PositionDisplayDataForVoting> result =
                new Dictionary<VotablePosition, PositionDisplayDataForVoting>(positions.Count);

            // Find positions that this student has already voted for
            List<VotablePosition> votedPositions = positions
                .Where(position => position.StudentVoteRecords.Any(record => record.Username == username))
                .ToList();

            foreach (VotablePosition position in votedPositions)
            {
                result[position] = new PositionDisplayDataForVoting(
                    PositionDisplayDataForVoting.PositionStatus.HasVoted, null
                );
            }

            // The ones that the student hasn't voted for
            List<VotablePosition> nonVotedPositions = positions
                .Where(position => !votedPositions.Contains(position))
                .ToList();

            // The ones that the student can vote for
            List<VotablePosition> votablePositions = nonVotedPositions
                .Where(position => position.EligibilityEntries.Any(
                    entry => entry.Username == username && entry.CanVote)
                )
                .ToList();

            // Get nominees for those
            IDictionary<VotablePosition, ISet<DisplayNomineeEntry>> nomineesForVotablePositions
                = new NomineeFetcher(new TimetableUserRepository()).Fetch(votablePositions);

            foreach (VotablePosition position in votablePositions)
            {
                result[position] = new PositionDisplayDataForVoting(
                    PositionDisplayDataForVoting.PositionStatus.CanVote,
                    nomineesForVotablePositions[position]
                );
            }

            // The ones that the student *cannot* vote for
            List<VotablePosition> nonVotablePositions = nonVotedPositions
                .Where(position => !votablePositions.Contains(position))
                .ToList();

            foreach (VotablePosition position in nonVotablePositions)
            {
                result[position] = new PositionDisplayDataForVoting(
                    PositionDisplayDataForVoting.PositionStatus.CannotVote,
                    null
                );
            }

            // We are done
            return result;
        }
    }

    public class PositionDisplayDataForVoting
    {
        public readonly PositionStatus Status;
        [CanBeNull] public readonly ISet<DisplayNomineeEntry> NomineeEntries;

        public PositionDisplayDataForVoting(PositionStatus status, [CanBeNull] ISet<DisplayNomineeEntry> nomineeEntries)
        {
            Status = status;
            NomineeEntries = nomineeEntries;
        }

        public enum PositionStatus
        {
            HasVoted,
            CanVote,
            CannotVote
        }
    }
}