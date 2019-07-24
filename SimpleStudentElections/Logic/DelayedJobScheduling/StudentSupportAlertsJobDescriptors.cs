using System;
using Hangfire;
using SimpleStudentElections.Helpers;
using SimpleStudentElections.Logic.Emails;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.DelayedJobScheduling
{
    public abstract class StudentSupportAlarmJobDescriptorBase : JobDescriptorBase<Election>
    {
        protected StudentSupportAlarmJobDescriptorBase(Election entity) : base(entity)
        {
        }

        public override bool ShouldBeScheduled()
        {
            ElectionPhaseBase jobPhase = GetPhase();

            return jobPhase.AlarmEnabled && Entity.State == jobPhase.AssociatedState;
        }

        public override DateTime GetIntendedRunAt()
        {
            return GetPhase().AlarmCheckAt;
        }

        public override PropertyReference<ScheduledJobInfo> GetJobInfoReference()
        {
            return PropertyReference.Create(GetPhase(), phase => phase.AlarmJobInfo);
        }

        protected abstract ElectionPhaseBase GetPhase();
    }

    public class NominationsAlertJobDescriptor : StudentSupportAlarmJobDescriptorBase
    {
        public NominationsAlertJobDescriptor(Election entity) : base(entity)
        {
        }

        public override string Schedule(IBackgroundJobClient jobClient)
        {
            return jobClient.Schedule<StudentSupportEmailJobs>(
                jobs => jobs.SendNominationsAlert(Entity.Id, JobCancellationToken.Null),
                GetIntendedRunAt()
            );
        }

        protected override ElectionPhaseBase GetPhase()
        {
            return Entity.Nominations;
        }
    }

    public class VotingAlertJobDescriptor : StudentSupportAlarmJobDescriptorBase
    {
        public VotingAlertJobDescriptor(Election entity) : base(entity)
        {
        }

        public override string Schedule(IBackgroundJobClient jobClient)
        {
            return jobClient.Schedule<StudentSupportEmailJobs>(
                jobs => jobs.SendVotingAlert(Entity.Id, JobCancellationToken.Null),
                GetIntendedRunAt()
            );
        }

        protected override ElectionPhaseBase GetPhase()
        {
            return Entity.Voting;
        }
    }
}