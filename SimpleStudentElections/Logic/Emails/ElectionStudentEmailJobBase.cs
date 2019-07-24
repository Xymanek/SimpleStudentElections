using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.Emails
{
    public abstract class ElectionStudentEmailJobBase : ElectionEmailJobBase
    {
        protected ElectionStudentEmailJobBase(VotingDbContext db) : base(db)
        {
        }

        protected CouncilElectionData GetCouncilData(Election election, string exceptionMessageIfNotFound)
        {
            CouncilElectionData data = Db.CouncilElectionData.Find(election.Id);

            if (data == null)
            {
                throw new Exception(exceptionMessageIfNotFound);
            }

            return data;
        }

        protected delegate IEnumerable<string> RecipientIdsGenerator(Election election);

        protected void SendCommonEmail(int electionId,
            IJobCancellationToken cancellationToken,
            Func<Election, EmailDefinition> emailDefinitionFetcher,
            RecipientIdsGenerator recipientIdsGenerator = null
        )
        {
            Election election = GetElection(electionId);
            EmailDefinition definition = emailDefinitionFetcher(election);

            cancellationToken.ThrowIfCancellationRequested();
            SendEmail(election, definition, recipientIdsGenerator, cancellationToken);
        }

        protected void SendCouncilEmail(
            int electionId,
            IJobCancellationToken cancellationToken,
            Func<CouncilElectionData, EmailDefinition> emailFetcher,
            RecipientIdsGenerator recipientIdsGenerator = null
        )
        {
            Election election = GetElection(electionId);
            EmailDefinition definition = emailFetcher(GetCouncilData(
                election,
                "Requested to send council-specific email for election that isn't a council one"
            ));

            SendEmail(election, definition, recipientIdsGenerator, cancellationToken);
        }

        private void SendEmail(
            Election election, EmailDefinition definition,
            RecipientIdsGenerator recipientIdsGenerator,
            IJobCancellationToken cancellationToken
        )
        {
            if (!definition.IsEnabled) return;

            if (recipientIdsGenerator == null)
            {
                recipientIdsGenerator = DefaultGenerateRecipientIds;
            }
            
            string[] recipientIds = recipientIdsGenerator(election).ToArray();
            int definitionId = definition.Id;

            cancellationToken.ThrowIfCancellationRequested();
            ScheduleJobsInTransaction(
                recipientIds,
                (userId, jobClient) => jobClient.Enqueue<StudentMailerJob>(
                    mailer => mailer.SendSingleEmail(definitionId, userId)
                )
            );
        }

        protected virtual IEnumerable<string> DefaultGenerateRecipientIds(Election election)
        {
            throw new Exception(
                $"Override {nameof(DefaultGenerateRecipientIds)} or pass recipient ids generator delegate"
            );
        }
    }
}