using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleStudentElections.Models
{
    public abstract class ElectionPhaseBase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Begins at")]
        public DateTime BeginsAt { get; set; }

        [Required]
        [Display(Name = "Ends at")]
        public DateTime EndsAt { get; set; }

        /// <summary>
        /// The integer-expressed percentage (so 40 means 40%) which will be used to check whether to trigger the alarm
        /// <br/>
        /// The alarm will be triggered if the participation ratio at the time of check is equal or below this value
        /// </summary>
        /// <seealso cref="ParticipationRatioToPercentage"/>
        [Range(1, 99)]
        [Display(Name = "Alarm threshold")]
        public int AlarmThreshold { get; set; } = 50;

        /// <summary>
        /// Specifies when to run the check
        /// </summary>
        [Display(Name = "Do alarm check at")]
        public DateTime AlarmCheckAt { get; set; }

        public bool AlarmEnabled { get; set; }

        // The default value is needed for elections which don't have this field set (they don't use almost over emails)
        // as otherwise SQL Server won't accept the default value
        [Display(Name = "Almost over emails at")]
        public DateTime AlmostOverEmailAt { get; set; } = new DateTime(1753, 1, 1); 

        public virtual ElectionStateChange BeginsInfo { get; set; }
        public virtual ElectionStateChange EndInfo { get; set; }

        public virtual ScheduledJobInfo AlarmJobInfo { get; set; }
        public virtual ScheduledJobInfo AlmostOverJobInfo { get; set; }

        [NotMapped]
        public abstract ElectionState AssociatedState { get; }

        public bool ShouldSendAlert(float participationRatio)
        {
            return ParticipationRatioToPercentage(participationRatio) <= AlarmThreshold;
        }

        public static int ParticipationRatioToPercentage(float participationRatio)
        {
            return (int) Math.Round(participationRatio * 100);
        }
    }

    public class NominationPhase : ElectionPhaseBase
    {
        public override ElectionState AssociatedState => ElectionState.Nominations;
    }

    public class VotingPhase : ElectionPhaseBase
    {
        public override ElectionState AssociatedState => ElectionState.Voting;
    }
}