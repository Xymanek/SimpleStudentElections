using System;
using Hangfire;
using JetBrains.Annotations;
using SimpleStudentElections.Logic.DelayedJobScheduling;
using SimpleStudentElections.Logic.Emails;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.ElectionMaintenanceJobs
{
    public class StateTransition
    {
        private enum Kind
        {
            NominationsStart,
            NominationsEnd,
            VotingStart,
            VotingEnd
        }

        private readonly VotingDbContext _db;

        [UsedImplicitly]
        public StateTransition()
        {
            _db = new VotingDbContext();
        }

        /// <summary>
        /// Validates and executes the scheduled background transition.
        /// </summary>
        /// <remarks>
        /// While this does very little itself, it's used to maintain consistent state of application and other jobs
        /// are keyed of this (they fire when this completes).<br/>
        /// The split in jobs also helps with splitting failure domains. For example: we don't want to fail transition
        /// if something went wrong while sending the emails 
        /// </remarks>
        /// <seealso cref="SendTransitionEmails"/>
        /// <seealso cref="SynchronizeDelayedJobsJob"/>
        /// <seealso cref="PhaseJobDescriptorBase"/>
        /// <param name="transitionId">The ID of ElectionStateChange (which will contain all info about the transition)</param>
        [Queue(BackgroundJobConstants.CriticalQueueName)]
        public void Execute(Guid transitionId)
        {
            ElectionStateChange transition = GetTransition(transitionId);

            if (transition.CompletedAt != null)
                throw new ArgumentOutOfRangeException(
                    nameof(transitionId),
                    "This transition is already executed"
                );

            // We don't need the return, but this does validation which we want (before executing the change) 
            DecideTransitionKind(transition);

            transition.Election.State = transition.TargetState;
            transition.CompletedAt = DateTime.Now;

            _db.SaveChanges();

            AuditLogManager.RecordElectionStateChange(transition);
        }

        /// <summary>
        /// Sends emails that result from a transition such as "nominations started" 
        /// </summary>
        /// <param name="transitionId">The ID of ElectionStateChange (which will contain all info about the transition)</param>
        public void SendTransitionEmails(Guid transitionId)
        {
            ElectionStateChange transition = GetTransition(transitionId);

            switch (DecideTransitionKind(transition))
            {
                case Kind.NominationsStart:
                    BackgroundJob.Enqueue<ElectionStudentSimpleEmailsJobs>(
                        job => job.SendNominationsStart(transition.Election.Id, JobCancellationToken.Null)
                    );
                    break;

                case Kind.NominationsEnd:
                    BackgroundJob.Enqueue<ElectionStudentSimpleEmailsJobs>(
                        job => job.SendNominationsEnd(transition.Election.Id, JobCancellationToken.Null)
                    );
                    break;

                case Kind.VotingStart:
                    if (transition.Election.Type == ElectionType.StudentCouncil)
                    {
                        BackgroundJob.Enqueue<ElectionStudentSimpleEmailsJobs>(
                            job => job.SendVotingStart(transition.Election.Id, JobCancellationToken.Null)
                        );
                    }

                    break;

                case Kind.VotingEnd:
                    BackgroundJob.Enqueue<ElectionStudentSimpleEmailsJobs>(
                        job => job.SendVotingEnd(transition.Election.Id, JobCancellationToken.Null)
                    );
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ElectionStateChange GetTransition(Guid transitionId)
        {
            ElectionStateChange transition = _db.ElectionStateChanges.Find(transitionId);

            if (transition == null)
                throw new ArgumentOutOfRangeException(
                    nameof(transitionId),
                    "No transition with such id"
                );

            if (!transition.PreviousState.HasValue)
                throw new ArgumentOutOfRangeException(
                    nameof(transitionId),
                    "This transition does not specify preceding state"
                );

            return transition;
        }

        private static Kind DecideTransitionKind(ElectionStateChange transition)
        {
            // ReSharper disable once PossibleInvalidOperationException (already checked)
            ElectionState precedingState = transition.PreviousState.Value;
            ElectionState targetState = transition.TargetState;

            if (precedingState == ElectionState.PreNominations && targetState == ElectionState.Nominations)
            {
                return Kind.NominationsStart;
            }

            if (precedingState == ElectionState.Nominations && targetState == ElectionState.PreVoting)
            {
                return Kind.NominationsEnd;
            }

            if (precedingState == ElectionState.PreVoting && targetState == ElectionState.Voting)
            {
                return Kind.VotingStart;
            }

            if (precedingState == ElectionState.Voting && targetState == ElectionState.Closed)
            {
                return Kind.VotingEnd;
            }

            throw new Exception($"This {nameof(ElectionStateChange)} is not a background one");
        }
    }
}