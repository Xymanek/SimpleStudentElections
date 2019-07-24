using System.Configuration;
using Serilog;
using SimpleStudentElections.Auth;
using SimpleStudentElections.Logic;

namespace SimpleStudentElections
{
    public static class LoggingConfig
    {
        /// <summary>
        /// Configures the Audit logger (suing Serilog library)
        /// </summary>
        public static void InitializeAuditLogger()
        {
            AuditLogManager.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(GetAuditLoggingConfiguration().FilePath)
                .CreateLogger();

            AuthHelpers.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(AppAuthConfiguration.Get().LogPath)
                .CreateLogger();
        }

        public static AuditLoggingConfiguration GetAuditLoggingConfiguration()
        {
            return (AuditLoggingConfiguration) ConfigurationManager.GetSection("appCustomConfig/auditLogging");
        }
    }

    /// <summary>
    /// Configuration definition for path to audit log file
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AuditLoggingConfiguration : ConfigurationSection
    {
        private const string FilePathKey = "filePath";

        [ConfigurationProperty(FilePathKey, IsRequired = true)]
        public string FilePath
        {
            get => (string) this[FilePathKey];
            set => this[FilePathKey] = value;
        }
    }
}