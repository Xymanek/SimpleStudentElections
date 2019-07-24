using System;
using Hangfire;
using SimpleStudentElections.Helpers;
using SimpleStudentElections.Logic.ElectionMaintenanceJobs;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.DelayedJobScheduling
{
    public abstract class PhaseJobDescriptorBase : JobDescriptorBase<Election>
    {
        protected PhaseJobDescriptorBase(Election entity) : base(entity)
        {
        }

        public override bool ShouldBeScheduled()
        {
            ElectionState intendedState = IsStart ? ElectionLifecycleInfo.Before(PhaseState) : PhaseState;

            return Entity.State == intendedState;
        }

        public override DateTime GetIntendedRunAt()
        {
            ElectionPhaseBase phase = Entity.GetPhaseByState(PhaseState);

            return IsStart ? phase.BeginsAt : phase.EndsAt;
        }

        public override PropertyReference<ScheduledJobInfo> GetJobInfoReference()
        {
            return PropertyReference.Create(GetStateChangeRecord(), change => change.JobInfo);
        }

        public override string Schedule(IBackgroundJobClient jobClient)
        {
            Guid changeInfoId = GetStateChangeRecord().Id;

            string jobId = jobClient.Schedule<StateTransition>(
                transition => transition.Execute(changeInfoId),
                GetIntendedRunAt()
            );

            jobClient.ContinueJobWith<SynchronizeDelayedJobsJob>(jobId, job => job.Execute(Entity.Id));
            jobClient.ContinueJobWith<StateTransition>(jobId, job => job.SendTransitionEmails(changeInfoId));

            return jobId;
        }

        protected abstract ElectionState PhaseState { get; }
        protected abstract bool IsStart { get; }

        private ElectionStateChange GetStateChangeRecord()
        {
            ElectionPhaseBase phase = Entity.GetPhaseByState(PhaseState);

            return IsStart ? phase.BeginsInfo : phase.EndInfo;
        }
    }

    // Nominations

    public class NominationsStartJobDescriptor : PhaseJobDescriptorBase
    {
        public NominationsStartJobDescriptor(Election entity) : base(entity)
        {
        }

        protected override ElectionState PhaseState => ElectionState.Nominations;
        protected override bool IsStart => true;
    }

    public class NominationsEndJobDescriptor : PhaseJobDescriptorBase
    {
        public NominationsEndJobDescriptor(Election entity) : base(entity)
        {
        }

        protected override ElectionState PhaseState => ElectionState.Nominations;
        protected override bool IsStart => false;
    }

    // Voting

    public class VotingStartJobDescriptor : PhaseJobDescriptorBase
    {
        public VotingStartJobDescriptor(Election entity) : base(entity)
        {
        }

        protected override ElectionState PhaseState => ElectionState.Voting;
        protected override bool IsStart => true;
    }

    public class VotingEndJobDescriptor : PhaseJobDescriptorBase
    {
        public VotingEndJobDescriptor(Election entity) : base(entity)
        {
        }

        protected override ElectionState PhaseState => ElectionState.Voting;
        protected override bool IsStart => false;
    }
}