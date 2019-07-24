using System;
using System.Net.Mail;
using JetBrains.Annotations;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.Emails
{
    public class StudentMailerJob
    {
        private readonly SmtpClient _smtpClient;
        private readonly VotingDbContext _db;
        private readonly TimetableUserRepository _timetableUserRepository;

        public StudentMailerJob(SmtpClient smtpClient, VotingDbContext db, TimetableUserRepository timetableUserRepository)
        {
            _smtpClient = smtpClient;
            _db = db;
            _timetableUserRepository = timetableUserRepository;
        }

        [UsedImplicitly] // Hangfire
        public StudentMailerJob() : this(new SmtpClient(), new VotingDbContext(), new TimetableUserRepository())
        {
        }

        public void SendSingleEmail(int emailDefinitionId, string recipientUserId)
        {
            EmailDefinition definition = _db.EmailDefinitions.Find(emailDefinitionId);
            TimetableUserEntry user = _timetableUserRepository.GetByUsername(recipientUserId);

            if (definition == null)
            {
                throw new ArgumentOutOfRangeException(nameof(emailDefinitionId), "No email definition with such id");
            }

            if (user == null)
            {
                throw new ArgumentOutOfRangeException(nameof(user), "No timetable user entry with such id");
            }
            
            MailMessage message = new MailMessage
            {
                Subject = definition.Subject,
                Body = definition.Body,
                IsBodyHtml = true,
                From = EmailHelpers.DefaultSender,
                To = {new MailAddress(user.InternalEmail, user.Fullname)},
            };

            _smtpClient.Send(message);
        }
    }
}