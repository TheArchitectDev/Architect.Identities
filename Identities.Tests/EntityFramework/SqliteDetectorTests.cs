using Architect.Identities.EntityFramework;
using Xunit;

namespace Architect.Identities.Tests.EntityFramework
{
	public sealed class SqliteDetectorTests
	{
		static SqliteDetectorTests()
		{
			// Assert that no SQLite assemblies are loaded (except System.Data.SQLite, which seems to be quite standard)
			var anySqliteAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly =>
				assembly.FullName?.Contains("SQLite", StringComparison.OrdinalIgnoreCase) == true);
			if (anySqliteAssembly is not null) throw new Exception("SQLite was inadvertently loaded in this unit test library.");
		}

		/// <summary>
		/// Although this is not a strict requirement at all, it allows our other tests to work without needing to start working with DbContexts in this unit test library.
		/// Let the integration tests handle that.
		/// </summary>
		[Fact]
		public void IsSqlite_WithNullDbContextAndNoSqliteAssembly_ShouldSucceed()
		{
			SqliteDetector.IsSqlite(dbContext: null);
		}

		[Fact]
		public void IsSqlite_WithoutSqliteAssemblyReference_ShouldReturnExpectedResult()
		{
			var result = SqliteDetector.IsSqlite(dbContext: null);

			Assert.False(result);
		}
	}
}
