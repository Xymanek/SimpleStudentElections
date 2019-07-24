using System.Configuration;
using System.Net.Mail;

namespace SimpleStudentElections.Logic.Emails
{
    public static class EmailHelpers
    {
        public static MailAddress DefaultSender
        {
            get
            {
                EmailSenderSection configuration = GetDefaultSenderConfiguration();
                return new MailAddress(configuration.Address, configuration.DisplayName);
            }
        }

        public static EmailSenderSection GetDefaultSenderConfiguration()
        {
            return (EmailSenderSection) ConfigurationManager.GetSection("appCustomConfig/defaultEmailSender");
        }
    }
}