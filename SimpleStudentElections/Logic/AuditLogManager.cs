using System.Security.Principal;
using Microsoft.AspNet.Identity;
using Serilog.Core;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic
{
    /// <summary>
    /// Handles audit log records that are written to disk
    /// </summary>
    public static class AuditLogManager
    {
        public static Logger Logger;

        #region Admin

        public static void RecordNewElection(ElectionStateChange createInfo)
        {
            int electionId = createInfo.Election.Id;

            Logger.Information(
                "{InstigatorUsername} created new {Type} election \"{Name}\"",
                createInfo.InstigatorUsername, createInfo.Election.Type, createInfo.Election.Name, electionId
            );
        }

        public static void RecordElectionStateChange(ElectionStateChange changeInfo)
        {
            int electionId = changeInfo.Election.Id;

            if (changeInfo.IsCausedByUser)
            {
                Logger.Information(
                    "{InstigatorUsername} changed \"{Name}\" election state from {PreviousState} to {TargetState}",
                    changeInfo.InstigatorUsername, changeInfo.Election.Name, changeInfo.PreviousState,
                    changeInfo.TargetState, electionId
                );                
            }
            else
            {
                Logger.Information(
                    "election \"{Name}\" State changed from {PreviousState} to {TargetState}",
                    changeInfo.Election.Name, changeInfo.PreviousState, changeInfo.TargetState, electionId
                );
            }
        }

        public static void RecordElectionEdit(IPrincipal user, Election election)
        {
            string userId = user.Identity.GetUserId();
            int electionId = election.Id;

            Logger.Information("{userId} edited election \"{Name}\"", userId, election.Name, electionId);
        }

        public static void RecordViewVotes(IPrincipal user, Election election)
        {
            string userId = user.Identity.GetUserId();
            int electionId = election.Id;

            Logger.Information("{userId} viewed votes for \"{Name}\"", userId, election.Name, electionId);
        }

        #endregion

        #region Students

        public static void RecordNominationUpdate(IPrincipal user, VotablePosition position, bool isNowNominated)
        {
            string userId = user.Identity.GetUserId();
            int electionId = position.Election.Id;
            int positionId = position.Id;
            string electionName = position.Election.Name;
            string positionName = position.HumanName;

            Logger.Information(
                "{userId} updated nomination status for \"{positionName}\" position for election \"{electionName}\": {isNowNominated}",
                userId, positionName, electionName, isNowNominated, positionId, electionId
            );
        }

        public static void RecordVoteCast(Vote vote)
        {
            int electionId = vote.NominationEntry.Position.ElectionId;
            int positionId = vote.NominationEntry.Position.Id;
            string nomineeId = vote.NominationEntry.Username;
            string electionName = vote.NominationEntry.Position.Election.Name;
            string positionName = vote.NominationEntry.Position.HumanName;

            Logger.Information(
                "A vote was cast for {nomineeId} for position \"{positionName}\" for election \"{electionName}\"",
                nomineeId, positionName, electionName, positionId, electionId
            );
        }

        #endregion
    }
}