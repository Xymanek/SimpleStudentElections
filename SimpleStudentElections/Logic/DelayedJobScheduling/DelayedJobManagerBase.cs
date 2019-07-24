using System;
using System.Collections.Generic;
using System.Transactions;
using Hangfire;
using Hangfire.SqlServer;
using SimpleStudentElections.Helpers;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.DelayedJobScheduling
{
    public abstract class DelayedJobManagerBase<TEntity>
    {
        private VotingDbContext _db;

        public void Synchronise(Func<VotingDbContext, TEntity> entityFetcher)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                using (_db = new VotingDbContext())
                {
                    TEntity entity = entityFetcher(_db);
                    BackgroundJobClient jobClient = new BackgroundJobClient(
                        new SqlServerStorage(Startup.HangfireConnectionName)
                    );

                    foreach (IDelayedJobDescriptor descriptor in GetDescriptors(entity))
                    {
                        ProcessDescriptor(descriptor, jobClient);
                    }

                    _db.SaveChanges();
                }

                scope.Complete();
            }
        }

        protected abstract IEnumerable<IDelayedJobDescriptor> GetDescriptors(TEntity entity);

        private void ProcessDescriptor(IDelayedJobDescriptor descriptor, IBackgroundJobClient jobClient)
        {
            PropertyReference<ScheduledJobInfo> jobInfoReference = descriptor.GetJobInfoReference();

            if (descriptor.ShouldBeScheduled())
            {
                DateTime intendedRunAt = descriptor.GetIntendedRunAt();

                if (jobInfoReference.Value == null)
                {
                    // Create the job
                    ScheduleJob(descriptor, jobClient);
                }
                else if (jobInfoReference.Value.RunAt != intendedRunAt)
                {
                    // Re-schedule the job
                    CancelJob(jobInfoReference, jobClient);
                    ScheduleJob(descriptor, jobClient);
                }

                return;
            }

            if (jobInfoReference.Value != null)
            {
                // The job isn't supposed to run but it's scheduled
                CancelJob(jobInfoReference, jobClient);
            }
        }

        private void ScheduleJob(IDelayedJobDescriptor descriptor, IBackgroundJobClient jobClient)
        {
            string jobId = descriptor.Schedule(jobClient);

            ScheduledJobInfo jobInfo = new ScheduledJobInfo()
            {
                JobId = jobId,
                RunAt = descriptor.GetIntendedRunAt()
            };

            descriptor.GetJobInfoReference().Value = jobInfo;
            _db.ScheduledJobInfos.Add(jobInfo);

            jobClient.ContinueJobWith<ScheduledJobCleaner>(jobId, cleaner => cleaner.Cleanup(jobInfo.Id));
        }

        private void CancelJob(PropertyReference<ScheduledJobInfo> jobInfoReference, IBackgroundJobClient jobClient)
        {
            jobClient.Delete(jobInfoReference.Value.JobId);
            _db.ScheduledJobInfos.Remove(jobInfoReference.Value);
            jobInfoReference.Value = null;
        }
    }
}