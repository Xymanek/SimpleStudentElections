using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.ElectionMaintenanceJobs
{
    public static class GeneratePositions
    {
        [DisableConcurrentExecution(5 /* minutes */ * 60)]
        public static void Execute(int electionId, IJobCancellationToken cancellationToken)
        {
            TimetableDbContext timetableDb = new TimetableDbContext();
            VotingDbContext db = new VotingDbContext();

            Election election = db.Elections.Find(electionId);
            if (election == null)
                throw new ArgumentOutOfRangeException(nameof(electionId), "No election with such id");

            if (election.Type != ElectionType.CourseRepresentative)
                throw new Exception("Election must be of type " + ElectionType.CourseRepresentative);

            if (!election.PositionGenerationInProcess)
                throw new Exception($"Election {election.Name} is not pending position generation");

            // Generate the list as it is intended to be now
            List<RepresentativePositionData> desiredPositionDatas = timetableDb.Users
                .WhereIsStudentActive()
                .Select(entry => new {entry.ProgrammeName, entry.ExpectedGraduationYearString})
                .Distinct()
                .AsEnumerable()
                .Select(combination =>
                {
                    var data = new RepresentativePositionData
                    {
                        ProgrammeName = combination.ProgrammeName,
                        ExpectedGraduationYearString = combination.ExpectedGraduationYearString,
                        PositionCommon = new VotablePosition {Election = election}
                    };

                    data.SetPositionName();

                    return data;
                })
                .ToList();

            // Get the current positions in DB 
            List<RepresentativePositionData> matchedDesiredDatas = new List<RepresentativePositionData>();
            IQueryable<RepresentativePositionData> positionsDataFromDb = db.RepresentativePositionData
                .Where(data => data.PositionCommon.ElectionId == electionId);

            foreach (RepresentativePositionData existingPositionData in positionsDataFromDb)
            {
                RepresentativePositionData matchingDesiredData = desiredPositionDatas.FirstOrDefault(desiredData =>
                    existingPositionData.ProgrammeName == desiredData.ProgrammeName &&
                    existingPositionData.ExpectedGraduationYearString == desiredData.ExpectedGraduationYearString
                );

                if (matchingDesiredData != null)
                {
                    // Found matching entry. Apply the position name in case the naming logic changed
                    existingPositionData.PositionCommon.HumanName = matchingDesiredData.PositionCommon.HumanName;
                    matchedDesiredDatas.Add(matchingDesiredData);
                }
                else
                {
                    // Did not match - no longer needed
                    db.RepresentativePositionData.Remove(existingPositionData);
                }
            }

            // Add the new positions (that didn't match existing) to the db
            db.RepresentativePositionData.AddRange(desiredPositionDatas.Except(matchedDesiredDatas));

            // Make sure we are allowed to submit our changes
            cancellationToken.ThrowIfCancellationRequested();

            // We are done
            election.PositionGenerationInProcess = false;
            db.SaveChanges();
        }
    }
}