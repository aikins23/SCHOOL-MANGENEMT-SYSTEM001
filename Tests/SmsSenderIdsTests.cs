using Xunit;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Tests
{
    public class SmsSenderIdsTests
    {
        [Fact]
        public void Build_UppercasesAndConcatenates()
        {
            Assert.Equal("KPSSTDADM", SmsSenderIds.Build("kps", SmsSenderIds.StudentSuffix));
            Assert.Equal("KPSEMPADM", SmsSenderIds.Build("KPS", SmsSenderIds.EmployeeSuffix));
            Assert.Equal("KPSFEES",   SmsSenderIds.Build("KPS", SmsSenderIds.FeeSuffix));
        }

        [Fact]
        public void Build_TrimsSpaces()
        {
            Assert.Equal("KPSFEES", SmsSenderIds.Build("  kps ", SmsSenderIds.FeeSuffix));
        }

        [Fact]
        public void ExceedsMaxLength_DetectsTooLong()
        {
            Assert.False(SmsSenderIds.ExceedsMaxLength("KPS"));      // KPSSTDADM = 9
            Assert.True(SmsSenderIds.ExceedsMaxLength("KINGDOM"));   // KINGDOMSTDADM = 13
        }
    }
}
