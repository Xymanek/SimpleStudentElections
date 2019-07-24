using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using JetBrains.Annotations;
using SimpleStudentElections.Auth;
using SimpleStudentElections.Controllers.EligibilityDebuggerHelpers;
using SimpleStudentElections.Helpers;
using SimpleStudentElections.Logic;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Controllers
{
    /// <summary>
    /// Eligibility debugger - allows admins to view and modify eligibility entries
    /// </summary>
    [Authorize(Roles = Roles.StudentSupport)]
    public class EligibilityDebuggerController : Controller
    {
        private readonly VotingDbContext _db = new VotingDbContext();
        private readonly TimetableUserRepository _userRepository = new TimetableUserRepository();

        #region Election

        public ActionResult Election(int id)
        {
            Election election = _db.Elections.Find(id);
            if (election == null) return HttpNotFound();

            return View(election);
        }

        public ActionResult ElectionData(int id)
        {
            Election election = _db.Elections.Find(id);
            if (election == null) return HttpNotFound();

            List<ElectionTableRow> rows = election.EligibilityEntries
                .Select(entry => new ElectionTableRow {UserId = entry.Username})
                .ToList();

            PopulateUserInfo(rows);

            return new JsonResult
            {
                Data = new
                {
                    success = true,
                    data = rows
                },
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpPost]
        public ActionResult NewElectionEntry(NewElectionEntry model)
        {
            if (!ModelState.IsValid) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // STEP 1: Fetch everything we got as IDs

            Election election = _db.Elections.Find(model.ElectionId);
            TimetableUserEntry user = _userRepository.GetByUsername(model.UserId);

            // STEP 2: Check that everything is correct

            List<string> errors = new List<string>();
            bool suggestReload = false;

            if (election == null)
            {
                errors.Add("Failed to find election");
                suggestReload = true;
            }

            if (user == null)
            {
                errors.Add("Invalid student ID");
            }

            if (user != null && !user.IsStudentActive)
            {
                errors.Add("Only active students can participate in elections");
            }

            if (errors.Count > 0)
            {
                ViewBag.SuggestReload = suggestReload;
                string errorHtml = PartialView("_ErrorsDisplay", errors).RenderToString();

                return Json(new
                {
                    Success = false,
                    HumanErrorHtml = errorHtml
                });
            }

            // STEP 3: Check that there isn't an entry already for this user

            // ReSharper disable PossibleNullReferenceException
            if (_db.ElectionEligibilityEntries.Find(user.UserId, election.Id) != null)
            {
                return Json(new
                {
                    Success = false,
                    HumanError = "Error: there is already an entry for this user"
                });
            }

            // STEP 4: Create a new entity and save it

            ElectionEligibilityEntry newEntry = new ElectionEligibilityEntry()
            {
                Username = model.UserId,
                Election = election
            };

            _db.ElectionEligibilityEntries.Add(newEntry);
            _db.SaveChanges();

            // STEP 5: Prepare the displayed table row

            ElectionTableRow tableRow = new ElectionTableRow {UserId = user.UserId};
            PopulateUserInfo(tableRow, user);

            // STEP 6: Reply that we are done

            return Json(new
            {
                Success = true,
                Entry = tableRow
            });
        }

        [HttpPost]
        public ActionResult RemoveElectionEntries(int electionId, RemovalModel<ElectionRemovalEntry> model)
        {
            if (!ModelState.IsValid) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            List<ElectionEligibilityEntry> entries = model.Entries
                .Select(entry => _db.ElectionEligibilityEntries.Find(entry.Username, electionId))
                .ToList();

            if (entries.Any(entry => entry == null))
            {
                return Json(new
                {
                    Success = false,
                    HumanError = "Not all entries were found. Please refresh the page and try again"
                });
            }

            entries.ForEach(entry => _db.ElectionEligibilityEntries.Remove(entry));
            _db.SaveChanges();

            return Json(new {Success = true});
        }

        #endregion

        #region Positions

        public ActionResult Positions(int electionId)
        {
            Election election = _db.Elections.Find(electionId);
            if (election == null) return HttpNotFound();

            return View(election);
        }

        public ActionResult PositionsData(int electionId)
        {
            Election election = _db.Elections.Find(electionId);
            if (election == null) return HttpNotFound();

            List<PositionsTableRow> rows = _db.PositionEligibilityEntries
                .Where(entry => entry.Position.ElectionId == electionId)
                .Include(entry => entry.Position)
                .AsEnumerable() // Materialize the query
                .Select(entry => new PositionsTableRow(entry))
                .ToList();

            PopulateUserInfo(rows);

            return new JsonResult
            {
                Data = new
                {
                    success = true,
                    data = rows
                },
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpPost]
        public ActionResult NewPositionEntry(NewPositionEntry model)
        {
            if (!ModelState.IsValid) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // STEP 1: Fetch everything we got as IDs

            Election election = _db.Elections.Find(model.ElectionId);
            VotablePosition position = _db.VotablePositions.Find(model.PositionId);
            TimetableUserEntry user = _userRepository.GetByUsername(model.UserId);

            // STEP 2: Check that everything is correct

            List<string> errors = new List<string>();
            bool suggestReload = false;

            if (election == null)
            {
                errors.Add("Failed to find election");
                suggestReload = true;
            }

            if (position == null)
            {
                errors.Add("Failed to find the selected position");
                suggestReload = true;
            }

            if (election != null && position != null && position.ElectionId != election.Id)
            {
                errors.Add("The selected position doesn't belong to current election");
                suggestReload = true;
            }

            if (user == null)
            {
                errors.Add("Invalid student ID");
            }

            if (user != null && !user.IsStudentActive)
            {
                errors.Add("Only active students can participate in elections");
            }

            if (errors.Count > 0)
            {
                ViewBag.SuggestReload = suggestReload;
                string errorHtml = PartialView("_ErrorsDisplay", errors).RenderToString();

                return Json(new
                {
                    Success = false,
                    HumanErrorHtml = errorHtml
                });
            }

            // STEP 3: Check that there isn't an entry already for this user-position tuple

            bool isDuplicateEntry = _db.PositionEligibilityEntries
                .Any(entry => entry.PositionId == position.Id && entry.Username == user.UserId);

            if (isDuplicateEntry)
            {
                return Json(new
                {
                    Success = false,
                    HumanError =
                        "Error: there is already an entry for this user-position tuple. Please edit that instead"
                });
            }

            // STEP 4: Create a new entity and save it

            PositionEligibilityEntry newEntry = new PositionEligibilityEntry()
            {
                Username = model.UserId,
                Position = position,

                CanNominate = model.CanNominate,
                CanVote = model.CanVote
            };

            _db.PositionEligibilityEntries.Add(newEntry);
            _db.SaveChanges();

            // STEP 5: Prepare the displayed table row

            PositionsTableRow tableRow = new PositionsTableRow(newEntry);
            PopulateUserInfo(tableRow, user);

            // STEP 6: Reply that we are done

            return Json(new
            {
                Success = true,
                Entry = tableRow
            });
        }

        [HttpPost]
        public ActionResult EditPositionEntry(PositionEntryEdit model)
        {
            if (!ModelState.IsValid) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            PositionEligibilityEntry entry = _db.PositionEligibilityEntries.Find(model.UserId, model.PositionId);
            if (entry == null)
            {
                return Json(new
                {
                    Success = false,
                    HumanError = "Error: record wasn't found. Please reload the page and try again"
                });
            }

            entry.CanNominate = model.CanNominate;
            entry.CanVote = model.CanVote;

            _db.SaveChanges();

            PositionsTableRow tableRow = new PositionsTableRow(entry);
            PopulateUserInfo(tableRow);

            return Json(new
            {
                Success = true,
                Entry = tableRow
            });
        }

        [HttpPost]
        public ActionResult RemovePositionEntries(RemovalModel<PositionRemovalEntry> model)
        {
            if (!ModelState.IsValid) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            List<PositionEligibilityEntry> entries = model.Entries
                .Select(entry => _db.PositionEligibilityEntries.Find(entry.Username, entry.PositionId))
                .ToList();

            if (entries.Any(entry => entry == null))
            {
                return Json(new
                {
                    Success = false,
                    HumanError = "Not all entries were found. Please refresh the page and try again"
                });
            }

            entries.ForEach(entry => _db.PositionEligibilityEntries.Remove(entry));
            _db.SaveChanges();

            return Json(new {Success = true});
        }

        #endregion

        private void PopulateUserInfo([InstantHandle] IEnumerable<ITableRow> rows)
        {
            // Note: right now this will fetch every user record one-by-one
            foreach (ITableRow row in rows)
            {
                PopulateUserInfo(row);
            }
        }

        private void PopulateUserInfo(ITableRow row)
        {
            TimetableUserEntry userEntry = _userRepository.GetByUsername(row.UserId);

            if (userEntry != null)
            {
                PopulateUserInfo(row, userEntry);
            }
        }

        private static void PopulateUserInfo(ITableRow row, TimetableUserEntry userEntry)
        {
            row.UserFullName = userEntry.Fullname;
        }
    }
}