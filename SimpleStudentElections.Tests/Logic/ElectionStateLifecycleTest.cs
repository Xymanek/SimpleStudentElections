using System;
using NUnit.Framework;
using SimpleStudentElections.Logic;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Tests.Logic
{
    public class ElectionStateLifecycleTest
    {
        [Test]
        public void TestIsInactive()
        {
            Assert.True(ElectionLifecycleInfo.IsInactive(new Election()));
            Assert.True(ElectionLifecycleInfo.IsInactive(new Election {State = ElectionState.Aborted}));

            Assert.False(ElectionLifecycleInfo.IsInactive(new Election {State = ElectionState.PreNominations}));
            Assert.False(ElectionLifecycleInfo.IsInactive(new Election {State = ElectionState.Nominations}));
            Assert.False(ElectionLifecycleInfo.IsInactive(new Election {State = ElectionState.PreVoting}));
            Assert.False(ElectionLifecycleInfo.IsInactive(new Election {State = ElectionState.Voting}));
            Assert.False(ElectionLifecycleInfo.IsInactive(new Election {State = ElectionState.Closed}));
            Assert.False(ElectionLifecycleInfo.IsInactive(new Election {State = ElectionState.ResultsPublished}));
        }

        [Test]
        public void TestBefore()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ElectionLifecycleInfo.Before(ElectionState.PreNominations));

            Assert.AreEqual(ElectionState.PreNominations, ElectionLifecycleInfo.Before(ElectionState.Nominations));
            Assert.AreEqual(ElectionState.Nominations, ElectionLifecycleInfo.Before(ElectionState.PreVoting));
            Assert.AreEqual(ElectionState.PreVoting, ElectionLifecycleInfo.Before(ElectionState.Voting));
            Assert.AreEqual(ElectionState.Voting, ElectionLifecycleInfo.Before(ElectionState.Closed));
            Assert.AreEqual(ElectionState.Closed, ElectionLifecycleInfo.Before(ElectionState.ResultsPublished));

            Assert.Throws<ArgumentOutOfRangeException>(() => ElectionLifecycleInfo.Before(ElectionState.Disabled));
            Assert.Throws<ArgumentOutOfRangeException>(() => ElectionLifecycleInfo.Before(ElectionState.Aborted));
        }

        [Test]
        public void TestIsBefore()
        {
            Assert.True(ElectionLifecycleInfo.IsBefore(
                new Election {State = ElectionState.PreNominations},
                ElectionState.Nominations
            ));

            Assert.False(ElectionLifecycleInfo.IsBefore(
                new Election {State = ElectionState.PreVoting},
                ElectionState.Nominations
            ));
            
            Assert.False(ElectionLifecycleInfo.IsBefore(
                new Election {State = ElectionState.Nominations},
                ElectionState.Nominations
            ));

            Assert.Throws<InactiveElectionNoLifecycleComparisonException>(() =>
            {
                ElectionLifecycleInfo.IsBefore(
                    new Election {State = ElectionState.Disabled},
                    ElectionState.Nominations
                );
            });
        }
        
        [Test]
        public void TestIsAfter()
        {
            Assert.True(ElectionLifecycleInfo.IsAfter(
                new Election {State = ElectionState.PreVoting},
                ElectionState.Nominations
            ));

            Assert.False(ElectionLifecycleInfo.IsAfter(
                new Election {State = ElectionState.PreNominations},
                ElectionState.Nominations
            ));
            
            Assert.False(ElectionLifecycleInfo.IsAfter(
                new Election {State = ElectionState.Nominations},
                ElectionState.Nominations
            ));

            Assert.Throws<InactiveElectionNoLifecycleComparisonException>(() =>
            {
                ElectionLifecycleInfo.IsAfter(
                    new Election {State = ElectionState.Disabled},
                    ElectionState.Nominations
                );
            });
        }
    }
}