using KingdomPrep.Web.Core.Auth;
using Xunit;

public class RoleParserTests
{
    [Theory]
    [InlineData("Director", UserRole.Director)]
    [InlineData("DIRECTORS", UserRole.Director)]
    [InlineData("Admin", UserRole.Administrator)]
    [InlineData("Administrator", UserRole.Administrator)]
    [InlineData("Teacher", UserRole.Teacher)]
    [InlineData("Accountant", UserRole.Accountant)]
    [InlineData("Headmaster", UserRole.Headmaster)]
    [InlineData("Parent", UserRole.Parent)]
    [InlineData("Guardian", UserRole.Parent)]
    [InlineData("  teacher  ", UserRole.Teacher)]
    [InlineData("something-unknown", UserRole.Unknown)]
    [InlineData(null, UserRole.Unknown)]
    public void Parse_MapsUserTypeStrings(string? userType, UserRole expected)
        => Assert.Equal(expected, RoleParser.Parse(userType));
}
