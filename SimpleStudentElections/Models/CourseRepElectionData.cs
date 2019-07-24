using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleStudentElections.Models
{
    public class RepresentativePositionData
    {
        [Required]
        [Key, ForeignKey("PositionCommon")]
        public int PositionId { get; set; }

        public virtual VotablePosition PositionCommon { get; set; }

        [Required]
        [MaxLength(150)]
        [Index("IX_UniqueProgrammeYear", 1, IsUnique = true)]
        public string ProgrammeName { get; set; }

        [NotMapped]
        public AcademicYear ExpectedGraduationYear
        {
            get => new AcademicYear(ExpectedGraduationYearString);
            set => ExpectedGraduationYearString = value.ToString();
        }

        [Required]
        [MaxLength(6)]
        [Index("IX_UniqueProgrammeYear", 2, IsUnique = true)]
        public string ExpectedGraduationYearString { get; set; }

        public void SetPositionName()
        {
            PositionCommon.HumanName = $"{ProgrammeName} graduating in {ExpectedGraduationYearString}";
        }
    }
}