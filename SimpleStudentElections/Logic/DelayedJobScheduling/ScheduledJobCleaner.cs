using System;
using JetBrains.Annotations;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.DelayedJobScheduling
{
    public class ScheduledJobCleaner
    {
        private readonly VotingDbContext _db;

        public ScheduledJobCleaner(VotingDbContext db)
        {
            _db = db;
        }

        [UsedImplicitly] // Hangfire
        public ScheduledJobCleaner() : this(new VotingDbContext())
        {
        }

        public void Cleanup(Guid jobInfoId)
        {
            ScheduledJobInfo jobInfo = _db.ScheduledJobInfos.Find(jobInfoId);

            if (jobInfo == null)
            {
                Console.WriteLine(
                    $"{nameof(ScheduledJobCleaner)}.{nameof(Cleanup)} called with id {jobInfoId} but no such job exists - skipping"
                );
                return;
            }

            _db.ScheduledJobInfos.Remove(jobInfo);
        }
    }
}