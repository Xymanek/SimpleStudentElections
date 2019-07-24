using System.Data.Entity.Migrations;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<VotingDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }
    } 
}