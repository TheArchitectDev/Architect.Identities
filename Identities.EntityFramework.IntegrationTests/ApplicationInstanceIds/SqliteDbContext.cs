using Microsoft.EntityFrameworkCore;

namespace Architect.Identities.EntityFramework.IntegrationTests.ApplicationInstanceIds
{
	internal sealed class SqliteDbContext : DbContext
	{
		public SqliteDbContext(DbContextOptions<SqliteDbContext> options)
			: base(options)
		{
		}
	}
}
