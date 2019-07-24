using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SimpleStudentElections.Helpers;

namespace SimpleStudentElections.Models.Forms
{
    [Serializable]
    public class CouncilElectionForm : ElectionForm
    {
        [UIHint("ListOfPositions")]
        public List<CouncilRoleForm> Roles { get; set; } = new List<CouncilRoleForm>();

        [Display(Name = "Nominations almost over")]
        [ChangeSetPrinterSection("Emails")]
        public EmailForm NominationsAlmostOverEmail { get; set; } = new EmailForm()
        {
            Subject = "Your chance to nominate is almost gone!"
        };

        [Display(Name = "Voting started")]
        [ChangeSetPrinterSection("Emails")]
        public EmailForm VotingStartedEmail { get; set; } = new EmailForm
        {
            Subject = "Voting is open!"
        };

        [Display(Name = "Voting almost over")]
        [ChangeSetPrinterSection("Emails")]
        public EmailForm VotingAlmostOverEmail { get; set; } = new EmailForm()
        {
            Subject = "Your chance to vote is almost gone!"
        };

        public static ModelFieldsAccessibility DefaultCouncilFieldsInfo(ModelFieldsAccessibility.Kind? defaultKind = null)
        {
            ModelFieldsAccessibility fieldsInfo = DefaultFieldsInfo(defaultKind);

            fieldsInfo.MarkComplexBatch(
                () => EmailForm.DefaultFieldsInfo(ModelFieldsAccessibility.Kind.Editable),
                new[]
                {
                    nameof(NominationsAlmostOverEmail),
                    nameof(VotingStartedEmail),
                    nameof(VotingAlmostOverEmail),
                }
            );

            return fieldsInfo;
        }
    }

    [FormComplex]
    [Serializable]
    public class CouncilRoleForm
    {
        // null means new
        [ChangeSetExclude]
        public int? Id { get; set; }

        public string Name { get; set; }
    }
}