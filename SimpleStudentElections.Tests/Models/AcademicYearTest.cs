using System;
using NUnit.Framework;
using SimpleStudentElections.Models;

namespace SimpleStudentElections.Tests.Models
{
    public class AcademicYearTest
    {
        [Test]
        public void TestTransformations()
        {
            AcademicYear year = new AcademicYear("201617");

            Assert.AreEqual(2016, year.StartYear);
            Assert.AreEqual(2017, year.EndYear);

            Assert.AreEqual("201617", year.ToString());
        }

        [Test]
        public void TestInvalidInput()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AcademicYear(""));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AcademicYear("12345"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AcademicYear("1234567"));
            
            Assert.Throws<ArgumentOutOfRangeException>(() => new AcademicYear("201618"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AcademicYear("201616"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AcademicYear("201615"));
            
            Assert.Throws<FormatException>(() => new AcademicYear("abcdef"));
            Assert.Throws<FormatException>(() => new AcademicYear("2014ab"));
            Assert.Throws<FormatException>(() => new AcademicYear("20ab15"));
        }
    }
}