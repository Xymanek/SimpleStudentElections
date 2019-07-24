using System.Configuration;

namespace SimpleStudentElections.Logic.Emails
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class EmailSenderSection : ConfigurationSection
    {
        private const string AddressKey = "address";
        private const string DisplayNameKey = "displayName";
        
        [ConfigurationProperty(AddressKey, IsRequired = true)]
        public string Address
        {
            get => (string) this[AddressKey];
            set => this[AddressKey] = value;
        }

        [ConfigurationProperty(DisplayNameKey, IsRequired = false)]
        public string DisplayName
        {
            get => (string) this[DisplayNameKey];
            set => this[DisplayNameKey] = value;
        }
    }
}