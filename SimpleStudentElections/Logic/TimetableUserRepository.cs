using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic
{
    public class TimetableUserRepository
    {
        private readonly TimetableDbContext db;

        public TimetableUserRepository(TimetableDbContext db)
        {
            this.db = db;
        }

        public TimetableUserRepository() : this(new TimetableDbContext())
        {
        }

        [CanBeNull]
        public TimetableUserEntry GetByUsername(string username)
        {
            string normalizedEmail = TimetableUserEntry.NormalizeUsernameToId(username) + "@example.com";

            return db.Users.FirstOrDefault(e => e.InternalEmail == normalizedEmail);
        }

        public async Task<TimetableUserEntry> GetByUsernameAsync(string username)
        {
            string normalizedEmail = TimetableUserEntry.NormalizeUsernameToId(username) + "@example.com";

            return await db.Users.FirstOrDefaultAsync(e => e.InternalEmail == normalizedEmail);
        }

        public IQueryable<TimetableUserEntry> GetAllStudentSupport()
        {
            return db.Users.WhereIsStudentSupport();
        }
    }
}