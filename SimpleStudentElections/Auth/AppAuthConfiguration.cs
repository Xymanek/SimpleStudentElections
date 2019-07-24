using System;
using System.Configuration;

namespace SimpleStudentElections.Auth
{
    public class AppAuthConfiguration : ConfigurationSection
    {
        private const string DebugModeKey = "debugMode";
        private const string TimetableLoginUrlKey = "timetableLoginUrl";
        private const string MaxSsoAttemptsKey = "maxSsoAttempts";
        private const string LogPathKey = "logPath";

        [ConfigurationProperty(DebugModeKey, DefaultValue = false)]
        public bool DebugMode
        {
            get => (bool) this[DebugModeKey];
            set => this[DebugModeKey] = value;
        }

        [ConfigurationProperty(TimetableLoginUrlKey, DefaultValue = "")]
        public string TimetableLoginUrl
        {
            get => (string) this[TimetableLoginUrlKey];
            set => this[TimetableLoginUrlKey] = value;
        }

        public string GetTimetableLoginUrlOrFail()
        {
            string url = TimetableLoginUrl;

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new Exception("TimetableLoginUrl is not configured but required");
            }

            return url;
        }

        [ConfigurationProperty(MaxSsoAttemptsKey, DefaultValue = 3)]
        public int MaxSsoAttempts
        {
            get => (int) this[MaxSsoAttemptsKey];
            set => this[MaxSsoAttemptsKey] = value;
        }


        [ConfigurationProperty(LogPathKey, IsRequired = true)]
        public string LogPath
        {
            get => (string)this[LogPathKey];
            set => this[LogPathKey] = value;
        }

        public static AppAuthConfiguration Get()
        {
            return (AppAuthConfiguration) ConfigurationManager.GetSection("appCustomConfig/auth");
        }
    }
}