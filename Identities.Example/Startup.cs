using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Architect.Identities.Example
{
	internal sealed class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			// Since our example uses Entity Framework, register the DbContext
			services.AddPooledDbContextFactory<ExampleDbContext>(context => context.UseSqlite(new SqliteConnection("Filename=:memory:")));

			// Obtain access to a unique ID for our application instance, using the DbContext's database to register a unique one
			services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<ExampleDbContext>());

			// Or use one of the other implementations, such as SQL server, Azure blob storage, Standard SQL, etc.
			//services.AddApplicationInstanceIdSource(source => source.UseSqlite(() => myOpenSqliteConnection));
			//services.AddApplicationInstanceIdSource(source => source.UseSqlServer<IServiceProvider>(serviceProvider => new SqlConnection("ConnectionString")));
			//services.AddApplicationInstanceIdSource(source => source.UseSqlServerDbContext<ExampleDbContext>());
			//services.AddApplicationInstanceIdSource(source => source.UseAzureBlobStorageContainer(serviceProvider => new BlobContainerClient("ConnectionString", "ContainerName")));

			// Register the Flexible, Locally-Unique ID (Fluid) as the ID generation mechanism
			services.AddIdGenerator(generator => generator.UseFluid());

			// Register our application's own types
			services.AddSingleton<UserFactory>();
		}

		public void Configure(IHost host) // IHost for generic hosts, IApplicationBuilder for web hosts
		{
			// Acquire the unique ID for the application instance (and relinquish it on shutdown)
			host.UseApplicationInstanceIdSource();

			// Optional:
			// Allow injection-free access to the registered IIdGenerator, through the Ambient Context IoC pattern
			host.UseIdGenerator();
		}
	}
}
