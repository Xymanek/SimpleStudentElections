using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Hangfire;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.Emails
{
    public class StudentSupportEmailJobs : ElectionEmailJobBase
    {
        private readonly TimetableUserRepository _userRepository;

        public StudentSupportEmailJobs(VotingDbContext db, TimetableUserRepository userRepository) : base(db)
        {
            _userRepository = userRepository;
        }

        public StudentSupportEmailJobs() : this(new VotingDbContext(), new TimetableUserRepository())
        {
        }

        public void SendNominationsAlert(int electionId, IJobCancellationToken cancellationToken)
        {
            Election election = GetElection(electionId);
            int eligibleForNomination = CountEligible(election, entry => entry.CanNominate);

            int alreadyNominated = Db.NominationEntries
                .Where(entry => entry.Position.ElectionId == election.Id)
                .Select(entry => entry.Username)
                .Distinct()
                .Count();

            float participationRatio = alreadyNominated / (float) eligibleForNomination;

            // Check that we actually need to send the alert
            if (!election.Nominations.ShouldSendAlert(participationRatio)) return;

            cancellationToken.ThrowIfCancellationRequested();
            StudentSupportEmail email = GenerateAlertEmail(election, participationRatio);

            Db.StudentSupportEmails.Add(email);
            Db.SaveChanges();

            ScheduleJobsInTransaction(
                _userRepository.GetAllStudentSupport(),
                (user, jobClient) => jobClient.Enqueue<StudentSupportMailerJob>(
                    mailer => mailer.SendSingleEmail(email.Id, user.InternalEmail, user.Fullname)
                )
            );
        }

        public void SendVotingAlert(int electionId, IJobCancellationToken cancellationToken)
        {
            Election election = GetElection(electionId);
            int eligibleForVoting = CountEligible(election, entry => entry.CanVote);

            int alreadyVoted = Db.StudentVoteRecords
                .Where(record => record.Position.ElectionId == election.Id)
                .Select(record => record.Username)
                .Distinct()
                .Count();

            float participationRatio = alreadyVoted / (float) eligibleForVoting;

            // Check that we actually need to send the alert
            if (!election.Voting.ShouldSendAlert(participationRatio)) return;

            cancellationToken.ThrowIfCancellationRequested();
            StudentSupportEmail email = GenerateAlertEmail(election, participationRatio);

            Db.StudentSupportEmails.Add(email);
            Db.SaveChanges();

            ScheduleJobsInTransaction(
                _userRepository.GetAllStudentSupport(),
                (user, jobClient) => jobClient.Enqueue<StudentSupportMailerJob>(
                    mailer => mailer.SendSingleEmail(email.Id, user.InternalEmail, user.Fullname)
                )
            );
        }

        private static StudentSupportEmail GenerateAlertEmail(Election election, float participationRatio)
        {
            int participation = ElectionPhaseBase.ParticipationRatioToPercentage(participationRatio);
            int alarmThreshold = GetAlarmThreshold(election);

            StringBuilder emailBodyBuilder = new StringBuilder();

            emailBodyBuilder.Append($"Election name: {election.Name}\r\n");
            emailBodyBuilder.Append($"Election type: {election.Type}\r\n");
            emailBodyBuilder.Append($"Election phase: {election.State}\r\n");
            emailBodyBuilder.Append("\r\n");
            emailBodyBuilder.Append($"Current participation: {participation}\r\n");
            emailBodyBuilder.Append($"Alarm threshold: {alarmThreshold}\r\n");

            return new StudentSupportEmail
            {
                Subject = "Election alert - low participation",
                Body = emailBodyBuilder.ToString()
            };
        }

        private static int GetAlarmThreshold(Election election)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (election.State)
            {
                case ElectionState.Nominations:
                    return election.Nominations.AlarmThreshold;
                case ElectionState.Voting:
                    return election.Voting.AlarmThreshold;
                default:
                    throw new Exception($"Election {election.Id} not in nominations or voting state. WTF?!");
            }
        }

        private int CountEligible(Election election, Expression<Func<PositionEligibilityEntry, bool>> eligibilityType)
        {
            return Db.PositionEligibilityEntries
                .Where(entry => entry.Position.ElectionId == election.Id)
                .Where(eligibilityType)
                .Select(entry => entry.Username)
                .Distinct()
                .Count();
        }
    }
}