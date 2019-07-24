using System.Collections.Generic;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Logic.DelayedJobScheduling
{
    public class ElectionJobManager : DelayedJobManagerBase<Election>
    {
        protected override IEnumerable<IDelayedJobDescriptor> GetDescriptors(Election election)
        {
            return new IDelayedJobDescriptor[]
            {
                // Automatic state transitions
                new NominationsStartJobDescriptor(election),
                new NominationsEndJobDescriptor(election),
                new VotingStartJobDescriptor(election),
                new VotingEndJobDescriptor(election),

                // Almost over emails
                new NominationsAlmostOverJobDescriptor(election),
                new VotingAlmostOverJobDescriptor(election),

                // Student support alerts
                new NominationsAlertJobDescriptor(election),
                new VotingAlertJobDescriptor(election)
            };
        }
    }
}