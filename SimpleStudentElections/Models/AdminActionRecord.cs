using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using JetBrains.Annotations;
using SimpleStudentElections.Helpers;

namespace SimpleStudentElections.Models
{
    /// <remarks>
    /// Note that the state changes that were caused by button press are not stored here to prevent data duplication
    /// </remarks>
    /// <seealso cref="ElectionStateChange"/>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class AdminActionRecord
    {
        public AdminActionRecord()
        {
        }

        public AdminActionRecord(Election election, string userId, RecordType type)
        {
            ElectionId = election.Id;
            Election = election;
            UserId = userId;
            Type = type;
            
            OccurredAt = DateTime.Now;
        }

        [Key]
        public Guid Id { get; [UsedImplicitly] set; } = Guid.NewGuid();

        [Required]
        [ForeignKey("Election")]
        public int ElectionId { get; private set; }
        
        public virtual Election Election { get; private set; }

        [Required]
        public string UserId { get; private set; }

        public DateTime OccurredAt { get; private set; }
        public RecordType Type { get; private set; }

        public byte[] FormChangeSetSerialized { get; set; }

        [CanBeNull, Pure]
        public FormChangeSet<TForm> GetChangeSet<TForm>()
        {
            if (FormChangeSetSerialized == null)
            {
                return null;
            }

            using (var memoryStream = new MemoryStream(FormChangeSetSerialized))
            {
                return (FormChangeSet<TForm>) new BinaryFormatter().Deserialize(memoryStream);
            }
        }

        public void SetFormChangeSet<TForm>([CanBeNull] FormChangeSet<TForm> changeSet)
        {
            if (changeSet == null)
            {
                FormChangeSetSerialized = null;
                return;
            }

            using (var memoryStream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(memoryStream, changeSet);
                FormChangeSetSerialized = memoryStream.ToArray();
            }
        }

        public enum RecordType
        {
            ViewVotes,
            Edit
        }
    }
}