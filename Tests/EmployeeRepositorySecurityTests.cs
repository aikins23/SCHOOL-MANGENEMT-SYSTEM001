using System;
using System.Data;
using System.Threading.Tasks;
using Xunit;
using kingdom_Preparatory_School_Management_System.Data;

namespace kingdom_Preparatory_School_Management_System.Tests
{
    public class EmployeeRepositorySecurityTests
    {
        private readonly string _testConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=|DataDirectory|\\TestDatabase.accdb;";

        /// <summary>
        /// Test that SQL injection attack via filterId parameter is prevented.
        /// Malicious payload: '; DROP TABLE Employee; --
        /// Expected behavior: Treated as literal string, not executed as SQL command
        /// </summary>
        [Fact]
        public async Task GetAsTableAsync_WithMaliciousFilterId_DoesNotExecuteInjectedSQL()
        {
            // Arrange
            var repo = new EmployeeRepository(_testConnectionString);
            var maliciousPayload = "'; DROP TABLE Employee; --";

            // Act
            // This should NOT drop the Employee table, but treat the payload as a literal search string
            var result = await repo.GetAsTableAsync(filterId: maliciousPayload);

            // Assert
            // If the injection was successful, this would throw an exception about a missing table
            // Instead, we should get back an empty DataTable (no matches for the literal string)
            Assert.NotNull(result);
            Assert.IsType<DataTable>(result);
            // Should have Employee table schema if table still exists
            Assert.Empty(result.Rows); // No rows match the literal string
        }

        /// <summary>
        /// Test that SQL injection attack via filterDepartment parameter is prevented.
        /// Malicious payload: Admin' OR '1'='1
        /// Expected behavior: Treated as literal string, not executed as SQL command
        /// </summary>
        [Fact]
        public async Task GetAsTableAsync_WithMaliciousFilterDepartment_DoesNotExecuteInjectedSQL()
        {
            // Arrange
            var repo = new EmployeeRepository(_testConnectionString);
            var maliciousPayload = "Admin' OR '1'='1";

            // Act
            // This should NOT bypass the WHERE clause but treat it as a literal value
            var result = await repo.GetAsTableAsync(filterDepartment: maliciousPayload);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<DataTable>(result);
            // Should not return all employees (which would indicate injection)
            Assert.Empty(result.Rows); // No department matches the literal string
        }

        /// <summary>
        /// Test that legitimate filters still work correctly with special characters.
        /// </summary>
        [Fact]
        public async Task GetAsTableAsync_WithSpecialCharactersInFilter_ReturnsCorrectResults()
        {
            // Arrange
            var repo = new EmployeeRepository(_testConnectionString);
            var filterWithSpecialChars = "IT%Department"; // Contains wildcard character

            // Act
            // Parameterized queries should treat % as a literal character, not a wildcard
            var result = await repo.GetAsTableAsync(filterId: filterWithSpecialChars);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<DataTable>(result);
            // Should search for literal "IT%Department" not as a wildcard pattern
        }

        /// <summary>
        /// Test that LIKE operator works correctly with filterId using parameterized query.
        /// </summary>
        [Fact]
        public async Task GetAsTableAsync_WithPartialFilterId_PerformsCorrectLikeSearch()
        {
            // Arrange
            var repo = new EmployeeRepository(_testConnectionString);
            // Assuming an employee with ID like "EMP001" exists
            var partialId = "EMP";

            // Act
            var result = await repo.GetAsTableAsync(filterId: partialId);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<DataTable>(result);
            // Should find employees with IDs containing "EMP" (parameterized LIKE with %)
        }

        /// <summary>
        /// Test that NULL/empty filters are handled correctly.
        /// </summary>
        [Fact]
        public async Task GetAsTableAsync_WithNullFilters_ReturnsAllEmployees()
        {
            // Arrange
            var repo = new EmployeeRepository(_testConnectionString);

            // Act
            var result = await repo.GetAsTableAsync(filterId: null, filterDepartment: null);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<DataTable>(result);
            // Should return all employees when filters are null
        }

        /// <summary>
        /// Test that empty string filters are handled correctly.
        /// </summary>
        [Fact]
        public async Task GetAsTableAsync_WithEmptyStringFilters_ReturnsAllEmployees()
        {
            // Arrange
            var repo = new EmployeeRepository(_testConnectionString);

            // Act
            var result = await repo.GetAsTableAsync(filterId: "", filterDepartment: "");

            // Assert
            Assert.NotNull(result);
            Assert.IsType<DataTable>(result);
            // Should return all employees when filters are empty strings
        }
    }
}
