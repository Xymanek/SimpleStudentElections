using System;
using System.Collections.Generic;
using System.Transactions;
using Hangfire;
using Hangfire.SqlServer;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.Emails
{
    public abstract class ElectionEmailJobBase
    {
        protected readonly VotingDbContext Db;

        protected ElectionEmailJobBase(VotingDbContext db)
        {
            Db = db;
        }

        protected Election GetElection(int electionId)
        {
            Election election = Db.Elections.Find(electionId);

            if (election == null)
            {
                throw new ArgumentOutOfRangeException(nameof(electionId), "No election with such id");
            }

            return election;
        }

        protected static void ScheduleJobsInTransaction<T>(
            IEnumerable<T> elements, Action<T, IBackgroundJobClient> scheduleDelegate
        )
        {
            using (TransactionScope scope = new TransactionScope())
            {
                BackgroundJobClient jobClient = new BackgroundJobClient(
                    new SqlServerStorage(Startup.HangfireConnectionName)
                );

                foreach (T element in elements)
                {
                    scheduleDelegate(element, jobClient);
                }

                scope.Complete();
            }
        }
    }
}