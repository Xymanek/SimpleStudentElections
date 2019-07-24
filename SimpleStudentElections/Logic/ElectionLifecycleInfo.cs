using System;
using System.Configuration;
using System.Linq;
using JetBrains.Annotations;
using SimpleStudentElections.Models;
using SimpleStudentElections.Models.Forms;

namespace SimpleStudentElections.Logic
{
    /// <summary>
    /// This class contains information and rules about what and when can happen to an election.
    /// <br/>
    /// It should never contain any "work" code
    /// </summary>
    public static class ElectionLifecycleInfo
    {
        public static readonly ElectionState[] NormalOrder =
        {
            ElectionState.PreNominations,
            ElectionState.Nominations,
            ElectionState.PreVoting,
            ElectionState.Voting,
            ElectionState.Closed,
            ElectionState.ResultsPublished,
        };

        /// <summary>
        /// States of election during which it is not controlled by automatic transitions.
        /// Only manual actions will affect the election
        /// </summary>
        public static readonly ElectionState[] InactiveStates =
        {
            ElectionState.Disabled,
            ElectionState.Aborted
        };

        /// <summary>
        /// State which cause elections to be listed in "Archived" section of admin panel
        /// </summary>
        public static readonly ElectionState[] ArchivedStates =
        {
            ElectionState.ResultsPublished,
            ElectionState.Aborted
        };

        public static bool IsInactive(Election election)
        {
            return InactiveStates.Contains(election.State);
        }

        public static bool IsFirst(ElectionState state)
        {
            ValidateNormalOrder(state, nameof(state));

            return NormalOrder[0] == state;
        }

        public static bool IsLast(ElectionState state)
        {
            ValidateNormalOrder(state, nameof(state));

            return NormalOrder.Last() == state;
        }

        public static ElectionState Before(ElectionState state)
        {
            // This will also check if the state is in normal order
            if (IsFirst(state))
            {
                throw new ArgumentOutOfRangeException(nameof(state), state,
                    "There is nothing before the first state in lifecycle");
            }

            return NormalOrder[Array.IndexOf(NormalOrder, state) - 1];
        }

        public static ElectionState After(ElectionState state)
        {
            // This will also check if the state is in normal order
            if (IsLast(state))
            {
                throw new ArgumentOutOfRangeException(nameof(state), state,
                    "There is nothing after the last state in lifecycle");
            }

            return NormalOrder[Array.IndexOf(NormalOrder, state) + 1];
        }

        public static bool IsBefore(Election election, ElectionState referenceState)
        {
            return Compare(election, referenceState) == -1;
        }

        public static bool IsAfter(Election election, ElectionState referenceState)
        {
            return Compare(election, referenceState) == 1;
        }

        private static int Compare(Election election, ElectionState referenceState)
        {
            if (IsInactive(election))
            {
                throw new InactiveElectionNoLifecycleComparisonException();
            }

            ValidateNormalOrder(election.State, nameof(election));
            ValidateNormalOrder(referenceState, nameof(referenceState));

            int current = Array.IndexOf(NormalOrder, election.State);
            int reference = Array.IndexOf(NormalOrder, referenceState);

            return current.CompareTo(reference);
        }

        private static void ValidateNormalOrder(ElectionState state, string argumentName)
        {
            if (!NormalOrder.Contains(state))
            {
                throw new ArgumentOutOfRangeException(
                    argumentName, state,
                    "The election state is not part of normal lifecycle order and cannot be used in lifecycle context"
                );
            }
        }

        public static SimpleValidationResult CanForcePositionGeneration(Election election)
        {
            SimpleValidationResult result = new SimpleValidationResult();
            result.RequireCurrentState(election, ElectionState.Disabled);
            result.RequireType(election, ElectionType.CourseRepresentative);

            return result;
        }

        public static SimpleValidationResult CanActivate(Election election)
        {
            SimpleValidationResult result = new SimpleValidationResult();
            result.RequireCurrentState(election, ElectionState.Disabled);

            int hoursRequired = GetConfiguration().MinHoursUntilActivation;
            if (hoursRequired > 0 && election.Nominations.BeginsAt < DateTime.Now.AddHours(hoursRequired))
            {
                result.Violations.Add(new NominationsTooClose(hoursRequired));
            }

            if (election.PositionGenerationInProcess)
            {
                result.Violations.Add(new PositionGenerationInProcess());
            }

            return result;
        }

        public static SimpleValidationResult CanDeactivate(Election election)
        {
            SimpleValidationResult result = new SimpleValidationResult();
            result.RequireCurrentState(election, ElectionState.PreNominations);

            return result;
        }

        public static SimpleValidationResult CanNominate(Election election)
        {
            SimpleValidationResult result = new SimpleValidationResult();
            result.RequireCurrentState(election, ElectionState.Nominations);

            return result;
        }

        public static SimpleValidationResult CanVote(Election election)
        {
            SimpleValidationResult result = new SimpleValidationResult();
            result.RequireCurrentState(election, ElectionState.Voting);

            return result;
        }

        public static SimpleValidationResult CanEdit(Election election)
        {
            SimpleValidationResult result = new SimpleValidationResult();
            result.ProhibitState(election, ElectionState.ResultsPublished);
            result.ProhibitState(election, ElectionState.Aborted);

            return result;
        }

        private static readonly ElectionState[] ResultsAdminStates =
        {
            ElectionState.Closed,
            ElectionState.ResultsPublished,
            ElectionState.Aborted
        };

        /// <summary>
        /// Whether to show "Results" field (and have it editable)
        /// If election == null, it means that it's a new one
        /// </summary>
        public static bool ShowResultsAdmin([CanBeNull] Election election)
        {
            return election != null && ResultsAdminStates.Contains(election.State);
        }

        public static bool ShowResultsStudent(Election election)
        {
            return election.State == ElectionState.ResultsPublished;
        }

        public static bool AreAlmostOverEmailsEnabled(Election election)
        {
            return election.Type == ElectionType.StudentCouncil;
        }

        public static ModelFieldsAccessibility GetWhatCanBeEditedCouncil(Election election)
        {
            if (!CanEdit(election))
            {
                throw new Exception(
                    $"Cannot call {nameof(GetWhatCanBeEditedCouncil)} when the election isn't editable"
                );
            }

            bool isCouncil = election.Type == ElectionType.StudentCouncil;

            // Mark everything editable by default
            ModelFieldsAccessibility fieldsInfo = isCouncil
                ? CouncilElectionForm.DefaultCouncilFieldsInfo(ModelFieldsAccessibility.Kind.Editable)
                : ElectionForm.DefaultFieldsInfo(ModelFieldsAccessibility.Kind.Editable);

            if (!ShowResultsAdmin(election))
            {
                fieldsInfo.MarkNotShown(nameof(ElectionForm.ResultsText));
            }

            if (election.State == ElectionState.Disabled)
            {
                // Everything (expect ResultsText) can be edited
                return fieldsInfo;
            }

            if (election.State == ElectionState.PreNominations || IsAfter(election, ElectionState.PreNominations))
            {
                if (isCouncil) fieldsInfo.MarkNotEditable(nameof(CouncilElectionForm.Roles));

                fieldsInfo
                    .GetSubFieldInfo(nameof(ElectionForm.Nominations))
                    .MarkNotEditable(nameof(ElectionPhaseForm.BeginsAt));

                if (election.State == ElectionState.PreNominations) return fieldsInfo;
            }

            if (election.State == ElectionState.Nominations || IsAfter(election, ElectionState.Nominations))
            {
                fieldsInfo.MarkNotShown(nameof(ElectionForm.NominationsStartedEmail));

                // Eligibility is not refreshed automatically after nominations have started
                fieldsInfo.MarkNotShown(nameof(ElectionForm.DisableAutomaticEligibility));

                if (election.State == ElectionState.Nominations) return fieldsInfo;
            }

            if (election.State == ElectionState.PreVoting || IsAfter(election, ElectionState.PreVoting))
            {
                fieldsInfo.MarkNotShown(nameof(ElectionForm.Nominations));
                if (isCouncil) fieldsInfo.MarkNotShown(nameof(CouncilElectionForm.NominationsAlmostOverEmail));
                fieldsInfo.MarkNotShown(nameof(ElectionForm.PostNominationsEmail));

                if (election.State == ElectionState.PreVoting) return fieldsInfo;
            }

            if (election.State == ElectionState.Voting || IsAfter(election, ElectionState.Voting))
            {
                if (isCouncil) fieldsInfo.MarkNotShown(nameof(CouncilElectionForm.VotingStartedEmail));

                fieldsInfo
                    .GetSubFieldInfo(nameof(ElectionForm.Voting))
                    .MarkNotEditable(nameof(ElectionPhaseForm.BeginsAt));

                if (election.State == ElectionState.Voting) return fieldsInfo;
            }

            if (election.State == ElectionState.Closed || IsAfter(election, ElectionState.Closed))
            {
                if (isCouncil) fieldsInfo.MarkNotShown(nameof(CouncilElectionForm.Roles));
                fieldsInfo.MarkNotShown(nameof(ElectionForm.Voting));
                if (isCouncil) fieldsInfo.MarkNotShown(nameof(CouncilElectionForm.VotingAlmostOverEmail));
                fieldsInfo.MarkNotShown(nameof(ElectionForm.PostVotingEmail));

                if (election.State == ElectionState.Closed) return fieldsInfo;
            }

            return fieldsInfo;
        }

        public static SimpleValidationResult CanPublishResults(Election election)
        {
            SimpleValidationResult result = new SimpleValidationResult();
            result.RequireCurrentState(election, ElectionState.Closed);

            if (election.ResultsText == null)
            {
                result.Violations.Add(new NoResultsEntered());
            }

            return result;
        }

        public static SimpleValidationResult CanDelete(Election election)
        {
            SimpleValidationResult result = new SimpleValidationResult();
            result.RequireCurrentState(election, ElectionState.Aborted);

            // Only check the time requirement when the state is correct
            if (result)
            {
                int daysRequired = GetConfiguration().MinDaysForDeletion;

                DateTime? abortedAt = new VotingDbContext().ElectionStateChanges
                    .Where(change => change.ElectionId == election.Id)
                    .Where(change => change.TargetState == ElectionState.Aborted)
                    .OrderByDescending(change => change.CompletedAt)
                    .Select(change => change.CompletedAt)
                    .First();

                if (!abortedAt.HasValue)
                {
                    throw new Exception(
                        $"Transition to {ElectionState.Aborted} has no {nameof(ElectionStateChange.CompletedAt)} set"
                    );
                }

                if (abortedAt.Value.AddDays(daysRequired) > DateTime.Now)
                {
                    result.Violations.Add(new NotEnoughTimeSinceAbort(daysRequired));
                }
            }

            return result;
        }

        public class NotInRequiredState : SimpleValidationResult.Violation
        {
            public readonly ElectionState RequiredState;

            public NotInRequiredState(ElectionState requiredState)
            {
                RequiredState = requiredState;
            }

            public override string HumanError => "Election must be in " + RequiredState + " state";
        }

        public class ProhibitedState : SimpleValidationResult.Violation
        {
            public readonly ElectionState CurrentState;

            public ProhibitedState(ElectionState prohibitedState)
            {
                CurrentState = prohibitedState;
            }

            public override string HumanError => "Election must NOT be in " + CurrentState + " state";
        }

        public class NominationsTooClose : SimpleValidationResult.Violation
        {
            public readonly int HoursDiffRequired;

            public NominationsTooClose(int hoursDiffRequired)
            {
                HoursDiffRequired = hoursDiffRequired;
            }

            public override string HumanError =>
                $"There must be at least {HoursDiffRequired} hours left until start of nominations";
        }

        public class NoResultsEntered : SimpleValidationResult.Violation
        {
            public override string HumanError => "The Results field is empty";
        }

        public class NotRequiredType : SimpleValidationResult.Violation
        {
            public readonly ElectionType RequiredElectionType;

            public NotRequiredType(ElectionType requiredElectionType)
            {
                RequiredElectionType = requiredElectionType;
            }

            public override string HumanError => $"Election must be of {RequiredElectionType} type";
        }

        public class PositionGenerationInProcess : SimpleValidationResult.Violation
        {
            public override string HumanError => "The list of positions for this election is currently being generated";
        }

        public class NotEnoughTimeSinceAbort : SimpleValidationResult.Violation
        {
            public readonly int MinDaysRequired;

            public NotEnoughTimeSinceAbort(int minDaysRequired)
            {
                MinDaysRequired = minDaysRequired;
            }

            public override string HumanError =>
                $"At least {MinDaysRequired} days must have passed since aborting the election";
        }

        private static void RequireCurrentState(
            this SimpleValidationResult result,
            Election election,
            ElectionState requiredState
        )
        {
            if (election.State != requiredState)
            {
                result.Violations.Add(new NotInRequiredState(requiredState));
            }
        }

        private static void ProhibitState(
            this SimpleValidationResult result,
            Election election,
            ElectionState prohibitedState
        )
        {
            if (election.State == prohibitedState)
            {
                result.Violations.Add(new ProhibitedState(prohibitedState));
            }
        }

        private static void RequireType(
            this SimpleValidationResult result,
            Election election,
            ElectionType requiredType
        )
        {
            if (election.Type != requiredType)
            {
                result.Violations.Add(new NotRequiredType(requiredType));
            }
        }

        public static ElectionLifecycleConfiguration GetConfiguration()
        {
            return (ElectionLifecycleConfiguration) ConfigurationManager.GetSection(
                "appCustomConfig/electionLifecycle"
            );
        }
    }

    public class InactiveElectionNoLifecycleComparisonException : Exception
    {
        public InactiveElectionNoLifecycleComparisonException()
            : base("Inactive elections do not abide by lifecycle rules and cannot be compared as such")
        {
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class ElectionLifecycleConfiguration : ConfigurationSection
    {
        private const string MinHoursUntilActivationKey = "minHoursForActivation";
        private const string MinDaysForDeletionKey = "minDaysForDeletion";

        [ConfigurationProperty(MinHoursUntilActivationKey, IsRequired = true)]
        public int MinHoursUntilActivation
        {
            get => (int) this[MinHoursUntilActivationKey];
            set => this[MinHoursUntilActivationKey] = value;
        }

        [ConfigurationProperty(MinDaysForDeletionKey, IsRequired = true)]
        public int MinDaysForDeletion
        {
            get => (int) this[MinDaysForDeletionKey];
            set => this[MinDaysForDeletionKey] = value;
        }
    }
}