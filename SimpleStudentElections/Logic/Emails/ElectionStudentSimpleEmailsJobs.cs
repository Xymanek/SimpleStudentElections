using System.Collections.Generic;
using System.Linq;
using Hangfire;
using JetBrains.Annotations;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.Emails
{
    /// <summary>
    /// This class is responsible for emails that are sent to all students who are eligible for the election
    /// </summary>
    public class ElectionStudentSimpleEmailsJobs : ElectionStudentEmailJobBase
    {
        [UsedImplicitly]
        public ElectionStudentSimpleEmailsJobs() : base(new VotingDbContext())
        {
        }

        public void SendNominationsStart(int electionId, IJobCancellationToken cancellationToken)
        {
            SendCommonEmail(electionId, cancellationToken, election => election.NominationsStartedEmail);
        }

        public void SendNominationsEnd(int electionId, IJobCancellationToken cancellationToken)
        {
            SendCommonEmail(electionId, cancellationToken, election => election.PostNominationsEmail);
        }

        public void SendVotingStart(int electionId, IJobCancellationToken cancellationToken)
        {
            SendCouncilEmail(electionId, cancellationToken, councilData => councilData.VotingStartedEmail);
        }

        public void SendVotingEnd(int electionId, IJobCancellationToken cancellationToken)
        {
            SendCommonEmail(electionId, cancellationToken, election => election.PostVotingEmail);
        }

        public void SendResultsPublished(int electionId, IJobCancellationToken cancellationToken)
        {
            SendCommonEmail(electionId, cancellationToken, election => election.ResultsPublishedEmail);
        }

        protected override IEnumerable<string> DefaultGenerateRecipientIds(Election election)
        {
            return election.EligibilityEntries.Select(entry => entry.Username);
        }
    }
}