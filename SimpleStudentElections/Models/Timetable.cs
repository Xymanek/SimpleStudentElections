using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using JetBrains.Annotations;

namespace SimpleStudentElections.Models
{
    public class TimetableDbContext : DbContext
    {
        public TimetableDbContext() : base("Timetable")
        {
        }

        public DbSet<TimetableUserEntry> Users { get; set; }

        public DbSet<AuthSession> AuthSessions { get; set; }
        public DbSet<AuthToken> AuthTokens { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Commented out as the names are omitted. Used in reality
            /*modelBuilder.Entity<AuthToken>()
                .MapToStoredProcedures(conf => 
                    conf.Delete(sp => sp.HasName("").Parameter(token => token.Guid, ""))
                    );*/
        }
    }

    [Table("VIEW_E_VOTING") /* This is a view in the database */]
    public class TimetableUserEntry
    {
        public string Fullname { get; set; }
        [CanBeNull] public string Username { get; set; } // Null for SS
        [CanBeNull] public string ProgrammeName { get; set; } // Null for SS
        [CanBeNull] public string YearAdmittedString { get; set; } // Null for SS
        [CanBeNull] public string ExpectedGraduationYearString { get; set; } // Null for SS
        [CanBeNull] public string StudentStatusDescription { get; set; } // Null for SS
        [Key] public string InternalEmail { get; set; }
        public string AccountTypeName { get; set; }

        /// <summary>
        /// Lower-case user ID for both Student Support and students
        /// </summary>
        [NotMapped] public string UserId => NormalizeUsernameToId(InternalEmail);
        
        public AcademicYear YearAdmitted => new AcademicYear(YearAdmittedString);
        public AcademicYear ExpectedGraduationYear => new AcademicYear(ExpectedGraduationYearString);

        [NotMapped] public bool IsStudentSupport => new[] {this}.AsQueryable().WhereIsStudentSupport().Any();
        [NotMapped] public bool IsStudentActive => new[] {this}.AsQueryable().WhereIsStudentActive().Any();

        public static string NormalizeUsernameToId(string email)
        {
            return email.ToLower().Replace("@example.com", "");
        }
    }

    // Hack(s) to use computed properties above in LINQ to entities   
    public static class TimetableUserEntryExtensions
    {
        public static IQueryable<TimetableUserEntry> WhereIsStudentSupport(this IQueryable<TimetableUserEntry> query)
        {
            return query.Where(entry => entry.AccountTypeName == "Student Support");
        }
        
        public static IQueryable<TimetableUserEntry> WhereIsNotStudentSupport(this IQueryable<TimetableUserEntry> query)
        {
            return query.Where(entry => entry.AccountTypeName != "Student Support");
        }

        public static IQueryable<TimetableUserEntry> WhereIsStudentActive(this IQueryable<TimetableUserEntry> query)
        {
            return query.Where(entry => entry.StudentStatusDescription == "Active");
        }
    }

    public struct AcademicYear
    {
        public bool Equals(AcademicYear other)
        {
            return StartYear == other.StartYear;
        }

        public override bool Equals(object obj)
        {
            return obj is AcademicYear other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StartYear;
        }

        public readonly int StartYear;
        public int EndYear => StartYear + 1;

        public AcademicYear(string stringRepresentation)
        {
            if (stringRepresentation.Length != 6)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stringRepresentation),
                    "stringRepresentation must be 6 characters long"
                );
            }

            // The first 4 characters represent the starting year
            StartYear = int.Parse(stringRepresentation.Remove(4));

            // Remove 3rd and 4th character to get the second year
            int lastYear = int.Parse(stringRepresentation.Remove(2, 2));

            if (lastYear - StartYear != 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stringRepresentation),
                    "Deduced start and end years are not 1 year apart"
                );
            }
        }

        public override string ToString()
        {
            return StartYear + EndYear.ToString().Remove(0, 2);
        }

        public static bool operator ==(AcademicYear lhs, AcademicYear rhs)
        {
            return lhs.StartYear == rhs.StartYear;
        }

        public static bool operator !=(AcademicYear lhs, AcademicYear rhs)
        {
            return !(lhs == rhs);
        }
    }

    public class AuthSession
    {
        [Key]
        public Guid Guid { get; set; }

        public int? UserId { get; set; }

        public string UserEmail { get; set; }

        public DateTime ExpiresAt { get; set; }
    }

    public class AuthToken
    {
        [Key]
        public Guid Guid { get; set; }

        public Guid SessionGuid { get; set; }

        public string UserHostAddress { get; set; }
    }
}