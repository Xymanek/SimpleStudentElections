using System.Linq;
using Hangfire;
using JetBrains.Annotations;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.Emails
{
    /// <summary>
    /// This class is responsible for sending emails to students notifying them that the election phase is almost over
    /// </summary>
    /// <remarks>
    /// Note that in the recipient list generation here we ignore the fact that technically a person might be eligible
    /// for the position, but not the actual election. This logically should never happen and if it does then the student
    /// will not be able to access the nomination and voting pages due to checks in StudentController anyway
    /// </remarks>
    public class AlmostOverNotificationJobs : ElectionStudentEmailJobBase
    {
        [UsedImplicitly]
        public AlmostOverNotificationJobs() : base(new VotingDbContext())
        {
        }

        public void SendForNominations(int electionId, IJobCancellationToken cancellationToken)
        {
            SendCouncilEmail(
                electionId, cancellationToken,
                councilData => councilData.NominationsAlmostOverEmail,
                election =>
                {
                    // TODO: Should the person be emailed if he already nominated for at least one position but there are still other positions available?

                    IQueryable<string> eligibleForNomination = Db.PositionEligibilityEntries
                        .Where(entry => entry.Position.ElectionId == election.Id && entry.CanNominate)
                        .Select(entry => entry.Username)
                        .Distinct();

                    IQueryable<string> alreadyNominated = Db.NominationEntries
                        .Where(entry => entry.Position.ElectionId == election.Id)
                        .Select(entry => entry.Username)
                        .Distinct();

                    return eligibleForNomination.Except(alreadyNominated);
                }
            );
        }

        public void SendForVoting(int electionId, IJobCancellationToken cancellationToken)
        {
            SendCouncilEmail(
                electionId, cancellationToken,
                councilData => councilData.VotingAlmostOverEmail,
                election => Db.PositionEligibilityEntries
                    .Where(
                        entry =>
                            entry.CanVote &&
                            entry.Position.ElectionId == election.Id &&

                            // Check that every recorded vote is not done by student in this eligibility entry
                            // In other words, there are no votes by user in this entry
                            entry.Position.StudentVoteRecords.All(record => record.Username != entry.Username)
                    )
                    .Select(entry => entry.Username)
                    .Distinct()
            );
        }
    }
}