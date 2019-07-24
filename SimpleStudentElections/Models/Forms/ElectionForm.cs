using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using SimpleStudentElections.Helpers;

namespace SimpleStudentElections.Models.Forms
{
    [FormComplex]
    [Serializable]
    public class ElectionForm
    {
        // null means new
        [ChangeSetExclude]
        public int? Id { get; set; }

        [Required, UniqueElectionName]
        [MinLength(4), MaxLength(100)]
        public string Name { get; set; }

        [DataType(DataType.MultilineText), UIHint("CKEditor"), AllowHtml]
        public string Description { get; set; }

        [Display(Name = "Disable automatic eligibility")]
        public bool DisableAutomaticEligibility { get; set; }

        [DataType(DataType.MultilineText), UIHint("CKEditor"), AllowHtml]
        [Display(Name = "Results")]
        public string ResultsText { get; set; }
        public ElectionPhaseForm Nominations { get; set; } = new ElectionPhaseForm()
        {
            BeginsAt = DateTime.Today.AddDays(2),
            EndsAt = DateTime.Today.AddDays(4),
            AlarmCheckAt = DateTime.Today.AddDays(3),
            AlmostOverEmailAt = DateTime.Today.AddDays(3),
        };

        [GreaterThanProperty(
            nameof(Nominations),
            ErrorMessage = "Voting cannot start before nominations finish",
            MemberName = nameof(ElectionPhaseForm.BeginsAt)
        )]
        public ElectionPhaseForm Voting { get; set; } = new ElectionPhaseForm()
        {
            BeginsAt = DateTime.Today.AddDays(5),
            EndsAt = DateTime.Today.AddDays(7),
            AlarmCheckAt = DateTime.Today.AddDays(6),
            AlmostOverEmailAt = DateTime.Today.AddDays(6),
        };

        // Common emails
        [Display(Name = "Nominations started")]
        [ChangeSetPrinterSection("Emails")]
        public EmailForm NominationsStartedEmail { get; set; } = new EmailForm
        {
            Subject = "Nominations are starting!"
        };

        [Display(Name = "Post nominations")]
        [ChangeSetPrinterSection("Emails")]
        public EmailForm PostNominationsEmail { get; set; } = new EmailForm
        {
            Subject = "Nominations are over"
        };

        [Display(Name = "Post voting")]
        [ChangeSetPrinterSection("Emails")]
        public EmailForm PostVotingEmail { get; set; } = new EmailForm
        {
            Subject = "Voting is over"
        };

        [Display(Name = "Results published")]
        [ChangeSetPrinterSection("Emails")]
        public EmailForm ResultsPublishedEmail { get; set; } = new EmailForm
        {
            Subject = "Election results are published"
        };

        public static ModelFieldsAccessibility DefaultFieldsInfo(ModelFieldsAccessibility.Kind? defaultKind = null)
        {
            ModelFieldsAccessibility fieldsInfo = new ModelFieldsAccessibility
            {
                DefaultKind = defaultKind
            };

            fieldsInfo.MarkComplex(nameof(Nominations), ElectionPhaseForm.DefaultFieldsInfo(defaultKind));
            fieldsInfo.MarkComplex(nameof(Voting), ElectionPhaseForm.DefaultFieldsInfo(defaultKind));

            fieldsInfo.MarkComplexBatch(
                () => EmailForm.DefaultFieldsInfo(ModelFieldsAccessibility.Kind.Editable),
                new[]
                {
                    nameof(NominationsStartedEmail),
                    nameof(PostNominationsEmail),
                    nameof(PostVotingEmail),
                    nameof(ResultsPublishedEmail),
                }
            );

            return fieldsInfo;
        }
    }

    [FormComplex]
    [Serializable]
    public class ElectionPhaseForm : IComparable
    {
        private const string WithinStartEnd = "Must be within begin-end range";

        [Required, InFuture]
        [Display(Name = "Begins at")]
        [DisplayFormat(DataFormatString = FormConstants.DefaultDateTimeFormat, ApplyFormatInEditMode = true)]
        public DateTime BeginsAt { get; set; }

        [Required, InFuture]
        [Display(Name = "Ends at")]
        [DisplayFormat(DataFormatString = FormConstants.DefaultDateTimeFormat, ApplyFormatInEditMode = true)]
        [GreaterThanProperty(nameof(BeginsAt), ErrorMessage = "Cannot end before starting")]
        public DateTime EndsAt { get; set; }

        [Display(Name = "Enable alarm")]
        public bool AlarmEnabled { get; set; } = true;

        [Range(1, 99)]
        [Display(Name = "Alarm threshold")]
        public int AlarmThreshold { get; set; } = 50;

        [Display(Name = "Do alarm check at")]
        [DisplayFormat(DataFormatString = FormConstants.DefaultDateTimeFormat, ApplyFormatInEditMode = true)]
        [GreaterThanProperty(nameof(BeginsAt), ErrorMessage = WithinStartEnd)]
        [LessThanProperty(nameof(EndsAt), ErrorMessage = WithinStartEnd)]
        [InFuture]
        public DateTime AlarmCheckAt { get; set; }

        [Display(Name = "Send \"Almost over\" emails at")]
        [DisplayFormat(DataFormatString = FormConstants.DefaultDateTimeFormat, ApplyFormatInEditMode = true)]
        [GreaterThanProperty(nameof(BeginsAt), ErrorMessage = WithinStartEnd)]
        [LessThanProperty(nameof(EndsAt), ErrorMessage = WithinStartEnd)]
        [InFuture]
        public DateTime AlmostOverEmailAt { get; set; }
        
        public int CompareTo(object otherObj)
        {
            if (!(otherObj is ElectionPhaseForm otherForm))
            {
                throw new Exception("Cannot compare ElectionPhaseForm with other types");
            }

            return BeginsAt.CompareTo(otherForm.EndsAt);
        }

        public static ModelFieldsAccessibility DefaultFieldsInfo(ModelFieldsAccessibility.Kind? defaultKind = null)
        {
            return new ModelFieldsAccessibility()
            {
                DefaultKind = defaultKind
            };
        }
    }
}