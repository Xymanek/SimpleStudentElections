using System;
using Hangfire;
using JetBrains.Annotations;
using SimpleStudentElections.Helpers;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.DelayedJobScheduling
{
    public interface IDelayedJobDescriptor
    {
        [Pure] bool ShouldBeScheduled();
        [Pure] DateTime GetIntendedRunAt();

        [Pure]
        PropertyReference<ScheduledJobInfo> GetJobInfoReference();

        /// <summary>
        /// Uses the HangFire API to actually schedule the job.
        /// For consistent results, the job should be scheduled at same time as <see cref="GetIntendedRunAt"/> returns
        /// </summary>
        /// <param name="jobClient"></param>
        /// <returns>The job ID as returned by hangfire API</returns>
        string Schedule(IBackgroundJobClient jobClient);
    }

    public abstract class JobDescriptorBase<TEntity> : IDelayedJobDescriptor
    {
        protected readonly TEntity Entity;

        protected JobDescriptorBase(TEntity entity)
        {
            Entity = entity;
        }

        public abstract bool ShouldBeScheduled();
        public abstract DateTime GetIntendedRunAt();
        public abstract PropertyReference<ScheduledJobInfo> GetJobInfoReference();
        public abstract string Schedule(IBackgroundJobClient jobClient);
    }
}