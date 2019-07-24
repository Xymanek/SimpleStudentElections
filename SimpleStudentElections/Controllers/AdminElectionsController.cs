using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Helpers;
using System.Web.Mvc;
using AutoMapper;
using Hangfire;
using JetBrains.Annotations;
using Microsoft.AspNet.Identity;
using SimpleStudentElections.Auth;
using SimpleStudentElections.Helpers;
using SimpleStudentElections.Logic;
using SimpleStudentElections.Logic.ElectionMaintenanceJobs;
using SimpleStudentElections.Logic.Emails;
using SimpleStudentElections.Models;
using SimpleStudentElections.Models.Forms;

namespace SimpleStudentElections.Controllers
{
    /// <summary>
    /// This controller holds all admin actions except for the eligibility debugger
    /// </summary>
    [Authorize(Roles = Roles.StudentSupport)]
    public class AdminElectionsController : Controller
    {
        private readonly VotingDbContext db = new VotingDbContext();

        public ActionResult Current()
        {
            IList<Election> elections = db.Elections
                .Where(election => !ElectionLifecycleInfo.ArchivedStates.Contains(election.State))
                .ToList();

            return View(elections);
        }

        public ActionResult Details(int id)
        {
            Election election = db.Elections.Find(id);
            if (election == null) return HttpNotFound();

            DetailsData data = new DetailsData
            {
                Common = election,
                NomineeEntries = new NomineeFetcher(new TimetableUserRepository()).Fetch(election.Positions)
            };

            if (election.Type == ElectionType.StudentCouncil)
            {
                data.CouncilElectionData = db.CouncilElectionData
                    .First(councilData => councilData.ElectionId == id);
            }

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (election.State)
            {
                case ElectionState.Disabled:
                {
                    DetailsData.Action action = new DetailsData.Action()
                    {
                        Title = "Activate",
                        Url = Url.Action("Activate", new {id}),
                        CssType = "success"
                    };

                    SimpleValidationResult validationResult = ElectionLifecycleInfo.CanActivate(election);

                    if (!validationResult.IsNoErrors())
                    {
                        action.Enabled = false;
                        action.Tooltip = validationResult.Violations[0].HumanError;
                    }
                    else
                    {
                        action.Enabled = true;
                    }

                    data.Actions.Add(action);
                    break;
                }

                case ElectionState.PreNominations:
                {
                    DetailsData.Action action = new DetailsData.Action()
                    {
                        Title = "Deactivate",
                        Url = Url.Action("Deactivate", new {id}),
                        CssType = "warning",
                        Enabled = true,
                        Tooltip = "Go back to deactivated state"
                    };

                    data.Actions.Add(action);
                    break;
                }

                case ElectionState.Closed:
                {
                    DetailsData.Action action = new DetailsData.Action()
                    {
                        Title = "Publish results",
                        Url = Url.Action("PublishResults", new {id}),
                        CssType = "success",
                        Enabled = true
                    };

                    data.Actions.Add(action);
                    break;
                }
            }

            if (election.State == ElectionState.Aborted)
            {
                data.Actions.Add(new DetailsData.Action()
                {
                    Title = "Remove from system",
                    Enabled = true,
                    Url = Url.Action("Delete", new {id}),
                    CssType = "danger",
                });
            }
            else
            {
                if (ElectionLifecycleInfo.CanEdit(election))
                {
                    data.Actions.Add(new DetailsData.Action()
                    {
                        Title = "Edit",
                        Enabled = true,
                        Url = Url.Action("Edit", new {id}),
                        CssType = "primary"
                    });
                }

                // Should be last
                data.Actions.Add(new DetailsData.Action()
                {
                    Title = "Abort",
                    Enabled = true,
                    Url = Url.Action("Abort", new {id}),
                    CssType = "danger"
                });
            }

            return View(data);
        }

        public ActionResult SelectNewType()
        {
            // This is just a static template
            return View();
        }

        public ActionResult NewCouncil(int numRoles = 6)
        {
            ViewData[FormConstants.FieldsInfoKey] = NewCouncilFieldsInfo;
            CouncilElectionForm form = new CouncilElectionForm
            {
                Roles = DefaultCouncilRoles
                    .GenerateRoles(numRoles)
                    .Select(role => new CouncilRoleForm() {Name = role})
                    .ToList()
            };

            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NewCouncil(CouncilElectionForm form)
        {
            ModelFieldsAccessibility fieldsInfo = NewCouncilFieldsInfo;

            // Ignore stuff that isn't supposed to be in new election
            fieldsInfo.ReplaceUneditableWithOldValues(form, new CouncilElectionForm());
            this.RemoveIgnoredErrors(fieldsInfo);

            if (ModelState.IsValid)
            {
                Election election = Mapper.Map<Election>(form);
                election.Type = ElectionType.StudentCouncil;

                CouncilElectionData councilData = Mapper.Map<CouncilElectionData>(form);
                councilData.Election = election;

                ElectionStateChange createChangeInfo = new ElectionStateChange
                {
                    Election = election,
                    PreviousState = null,
                    TargetState = election.State,
                    IsCausedByUser = true,
                    InstigatorUsername = User.Identity.GetUserId(),
                    CompletedAt = DateTime.Now
                };

                db.Elections.Add(election);
                db.CouncilElectionData.Add(councilData);
                db.ElectionStateChanges.Add(createChangeInfo);

                using (DbContextTransaction transaction = db.Database.BeginTransaction())
                {
                    db.SaveChanges();

                    // This needs to be after the first big transaction - otherwise EF gets confused about order of actions 
                    election.PopulateAutomaticStateTransitions();
                    db.SaveChanges();

                    transaction.Commit();
                }

                AuditLogManager.RecordNewElection(createChangeInfo);

                return RedirectToAction("Details", new {id = election.Id});
            }

            ViewData[FormConstants.FieldsInfoKey] = fieldsInfo;

            return View(form);
        }

        private static ModelFieldsAccessibility NewCouncilFieldsInfo
        {
            get
            {
                ModelFieldsAccessibility fieldsInfo =
                    CouncilElectionForm.DefaultCouncilFieldsInfo(ModelFieldsAccessibility.Kind.Editable);

                if (!ElectionLifecycleInfo.ShowResultsAdmin(null))
                {
                    fieldsInfo.MarkNotShown(nameof(ElectionForm.ResultsText));
                }

                return fieldsInfo;
            }
        }

        [HttpGet]
        public ActionResult NewCourseRep()
        {
            ViewData[FormConstants.FieldsInfoKey] = NewDefaultFieldsInfo;

            return View(new ElectionForm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NewCourseRep(ElectionForm form)
        {
            ModelFieldsAccessibility fieldsInfo = NewDefaultFieldsInfo;

            // Ignore stuff that isn't supposed to be in new election
            fieldsInfo.ReplaceUneditableWithOldValues(form, new ElectionForm());
            this.RemoveIgnoredErrors(fieldsInfo);

            if (ModelState.IsValid)
            {
                Election election = Mapper.Map<Election>(form);
                election.Type = ElectionType.CourseRepresentative;
                election.PositionGenerationInProcess = true;

                ElectionStateChange createChangeInfo = new ElectionStateChange
                {
                    Election = election,
                    PreviousState = null,
                    TargetState = election.State,
                    IsCausedByUser = true,
                    InstigatorUsername = User.Identity.GetUserId(),
                    CompletedAt = DateTime.Now
                };

                db.Elections.Add(election);
                db.ElectionStateChanges.Add(createChangeInfo);

                using (DbContextTransaction transaction = db.Database.BeginTransaction())
                {
                    db.SaveChanges();

                    // This needs to be after the first big transaction - otherwise EF gets confused about order of actions 
                    election.PopulateAutomaticStateTransitions();
                    db.SaveChanges();

                    transaction.Commit();
                }

                AuditLogManager.RecordNewElection(createChangeInfo);

                // Schedule the job to loop over the DB to generate the positions
                BackgroundJob.Enqueue(() => GeneratePositions.Execute(election.Id, JobCancellationToken.Null));

                return RedirectToAction("Details", new {id = election.Id});
            }

            ViewData[FormConstants.FieldsInfoKey] = fieldsInfo;

            return View(form);
        }

        private static ModelFieldsAccessibility NewDefaultFieldsInfo
        {
            get
            {
                ModelFieldsAccessibility fieldsInfo =
                    ElectionForm.DefaultFieldsInfo(ModelFieldsAccessibility.Kind.Editable);

                if (!ElectionLifecycleInfo.ShowResultsAdmin(null))
                {
                    fieldsInfo.MarkNotShown(nameof(ElectionForm.ResultsText));
                }

                return fieldsInfo;
            }
        }

        [HttpPost]
        public ActionResult RegeneratePositions(int id)
        {
            Election election = db.Elections.Find(id);
            if (election == null) return HttpNotFound();

            if (ElectionLifecycleInfo.CanForcePositionGeneration(election))
            {
                using (DbContextTransaction transaction = db.Database.BeginTransaction())
                {
                    election.PositionGenerationInProcess = true;
                    db.SaveChanges();

                    // Undo the PositionGenerationInProcess change if this fails - otherwise the election will be stuck in limbo forever
                    BackgroundJob.Enqueue(() => GeneratePositions.Execute(election.Id, JobCancellationToken.Null));

                    transaction.Commit();
                }
            }

            return RedirectToAction("Details", new {id});
        }

        public ActionResult Activate(int id)
        {
            Election election = db.Elections.Find(id);
            if (election == null) return HttpNotFound();

            if (!ElectionLifecycleInfo.CanActivate(election))
            {
                // No idea how we got here... History?
                return RedirectToAction("Details", new {id});
            }

            if (Request.HttpMethod.ToUpper() != "POST")
            {
                // Just show the template
                return View(election);
            }

            AntiForgery.Validate();

            ChangeStateByUserAndRecord(election, ElectionState.PreNominations);
            db.SaveChanges();

            BackgroundJob.Enqueue<SynchronizeDelayedJobsJob>(job => job.Execute(election.Id));
            BackgroundJob.Enqueue(() => GenerateEligibilityEntries.Execute(election.Id, JobCancellationToken.Null));

            return RedirectToAction("Details", new {id});
        }

        public ActionResult Deactivate(int id)
        {
            Election election = db.Elections.Find(id);
            if (election == null) return HttpNotFound();

            if (!ElectionLifecycleInfo.CanDeactivate(election))
            {
                // No idea how we got here... History?
                return RedirectToAction("Details", new {id});
            }

            if (Request.HttpMethod.ToUpper() != "POST")
            {
                // Just show the template
                return View(election);
            }

            AntiForgery.Validate();

            ChangeStateByUserAndRecord(election, ElectionState.Disabled);
            db.SaveChanges();

            BackgroundJob.Enqueue<SynchronizeDelayedJobsJob>(job => job.Execute(election.Id));

            return RedirectToAction("Details", new {id});
        }

        public ActionResult Edit(int id)
        {
            Election election = db.Elections.Find(id);
            if (election == null) return HttpNotFound();

            CouncilElectionData councilData = null;
            CouncilElectionForm councilForm = null;
            ElectionForm form;

            if (election.Type == ElectionType.StudentCouncil)
            {
                councilData = db.CouncilElectionData.First(data => data.ElectionId == election.Id);
                form = councilForm = GenerateFormForCouncil(election, councilData);
            }
            else
            {
                form = GenerateFormForCourseRep(election);
            }

            ModelFieldsAccessibility fieldsInfo = ElectionLifecycleInfo.GetWhatCanBeEditedCouncil(election);

            ViewData[FormConstants.FieldsInfoKey] = fieldsInfo;
            ViewBag.Election = election;

            fieldsInfo.EnsureAllowedDefaultKind(
                ModelFieldsAccessibility.Kind.Editable,
                nameof(AdminElectionsController) + "." + nameof(Edit)
            );

            if (Request.HttpMethod.ToUpper() != "POST")
            {
                // Just show the template
                return View("Edit", form);
            }

            AntiForgery.Validate();

            // Update the form based on data that we received
            // ReSharper disable once ConvertIfStatementToNullCoalescingExpression - we need the compiler to specify different generic arguments
            if (councilForm != null)
            {
                TryUpdateModel(councilForm);
            }
            else
            {
                TryUpdateModel(form);
            }

            // Get the original form so that we use old values for uneditable fields
            CouncilElectionForm councilOriginalForm = null;
            ElectionForm originalForm;

            if (councilForm != null)
            {
                originalForm = councilOriginalForm = GenerateFormForCouncil(election, councilData);
            }
            else
            {
                originalForm = GenerateFormForCourseRep(election);
            }

            // Replace all uneditable values with old ones
            fieldsInfo.ReplaceUneditableWithOldValues(form, originalForm);

            // As the role IDs are sent from user, we need to make sure that they weren't changed
            if (councilForm != null && fieldsInfo.CanBeChangedByUser(nameof(CouncilElectionForm.Roles)))
            {
                IEnumerable<int?> initialRoleIds = councilOriginalForm.Roles.Select(role => role.Id);
                IEnumerable<int?> newRoleIds = councilForm.Roles.Select(role => role.Id);

                if (!initialRoleIds.SequenceEqual(newRoleIds))
                {
                    throw new Exception("The IDs of roles were changed by user input");
                }
            }

            // Validate again (since some rules are relative to other fields and can be affected by operations above)
            TryValidateModel(form);

            // Ignore the failures from uneditable fields
            this.RemoveIgnoredErrors(fieldsInfo);

            if (!ModelState.IsValid)
            {
                // The validation failed so we just display the form again
                return View("Edit", form);
            }

            // Record the admin action
            AdminActionRecord actionRecord = CreateActionRecord(election, AdminActionRecord.RecordType.Edit);
            actionRecord.SetFormChangeSet(FormChangeSet.Generate(form, originalForm));
            db.AdminActionRecords.Add(actionRecord);

            // Validation passed with the fields that are allowed to change. Persist the changes
            Mapper.Map(form, election);
            if (councilData != null) Mapper.Map(form, councilData);

            db.SaveChanges();

            BackgroundJob.Enqueue<SynchronizeDelayedJobsJob>(job => job.Execute(election.Id));
            AuditLogManager.RecordElectionEdit(User, election);

            return RedirectToAction("Details", new {id});
        }

        private AdminActionRecord CreateActionRecord(Election election, AdminActionRecord.RecordType type)
        {
            return new AdminActionRecord(election, User.Identity.GetUserId(), type);
        }

        private static CouncilElectionForm GenerateFormForCouncil(Election election, CouncilElectionData councilData)
        {
            // Construct the form using the base election
            CouncilElectionForm form = Mapper.Map<CouncilElectionForm>(election);

            // Add the council-specific data
            Mapper.Map(councilData, form);

            return form;
        }

        private static ElectionForm GenerateFormForCourseRep(Election election)
        {
            return Mapper.Map<ElectionForm>(election);
        }

        public ActionResult PublishResults(int id)
        {
            Election election = db.Elections.Find(id);
            if (election == null) return HttpNotFound();

            bool canDo = ElectionLifecycleInfo.CanPublishResults(election);
            ViewBag.CanConfirm = canDo;

            if (Request.HttpMethod.ToUpper() != "POST")
            {
                // Just show the template
                return View(election);
            }

            if (!canDo)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            AntiForgery.Validate();

            ChangeStateByUserAndRecord(election, ElectionState.ResultsPublished);
            db.SaveChanges();

            BackgroundJob.Enqueue<ElectionStudentSimpleEmailsJobs>(
                jobs => jobs.SendResultsPublished(election.Id, JobCancellationToken.Null)
            );

            return RedirectToAction("Details", new {id});
        }

        public ActionResult Abort(int id)
        {
            Election election = db.Elections.Find(id);
            if (election == null) return HttpNotFound();

            if (Request.HttpMethod.ToUpper() != "POST")
            {
                // Just show the template
                return View(election);
            }

            AntiForgery.Validate();

            ChangeStateByUserAndRecord(election, ElectionState.Aborted);
            db.SaveChanges();

            // This will cancel any scheduled state changes
            BackgroundJob.Enqueue<SynchronizeDelayedJobsJob>(job => job.Execute(election.Id));

            return RedirectToAction("Details", new {id});
        }

        public ActionResult Delete(int id)
        {
            Election election = db.Elections.Find(id);
            if (election == null) return HttpNotFound();

            bool canDo = ElectionLifecycleInfo.CanDelete(election);
            ViewBag.CanConfirm = canDo;

            if (Request.HttpMethod.ToUpper() != "POST")
            {
                // Just show the template
                return View(election);
            }

            if (!canDo)
            {
                // Just reload the page
                return RedirectToAction("Delete", new {id});
            }

            AntiForgery.Validate();

            using (DbContextTransaction transaction = db.Database.BeginTransaction())
            {
                // Need to cleanup transitions that have explicit references
                // as otherwise we have cascade cycles and everything spazzes out
                election.RemoveAutomaticStateTransitions(db);
                db.SaveChanges();

                db.Elections.Remove(election);
                db.SaveChanges();

                transaction.Commit();
            }

            return RedirectToAction("Current");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ViewVotes(int id)
        {
            Election election = db.Elections.Find(id);
            if (election == null) return HttpNotFound();

            db.AdminActionRecords.Add(
                CreateActionRecord(election, AdminActionRecord.RecordType.ViewVotes)
            );
            db.SaveChanges();

            AuditLogManager.RecordViewVotes(User, election);

            return View(new ViewVotesData(
                election,
                new NomineeFetcher(new TimetableUserRepository()).Fetch(election.Positions))
            );
        }

        [HttpPost]
        public ActionResult VotesDetails(int id)
        {
            Election election = db.Elections.Find(id);
            if (election == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            List<DetailedVoteData> votesData = db.Votes
                .Select(vote => new DetailedVoteData
                {
                    Position = vote.NominationEntry.Position.HumanName,
                    NomineeId = vote.NominationEntry.Username,
                    VotedAt = vote.VotedAt
                })
                .ToList();

            foreach (DetailedVoteData voteData in votesData)
            {
                voteData.VotedAtText = voteData.VotedAt.ToString("dd/MM/yyyy HH:mm");
                voteData.VotedAtSort = ((DateTimeOffset) voteData.VotedAt).ToUnixTimeSeconds();
            }

            IDictionary<string, TimetableUserEntry> users = GetTimetableUsers(votesData.Select(data => data.NomineeId));

            foreach (DetailedVoteData data in votesData)
            {
                if (users.TryGetValue(data.NomineeId, out TimetableUserEntry userEntry))
                {
                    data.NomineeName = userEntry.Fullname;
                }
            }

            return Json(new
            {
                success = true,
                data = votesData
            });
        }

        public ActionResult EventLog(int id)
        {
            Election election = db.Elections.Find(id);
            if (election == null) return HttpNotFound();

            List<EventLogEntry> entries = new List<EventLogEntry>();

            entries.AddRange(
                db.AdminActionRecords
                    .Where(record => record.ElectionId == election.Id)
                    .AsEnumerable()
                    .Select(record =>
                    {
                        EventLogEntry entry = new EventLogEntry
                        {
                            User = record.UserId,
                            OccuredAt = record.OccurredAt,
                        };

                        switch (record.Type)
                        {
                            case AdminActionRecord.RecordType.ViewVotes:
                                entry.Text = "Viewed votes";
                                break;

                            case AdminActionRecord.RecordType.Edit:
                                FormChangeSet<ElectionForm> changeSet =
                                    record.GetChangeSet<ElectionForm>();
                                Debug.Assert(changeSet != null, nameof(changeSet) + " != null");

                                entry.Text = "Edited election properties. Changed fields:"
                                             + FormChangeSetPrinter.GenerateHtmlList(changeSet.Delta);
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        return entry;
                    })
            );

            entries.AddRange(
                db.ElectionStateChanges
                    .Where(change => change.ElectionId == election.Id && change.CompletedAt != null)
                    .AsEnumerable()
                    .Select(change => new EventLogEntry
                    {
                        // ReSharper disable once PossibleInvalidOperationException
                        OccuredAt = (DateTime) change.CompletedAt,
                        User = change.InstigatorUsername,
                        Text = change.PreviousState.HasValue
                            ? "State changed from " + change.PreviousState.ToString() + " to " +
                              change.TargetState.ToString()
                            : "Election created"
                    })
            );

            IDictionary<string, TimetableUserEntry> users = GetTimetableUsers(
                entries.Select(data => data.User).Where(userid => !string.IsNullOrEmpty(userid))
            );
            foreach (EventLogEntry entry in entries)
            {
                if (entry.User != null && users.TryGetValue(entry.User, out TimetableUserEntry userEntry))
                {
                    entry.User = userEntry.Fullname;
                }
            }

            return View(new EventLogData
            {
                Election = election,
                Entries = entries.OrderByDescending(entry => entry.OccuredAt).ToArray()
            });
        }

        private static IDictionary<string, TimetableUserEntry> GetTimetableUsers(IEnumerable<string> userIds)
        {
            TimetableUserRepository userRepository = new TimetableUserRepository();

            Dictionary<string, TimetableUserEntry> users = userIds
                .Distinct()
                .Select(userRepository.GetByUsername)
                .Where(entry => entry != null)
                .ToDictionary(entry => entry.UserId);

            return users;
        }

        [HttpGet]
        public ActionResult Archived()
        {
            List<Election> elections = db.Elections
                .Where(election => ElectionLifecycleInfo.ArchivedStates.Contains(election.State))
                .ToList();

            return View(elections);
        }

        [HttpGet]
        public ActionResult Help()
        {
            return View();
        }

        private void ChangeStateByUserAndRecord(Election election, ElectionState newState)
        {
            ElectionStateChange stateChange = new ElectionStateChange()
            {
                Election = election,
                PreviousState = election.State,
                TargetState = newState,
                IsCausedByUser = true,
                InstigatorUsername = User.Identity.GetUserId(),
                CompletedAt = DateTime.Now
            };

            db.ElectionStateChanges.Add(stateChange);
            election.State = newState;

            AuditLogManager.RecordElectionStateChange(stateChange);
        }

        public class DetailsData
        {
            public Election Common;
            public CouncilElectionData CouncilElectionData;
            public readonly IList<Action> Actions = new List<Action>();
            public IDictionary<VotablePosition, ISet<DisplayNomineeEntry>> NomineeEntries;

            public class Action
            {
                public string Title;
                public string Url;
                public bool Enabled;

                /// <summary>
                /// Bootstrap class that will be appended to "btn-". So "success" will create "btn-success"
                /// </summary>
                public string CssType;

                public string Tooltip;
            }
        }

        public class ViewVotesData
        {
            [NotNull] public readonly Election Election;
            [NotNull] public readonly IDictionary<VotablePosition, ISet<DisplayNomineeEntry>> NomineeEntries;

            public ViewVotesData(
                [NotNull] Election election,
                [NotNull] IDictionary<VotablePosition, ISet<DisplayNomineeEntry>> nomineeEntries
            )
            {
                Election = election;
                NomineeEntries = nomineeEntries;
            }
        }

        private class DetailedVoteData
        {
            public string NomineeId { get; set; }
            public string NomineeName { get; set; }
            public string Position { get; set; }
            public DateTime VotedAt { get; set; }
            public string VotedAtText { get; set; }
            public long VotedAtSort { get; set; }
        }

        public class EventLogData
        {
            public Election Election { get; set; }
            public EventLogEntry[] Entries { get; set; }
        }

        public class EventLogEntry
        {
            public DateTime OccuredAt { get; set; }
            public string Text { get; set; }
            public string User { get; set; }
        }
    }
}