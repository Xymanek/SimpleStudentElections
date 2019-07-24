using System.Linq;
using NUnit.Framework;
using SimpleStudentElections.Logic;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Tests.Logic
{
    public class ElectionEligibilityGeneratorTest
    {
        [Test]
        public void TestElectionEntries()
        {
            Election election = new Election();
            TimetableUserEntry[] users = {
                new TimetableUserEntry()
                {
                    Username = "abc",
                    AccountTypeName = "Student",
                    StudentStatusDescription = "Active"
                },
                new TimetableUserEntry()
                {
                    Username = "abc2",
                    AccountTypeName = "Student",
                    StudentStatusDescription = "Inactive Program Completed"
                },
                new TimetableUserEntry()
                {
                    AccountTypeName = "Student Support"
                },
            };

            ElectionEligibilityEntry[] result = EligibilityGenerator
                .GenerateElectionEntries(election, users.AsQueryable())
                .ToArray();
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("abc", result[0].Username);
            Assert.AreSame(election, result[0].Election);
        }
    }
}