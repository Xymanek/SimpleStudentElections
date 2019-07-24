using System;
using Hangfire;
using JetBrains.Annotations;
using SimpleStudentElections.Logic.DelayedJobScheduling;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.ElectionMaintenanceJobs
{
    public class SynchronizeDelayedJobsJob
    {
        private readonly VotingDbContext _db;

        public SynchronizeDelayedJobsJob(VotingDbContext db)
        {
            _db = db;
        }

        [UsedImplicitly] // Hangfire
        public SynchronizeDelayedJobsJob() : this(new VotingDbContext())
        {
        }

        [Queue(BackgroundJobConstants.CriticalQueueName)]
        public void Execute(int electionId)
        {
            new ElectionJobManager().Synchronise(context =>
            {
                Election election = _db.Elections.Find(electionId);

                if (election == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(electionId), "No election with such id");
                }

                return election;
            });
        }
    }
}