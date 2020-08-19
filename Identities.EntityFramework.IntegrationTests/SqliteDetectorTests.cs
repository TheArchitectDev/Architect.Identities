using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Architect.Identities.EntityFramework.IntegrationTests
{
	public sealed class SqliteDetectorTests
	{
		[Fact]
		public void IsSqlite_WithSqliteAssemblyLoaded_ShouldReturnExpectedResult()
		{
			using var dbContext = new SqliteDbContext();
			var result = SqliteDetector.IsSqlite(dbContext);

			Assert.True(result);
		}

		private sealed class SqliteDbContext : DbContext
		{
			public SqliteDbContext()
				: base(new DbContextOptionsBuilder().UseSqlite("Filename=:memory:").Options)
			{
			}
		}
	}
}
