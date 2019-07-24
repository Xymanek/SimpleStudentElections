using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JetBrains.Annotations;

namespace SimpleStudentElections.Models
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class Election
    {
        [Key] public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Index(IsUnique = true)]
        public string Name { get; set; }

        [Required]
        public ElectionType Type { get; set; }

        [Column(TypeName = "text")] public string Description { get; set; }
        [Column(TypeName = "text")] public string ResultsText { get; set; }
        public bool DisableAutomaticEligibility { get; set; }
        public bool PositionGenerationInProcess { get; set; }

        public ElectionState State { get; set; } = ElectionState.Disabled;

        [Required] public virtual NominationPhase Nominations { get; set; }
        [Required] public virtual VotingPhase Voting { get; set; }

        // Cannot specify [Required] on these as SQL Server complains about "multiple cascade paths"
        // Will need to fix this someday (when election deletion is implemented) 
        
        public virtual EmailDefinition NominationsStartedEmail { get; set; }
        public virtual EmailDefinition PostNominationsEmail { get; set; }
        public virtual EmailDefinition PostVotingEmail { get; set; }
        public virtual EmailDefinition ResultsPublishedEmail { get; set; }

        public virtual ICollection<VotablePosition> Positions { get; set; }
        public virtual ICollection<ElectionEligibilityEntry> EligibilityEntries { get; set; }

        public virtual ICollection<ElectionStateChange> StateChanges { get; set; }

        [Timestamp] [UsedImplicitly] public byte[] TimeStamp { get; set; }

        public IEnumerable<VotablePosition> PositionsSorted => Positions.OrderBy(position => position.Id);

        public ElectionPhaseBase GetPhaseByState(ElectionState stateInQuestion)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (stateInQuestion)
            {
                case ElectionState.Nominations:
                    return Nominations;
                
                case ElectionState.Voting:
                    return Voting;
                
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(stateInQuestion),
                        $"Only {ElectionState.Nominations} and {ElectionState.Voting} are allowed"
                    );
            }
        }

        public void PopulateAutomaticStateTransitions()
        {
            Nominations.BeginsInfo = new ElectionStateChange
            {
                Election = this,
                PreviousState = ElectionState.PreNominations,
                TargetState = ElectionState.Nominations
            };
            Nominations.EndInfo = new ElectionStateChange
            {
                Election = this,
                PreviousState = ElectionState.Nominations,
                TargetState = ElectionState.PreVoting
            };
            
            Voting.BeginsInfo = new ElectionStateChange
            {
                Election = this,
                PreviousState = ElectionState.PreVoting,
                TargetState = ElectionState.Voting
            };
            Voting.EndInfo = new ElectionStateChange
            {
                Election = this,
                PreviousState = ElectionState.Voting,
                TargetState = ElectionState.Closed
            };
        }

        public void RemoveAutomaticStateTransitions(VotingDbContext db)
        {
            db.ElectionStateChanges.Remove(Nominations.BeginsInfo);
            db.ElectionStateChanges.Remove(Nominations.EndInfo);
            db.ElectionStateChanges.Remove(Voting.BeginsInfo);
            db.ElectionStateChanges.Remove(Voting.EndInfo);

            Nominations.BeginsInfo = null;
            Nominations.EndInfo = null;
            Voting.BeginsInfo = null;
            Voting.EndInfo = null;
        }
    }

    public enum ElectionType
    {
        [Display(Name = "Student council")]
        StudentCouncil,

        [Display(Name = "Course representatives")]
        CourseRepresentative
    }

    /// <remarks>
    /// Do not compare these values directly - use ElectionLifecycleInfo
    /// <br/>
    /// Please keep in sync with Views\AdminElections\Help.cshtml
    /// </remarks>
    public enum ElectionState
    {
        /// <summary>
        /// The election has been created but it is not active and will not fire emails or be accessible by students
        /// <br/>
        /// This is the initial state and cannot be returned to once nominations have started.
        /// <br/>
        /// It isn't possible to advance from this state unless there is a day left until nominations start 
        /// </summary>
        Disabled,

        /// <summary>
        /// The election is waiting to start. It is still possible to revert back to Disabled
        /// </summary>
        PreNominations,

        /// <summary>
        /// The nomination phase is currently underway
        /// </summary>
        Nominations,

        /// <summary>
        /// The nominations have been closed and we are currently waiting to start the voting phase
        /// </summary>
        PreVoting,

        /// <summary>
        /// The voting phase is underway
        /// </summary>
        Voting,

        /// <summary>
        /// The voting phase is complete and we are currently waiting for student support to publish results
        /// </summary>
        Closed,

        /// <summary>
        /// The student support published the result and the election is now archived
        /// </summary>
        [Display(Name = "Results are published")]
        ResultsPublished,

        /// <summary>
        /// The election was aborted for some reason. It is not visible to students
        /// <br/>
        /// It remains for archival purposes and cannot be reenabled, restarted, etc.
        /// </summary>
        Aborted
    }
}