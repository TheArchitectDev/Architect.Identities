using Microsoft.EntityFrameworkCore;

namespace Architect.Identities.Example
{
	internal sealed class ExampleDbContext : DbContext
	{
		public ExampleDbContext(DbContextOptions options)
			: base(options)
		{
		}
	}
}
