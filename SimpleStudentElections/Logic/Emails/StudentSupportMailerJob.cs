using System;
using System.Net.Mail;
using JetBrains.Annotations;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.Emails
{
    public class StudentSupportMailerJob
    {
        private readonly VotingDbContext _db;
        private readonly SmtpClient _smtpClient;

        public StudentSupportMailerJob(VotingDbContext db, SmtpClient smtpClient)
        {
            _smtpClient = smtpClient;
            _db = db;
        }

        [UsedImplicitly] /* Hangfire */
        public StudentSupportMailerJob() : this(new VotingDbContext(), new SmtpClient())
        {
        }

        public void SendSingleEmail(Guid emailId, string recipientAddress, string recipientName)
        {
            StudentSupportEmail email = _db.StudentSupportEmails.Find(emailId);

            if (email == null)
            {
                throw new ArgumentOutOfRangeException(nameof(emailId), "No student support email with such id");
            }
            
            _smtpClient.Send(new MailMessage
            {
                Subject = email.Subject,
                Body = email.Body,
                IsBodyHtml = false,
                From = EmailHelpers.DefaultSender,
                To = {new MailAddress(recipientAddress, recipientName)}
            });
        }
    }
}