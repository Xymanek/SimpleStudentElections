using System;
using Hangfire;
using SimpleStudentElections.Helpers;
using SimpleStudentElections.Logic.Emails;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.DelayedJobScheduling
{
    public abstract class AlmostOverJobDescriptorBase : JobDescriptorBase<Election>
    {
        protected AlmostOverJobDescriptorBase(Election entity) : base(entity)
        {
        }

        public override bool ShouldBeScheduled()
        {
            return
                ElectionLifecycleInfo.AreAlmostOverEmailsEnabled(Entity) &&
                Entity.State == GetPhase().AssociatedState;
        }

        public override DateTime GetIntendedRunAt()
        {
            return GetPhase().AlmostOverEmailAt;
        }

        public override PropertyReference<ScheduledJobInfo> GetJobInfoReference()
        {
            return PropertyReference.Create(GetPhase(), phase => phase.AlmostOverJobInfo);
        }

        protected abstract ElectionPhaseBase GetPhase();
    }

    public class NominationsAlmostOverJobDescriptor : AlmostOverJobDescriptorBase
    {
        public NominationsAlmostOverJobDescriptor(Election entity) : base(entity)
        {
        }

        public override string Schedule(IBackgroundJobClient jobClient)
        {
            return jobClient.Schedule<AlmostOverNotificationJobs>(
                jobs => jobs.SendForNominations(Entity.Id, JobCancellationToken.Null),
                GetIntendedRunAt()
            );
        }

        protected override ElectionPhaseBase GetPhase()
        {
            return Entity.Nominations;
        }
    }

    public class VotingAlmostOverJobDescriptor : AlmostOverJobDescriptorBase
    {
        public VotingAlmostOverJobDescriptor(Election entity) : base(entity)
        {
        }

        public override string Schedule(IBackgroundJobClient jobClient)
        {
            return jobClient.Schedule<AlmostOverNotificationJobs>(
                jobs => jobs.SendForVoting(Entity.Id, JobCancellationToken.Null),
                GetIntendedRunAt()
            );
        }

        protected override ElectionPhaseBase GetPhase()
        {
            return Entity.Voting;
        }
    }
}