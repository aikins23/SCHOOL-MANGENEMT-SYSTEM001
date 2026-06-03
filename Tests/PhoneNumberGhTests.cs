using Xunit;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Tests
{
    public class PhoneNumberGhTests
    {
        [Theory]
        [InlineData("0241234567", "233241234567")]
        [InlineData("+233241234567", "233241234567")]
        [InlineData("233241234567", "233241234567")]
        [InlineData("024 123 4567", "233241234567")]
        [InlineData("024-123-4567", "233241234567")]
        public void NormalizeGh_ValidNumbers_Returns233Format(string input, string expected)
        {
            Assert.Equal(expected, PhoneNumberGh.NormalizeGh(input));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("hello")]
        [InlineData("12345")]
        [InlineData(null)]
        public void NormalizeGh_Invalid_ReturnsNull(string input)
        {
            Assert.Null(PhoneNumberGh.NormalizeGh(input));
        }
    }
}
