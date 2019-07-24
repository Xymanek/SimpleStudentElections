using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using JetBrains.Annotations;
using Microsoft.AspNet.Identity;
using SimpleStudentElections.Auth;
using SimpleStudentElections.Logic;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Controllers
{
    /// <summary>
    /// Controller for all actions that are accessible to students
    /// </summary>
    [Authorize(Roles = Roles.Student)]
    public class StudentController : Controller
    {
        private static readonly ElectionState[] FutureElectionsStates =
        {
            ElectionState.PreNominations,
        };

        private static readonly ElectionState[] PreviousElectionsStates =
        {
            ElectionState.Closed,
            ElectionState.ResultsPublished,
        };

        private VotingDbContext db = new VotingDbContext();

        public ActionResult Index()
        {
            string username = User.Identity.GetUserId();

            IQueryable<Election> query = db.ElectionEligibilityEntries
                .Where(entry => entry.Username == username)
                .Select(entry => entry.Election);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (ElectionState inactiveState in ElectionLifecycleInfo.InactiveStates)
            {
                query = query.Where(election => election.State != inactiveState);
            }

            List<Election> upcomingElections = new List<Election>();
            List<Election> currentElections = new List<Election>();
            List<Election> pastElections = new List<Election>();

            foreach (Election election in query)
            {
                if (FutureElectionsStates.Contains(election.State))
                {
                    upcomingElections.Add(election);
                }
                else if (PreviousElectionsStates.Contains(election.State))
                {
                    pastElections.Add(election);
                }
                else
                {
                    currentElections.Add(election);
                }
            }

            IndexData data = new IndexData(
                upcomingElections.ToArray(),
                currentElections.ToArray(),
                pastElections.ToArray()
            );

            return View(data);
        }

        public ActionResult Nominations(int id)
        {
            Election election = db.Elections.Find(id);
            if (election == null) return HttpNotFound();

            string currentUsername = User.Identity.GetUserId();

            // ReSharper disable once SimplifyLinqExpression
            if (!election.EligibilityEntries.Any(entry => entry.Username == currentUsername))
            {
                // This student is not eligible for this election - pretend it doesn't exist
                return HttpNotFound();
            }

            List<PositionEligibilityEntry> positionEligibilityEntries = db.PositionEligibilityEntries
                .Where(entry => entry.Username == currentUsername)
                .Where(entry => entry.Position.ElectionId == election.Id)
                .ToList();

            NomineeFetcher nomineeFetcher = new NomineeFetcher(new TimetableUserRepository());
            NominationsData data = new NominationsData(
                election,
                ElectionLifecycleInfo.CanNominate(election),
                nomineeFetcher.Fetch(positionEligibilityEntries.Select(entry => entry.Position))
            );

            foreach (PositionEligibilityEntry entry in positionEligibilityEntries)
            {
                data.ElgibilePositions.Add(new NominationsData.PositionData(
                    entry.Position,
                    entry.CanNominate,
                    entry.Position.NominationEntries.Any(nomination => nomination.Username == currentUsername)
                ));
            }

            return View(data);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult UpdateNominationsStatus(int positionId, bool newStatus)
        {
            VotablePosition position = db.VotablePositions.Find(positionId);
            if (position == null) return HttpNotFound();

            Election election = position.Election;
            string currentUsername = User.Identity.GetUserId();

            // ReSharper disable once SimplifyLinqExpression
            if (!election.EligibilityEntries.Any(entry => entry.Username == currentUsername))
            {
                // This student is not eligible for this election - pretend it doesn't exist
                return HttpNotFound();
            }

            PositionEligibilityEntry positionEligibilityEntry = db.PositionEligibilityEntries
                .FirstOrDefault(entry => entry.PositionId == positionId && entry.Username == currentUsername);

            if (positionEligibilityEntry == null)
            {
                // This student is not eligible for this position - pretend it doesn't exist
                return HttpNotFound();
            }

            // At this point we are sure that the position is visible to this student
            // Now we need to check whether (s)he can change the status (at this time)

            if (!ElectionLifecycleInfo.CanNominate(election) || !positionEligibilityEntry.CanNominate)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // At this point we are sure that the student can change the nomination status for this position
            // Now we just need to create or delete the record in database (or do nothing if it's already matching)

            NominationEntry exitingNomination = position.NominationEntries
                .FirstOrDefault(entry => entry.Username == currentUsername);

            if (exitingNomination != null)
            {
                if (newStatus)
                {
                    // Do nothing
                }
                else
                {
                    // There is an existing one, remove it
                    db.NominationEntries.Remove(exitingNomination);
                    db.SaveChanges();
                }
            }
            else
            {
                if (newStatus)
                {
                    // No nomination currently, create one
                    db.NominationEntries.Add(new NominationEntry() {Position = position, Username = currentUsername});
                    db.SaveChanges();
                }
                else
                {
                    // Do nothing
                }
            }
            
            AuditLogManager.RecordNominationUpdate(User, position, newStatus);

            return RedirectToAction("Nominations", new {id = election.Id});
        }

        [HttpGet]
        public ActionResult Vote(int id /* Election */)
        {
            Election election = db.Elections.Find(id);
            if (election == null) return HttpNotFound();

            string currentUsername = User.Identity.GetUserId();

            // ReSharper disable once SimplifyLinqExpression
            if (!election.EligibilityEntries.Any(entry => entry.Username == currentUsername))
            {
                // This student is not eligible for this election - pretend it doesn't exist
                return HttpNotFound();
            }

            if (!ElectionLifecycleInfo.CanVote(election))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            return View(new VotingData(
                election,
                new VotingManager().PrepareVotingDataFor(currentUsername, election)
            ));
        }

        [HttpPost]
        public ActionResult VoteConfirmation(int nominationId)
        {
            DisplayNomineeEntry entry = GetNomineeForVoting(nominationId);
            if (entry == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            return View(entry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DoVote(int nominationId)
        {
            DisplayNomineeEntry displayNomineeEntry = GetNomineeForVoting(nominationId);
            if (displayNomineeEntry == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // Everything validated, checked, etc. Let's do it!
            NominationEntry nominationEntry = displayNomineeEntry.ModelEntry;

            // Record that the student has voted for this position
            db.StudentVoteRecords.Add(new StudentVoteRecord()
            {
                Position = nominationEntry.Position,
                Username = User.Identity.GetUserId()
            });

            // Record the vote for this candidate
            Vote vote = new Vote
            {
                NominationEntry = nominationEntry,
                VotedAt = DateTime.Now
            };

            db.Votes.Add(vote);
            db.SaveChanges();
            
            AuditLogManager.RecordVoteCast(vote);

            return RedirectToAction("Vote", new {id = nominationEntry.Position.Election.Id});
        }

        [CanBeNull]
        private DisplayNomineeEntry GetNomineeForVoting(int nominationId)
        {
            NominationEntry nominationEntry = db.NominationEntries.Find(nominationId);
            if (nominationEntry == null) return null;

            Election election = nominationEntry.Position.Election;
            VotablePosition position = nominationEntry.Position;
            string currentUsername = User.Identity.GetUserId();

            // ReSharper disable once SimplifyLinqExpression
            if (!election.EligibilityEntries.Any(entry => entry.Username == currentUsername))
            {
                // This student is not eligible for this election
                return null;
            }

            IDictionary<VotablePosition, PositionDisplayDataForVoting> positionsData = new VotingManager()
                .PrepareVotingDataFor(
                    currentUsername, election,
                    positions => positions.Where(p => p == position)
                );

            if (
                !positionsData.ContainsKey(position) ||
                positionsData[position].Status != PositionDisplayDataForVoting.PositionStatus.CanVote
            )
            {
                // Student doesn't have access to this position or cannot vote for it or has voted already
                return null;
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            return positionsData[position].NomineeEntries.First(entry => entry.ModelEntry == nominationEntry);
        }

        public class IndexData
        {
            public readonly Election[] UpcomingElections;
            public readonly Election[] CurrentElections;
            public readonly Election[] PastElections;

            public IndexData(Election[] upcomingElections, Election[] currentElections, Election[] pastElections)
            {
                UpcomingElections = upcomingElections;
                CurrentElections = currentElections;
                PastElections = pastElections;
            }
        }

        public class NominationsData
        {
            [NotNull] public readonly Election Election;
            public readonly bool AreNominationsOpen;

            public readonly List<PositionData> ElgibilePositions = new List<PositionData>();
            public readonly IDictionary<VotablePosition, ISet<DisplayNomineeEntry>> NomineeEntries;

            public NominationsData(
                [NotNull] Election election,
                bool areNominationsOpen,
                IDictionary<VotablePosition, ISet<DisplayNomineeEntry>> nomineeEntries
            )
            {
                Election = election;
                AreNominationsOpen = areNominationsOpen;
                NomineeEntries = nomineeEntries;
            }

            public class PositionData
            {
                [NotNull] public readonly VotablePosition Position;
                public readonly bool CanNominate;
                public readonly bool IsNominated;

                public PositionData([NotNull] VotablePosition position, bool canNominate, bool isNominated)
                {
                    Position = position;
                    CanNominate = canNominate;
                    IsNominated = isNominated;
                }
            }
        }

        public class VotingData
        {
            public readonly Election Election;
            public readonly IDictionary<VotablePosition, PositionDisplayDataForVoting> PositionsData;

            public VotingData(
                Election election,
                IDictionary<VotablePosition, PositionDisplayDataForVoting> positionsData
            )
            {
                Election = election;
                PositionsData = positionsData;
            }
        }
    }
}