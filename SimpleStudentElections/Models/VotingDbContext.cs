using System.Data.Entity;

namespace SimpleStudentElections.Models
{
    public class VotingDbContext : DbContext
    {
        public VotingDbContext() : base("Voting")
        {
        }

        public DbSet<Election> Elections { get; set; }
        public DbSet<CouncilElectionData> CouncilElectionData { get; set; }

        public DbSet<NominationPhase> NominationsPhases { get; set; }
        public DbSet<VotingPhase> VotingPhases { get; set; }

        public DbSet<ElectionEligibilityEntry> ElectionEligibilityEntries { get; set; }
        public DbSet<PositionEligibilityEntry> PositionEligibilityEntries { get; set; }

        public DbSet<NominationEntry> NominationEntries { get; set; }
        public DbSet<VotablePosition> VotablePositions { get; set; }
        public DbSet<RepresentativePositionData> RepresentativePositionData { get; set; }

        public DbSet<Vote> Votes { get; set; }
        public DbSet<StudentVoteRecord> StudentVoteRecords { get; set; }

        public DbSet<AdminActionRecord> AdminActionRecords { get; set; }
        public DbSet<ElectionStateChange> ElectionStateChanges { get; set; }
        public DbSet<ScheduledJobInfo> ScheduledJobInfos { get; set; }

        public DbSet<EmailDefinition> EmailDefinitions { get; set; }
        public DbSet<StudentSupportEmail> StudentSupportEmails { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CouncilElectionData>()
                .HasRequired(data => data.Election)
                .WithRequiredDependent()
                .WillCascadeOnDelete();

            modelBuilder.Entity<RepresentativePositionData>()
                .HasRequired(data => data.PositionCommon)
                .WithRequiredDependent()
                .WillCascadeOnDelete();
        }
    }
}