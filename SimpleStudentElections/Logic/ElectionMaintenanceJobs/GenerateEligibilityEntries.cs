using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Hangfire;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.ElectionMaintenanceJobs
{
    public static class GenerateEligibilityEntries
    {
        /// <summary>
        /// Generates eligibility entries for election and positions for all students in database
        /// <br />
        /// This job is idempotent and can be run multiple times on same election 
        /// <br />
        /// Since this involves iterating over the entire database of users I've allowed only 1 of those jobs
        /// to run simultaneously. Additionally running multiple instances of this job on same election simultaneously
        /// can break its idempotency and cause exceptions (most likely SQL transactions exceptions)
        /// </summary>
        /// <param name="electionId">The ID of election </param>
        /// <param name="cancellationToken"></param>
        [DisableConcurrentExecution(5 /* minutes */ * 60)]
        public static void Execute(int electionId, IJobCancellationToken cancellationToken)
        {
            using (var db = new VotingDbContext())
            {
                Election election = db.Elections.Find(electionId);
                if (election == null)
                    throw new ArgumentOutOfRangeException(nameof(electionId), "No election with such id");

                if (election.DisableAutomaticEligibility)
                {
                    // Nothing to do
                    return;
                }
                
                using (var timetable = new TimetableDbContext())
                {
                    IEnumerable<ElectionEligibilityEntry> electionEntries =
                        EligibilityGenerator.GenerateElectionEntries(election, timetable.Users);
                    cancellationToken.ThrowIfCancellationRequested();

                    IEnumerable<PositionEligibilityEntry> positionEntries =
                        EligibilityGenerator.GeneratePositionEntries(election, timetable.Users, db);
                    cancellationToken.ThrowIfCancellationRequested();

                    // Note that we cannot dispose the timetable db context here
                    // since the queries are only "planned" yet and not actually executed

                    // Save election entries, only ones that do not exist in database
                    AddNewEntries(
                        electionEntries, db.ElectionEligibilityEntries,
                        (entry, set) => set.Find(entry.Username, entry.Election.Id)
                    );

                    cancellationToken.ThrowIfCancellationRequested();

                    // Save position entries, only ones that do not exist in database
                    AddNewEntries(
                        positionEntries, db.PositionEligibilityEntries,
                        (entry, set) => set.Find(entry.Username, entry.Position.Id)
                    );

                    cancellationToken.ThrowIfCancellationRequested();
                    db.SaveChanges();
                }
            }
        }

        private static void AddNewEntries<TEntry>(
            IEnumerable<TEntry> entries,
            DbSet<TEntry> dbSet,
            Func<TEntry, DbSet<TEntry>, TEntry> existingEntryFetcher
        ) where TEntry : class
        {
            dbSet.AddRange(entries.Where(entry => existingEntryFetcher(entry, dbSet) == null));
        }
    }
}